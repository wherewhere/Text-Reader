// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using TextReader.Extensions;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace TextReader.Controls
{
    /// <summary>
    /// The <see cref="ImageCropper"/> control allows user to crop image freely.
    /// </summary>
    public partial class ImageCropper
    {
        private void ImageCropperThumb_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            bool changed = false;
            Point diffPos = default;
            if (e.Key == VirtualKey.Left)
            {
                diffPos.X--;
                CoreVirtualKeyStates upKeyState = Window.Current.CoreWindow.GetAsyncKeyState(VirtualKey.Up);
                CoreVirtualKeyStates downKeyState = Window.Current.CoreWindow.GetAsyncKeyState(VirtualKey.Down);
                if (upKeyState == CoreVirtualKeyStates.Down)
                {
                    diffPos.Y--;
                }

                if (downKeyState == CoreVirtualKeyStates.Down)
                {
                    diffPos.Y++;
                }

                changed = true;
            }
            else if (e.Key == VirtualKey.Right)
            {
                diffPos.X++;
                CoreVirtualKeyStates upKeyState = Window.Current.CoreWindow.GetAsyncKeyState(VirtualKey.Up);
                CoreVirtualKeyStates downKeyState = Window.Current.CoreWindow.GetAsyncKeyState(VirtualKey.Down);
                if (upKeyState == CoreVirtualKeyStates.Down)
                {
                    diffPos.Y--;
                }

                if (downKeyState == CoreVirtualKeyStates.Down)
                {
                    diffPos.Y++;
                }

                changed = true;
            }
            else if (e.Key == VirtualKey.Up)
            {
                diffPos.Y--;
                CoreVirtualKeyStates leftKeyState = Window.Current.CoreWindow.GetAsyncKeyState(VirtualKey.Left);
                CoreVirtualKeyStates rightKeyState = Window.Current.CoreWindow.GetAsyncKeyState(VirtualKey.Right);
                if (leftKeyState == CoreVirtualKeyStates.Down)
                {
                    diffPos.X--;
                }

                if (rightKeyState == CoreVirtualKeyStates.Down)
                {
                    diffPos.X++;
                }

                changed = true;
            }
            else if (e.Key == VirtualKey.Down)
            {
                diffPos.Y++;
                CoreVirtualKeyStates leftKeyState = Window.Current.CoreWindow.GetAsyncKeyState(VirtualKey.Left);
                CoreVirtualKeyStates rightKeyState = Window.Current.CoreWindow.GetAsyncKeyState(VirtualKey.Right);
                if (leftKeyState == CoreVirtualKeyStates.Down)
                {
                    diffPos.X--;
                }

                if (rightKeyState == CoreVirtualKeyStates.Down)
                {
                    diffPos.X++;
                }

                changed = true;
            }

            if (changed)
            {
                ImageCropperThumb imageCropperThumb = (ImageCropperThumb)sender;
                UpdateCroppedRect(imageCropperThumb.Position, diffPos);
            }
        }

        private void ImageCropperThumb_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            Rect selectedRect = new Point(_startX, _startY).ToRect(new Point(_endX, _endY));
            Rect croppedRect = _inverseImageTransform.TransformBounds(selectedRect);
            if (croppedRect.Width > MinCropSize.Width && croppedRect.Height > MinCropSize.Height)
            {
                croppedRect.Intersect(_restrictedCropRect);
                _currentCroppedRect = croppedRect;
            }

            UpdateImageLayout(true);
        }

        private void ImageCropperThumb_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            Rect selectedRect = new Point(_startX, _startY).ToRect(new Point(_endX, _endY));
            Rect croppedRect = _inverseImageTransform.TransformBounds(selectedRect);
            if (croppedRect.Width > MinCropSize.Width && croppedRect.Height > MinCropSize.Height)
            {
                croppedRect.Intersect(_restrictedCropRect);
                _currentCroppedRect = croppedRect;
            }

            UpdateImageLayout(true);
        }

        private void ImageCropperThumb_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            ImageCropperThumb imageCropperThumb = (ImageCropperThumb)sender;
            Point currentPointerPosition = new Point(
                imageCropperThumb.X + e.Position.X + e.Delta.Translation.X - (imageCropperThumb.ActualWidth / 2),
                imageCropperThumb.Y + e.Position.Y + e.Delta.Translation.Y - (imageCropperThumb.ActualHeight / 2));
            Point safePosition = GetSafePoint(_restrictedSelectRect, currentPointerPosition);
            Point safeDiffPoint = new Point(safePosition.X - imageCropperThumb.X, safePosition.Y - imageCropperThumb.Y);
            UpdateCroppedRect(imageCropperThumb.Position, safeDiffPoint);
        }

        private void SourceImage_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            double offsetX = -e.Delta.Translation.X;
            double offsetY = -e.Delta.Translation.Y;
            offsetX = offsetX > 0
                ? Math.Min(offsetX, _restrictedSelectRect.X + _restrictedSelectRect.Width - _endX)
                : Math.Max(offsetX, _restrictedSelectRect.X - _startX);

            offsetY = offsetY > 0
                ? Math.Min(offsetY, _restrictedSelectRect.Y + _restrictedSelectRect.Height - _endY)
                : Math.Max(offsetY, _restrictedSelectRect.Y - _startY);

            Rect selectedRect = new Point(_startX, _startY).ToRect(new Point(_endX, _endY));
            selectedRect.X += offsetX;
            selectedRect.Y += offsetY;
            Rect croppedRect = _inverseImageTransform.TransformBounds(selectedRect);
            croppedRect.Intersect(_restrictedCropRect);
            _currentCroppedRect = croppedRect;
            UpdateImageLayout();
        }

        private void ImageCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Source == null)
            {
                return;
            }

            UpdateImageLayout();
            UpdateMaskArea();
        }
    }
}