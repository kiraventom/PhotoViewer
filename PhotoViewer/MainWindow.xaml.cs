using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace PhotoViewer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            model = new Model(this.MainImageVw, this.MockBt);

            this.Loaded += this.MainWindow_Loaded;
            OpenFileBt.Click += this.OpenFileBt_Click;
            PreviousImageBt.Click += this.PreviousImageBt_Click;
            PreviousImageBt.MouseEnter += this.ChangeImageBt_MouseEnter;
            PreviousImageBt.MouseLeave += this.ImageBt_MouseLeave;
            NextImageBt.Click += this.NextImageBt_Click;
            NextImageBt.MouseEnter += this.ChangeImageBt_MouseEnter;
            NextImageBt.MouseLeave += this.ImageBt_MouseLeave;
            TurnLeftBt.Click += this.TurnLeftBt_Click;
            TurnRightBt.Click += this.TurnRightBt_Click;
            this.SizeChanged += this.MainWindow_SizeChanged;
            this.ZoomSl.ValueChanged += this.ZoomSl_ValueChanged;
        }

        readonly Model model = null;

        private void ZoomSl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            model.ChangeZoom(this.ZoomSl.Value);
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

        private void ChangeImageBt_MouseEnter(object sender, MouseEventArgs e)
        {
            (sender as Button).Foreground = Brushes.Black;
        }

        private void ImageBt_MouseLeave(object sender, MouseEventArgs e)
        {
            (sender as Button).Foreground = Brushes.Transparent;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            NextImageBt.Foreground = Brushes.Transparent;
            PreviousImageBt.Foreground = Brushes.Transparent;
        }

        private void NextImageBt_Click(object sender, RoutedEventArgs e)
        {
            model.SelectImageByOffset(+1);
            ResetZoomSlider();
        }

        private void PreviousImageBt_Click(object sender, RoutedEventArgs e)
        {
            model.SelectImageByOffset(-1);
            ResetZoomSlider();
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

        private void ResetZoomSlider()
        {
            this.ZoomSl.ValueChanged -= this.ZoomSl_ValueChanged;
            this.ZoomSl.Value = this.ZoomSl.Minimum;
            this.ZoomSl.ValueChanged += this.ZoomSl_ValueChanged;
        }
    }
}
