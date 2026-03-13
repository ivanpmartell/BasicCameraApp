using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using Domain;
using Infrastructure;

namespace Application
{
    public class CameraApplication
    {
        private readonly CameraFactory _cameraFactory;
        private readonly ICameraStorage _storageService;
        private ICamera _currentCamera;
        
        public CameraApplication()
        {
            _cameraFactory = new CameraFactory();
            _storageService = new FileStorageService();
        }
        
        public Dictionary<string,string> GetCameras()
        {
            return _cameraFactory.FindCameras();
        }
        
        public void LoadCamera(string cameraName)
        {
            StopCamera();
            
            _currentCamera = _cameraFactory.CreateCamera(cameraName);
            _currentCamera.FrameCaptured += OnFrameCaptured;
        }
        
        public void ResizeCameraWindow(Size rendererSize)
        {
            if (_currentCamera == null)
                return;
            
            _currentCamera.ResizeVideoWindow(rendererSize);
        }

        public void FlipCameraX()
        {
            if (_currentCamera == null)
                return;
            
            _currentCamera.FlipX();
        }
        
        public void FlipCameraY()
        {
            if (_currentCamera == null)
                return;
            
            _currentCamera.FlipY();
        }
        
        private void OnFrameCaptured(object sender, FrameCapturedEventArgs e)
        {
            _storageService.SaveFrame(e);
        }

        public void StartCamera(IntPtr rendererHandle, Size rendererSize)
        {
            if (_currentCamera == null)
                return;
            
            _currentCamera.Start(rendererHandle, rendererSize);
        }
        
        public void StopCamera()
        {
            if (_currentCamera == null)
                return;
            
            _currentCamera.Stop();
        }
        
        public void Capture()
        {
            if (_currentCamera == null)
                return;
            
            _currentCamera.CaptureFrame();
        }

        public void OpenStorageFolder()
        {
            Process.Start("explorer.exe", _storageService.GetDefaultStoragePath());
        }
    }
}