using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Printing;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace PhotoViewer
{
    class Model
    {
        #region .ctor

        public Model(Image imageViewer, Button mockBt)
        {
            _imageViewer = imageViewer;
            _mockBt = mockBt;
        }

        #endregion

        #region Properties

        #region Public Properties

        public bool WaitingToRedrawImage { get; set; } = false;
        public string Filter
        {
            get
            {
                if (_filter is null)
                {
                    StringBuilder sb = new StringBuilder("Image Files|");
                    foreach (var ext in _extensions)
                    {
                        sb.Append('*');
                        sb.Append(ext);
                        sb.Append(';');
                    }
                    _filter = sb.ToString();
                }

                return _filter;
            }
        }

        #endregion

        #region Private Properties

        private readonly Image _imageViewer;
        private readonly Button _mockBt;
        private IList<string> _imagePaths = null;
        private int _currentIndex = -1;
        private Rotation _currentRotation = Rotation.Rotate0;
        private string _filter = null;
        private double _currentZoom = 1;

        #region Static Properties

        private static readonly IReadOnlyCollection<string> _extensions = new List<string> { ".png", ".jpg" }.AsReadOnly();

        #endregion

        #endregion

        #endregion

        #region Methods

        #region Public Methods

        public void OpenImageFromPath(string fileName)
        {
            var currentDir = new DirectoryInfo(Path.GetDirectoryName(fileName));
            _imagePaths = currentDir.EnumerateFiles()
                .Where(fi => _extensions.Contains(fi.Extension, StringComparer.OrdinalIgnoreCase))
                .Select(fi => fi.FullName)
                .ToList();
            var currentImage = _imagePaths.Single(path => path.Equals(fileName, StringComparison.OrdinalIgnoreCase));
            _currentIndex = _imagePaths.IndexOf(currentImage);
            LoadImage();
        }

        public void ResizeImage()
        {
            LoadImage();
        }

        public void ChangeZoom(double modificator)
        {
            _currentZoom = modificator;
            LoadImage();
        }

        public void RotateImage(int rotationsCount)
        {
            if (_imagePaths is null || _currentIndex == -1)
                return;

            var rotations = Enum.GetValues(typeof(Rotation));
            var requestedRotation = GetIndexByOffset((int)_currentRotation, rotationsCount, rotations.Length);
            if (_currentRotation != (Rotation)requestedRotation)
            {
                _currentRotation = (Rotation)requestedRotation;
                LoadImage();
            }
        }

        public void SelectImageByOffset(int offset)
        {
            if (_imagePaths is null || _currentIndex == -1)
                return;

            _currentRotation = Rotation.Rotate0;
            _currentZoom = 1;
            var requestedIndex = GetIndexByOffset(_currentIndex, offset, _imagePaths.Count);
            if (_currentIndex != requestedIndex)
            {
                _currentIndex = requestedIndex;
                LoadImage();
            }
        }

        #endregion

        #region Private Methods

        private void LoadImage()
        {
            if (_imagePaths is null || _currentIndex == -1)
                return;

            WaitingToRedrawImage = false;

            var uri = new Uri(_imagePaths[_currentIndex]);
            BitmapImage bmpImg = new BitmapImage(uri); // cuz we need image size to adjust it
            Size originalSize = new Size(bmpImg.PixelWidth, bmpImg.PixelHeight);
            Size controlSize = _mockBt.RenderSize;
            _imageViewer.Source = null;
            bmpImg = new BitmapImage();
            bmpImg.BeginInit();
            bmpImg.UriSource = uri;
            var sourceRect = Zoom(new Size(originalSize.Width, originalSize.Height), controlSize, _currentZoom);
            Size adjustedSize = AdjustImageSizeToControlSize(new Size(sourceRect.Width, sourceRect.Height), controlSize);
            bmpImg.SourceRect = sourceRect;
            bmpImg.Rotation = _currentRotation;
            bmpImg.DecodePixelWidth = (int)adjustedSize.Width;
            bmpImg.DecodePixelHeight = (int)adjustedSize.Height;
            bmpImg.CacheOption = BitmapCacheOption.OnLoad;
            bmpImg.EndInit();
            bmpImg.Freeze();
            _imageViewer.Source = bmpImg;
        }

        #region Static Methods

        private static int GetIndexByOffset(int currentIndex, int offset, int collectionSize)
        {
            if (Math.Abs(offset) >= collectionSize)
                offset -= (offset / collectionSize) * collectionSize;

            int unsafeRequestedIndex = currentIndex + offset;
            var safeRequestedIndex = (unsafeRequestedIndex) switch
            {
                int reqIdx when reqIdx >= collectionSize => reqIdx - collectionSize,
                int reqIdx when reqIdx < 0 => collectionSize + reqIdx,
                _ => unsafeRequestedIndex
            };

            return safeRequestedIndex;
        }

        private static Size AdjustImageSizeToControlSize(Size imageSize, Size controlSize)
        {
            // check which side of image is filling its axis and scale its size down: 
            // double mod = control.Side / image.Side
            // image.Size * mod
            // Example:
            // img = 2000:1000, control = 200:200, 2000/200 = 10, 1000 / 200 = 5, 10 > 5 -> width is side
            // img = 2000:1000, control = 200:2, 2000/200 = 10, 1000 / 2 = 50, 10 < 50 -> height is side
            double widthRatio = imageSize.Width / controlSize.Width;
            double heightRatio = imageSize.Height / controlSize.Height;
            double mod = widthRatio > heightRatio
                ? widthRatio
                : heightRatio;

            double newWidth = imageSize.Width / mod;
            double newHeight = imageSize.Height / mod;
            return new Size(newWidth, newHeight);
        }

        private static Int32Rect Zoom(Size origImgSize, Size controlSize, double zoomModificator)
        {
            // resize frame according to original image size
            double widthRatio = origImgSize.Width / controlSize.Width;
            double heightRatio = origImgSize.Height / controlSize.Height;
            double mod = widthRatio > heightRatio
                ? widthRatio
                : heightRatio;
            var adjustedControlSize = new Size(controlSize.Width * mod, controlSize.Height * mod);

            // locate img on control
            double imgX = (adjustedControlSize.Width - origImgSize.Width) / 2;
            double imgY = (adjustedControlSize.Height - origImgSize.Height) / 2;

            // change size of frame according to mod
            double frameWidth = adjustedControlSize.Width / zoomModificator;
            double frameHeight = adjustedControlSize.Height / zoomModificator;
            double frameX = (adjustedControlSize.Width - frameWidth) / 2;
            double frameY = (adjustedControlSize.Height - frameHeight) / 2;

            int sourceRectWidth;
            int sourceRectHeight;
            int sourceRectX;
            int sourceRectY;

            // crop the source rectangle to borders of img to get actual rectangle to crop
            if (origImgSize.Width < frameWidth)
                sourceRectWidth = (int)origImgSize.Width;
            else
                sourceRectWidth = (int)frameWidth;
            if (origImgSize.Height < frameHeight)
                sourceRectHeight = (int)origImgSize.Height;
            else
                sourceRectHeight = (int)frameHeight;
            if (frameX > imgX)
                sourceRectX = (int)(frameX - imgX);
            else
                sourceRectX = 0;
            if (frameY > imgY)
                sourceRectY = (int)(frameY - imgY);
            else
                sourceRectY = 0;

            return new Int32Rect(sourceRectX, sourceRectY, sourceRectWidth, sourceRectHeight);
        }

        #endregion

        #endregion

        #endregion
    }
}
