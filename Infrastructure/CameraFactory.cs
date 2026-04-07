using System.Collections.Generic;
using DirectShowLib;
using Domain;

namespace Infrastructure
{
    public class CameraFactory
    {
        public ICamera CreateCamera(string cameraName)
        {
            return new DirectShowCamera(cameraName);
        }
        
        public Dictionary<string,ICamera> FindCameras()
        {
            var devices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            var cameras = new Dictionary<string, ICamera>();
            foreach (var device in devices)
            {
                cameras.Add(device.DevicePath, CreateCamera(device.DevicePath));
            }

            return cameras;
        }
    }
}