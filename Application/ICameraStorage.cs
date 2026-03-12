using Domain;

namespace Application
{
    public interface ICameraStorage
    {
        string GetDefaultStoragePath();
        void SaveFrame(FrameCapturedEventArgs args, string filename = null);
    }
}