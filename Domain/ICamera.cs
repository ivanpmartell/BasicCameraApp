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
        void ResizeVideoWindow(Size rendererSize);
        void FlipX();
        void FlipY();
        
        event EventHandler<FrameCapturedEventArgs> FrameCaptured;
    }
}