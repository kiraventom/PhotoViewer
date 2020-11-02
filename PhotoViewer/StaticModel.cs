using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace PhotoViewer
{
    partial class Model
    {
        private static string ConstructFilter(IEnumerable<string> allowedExtensions)
        {
            StringBuilder sb = new StringBuilder("Image Files|");
            foreach (var ext in allowedExtensions)
            {
                sb.Append('*');
                sb.Append(ext);
                sb.Append(';');
            }
            return sb.ToString();
        }

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
        private static Int32Rect Zoom(Size imgSize, Size controlSize, double zoomModificator, ref Vector frameOffset)
        {
            // resize frame according to original image size
            double widthRatio = imgSize.Width / controlSize.Width;
            double heightRatio = imgSize.Height / controlSize.Height;
            double mod = widthRatio > heightRatio ? widthRatio : heightRatio;
            controlSize = new Size(controlSize.Width * mod, controlSize.Height * mod);

            // locate img on control
            double imgX = (controlSize.Width - imgSize.Width) / 2;
            double imgY = (controlSize.Height - imgSize.Height) / 2;

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
            int sourceRectWidth = frameWidth > imgSize.Width ? (int)imgSize.Width : (int)frameWidth;
            int sourceRectHeight = frameHeight > imgSize.Height ? (int)imgSize.Height : (int)frameHeight;
            sourceRectX = sourceRectX + sourceRectWidth > (int)imgSize.Width ? (int)imgSize.Width - sourceRectWidth : sourceRectX;
            sourceRectY = sourceRectY + sourceRectHeight > (int)imgSize.Height ? (int)imgSize.Height - sourceRectHeight : sourceRectY;

            return new Int32Rect(sourceRectX, sourceRectY, sourceRectWidth, sourceRectHeight);
        }
    }
}
