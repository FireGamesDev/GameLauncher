using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Windows;
using System.Windows.Forms;

namespace GameLauncher
{
    enum LauncherStatus
    {
        ready,
        failed,
        downloadingGame,
        downloadingUpdate
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string rootPath;
        private string versionFile;
        private string gameZip;
        private string gameExe;

        private LauncherStatus _status;
        internal LauncherStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                switch (_status)
                {
                    case LauncherStatus.ready:
                        PlayButton.IsEnabled = true;
                        PlayButton.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#05E8FA"));
                        PlayButton.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White); ;
                        PlayButton.Content = "Play";
                        break;
                    case LauncherStatus.failed:
                        PlayButton.IsEnabled = false;
                        PlayButton.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
                        PlayButton.Content = "Update Failed - Retry";
                        break;
                    case LauncherStatus.downloadingGame:
                        PlayButton.IsEnabled = false;
                        PlayButton.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
                        PlayButton.Content = "Downloading Game...";
                        break;
                    case LauncherStatus.downloadingUpdate:
                        PlayButton.IsEnabled = false;
                        PlayButton.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
                        PlayButton.Content = "Downloading Update...";
                        break;
                    default:
                        break;
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            rootPath = Directory.GetCurrentDirectory();
            versionFile = Path.Combine(rootPath, "Version.txt");
            gameZip = Path.Combine(rootPath, "Build.zip");
            gameExe = Path.Combine(rootPath, "Build", "Tanks and Magic.exe");
        }

        private void CheckForUpdates()
        {
            if (File.Exists(versionFile))
            {
                Version localVersion = new Version(File.ReadAllText(versionFile));
                VersionText.Text = localVersion.ToString();

                try
                {
                    WebClient webClient = new WebClient();
                    Version onlineVersion = new Version(webClient.DownloadString("https://drive.google.com/uc?export=download&id=1Zl8vOKtzm1J94e6UcYJ-0Q7geiyZlViW"));

                    if (onlineVersion.IsDifferentThan(localVersion))
                    {
                        InstallGameFiles(true, onlineVersion);
                    }
                    else
                    {
                        Status = LauncherStatus.ready;
                    }
                }
                catch (Exception ex)
                {
                    Status = LauncherStatus.failed;
                    System.Windows.MessageBox.Show($"Error checking for game updates: {ex}");
                }
            }
            else
            {
                InstallGameFiles(false, Version.zero);
            }
        }

        private void InstallGameFiles(bool _isUpdate, Version _onlineVersion)
        {
            try
            {
                WebClient webClient = new WebClient();
                if (_isUpdate)
                {
                    Status = LauncherStatus.downloadingUpdate;
                }
                else
                {
                    Status = LauncherStatus.downloadingGame;
                    _onlineVersion = new Version(webClient.DownloadString("https://drive.google.com/uc?export=download&id=1Zl8vOKtzm1J94e6UcYJ-0Q7geiyZlViW"));
                }

                webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(webClient_DownloadProgressChanged);
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallback);

                downloadedAmountText.Visibility = Visibility.Visible;
                blackImage.Visibility = Visibility.Visible;
                progressBar.Visibility = Visibility.Visible;

                webClient.DownloadFileAsync(new Uri("https://www.googleapis.com/drive/v3/files/1fg1B2wpx3zeK-TOlMFU712uiQjIZX0hS?alt=media&key=AIzaSyDIhilHqrIfrL1SrIwt55_o0Nc1IJvHSGU"), gameZip, _onlineVersion);
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                System.Windows.MessageBox.Show($"Error installing game files: {ex}");
            }
        }

        private void DownloadGameCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                downloadedAmountText.Visibility = Visibility.Hidden;
                blackImage.Visibility = Visibility.Hidden;
                progressBar.Visibility = Visibility.Hidden;

                string onlineVersion = ((Version)e.UserState).ToString();

                ZipFile.ExtractToDirectory(gameZip, rootPath);
                File.Delete(gameZip);

                File.WriteAllText(versionFile, onlineVersion);

                VersionText.Text = onlineVersion;
                Status = LauncherStatus.ready;
            }
            catch (Exception ex)
            {
                if (ex is IOException)
                {
                    Status = LauncherStatus.failed;
                    System.Windows.MessageBox.Show("Try to download the game on a different drive!");
                }
                Status = LauncherStatus.failed;
                System.Windows.MessageBox.Show($"Error finishing download: {ex}");
            }
        }

        private void webClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Dispatcher.BeginInvoke((MethodInvoker)delegate {
                double bytesIn = double.Parse(e.BytesReceived.ToString());
                double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
                double percentage = bytesIn / totalBytes * 100;
                downloadedAmountText.Text = "Downloaded " + e.BytesReceived + " of " + e.TotalBytesToReceive;
                progressBar.Value = int.Parse(Math.Truncate(percentage).ToString());
            });
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            PlayButton.MouseEnter += Hover;
            PlayButton.MouseLeave += Leave;
            CheckForUpdates();
        }

        private void Hover(object sender, EventArgs e)
        {
            PlayButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
            PlayButton.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
        }

        private void Leave(object sender, EventArgs e)
        {
            PlayButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
            PlayButton.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(gameExe) && Status == LauncherStatus.ready)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(gameExe);
                startInfo.WorkingDirectory = Path.Combine(rootPath, "Build");
                Process.Start(startInfo);

                Close();
            }
            else if (Status == LauncherStatus.failed)
            {
                CheckForUpdates();
            }
        }
    }



    struct Version
    {
        internal static Version zero = new Version(0, 0, 0);

        private short major;
        private short minor;
        private short subMinor;

        internal Version(short _major, short _minor, short _subMinor)
        {
            major = _major;
            minor = _minor;
            subMinor = _subMinor;
        }
        internal Version(string _version)
        {
            string[] versionStrings = _version.Split('.');
            if (versionStrings.Length != 3)
            {
                major = 0;
                minor = 0;
                subMinor = 0;
                return;
            }

            major = short.Parse(versionStrings[0]);
            minor = short.Parse(versionStrings[1]);
            subMinor = short.Parse(versionStrings[2]);
        }

        internal bool IsDifferentThan(Version _otherVersion)
        {
            if (major != _otherVersion.major)
            {
                return true;
            }
            else
            {
                if (minor != _otherVersion.minor)
                {
                    return true;
                }
                else
                {
                    if (subMinor != _otherVersion.subMinor)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override string ToString()
        {
            return $"{major}.{minor}.{subMinor}";
        }
    }
}
