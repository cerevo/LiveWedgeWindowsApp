using System.ComponentModel;
using System.Windows;
using Cerevo.UB300_Win.Controls;
using Cerevo.UB300_Win.ViewModels;


namespace Cerevo.UB300_Win.Views {
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow {
        private VideoHost _videoHost;
        public MainWindowViewModel ViewModel { get; }

        public MainWindow() {
            ViewModel = new MainWindowViewModel();
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            _videoHost = new VideoHost(PreviewVideoImage.ActualHeight, PreviewVideoImage.ActualWidth);
            _videoHost.PositionChanged += (s, a) => {
                ViewModel.VideoPlayer.VideoHostPosition = new Int32Rect(0, 0, _videoHost.HostWidth, _videoHost.HostHeight);
            };
            _videoHost.Paint += (s, a) => {
                if(!ViewModel.VideoPlayer.DrawVideo()) _videoHost.DoDefaultPaint();
            };

            PreviewVideoImage.Child = _videoHost;
            ViewModel.VideoPlayer.SetVideoHostHandle(_videoHost.Handle);
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e) {
            ViewModel.Dispose();
        }
    }
}
