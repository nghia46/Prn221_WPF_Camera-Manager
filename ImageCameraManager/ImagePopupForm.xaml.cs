using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using MessageBox = System.Windows.MessageBox;
using WinForms = System.Windows.Forms;

namespace ImageCameraManager
{
    /// <summary>
    /// Interaction logic for ImagePopupForm.xaml
    /// </summary>
    public partial class ImagePopupForm : Window
    {
        private readonly dynamic? _selectedItem;

        public ImagePopupForm()
        {
            InitializeComponent();
            _selectedItem = MainWindow.Instance.SelectedItem as dynamic;
            LoadImage();
        }

        private void LoadImage()
        {
            try
            {
                var getPath = _selectedItem?.Path;

                if (string.IsNullOrEmpty(getPath))
                {
                    MessageBox.Show("Selected item path is null or empty.", "Error");
                    return;
                }

                string fileNameWithoutExtension = GetFileNameWithoutExtension(getPath);

                SetNameWithoutExtension(fileNameWithoutExtension);

                BitmapImage bitmap = LoadBitmapImage(getPath);

                SetImageSource(bitmap);
            }
            catch (FileNotFoundException ex)
            {
                MessageBox.Show($"Error loading image: {ex.Message}", "Error");
            }
            catch (UriFormatException ex)
            {
                MessageBox.Show($"Error loading image: {ex.Message}", "Error");
            }
        }

        private string GetFileNameWithoutExtension(string path)
        {
            return Path.GetFileNameWithoutExtension(path);
        }

        private void SetNameWithoutExtension(string name)
        {
            NameTxt.Text = name;
        }

        private BitmapImage LoadBitmapImage(string path)
        {
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
            bitmap.EndInit();
            return bitmap;
        }

        private void SetImageSource(BitmapImage bitmap)
        {
            ImageSrc.Source = bitmap;
        }

        private async void SubmitBtn_Click(object sender, RoutedEventArgs e)
        {
            // Show a loading indicator or disable the button to indicate the operation is in progress
            // YourLoadingIndicator.Visibility = Visibility.Visible;
            // SubmitBtn.IsEnabled = false;

            await Task.Run(() => RenameSelectedItem());

            // Hide the loading indicator or enable the button once the operation is complete
            // YourLoadingIndicator.Visibility = Visibility.Collapsed;
            // SubmitBtn.IsEnabled = true;

            this.Close();
        }

        private void RenameSelectedItem()
        {
            string newName = NameTxt.Text.Trim();

            if (string.IsNullOrEmpty(newName))
                return;

            try
            {
                // Get the directory of the selected item
                string parentDirectory = Path.GetDirectoryName(_selectedItem?.Path);

                if (string.IsNullOrEmpty(parentDirectory))
                {
                    MessageBox.Show("Parent directory is null or empty.", "Error");
                    return;
                }

                // Create the new path with the new name using Path.Combine
                string newPath = Path.Combine(parentDirectory, newName);

                // Check if the file exists before attempting to move it
                if (File.Exists(_selectedItem?.Path))
                {
                    // Perform the renaming operation for files
                    File.Move(_selectedItem.Path, newPath);
                }
                else
                {
                    MessageBox.Show("The file does not exist.", "Error");
                }

                // Update the UI in the main window after renaming
                MainWindow.Instance?.UpdateFileList(parentDirectory);
            }
            catch (IOException ex)
            {
                MessageBox.Show($"Error renaming item: {ex.Message}", "Error");
            }
        }
    }
}
