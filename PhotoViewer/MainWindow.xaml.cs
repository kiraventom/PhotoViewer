using Microsoft.Win32;
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
            this.MainImageVw.MouseLeftButtonDown += this.MainImageVw_MouseLeftButtonDown;
            this.MainImageVw.MouseMove += this.MainImageVw_MouseMove;
            this.MainImageVw.MouseLeftButtonUp += this.MainImageVw_MouseLeftButtonUp;
            this.Loaded += this.MainWindow_Loaded;
        }

        private readonly Model model = null;
        const int minZoom = 1;
        const int maxZoom = 10;

        private double _currentZoom;
        private void SetCurrentZoom(double value)
        {
            if (value < minZoom || value > maxZoom)
                return;
            _currentZoom = value;
            model.SetZoom(_currentZoom);
        }

        bool moveImageToggle = false;
        Point lastPosition = new Point(-1, -1);

        private void MainImageVw_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => moveImageToggle = true;

        private void MainImageVw_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) => moveImageToggle = false;

        private void MainImageVw_MouseMove(object sender, MouseEventArgs e)
        {
            var currentPosition = e.GetPosition(MockBt);
            if (moveImageToggle)
            {
                if (lastPosition != new Point(-1, -1))
                {
                    var offset = Point.Subtract(lastPosition, currentPosition);
                    lastPosition = currentPosition;
                    if (offset.Length >= 1)
                    {
                        model.MoveZoom(offset);
                    }
                        
                }
            }
            lastPosition = currentPosition;
        }

        private void MainImageVw_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                SetCurrentZoom(_currentZoom + 1);
            else
                SetCurrentZoom(_currentZoom - 1);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e) => _ = 1;
            //RenderOptions.SetBitmapScalingMode(this.MainImageVw, BitmapScalingMode.NearestNeighbor);

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
            SetCurrentZoom(minZoom);
        }

        private void PreviousImageBt_Click(object sender, RoutedEventArgs e)
        {
            model.SelectImageByOffset(-1);
            SetCurrentZoom(minZoom);
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
