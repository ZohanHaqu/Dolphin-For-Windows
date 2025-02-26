using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Runtime.InteropServices;

namespace DolphinForWindows
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadDrives();
        }

        // Load all available drives into the ComboBox
        private void LoadDrives()
        {
            DriveSelector.Items.Clear();
            foreach (var drive in DriveInfo.GetDrives())
            {
                DriveSelector.Items.Add(drive.Name);
            }

            if (DriveSelector.Items.Count > 0)
                DriveSelector.SelectedIndex = 0; // Select the first drive
        }

        // Handle drive selection changes
        private void DriveSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DriveSelector.SelectedItem != null)
            {
                string selectedDrive = DriveSelector.SelectedItem.ToString();
                LoadDirectory(selectedDrive);
            }
        }

        // Load files and folders of a directory
        private void LoadDirectory(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    MessageBox.Show("The path does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                FileListView.Items.Clear();
                AddressBar.Text = path;

                var directories = Directory.GetDirectories(path)
                    .Select(dir => new FileItem { Name = System.IO.Path.GetFileName(dir), Type = "Folder", FullPath = dir });

                var files = Directory.GetFiles(path)
                    .Select(file => new FileItem
                    {
                        Name = System.IO.Path.GetFileName(file),
                        Type = "File",
                        Size = new FileInfo(file).Length.ToString() + " bytes",
                        FullPath = file
                    });

                foreach (var item in directories.Concat(files))
                {
                    FileListView.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading directory: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Open a folder or file when an item is double-clicked
        private void FileListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (FileListView.SelectedItem is FileItem item)
            {
                string path = item.FullPath;
                if (Directory.Exists(path))
                {
                    LoadDirectory(path);
                }
                else if (File.Exists(path))
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error opening file: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("The selected item is not a valid file or folder.", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        // Handle Quick Access toolbar button clicks
        private void QuickAccess_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                string folderName = btn.Tag.ToString();
                string folderPath;

                switch (folderName)
                {
                    case "Downloads":
                        folderPath = KnownFolders.GetPath(KnownFolder.Downloads);
                        break;
                    case "Desktop":
                        folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                        break;
                    case "Documents":
                        folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                        break;
                    case "Pictures":
                        folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                        break;
                    case "Music":
                        folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                        break;
                    case "Videos":
                        folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
                        break;
                    default:
                        throw new ArgumentException($"Requested value '{folderName}' was not found.");
                }

                LoadDirectory(folderPath);
            }
        }

        // Handle Go Button Click
        private void GoButton_Click(object sender, RoutedEventArgs e)
        {
            LoadDirectory(AddressBar.Text);
        }

        // Handle Address Bar "Enter" key press
        private void AddressBar_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                LoadDirectory(AddressBar.Text);
            }
        }
    }

    // File Item Class (For ListView)
    public class FileItem
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Size { get; set; }
        public string FullPath { get; set; } // Stores the full path for navigation
    }

    public static class KnownFolders
    {
        private static readonly string[] KnownFolderGuids = new string[]
        {
            "{374DE290-123F-4565-9164-39C4925E467B}" // Downloads
        };

        public static string GetPath(KnownFolder knownFolder)
        {
            return GetPath(knownFolder, false);
        }

        public static string GetPath(KnownFolder knownFolder, bool defaultUser)
        {
            int result = SHGetKnownFolderPath(new Guid(KnownFolderGuids[(int)knownFolder]), 0, new IntPtr(defaultUser ? -1 : 0), out IntPtr outPath);
            if (result >= 0)
            {
                string path = Marshal.PtrToStringUni(outPath);
                Marshal.FreeCoTaskMem(outPath);
                return path;
            }
            else
            {
                throw new ExternalException("Unable to retrieve the known folder path. It may not be available on this system.", result);
            }
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr ppszPath);
    }

    public enum KnownFolder
    {
        Downloads
    }
}
