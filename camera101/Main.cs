using System;
using System.Drawing;
using System.Windows.Forms;
using Application;

namespace camera101
{
    public partial class Main : Form
    {
        private readonly CameraApplication _cameraApplication;
        
        private bool _mouseInCap;
        private Timer _statusTimer;
        
        public Main()
        {
            _cameraApplication = new CameraApplication();
            InitializeComponent();
            _statusTimer = new Timer { Interval = 1000 };
            _statusTimer.Tick += StatusTimer_Tick;
            _statusTimer.Start();
        }
        
        private void StatusTimer_Tick(object sender, EventArgs e)
        {
            ReloadSources();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            BackColor = Colors.darkBack;
            ActiveControl = captureButton;
        }

        private void sourceBox_SourceChanged(object sender, EventArgs e)
        {
            var cameraItem = ((ComboBox)sender).SelectedItem as ComboboxItem;
            _cameraApplication.LoadCamera(cameraItem?.Value.ToString());
            _cameraApplication.StartCamera(cameraDisplay.Handle, cameraDisplay.Size);
        }

        private void ReloadSources()
        {
            var cameras = _cameraApplication.GetCameras();
            if (cameras.Count == sourceBox.Items.Count)
                return;
            
            bool addedACamera = false;

            foreach (ComboboxItem item in sourceBox.Items)
            {
                var cameraDevice = item.Value.ToString();
                cameras.Remove(cameraDevice);
            }
            
            foreach (var camera in cameras)
            {
                ComboboxItem cameraItem = new ComboboxItem { Text = camera.Value, Value = camera.Key };
                sourceBox.Items.Add(cameraItem);
                addedACamera = true;
            }

            if (addedACamera)
                AdjustDropDownWidth(sourceBox);
        }
        
        private void AdjustDropDownWidth(ComboBox combo)
        {
            int maxWidth = combo.DropDownWidth;
            using (Graphics g = combo.CreateGraphics())
            {
                foreach (var item in combo.Items)
                {
                    // Measure the string width
                    int newWidth = (int)g.MeasureString(item.ToString(), combo.Font).Width;
                    if (newWidth > maxWidth)
                        maxWidth = newWidth;
                }
            }

            // Add some padding for the scrollbar and margins
            combo.DropDownWidth = maxWidth + SystemInformation.VerticalScrollBarWidth;
        }

        private void captureButton_Click(object sender, EventArgs e)
        {
            captureButton.Image = Properties.Resources.capPress;
            _cameraApplication.Capture();
        }

        private void captureButton_Hover(object sender, EventArgs e)
        {
            _mouseInCap = true;
            captureButton.Image = Properties.Resources.capactive;
        }

        private void captureButton_Leave(object sender, EventArgs e)
        {
            _mouseInCap = false;
            captureButton.Image = Properties.Resources.capRed;
        }

        private void captureButton_UC(object sender, EventArgs e)
        {
            captureButton.Image = _mouseInCap ? Properties.Resources.capactive
                                             : Properties.Resources.capRed;
        }

        private void openFolderButton_Click(object sender, EventArgs e)
        {
            _cameraApplication.OpenStorageFolder();
        }

        private void cameraDisplay_Resize(object sender, EventArgs e)
        {
            _cameraApplication.ResizeCameraWindow(cameraDisplay.Size);
        }
    }
    
    public class ComboboxItem
    {
        public string Text { get; set; }
        public object Value { get; set; }

        public override string ToString()
        {
            return Text;
        }
    }
}
