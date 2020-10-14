using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup.Localizer;
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
        private IList<string> _imagePaths;
        private int _currentIndex = -1;
        private Rotation _currentRotation = Rotation.Rotate0;
        private string _filter;
        private double _currentZoom = 1;
        private Vector _currentFrameOffset;

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

        public void SetZoom(double modificator)
        {
            _currentZoom = modificator;
            LoadImage();
        }

        public void MoveZoom(Vector offset)
        {
            offset = (_currentRotation) switch
            {
                Rotation.Rotate0 => offset,
                Rotation.Rotate90 => new Vector(offset.Y, -offset.X),
                Rotation.Rotate180 => new Vector(-offset.X, - offset.Y),
                Rotation.Rotate270 => new Vector(-offset.Y, offset.X),
                _ => throw new ArgumentException($"Unexpected value '{_currentRotation}' of argument '{nameof(_currentRotation)}'")
            };

            _currentFrameOffset = Vector.Add(_currentFrameOffset, offset);
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
            _currentFrameOffset = new Vector(0, 0);
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
            WaitingToRedrawImage = false;
            if (_imagePaths is null || _currentIndex == -1)
                return;

            var uri = new Uri(_imagePaths[_currentIndex]);
            BitmapImage bmpImg = new BitmapImage(uri);
            Size originalSize = new Size(bmpImg.PixelWidth, bmpImg.PixelHeight);
            if (!originalSize.IsRenderable())
                return;

            Size controlSize = _mockBt.RenderSize;
            if (!controlSize.IsRenderable())
                return;

            if (_currentRotation == Rotation.Rotate90 || _currentRotation == Rotation.Rotate270)
                controlSize = new Size(controlSize.Height, controlSize.Width);

            Int32Rect sourceRect;
            if (_currentZoom != 1)
                sourceRect = Zoom(originalSize, controlSize, _currentZoom, ref _currentFrameOffset);
            else
                sourceRect = new Int32Rect(0, 0, (int)originalSize.Width, (int)originalSize.Height);

            if (!sourceRect.IsRenderable())
                return;

            Size adjustedSize = AdjustImageSizeToControlSize(new Size(sourceRect.Width, sourceRect.Height), controlSize);
            if (!adjustedSize.IsRenderable())
                return;

            bmpImg = new BitmapImage();
            bmpImg.BeginInit();
            bmpImg.UriSource = uri;
            bmpImg.Rotation = _currentRotation;
            bmpImg.SourceRect = sourceRect;
            bmpImg.CacheOption = BitmapCacheOption.OnLoad;
            bmpImg.DecodePixelWidth = (int)adjustedSize.Width;
            bmpImg.DecodePixelHeight = (int)adjustedSize.Height;
            bmpImg.EndInit();
            bmpImg.Freeze();

            _imageViewer.Source = null;
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

        // fix this offset pass by reference
        private static Int32Rect Zoom(Size origImgSize, Size controlSize, double zoomModificator, ref Vector frameOffset)
        {
            // resize frame according to original image size
            double widthRatio = origImgSize.Width / controlSize.Width;
            double heightRatio = origImgSize.Height / controlSize.Height;
            double mod = widthRatio > heightRatio ? widthRatio : heightRatio;
            controlSize = new Size(controlSize.Width * mod, controlSize.Height * mod);

            // locate img on control
            double imgX = (controlSize.Width - origImgSize.Width) / 2;
            double imgY = (controlSize.Height - origImgSize.Height) / 2;

            // change size of frame according to mod
            double frameWidth = controlSize.Width / zoomModificator;
            double frameHeight = controlSize.Height / zoomModificator;
            double frameX = (controlSize.Width - frameWidth) / 2;
            double frameY = (controlSize.Height - frameHeight) / 2;

            if (frameX + frameOffset.X < 0) // рамка уехала за левую границу контрола
            {
                frameOffset.X -= frameX + frameOffset.X;
                frameX = 0;
            }
            else
            {
                if (frameX + frameOffset.X + frameWidth > controlSize.Width) // рамка уехала за правую границу контрола
                {
                    frameOffset.X -= frameX + frameOffset.X + frameWidth - controlSize.Width;
                    frameX = controlSize.Width - frameWidth;
                }
                else // рамка в границах горизонтали контрола
                {
                    frameX += frameOffset.X;
                }
            }

            if (frameY + frameOffset.Y < 0) // рамка уехала за верхнюю границу контрола
            {
                frameOffset.Y -= frameY + frameOffset.Y;
                frameY = 0;
            }
            else
            {
                if (frameY + frameOffset.Y + frameHeight > controlSize.Height) // рамка уехала за нижнюю границу контрола
                {
                    frameOffset.Y -= frameY + frameOffset.Y + frameHeight - controlSize.Height;
                    frameY = controlSize.Height - frameHeight;
                }
                else // рамка в границах вертикали контрола
                {
                    frameY += frameOffset.Y;
                }
            }

            // crop the frame to borders of img to get actual rectangle to crop
            int sourceRectX = frameX > imgX ? (int)(frameX - imgX) : 0;
            int sourceRectY = frameY > imgY ? (int)(frameY - imgY) : 0;
            int sourceRectWidth = frameWidth > origImgSize.Width ? (int)origImgSize.Width : (int)frameWidth;
            int sourceRectHeight = frameHeight > origImgSize.Height ? (int)origImgSize.Height : (int)frameHeight;
            sourceRectX = sourceRectX + sourceRectWidth > (int)origImgSize.Width ? (int)origImgSize.Width - sourceRectWidth : sourceRectX;
            sourceRectY = sourceRectY + sourceRectHeight > (int)origImgSize.Height ? (int)origImgSize.Height - sourceRectHeight : sourceRectY;

            return new Int32Rect(sourceRectX, sourceRectY, sourceRectWidth, sourceRectHeight);
        }

        #endregion

        #endregion

        #endregion
    }
}
