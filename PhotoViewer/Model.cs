using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace PhotoViewer
{
    partial class Model
    {
        public Model(Image imageViewer, Button mockBt)
        {
            _imageViewer = imageViewer;
            _mockBt = mockBt;
        }

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

        private readonly Image _imageViewer;
        private readonly Button _mockBt;
        private IList<string> _imagePaths;
        private Rotation _currentRotation = Rotation.Rotate0;
        private string _filter;
        private double _currentZoom = 1;
        private Vector _currentFrameOffset;
        private Size _currentImageSize;
        private Uri _currentImageUri => CurrentIndex != -1 && _imagePaths != null ? new Uri(_imagePaths[CurrentIndex]) : null;
        private int _currentIndex = -1;
        private int CurrentIndex
        {
            get => _currentIndex;
            set
            {
                _currentIndex = value;

                if (_currentImageUri is null)
                    return;

                BitmapImage bmpImg = new BitmapImage(_currentImageUri);
                _currentImageSize = new Size(bmpImg.PixelWidth, bmpImg.PixelHeight);
            }
        }

        private static readonly IReadOnlyCollection<string> _extensions = new List<string> { ".png", ".jpg" }.AsReadOnly();

        public void OpenImageFromPath(string fileName)
        {
            var currentDir = new DirectoryInfo(Path.GetDirectoryName(fileName));
            _imagePaths = currentDir.EnumerateFiles()
                .Where(fi => _extensions.Contains(fi.Extension, StringComparer.OrdinalIgnoreCase))
                .Select(fi => fi.FullName)
                .ToList();
            var currentImage = _imagePaths.Single(path => path.Equals(fileName, StringComparison.OrdinalIgnoreCase));
            CurrentIndex = _imagePaths.IndexOf(currentImage);
            UpdateImage();
        }

        public void ResizeImage()
        {
            UpdateImage();
        }

        public void SetZoom(double modificator)
        {
            _currentZoom = modificator;
            UpdateImage();
        }

        public void MoveZoom(Vector offset)
        {
            offset = (_currentRotation) switch
            {
                Rotation.Rotate0 => offset,
                Rotation.Rotate90 => new Vector(offset.Y, - offset.X),
                Rotation.Rotate180 => new Vector(- offset.X, - offset.Y),
                Rotation.Rotate270 => new Vector(- offset.Y, offset.X),
                _ => throw new ArgumentException($"Unexpected value '{_currentRotation}' of argument '{nameof(_currentRotation)}'")
            };

            _currentFrameOffset = Vector.Add(_currentFrameOffset, offset);
            UpdateImage();
        }

        public void RotateImage(int rotationsCount)
        {
            if (_imagePaths is null || CurrentIndex == -1)
                return;

            var rotations = Enum.GetValues(typeof(Rotation));
            var requestedRotation = GetIndexByOffset((int)_currentRotation, rotationsCount, rotations.Length);
            if (_currentRotation != (Rotation)requestedRotation)
            {
                _currentRotation = (Rotation)requestedRotation;
                UpdateImage();
            }
        }

        public void SelectImageByOffset(int offset)
        {
            if (_currentImageUri is null)
                return;

            _currentRotation = Rotation.Rotate0;
            _currentZoom = 1;
            _currentFrameOffset = new Vector(0, 0);
            var requestedIndex = GetIndexByOffset(CurrentIndex, offset, _imagePaths.Count);
            if (CurrentIndex != requestedIndex)
            {
                CurrentIndex = requestedIndex;
                UpdateImage();
            }
        }

        private void UpdateImage()
        {
            WaitingToRedrawImage = false;

            Size originalSize = _currentImageSize;
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

            var bmpImg = new BitmapImage();
            bmpImg.BeginInit();
            bmpImg.UriSource = _currentImageUri;
            bmpImg.Rotation = _currentRotation;
            bmpImg.SourceRect = sourceRect;
            bmpImg.CacheOption = BitmapCacheOption.OnLoad;
            bmpImg.DecodePixelWidth = (int)adjustedSize.Width;
            bmpImg.DecodePixelHeight = (int)adjustedSize.Height;
            bmpImg.EndInit();
            bmpImg.Freeze();

            _imageViewer.Source = bmpImg;
        }
    }
}
