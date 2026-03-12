using System;

namespace Domain
{
    public class FrameCapturedEventArgs : EventArgs
    {
        public byte[] Frame { get; set; }
        public DateTime Timestamp { get; set; }
    }
}