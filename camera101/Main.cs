using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using DirectShowLib;

namespace camera101
{
    public partial class Main : Form
    {
        public delegate void FrameCallback();
        private ISampleGrabber sampleGrabber = null;
        private int videoWidth;
        private int videoHeight;
        private int stride;
        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
        private static extern void CopyMemory(IntPtr dest, IntPtr src, int count);
        private string destfolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "camera101");

        class FrameGrabber : ISampleGrabberCB
        {
            private FrameCallback onFrame;

            public FrameGrabber(FrameCallback onFrameCallback)
            {
                this.onFrame = onFrameCallback;
            }

            public int BufferCB(double SampleTime, IntPtr pBuffer, int BufferLen)
            {
                if (onFrame != null)
                    onFrame();
                return 0;
            }

            public int SampleCB(double SampleTime, IMediaSample pSample)
            {
                return 0;
            }
        }


        public Main()
        {
            InitializeComponent();            
        }

        private void Main_Load(object sender, EventArgs e)
        {
            this.BackColor = color.darkBack;
            this.ActiveControl = captureButton;
        }

        private DsDevice[] systemCameras;

        private void sourceBox_SourceChanged(object sender, EventArgs e)
        {
            SetSource(sourceBox.SelectedIndex);    
        }

        private void GetSource() // finds sources, populates sourceBox
        {
            // Find first video device
            systemCameras = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);

            if (systemCameras.Length == 0)
            {
                MessageBox.Show("No camera found!");
                return;
            }

            else
            {
                foreach (DsDevice source in systemCameras)
                {
                    sourceBox.Items.Add(source.Name);
                }

                SetSource(0);
            }
        }

        private void SetSource(int camnum) // sets the source to a source number
        {            
             AddCamera(systemCameras[camnum]);
             sourceBox.SelectedIndex = camnum;                            
        }

        private void AddCamera(DsDevice camera) // sets stream to source
        {
            // Create graph
            IFilterGraph2 filterGraph = new FilterGraph() as IFilterGraph2;

            // Create capture graph builder
            ICaptureGraphBuilder2 captureGraphBuilder = (ICaptureGraphBuilder2)new CaptureGraphBuilder2();
            captureGraphBuilder.SetFiltergraph(filterGraph);

            // Add the camera to the graph
            IBaseFilter capFilter;
            filterGraph.AddSourceFilterForMoniker(camera.Mon, null, "Camera", out capFilter);

            // Create SampleGrabber filter
            sampleGrabber = new SampleGrabber() as ISampleGrabber;

            IBaseFilter baseGrabFlt = sampleGrabber as IBaseFilter;
            filterGraph.AddFilter(baseGrabFlt, "SampleGrabber");

            // Configure media type (RGB24 is easiest)
            AMMediaType media = new AMMediaType();
            media.majorType = MediaType.Video;
            media.subType = MediaSubType.RGB24;
            media.formatType = FormatType.VideoInfo;
            sampleGrabber.SetMediaType(media);

            // Connect camera -> sampleGrabber -> renderer
            captureGraphBuilder.RenderStream(PinCategory.Preview, MediaType.Video, capFilter, baseGrabFlt, null);

            // Configure grabber
            sampleGrabber.SetBufferSamples(true);
            sampleGrabber.SetOneShot(false);

            // Get video info (width, height, stride)
            AMMediaType mt = new AMMediaType();
            sampleGrabber.GetConnectedMediaType(mt);
            VideoInfoHeader vih = (VideoInfoHeader)Marshal.PtrToStructure(mt.formatPtr, typeof(VideoInfoHeader));
            videoWidth = vih.BmiHeader.Width;
            videoHeight = vih.BmiHeader.Height;
            stride = videoWidth * (vih.BmiHeader.BitCount / 8);
            DsUtils.FreeAMMediaType(mt);

            // Render the preview pin
            captureGraphBuilder.RenderStream(PinCategory.Preview, MediaType.Video, capFilter, null, null);
            // ---- Embed video inside your PictureBox ----
            IVideoWindow videoWindow = filterGraph as IVideoWindow;
            videoWindow.put_Owner(cameraDisplay.Handle); // give ownership to PictureBox
            videoWindow.put_WindowStyle(DirectShowLib.WindowStyle.Child | DirectShowLib.WindowStyle.ClipSiblings); // make it a child window
            videoWindow.put_Visible(DirectShowLib.OABool.True);
            videoWindow.SetWindowPosition(0, 0, cameraDisplay.Width, cameraDisplay.Height);

            // Run the graph
            IMediaControl mediaControl = filterGraph as IMediaControl;
            int hr = mediaControl.Run();
            DsError.ThrowExceptionForHR(hr);
        }

        private void CaptureFrame()
{
    int bufferSize = 0;
    sampleGrabber.GetCurrentBuffer(ref bufferSize, IntPtr.Zero);

    if (bufferSize <= 0) return;

    IntPtr bufferPtr = Marshal.AllocCoTaskMem(bufferSize);
    try
    {
        sampleGrabber.GetCurrentBuffer(ref bufferSize, bufferPtr);

        Bitmap bmp = new Bitmap(videoWidth, videoHeight, PixelFormat.Format24bppRgb);
        BitmapData bmpData = bmp.LockBits(
            new Rectangle(0, 0, videoWidth, videoHeight),
            ImageLockMode.WriteOnly, bmp.PixelFormat);

        CopyMemory(bmpData.Scan0, bufferPtr, bufferSize);
        bmp.UnlockBits(bmpData);

        

        // Create folder if missing
        if (!Directory.Exists(destfolder))
            Directory.CreateDirectory(destfolder);

        // Build file name
        string fileName = string.Format("capture_{0:yyyyMMdd_HHmmss}.png", DateTime.Now);
        string path = Path.Combine(destfolder, fileName);
        bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
        bmp.Save(path, ImageFormat.Png);
    }
    finally
    {
        Marshal.FreeCoTaskMem(bufferPtr);
    }
}

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void Main_Shown(object sender, EventArgs e)
        {
            GetSource();
        }

        private void captureButton_Click(object sender, EventArgs e)
        {
            captureButton.Image = camera101.Properties.Resources.capPress;
            CaptureFrame();
        }

        private bool mouseInCap = false;

        private void captureButton_Hover(object sender, EventArgs e)
        {
            mouseInCap = true;
            captureButton.Image = camera101.Properties.Resources.capactive;
        }

        private void captureButton_Leave(object sender, EventArgs e)
        {
            mouseInCap = false;
            captureButton.Image = camera101.Properties.Resources.capRed;
        }

        private void captureButton_UC(object sender, EventArgs e)
        {
            if (mouseInCap)
            {
                captureButton.Image = camera101.Properties.Resources.capactive;
            }
            else
            {
                captureButton.Image = camera101.Properties.Resources.capRed;
            }
        }

        private void openFolderButton_Click(object sender, EventArgs e)
        {
            if (!(Directory.Exists(destfolder)))
            {
                Directory.CreateDirectory(destfolder);
            }
            Process.Start("explorer.exe", destfolder);
        }       
    }
}
