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
        
        public Dictionary<string,string> FindCameras()
        {
            var devices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            var cameras = new Dictionary<string, string>();
            foreach (var device in devices)
            {
                cameras.Add(device.DevicePath, device.Name);
            }

            return cameras;
        }
    }
}