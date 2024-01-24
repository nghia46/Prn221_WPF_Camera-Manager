using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using AForge.Video;
using AForge.Video.DirectShow;
using WinForms = System.Windows.Forms;


namespace ImageCameraManager
{
    public partial class MainWindow
    {
        private FilterInfoCollection? _filterInfoCollection = null;
        private VideoCaptureDevice? _videoCaptureDevice;
        private readonly Stack<string> _folderHistory = new Stack<string>();
        private readonly Stack<string> _forwardHistory = new Stack<string>();
        private readonly FileSystemWatcher _fileSystemWatcher = new FileSystemWatcher();
        public static MainWindow? Instance { get; private set; } 
        public dynamic? SelectedItem => ListView.SelectedItem as dynamic;
        public MainWindow()
        {
            InitializeComponent();
            UpdateButtonStates();
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing!;
            Instance = this;
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _filterInfoCollection = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (_filterInfoCollection.Count <= 0) return;
            _videoCaptureDevice = new VideoCaptureDevice(_filterInfoCollection[0].MonikerString);
            _videoCaptureDevice.NewFrame += VideoCaptureDevice_NewFrame;
            _videoCaptureDevice.Start();
        }

        private void VideoCaptureDevice_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            webcamPictureBox.Image = (Bitmap)eventArgs.Frame.Clone();
        }
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_videoCaptureDevice is { IsRunning: true })
            {
                _videoCaptureDevice.SignalToStop();
                _videoCaptureDevice.WaitForStop();
            }

            _fileSystemWatcher.Dispose();
        }

        private void TackePicBtn_Click(object sender, RoutedEventArgs e)
        {
            if (webcamPictureBox.Image != null)
            {
                var snapshot = (Bitmap)webcamPictureBox.Image.Clone();
                var currentFolder = _folderHistory.Peek();
                var fileName = $"selfie_{DateTime.Now:yyyyMMddHHmmss}.png";
                var filePath = Path.Combine(currentFolder, fileName);
                snapshot.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
                System.Windows.MessageBox.Show($"Snapshot saved to {filePath}", "Selfie Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                UpdateFileList(_folderHistory.Peek());
            }
            else
            {
                System.Windows.MessageBox.Show("No frame captured.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedFolder = BrowseForFolder();
            if (string.IsNullOrEmpty(selectedFolder)) return;

            _folderHistory.Push(selectedFolder);
            _forwardHistory.Clear();

            UpdateFileList(selectedFolder);
            UpdateButtonStates();
        }

        private string? BrowseForFolder()
        {
            var dialog = new FolderBrowserDialog();
            dialog.SelectedPath = @"";
            var result = dialog.ShowDialog();
            UpdateFilePathIntoTextBox(dialog.SelectedPath);
            return result == WinForms.DialogResult.OK ? dialog.SelectedPath : null;
        }

        private void UpdateFilePathIntoTextBox(string path)
        {
            DirectoryTxt.Text = !string.IsNullOrWhiteSpace(path) ? path : "Invalid Path";
        }
        private void ForwardBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_forwardHistory.Count <= 0) return;
            var forwardFolder = _forwardHistory.Pop();
            _folderHistory.Push(forwardFolder);
            UpdateFileList(forwardFolder);
            UpdateFilePathIntoTextBox(forwardFolder);
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            TackePicBtn.IsEnabled = DirectoryTxt.Text != string.Empty;
            BackwardBtn.IsEnabled = _folderHistory.Count > 1;
            ForwardBtn.IsEnabled = _forwardHistory.Count > 0;
        }
        private void BackwardBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_folderHistory.Count <= 1) return;
            var currentFolder = _folderHistory.Pop();
            _forwardHistory.Push(currentFolder);

            var previousFolder = _folderHistory.Peek();
            UpdateFileList(previousFolder);
            UpdateFilePathIntoTextBox(previousFolder);
            UpdateButtonStates();
        }

        public void UpdateFileList(string? selectedFolder)
        {
            if (selectedFolder == null) return;

            var entries = Directory.GetFileSystemEntries(selectedFolder); 
            var fileInfos = entries.Select(entry => CreateFileInfo(entry))
                                   .Where(item => item.Type == "Folder" || item.Type == "Image")
                                   .OrderByDescending(item => item.Type == "Folder").ToList();

            ListView.ItemsSource = fileInfos;
        }
        private static bool IsImageFile(string? extension)
        {
            string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
            return imageExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }
        private static dynamic CreateFileInfo(string entry)
        {
            return new
            {
                Type = GetFileType(entry),
                Name = Path.GetFileName(entry),
                Path = entry,
                Icon = GetFileIcon(entry)
            };
        }
        private static string GetFileType(string path)
        {
            var extension = Path.GetExtension(path)?.ToLower();

            if (File.GetAttributes(path).HasFlag(FileAttributes.Directory))
            {
                return "Folder";
            }
            else if (IsImageFile(extension))
            {
                return "Image";
            }
            else
            {
                return "File";
            }
        }
        private void ListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ListView.SelectedItem == null) return;
            var selectedItem = (dynamic)ListView.SelectedItem;

            if (selectedItem.Type == "Folder")
            {
                _folderHistory.Push(selectedItem.Path);
                _forwardHistory.Clear();
                _forwardHistory.Clear();

                UpdateFilePathIntoTextBox(selectedItem.Path);
                UpdateFileList(selectedItem.Path);
                UpdateButtonStates();
            }
            else if (selectedItem.Type == "Image")
            {
                //OpenFile(selectedItem.Path);
                ImagePopupForm imagePopupForm = new ImagePopupForm();
                imagePopupForm.ShowDialog();
            }
        }

        private static void OpenFile(string filePath)
        {
            var processStartInfo = new ProcessStartInfo(filePath)
            {
                UseShellExecute = true
            };
            Process.Start(processStartInfo);
        }

        private static BitmapImage GetFileIcon(string path)
        {
            var isDirectory = File.GetAttributes(path).HasFlag(FileAttributes.Directory);
            var iconPath = isDirectory
                ? @"D:\Repos\PRN221\ImageCameraManager\ImageCameraManager\Resources\folder.png"
                : @"D:\Repos\PRN221\ImageCameraManager\ImageCameraManager\Resources\file.png";
            return new BitmapImage(new Uri(iconPath));
        }
    }
}
