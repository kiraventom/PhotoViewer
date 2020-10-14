using Microsoft.Win32;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace PhotoViewer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            model = new Model(this.MainImageVw, this.MockBt);

            this.OpenFileBt.Click += this.OpenFileBt_Click;
            this.PreviousImageBt.Click += this.PreviousImageBt_Click;
            this.NextImageBt.Click += this.NextImageBt_Click;
            this.TurnLeftBt.Click += this.TurnLeftBt_Click;
            this.TurnRightBt.Click += this.TurnRightBt_Click;
            this.SizeChanged += this.MainWindow_SizeChanged;
            this.MainImageVw.MouseWheel += this.MainImageVw_MouseWheel;
        }

        private readonly Model model = null;
        const int minZoom = 1;
        const int maxZoom = 10;

        private double _currentZoom;
        private double CurrentZoom
        {
            get => _currentZoom;
            set
            {
                if (value < minZoom || value > maxZoom)
                    return;
                _currentZoom = value;
                model.ChangeZoom(_currentZoom);
            }
        }

        private void MainImageVw_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                CurrentZoom += 1;
            else
                CurrentZoom -= 1;
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!model.WaitingToRedrawImage)
            {
                model.WaitingToRedrawImage = true;
                Dispatcher.BeginInvoke(model.ResizeImage, DispatcherPriority.ApplicationIdle);
            }
        }

        private void TurnLeftBt_Click(object sender, RoutedEventArgs e)
        {
            model.RotateImage(-1);
        }

        private void TurnRightBt_Click(object sender, RoutedEventArgs e)
        {
            model.RotateImage(+1);
        }

        private void NextImageBt_Click(object sender, RoutedEventArgs e)
        {
            model.SelectImageByOffset(+1);
            CurrentZoom = minZoom;
        }

        private void PreviousImageBt_Click(object sender, RoutedEventArgs e)
        {
            model.SelectImageByOffset(-1);
            CurrentZoom = minZoom;
        }

        private void OpenFileBt_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog()
            {
                Filter = model.Filter
            };
            if (ofd.ShowDialog() ?? false)
            {
                model.OpenImageFromPath(ofd.FileName);
            }
        }
    }
}
