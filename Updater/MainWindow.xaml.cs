using NLog;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Management.Deployment;
using Windows.Storage;

namespace Updater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Logger logger;
        private PackageManager? pm;
        private string? processPath;

        public MainWindow()
        {
            InitializeComponent();
            logger = LogManager.GetCurrentClassLogger();
            pm = new PackageManager();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey("AppInstallerUri"))
            {
                txtAppInstallerUri.Text = ApplicationData.Current.LocalSettings.Values["AppInstallerUri"].ToString();
                await CheckForUpdatesAsync();
            }
            else
            {
                txtUpdateStatus.Text = "Set an App Installer URI before continuing";
            }
        }

        private async void OnInstallUpdate(object sender, RoutedEventArgs e)
        {
            try
            {
                txtUpdateStatus.Text = "Installing update. At the end of the process, the application will be closed. Launch it again to use the new version";

                var appInstallerUri = new Uri(txtAppInstallerUri.Text);
                logger.Info($"AppInstaller Url: {appInstallerUri}");

                var deploymentTask = pm.RequestAddPackageByAppInstallerFileAsync(appInstallerUri,
                                AddPackageByAppInstallerOptions.ForceTargetAppShutdown, pm.GetDefaultPackageVolume());


                deploymentTask.Progress = (task, progress) =>
                {
                    logger.Info($"Progress: {progress.percentage} - Status: {task.Status}");
                    Dispatcher.Invoke(() =>
                    {
                        txtUpdateProgress.Text = $"Progress: {progress.percentage}";
                    });

                };

                var result = await deploymentTask;

                if (deploymentTask.Status == AsyncStatus.Error)
                {
                    txtUpdateStatus.Text = "Update failed";
                    var results = deploymentTask.GetResults();
                    logger.Error($"Error: {results.ErrorText}");
                }
                else if (deploymentTask.Status == AsyncStatus.Canceled)
                {
                    txtUpdateStatus.Text = "Update canceled";
                    logger.Info("Update canceled");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }

        public async Task CheckForUpdatesAsync()
        {
            try
            {
                txtUpdateStatus.Text = "Checking for updates...";
                Package package = Package.Current;

                string location = System.Reflection.Assembly.GetExecutingAssembly().Location;
                int index = location.IndexOf("\\Updater");
                processPath = $"{location.Substring(0, index)}\\MainApplication\\MainApplication.exe";

                PackageUpdateAvailabilityResult result = await package.CheckUpdateAvailabilityAsync();
                switch (result.Availability)
                {
                    case PackageUpdateAvailability.Available:
                    case PackageUpdateAvailability.Required:
                        txtUpdateStatus.Text = "Update is available";
                        logger.Info("Update is available");
                        UpdatePanel.Visibility = Visibility.Visible;
                        break;
                    case PackageUpdateAvailability.NoUpdates:
                        txtUpdateStatus.Text = "Update not available, launching the app...";
                        await Task.Delay(2000);
                        logger.Info("No updates available");
                        Process.Start(processPath);
                        Application.Current.Shutdown();
                        break;
                    case PackageUpdateAvailability.Unknown:
                    default:
                        break;
                }
            }
            catch (Exception exc)
            {
                logger.Error(exc);
            }
        }

        private void OnLaunchApp(object sender, RoutedEventArgs e)
        {
            Process.Start(processPath);
            Application.Current.Shutdown();
        }

        private async void OnSaveAppInstallerUri(object sender, RoutedEventArgs e)
        {
            ApplicationData.Current.LocalSettings.Values["AppInstallerUri"] = txtAppInstallerUri.Text;
            await CheckForUpdatesAsync();
        }
    }
}
