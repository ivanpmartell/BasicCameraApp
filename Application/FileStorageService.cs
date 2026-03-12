using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Domain;

namespace Application
{
    public class FileStorageService : ICameraStorage
    {
        private readonly string _storageFolder;
        
        public FileStorageService()
        {
            _storageFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), 
                "camera101");
            
            CreateStorageDirectoryIfNotExists();
        }
        
        public string GetDefaultStoragePath()
        {
            return _storageFolder;
        }
        
        public void SaveFrame(FrameCapturedEventArgs args, string filename = null)
        {
            if (args.Frame == null)
                return;
            
            CreateStorageDirectoryIfNotExists();
            
            if (filename == null)
            {
                filename = GenerateFilename(args.Timestamp);
            }
            
            var fullPath = Path.Combine(_storageFolder, filename);
            using (MemoryStream ms = new MemoryStream(args.Frame))
            {
                var bitmap = new Bitmap(ms);
                bitmap.Save(fullPath, ImageFormat.Jpeg);
            }
        }
        
        private void CreateStorageDirectoryIfNotExists()
        {
            if (!Directory.Exists(_storageFolder))
            {
                Directory.CreateDirectory(_storageFolder);
            }
        }
        
        private string GenerateFilename(DateTime dateTime)
        {
            return $"capture_{dateTime:yyyyMMdd_HHmmss}.jpg";
        }
    }
}