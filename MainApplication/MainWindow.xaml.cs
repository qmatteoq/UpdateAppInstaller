using System;
using System.IO;
using System.Windows;
using Windows.ApplicationModel;

namespace MainApplication
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var version = $"{Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}.{Package.Current.Id.Version.Revision}"; 
            txtVersion.Text = $"Version {version}";
        }

        private void OnReadFile(object sender, RoutedEventArgs e)
        {
            var file = @$"{Environment.CurrentDirectory}\settings.json";
            var settings = File.ReadAllText(file);
            txtFileContent.Text = settings;
        }
    }
}
