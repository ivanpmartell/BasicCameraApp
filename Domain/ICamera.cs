using System;
using System.Drawing;

namespace Domain
{
    public interface ICamera
    {
        int Width { get; }
        int Height { get; }
        
        void Start(IntPtr rendererHandle, Size rendererSize);
        void Stop();
        void CaptureFrame();
        void ResizeVideoWindow(IntPtr rendererHandle, Size rendererSize);
        
        event EventHandler<FrameCapturedEventArgs> FrameCaptured;
    }
}