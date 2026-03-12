using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using DirectShowLib;
using Domain;

namespace Infrastructure
{
    public class DirectShowCamera : ICamera, IDisposable
    {
        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
        private static extern void CopyMemory(IntPtr dest, IntPtr src, int count);
        
        private readonly DsDevice _dsDevice;
        
        private IFilterGraph2 _filterGraph;
        private ISampleGrabber _sampleGrabber;

        public event EventHandler<FrameCapturedEventArgs> FrameCaptured;
        
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
        }
        
        public int Width { get; private set; }
        public int Height { get; private set; }
        public bool IsRunning { get; private set;  }

        public void Start(IntPtr rendererHandle, Size rendererSize)
        {
            if (IsRunning)
                return;

            IsRunning = true;
            
            var captureGraphBuilder = new CaptureGraphBuilder2() as ICaptureGraphBuilder2;
            captureGraphBuilder.SetFiltergraph(_filterGraph);
            
            IBaseFilter capFilter;
            _filterGraph.AddSourceFilterForMoniker(_dsDevice.Mon, null, "Camera", out capFilter);
            
            IBaseFilter baseGrabFlt = _sampleGrabber as IBaseFilter;
            _filterGraph.AddFilter(baseGrabFlt, "SampleGrabber");
            
            AMMediaType media = new AMMediaType();
            media.majorType = MediaType.Video;
            media.subType = MediaSubType.RGB24;
            media.formatType = FormatType.VideoInfo;
            
            _sampleGrabber.SetMediaType(media);
            _sampleGrabber.SetBufferSamples(true);
            _sampleGrabber.SetOneShot(false);
            
            captureGraphBuilder.RenderStream(PinCategory.Preview, MediaType.Video, capFilter, baseGrabFlt, null);

            AMMediaType mt = new AMMediaType();
            _sampleGrabber.GetConnectedMediaType(mt);
            VideoInfoHeader vih = (VideoInfoHeader)Marshal.PtrToStructure(mt.formatPtr, typeof(VideoInfoHeader));
            Width = vih.BmiHeader.Width;
            Height = vih.BmiHeader.Height;
            DsUtils.FreeAMMediaType(mt);

            captureGraphBuilder.RenderStream(PinCategory.Preview, MediaType.Video, capFilter, null, null);
            
            ResizeVideoWindow(rendererHandle, rendererSize);
            
            IMediaControl mediaControl = _filterGraph as IMediaControl;
            int hr = mediaControl.Run();
            DsError.ThrowExceptionForHR(hr);
        }

        public void ResizeVideoWindow(IntPtr rendererHandle, Size rendererSize)
        {
            IVideoWindow videoWindow = _filterGraph as IVideoWindow;
            videoWindow.put_Owner(rendererHandle);
            videoWindow.put_WindowStyle(WindowStyle.Child | WindowStyle.ClipSiblings);
            videoWindow.put_Visible(OABool.True);
            videoWindow.SetWindowPosition(0, 0, rendererSize.Width, rendererSize.Height);
        }
        
        public void Stop()
        {
            if (!IsRunning)
                return;
            
            IsRunning = false;
            
            IMediaControl mediaControl = _filterGraph as IMediaControl;
            mediaControl.Stop();
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
        
        protected virtual void OnFrameCaptured(FrameCapturedEventArgs e)
        {
            FrameCaptured?.Invoke(this, e);
        }

        public void Dispose()
        {
            _dsDevice?.Dispose();
        }
    }
}