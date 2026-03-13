using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using DirectShowLib;
using Domain;

namespace Infrastructure
{
    public sealed class DirectShowCamera : ICamera, IDisposable
    {
        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
        private static extern void CopyMemory(IntPtr dest, IntPtr src, int count);
        
        private readonly DsDevice _dsDevice;
        private readonly IFilterGraph2 _filterGraph;
        private readonly ISampleGrabber _sampleGrabber;
        private readonly IBaseFilter _vmr9;

        public event EventHandler<FrameCapturedEventArgs> FrameCaptured;
        
        public int Width { get; private set; }
        public int Height { get; private set; }
        public bool IsRunning { get; private set;  }
        
        public DirectShowCamera(string devicePath)
        {
            var devices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            foreach (var device in devices)
            {
                if (device.DevicePath == devicePath)
                {
                    _dsDevice = device;
                    break;
                }
            }

            if (_dsDevice == null)
                throw new NullReferenceException(typeof(DsDevice).FullName);
            
            _filterGraph = new FilterGraph() as IFilterGraph2;
            _sampleGrabber = new SampleGrabber() as ISampleGrabber;
            _vmr9 = new VideoMixingRenderer9() as IBaseFilter;
        }

        public void Start(IntPtr rendererHandle, Size rendererSize)
        {
            if (IsRunning) return;
            IsRunning = true;

            var captureGraphBuilder = new CaptureGraphBuilder2() as ICaptureGraphBuilder2;
            captureGraphBuilder.SetFiltergraph(_filterGraph);

            _filterGraph.AddFilter(_vmr9, "VMR9");
            var config = _vmr9 as IVMRFilterConfig9;
            config.SetRenderingMode(VMR9Mode.Windowless);
            var windowlessControl = _vmr9 as IVMRWindowlessControl9;
            windowlessControl.SetVideoClippingWindow(rendererHandle);

            _filterGraph.AddSourceFilterForMoniker(_dsDevice.Mon, null, "Camera", out var capFilter);

            var sampleGrabberFilter = _sampleGrabber as IBaseFilter;
            _filterGraph.AddFilter(sampleGrabberFilter, "SampleGrabber");

            AMMediaType media = new AMMediaType
            {
                majorType = MediaType.Video,
                subType = MediaSubType.RGB24
            };
            _sampleGrabber.SetMediaType(media);
            _sampleGrabber.SetBufferSamples(true);
            
            int hr = captureGraphBuilder.RenderStream(PinCategory.Preview, MediaType.Video, capFilter, sampleGrabberFilter, _vmr9);
            DsError.ThrowExceptionForHR(hr);
            
            var mixer = _vmr9 as IVMRMixerControl9;
            NormalizedRect rect = new NormalizedRect(1, 1, 0, 0);
            mixer.SetOutputRect(0, ref rect);

            AMMediaType mt = new AMMediaType();
            _sampleGrabber.GetConnectedMediaType(mt);
            VideoInfoHeader vih = (VideoInfoHeader)Marshal.PtrToStructure(mt.formatPtr, typeof(VideoInfoHeader));
            Width = vih.BmiHeader.Width;
            Height = vih.BmiHeader.Height;
            DsUtils.FreeAMMediaType(mt);

            ResizeVideoWindow(rendererSize);
            
            (_filterGraph as IMediaControl).Run();
        }

        public void ResizeVideoWindow(Size rendererSize)
        {
            var windowlessControl = _vmr9 as IVMRWindowlessControl9;
            windowlessControl?.SetVideoPosition(null, DsRect.FromRectangle(new Rectangle(Point.Empty, rendererSize)));
        }

        public void FlipX()
        {
            var mixer = _vmr9 as IVMRMixerControl9;
            mixer.GetOutputRect(0, out NormalizedRect currentRect);
            if (IsXFlipped())
            {
                currentRect.left = 1;
                currentRect.right = 0;
            }
            else
            {
                currentRect.left = 0;
                currentRect.right = 1;
            }
            mixer.SetOutputRect(0, ref currentRect);
        }

        private bool IsXFlipped()
        {
            var mixer = _vmr9 as IVMRMixerControl9;
            mixer.GetOutputRect(0, out NormalizedRect currentRect);
            return currentRect.left == 0;
        }
        
        public void FlipY()
        {
            var mixer = _vmr9 as IVMRMixerControl9;
            mixer.GetOutputRect(0, out NormalizedRect currentRect);
            if (IsYFlipped())
            {
                currentRect.top = 1;
                currentRect.bottom = 0;
            }
            else
            {
                currentRect.top = 0;
                currentRect.bottom = 1;
            }
            mixer.SetOutputRect(0, ref currentRect);
        }

        private bool IsYFlipped()
        {
            var mixer = _vmr9 as IVMRMixerControl9;
            mixer.GetOutputRect(0, out NormalizedRect currentRect);
            return currentRect.top == 0;
        }
        
        public void Stop()
        {
            if (!IsRunning)
                return;
            
            IsRunning = false;
            
            var mediaControl = _filterGraph as IMediaControl;
            mediaControl?.Stop();
        }
        
        public void CaptureFrame()
        {
            int bufferSize = 0;
            _sampleGrabber.GetCurrentBuffer(ref bufferSize, IntPtr.Zero);

            if (bufferSize <= 0)
                return;
            
            IntPtr bufferPtr = Marshal.AllocCoTaskMem(bufferSize);
            try
            {
                _sampleGrabber.GetCurrentBuffer(ref bufferSize, bufferPtr);

                Bitmap bitmap = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);
                BitmapData bitmapData = bitmap.LockBits(
                    new Rectangle(0, 0, Width, Height),
                    ImageLockMode.WriteOnly, bitmap.PixelFormat);

                CopyMemory(bitmapData.Scan0, bufferPtr, bufferSize);
                bitmap.UnlockBits(bitmapData);

                if (!IsXFlipped())
                    bitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
                if (IsYFlipped())
                    bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);

                using (MemoryStream ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Jpeg);
                    CaptureFrameInternal(ms.ToArray());
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(bufferPtr);
            }
        }
        
        private void CaptureFrameInternal(byte[] frame)
        {
            var args = new FrameCapturedEventArgs
            {
                Frame = frame,
                Timestamp = DateTime.Now
            };
            
            OnFrameCaptured(args);
        }

        private void OnFrameCaptured(FrameCapturedEventArgs e)
        {
            FrameCaptured?.Invoke(this, e);
        }

        public void Dispose()
        {
            _dsDevice?.Dispose();
        }
    }
}