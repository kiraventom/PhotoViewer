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

            this.OpenFileBt.Click += this.OpenFileBt_Click;
            this.PreviousImageBt.Click += this.PreviousImageBt_Click;
            this.NextImageBt.Click += this.NextImageBt_Click;
            this.TurnLeftBt.Click += this.TurnLeftBt_Click;
            this.TurnRightBt.Click += this.TurnRightBt_Click;
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
