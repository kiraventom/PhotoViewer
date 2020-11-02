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
        public string Filter => _filter ??= ConstructFilter(_allowedExtensions);

        private readonly Image _imageViewer;
        private readonly Button _mockBt;
        private IList<string> _imagePaths;
        private Rotation _currentRotation = Rotation.Rotate0;
        private string _filter;
        private double _currentZoom = 1;
        private double CurrentZoom
        {
            get => _currentZoom;
            set
            {
                _optimizedBitmap = null;
                _currentZoom = value;
                if (_currentZoom == 1)
                {
                    _currentFrameOffset = new Vector(0, 0);
                }
            }
        }

        private Vector _currentFrameOffset;
        private Uri CurrentImageUri => CurrentIndex != -1 && _imagePaths != null ? new Uri(_imagePaths[CurrentIndex]) : null;
        private int _currentIndex = -1;
        private int CurrentIndex 
        {
            get => _currentIndex;
            set
            {
                _currentIndex = value;
                Reset();
            }
        }

        private BitmapImage _rawBitmap;
        private BitmapImage _optimizedBitmap;
        private BitmapImage GetOptimizedBitmap(Size controlSize)
        {
            if (_optimizedBitmap is null)
            {
                if (CurrentImageUri is null)
                    return null;

                if (_rawBitmap is null)
                    _rawBitmap = new BitmapImage(CurrentImageUri);

                var currentBitmapSize = new Size(_rawBitmap.PixelWidth, _rawBitmap.PixelHeight);
                if (!currentBitmapSize.IsRenderable())
                    return null;

                if (!controlSize.IsRenderable())
                    return null;

                Size adjustedSize = AdjustImageSizeToControlSize(currentBitmapSize, controlSize);
                if (!adjustedSize.IsRenderable())
                    return null;

                var bmpImage = new BitmapImage();
                bmpImage.BeginInit();
                bmpImage.UriSource = CurrentImageUri;
                bmpImage.CacheOption = BitmapCacheOption.OnLoad;
                bmpImage.DecodePixelWidth = (int)adjustedSize.Width;
                bmpImage.DecodePixelHeight = (int)adjustedSize.Height;
                bmpImage.EndInit();
                bmpImage.Freeze();

                _optimizedBitmap = bmpImage;
            }
            
            return _optimizedBitmap;
        }

        private static readonly IReadOnlyCollection<string> _allowedExtensions = new List<string> { ".png", ".jpg" }.AsReadOnly();

        public void OpenImageFromPath(string fileName)
        {
            var currentDir = new DirectoryInfo(Path.GetDirectoryName(fileName));
            _imagePaths = currentDir.EnumerateFiles()
                .Where(fi => _allowedExtensions.Contains(fi.Extension, StringComparer.OrdinalIgnoreCase))
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
            CurrentZoom = modificator;
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
            if (CurrentImageUri is null)
                return;

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

            Size controlSize = new Size(_mockBt.RenderSize.Width * CurrentZoom, _mockBt.RenderSize.Height * CurrentZoom);
            if (!controlSize.IsRenderable())
                return;

            BitmapImage bitmap;
            if (CurrentZoom != 1 && _rawBitmap != null && (controlSize.Width > _rawBitmap.Width || controlSize.Height > _rawBitmap.Height))
            {
                bitmap = _rawBitmap;
                controlSize.Width = _mockBt.RenderSize.Width;
                controlSize.Height = _mockBt.RenderSize.Height;
            }
            else
            {
                bitmap = GetOptimizedBitmap(controlSize);
            }
            
            if (bitmap is null)
                return;

            if (_currentRotation == Rotation.Rotate90 || _currentRotation == Rotation.Rotate270)
                controlSize = new Size(controlSize.Height, controlSize.Width);

            Int32Rect sourceRect;
            if (CurrentZoom != 1)
            {
                var size = new Size(bitmap.PixelWidth, bitmap.PixelHeight);
                sourceRect = Zoom(size, controlSize, CurrentZoom, ref _currentFrameOffset);
            }
            else
                sourceRect = Int32Rect.Empty;

            var croppedImage = new CroppedBitmap();
            croppedImage.BeginInit();
            croppedImage.Source = bitmap;
            croppedImage.SourceRect = sourceRect;
            croppedImage.EndInit();
            croppedImage.Freeze();

            var bmpImage = new BitmapImage();
            bmpImage.BeginInit();
            bmpImage.StreamSource = new MemoryStream(croppedImage.GetBytes());
            bmpImage.Rotation = _currentRotation;
            bmpImage.CacheOption = BitmapCacheOption.OnLoad;
            bmpImage.EndInit();
            bmpImage.Freeze();

            _imageViewer.Source = bmpImage;
        }

        private void Reset()
        {
            _rawBitmap = null;
            CurrentZoom = 1;
            _currentRotation = Rotation.Rotate0;
            _currentFrameOffset = new Vector(0, 0);
        }
    }
}
