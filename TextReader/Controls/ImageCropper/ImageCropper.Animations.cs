// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Numerics;
using TextReader.Extensions;
using Windows.Foundation;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace TextReader.Controls
{
    /// <summary>
    /// The <see cref="ImageCropper"/> control allows user to crop image freely.
    /// </summary>
    public partial class ImageCropper
    {
        private static void AnimateUIElementOffset(Point to, TimeSpan duration, UIElement target)
        {
            if (IsCompositionSupported)
            {
                Visual targetVisual = ElementCompositionPreview.GetElementVisual(target);
                Compositor compositor = targetVisual.Compositor;
                LinearEasingFunction linear = compositor.CreateLinearEasingFunction();
                Vector3KeyFrameAnimation offsetAnimation = compositor.CreateVector3KeyFrameAnimation();
                offsetAnimation.Duration = duration;
                offsetAnimation.Target = nameof(Visual.Offset);
                offsetAnimation.InsertKeyFrame(1.0f, new Vector3((float)to.X, (float)to.Y, 0), linear);
                targetVisual.StartAnimation(nameof(Visual.Offset), offsetAnimation);
            }
            else
            {
                if (!(target.RenderTransform is CompositeTransform))
                {
                    target.RenderTransform = new CompositeTransform
                    {
                        CenterX = 0.5,
                        CenterY = 0.5
                    };
                }
                Storyboard storyboard = new Storyboard();
                storyboard.Children.Add(CreateDoubleAnimation(to.X, duration, target, s_translateXPath, false));
                storyboard.Children.Add(CreateDoubleAnimation(to.Y, duration, target, s_translateYPath, false));
                storyboard.Begin();
            }
        }

        private static void AnimateUIElementScale(double to, TimeSpan duration, UIElement target)
        {
            if (IsCompositionSupported)
            {
                Visual targetVisual = ElementCompositionPreview.GetElementVisual(target);
                Compositor compositor = targetVisual.Compositor;
                LinearEasingFunction linear = compositor.CreateLinearEasingFunction();
                Vector3KeyFrameAnimation scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
                scaleAnimation.Duration = duration;
                scaleAnimation.Target = nameof(Visual.Scale);
                scaleAnimation.InsertKeyFrame(1.0f, new Vector3((float)to), linear);
                targetVisual.StartAnimation(nameof(Visual.Scale), scaleAnimation);
            }
            else
            {
                if (!(target.RenderTransform is CompositeTransform))
                {
                    target.RenderTransform = new CompositeTransform
                    {
                        CenterX = 0.5,
                        CenterY = 0.5
                    };
                }
                Storyboard storyboard = new Storyboard();
                storyboard.Children.Add(CreateDoubleAnimation(to, duration, target, s_scaleXPath, false));
                storyboard.Children.Add(CreateDoubleAnimation(to, duration, target, s_scaleYPath, false));
                storyboard.Begin();
            }
        }

        private static DoubleAnimation CreateDoubleAnimation(double to, TimeSpan duration, DependencyObject target, string propertyName, bool enableDependentAnimation)
        {
            DoubleAnimation animation = new DoubleAnimation()
            {
                To = to,
                Duration = duration,
                EnableDependentAnimation = enableDependentAnimation
            };

            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation, propertyName);

            return animation;
        }

        private static PointAnimation CreatePointAnimation(Point to, TimeSpan duration, DependencyObject target, string propertyName, bool enableDependentAnimation)
        {
            PointAnimation animation = new PointAnimation()
            {
                To = to,
                Duration = duration,
                EnableDependentAnimation = enableDependentAnimation
            };

            Storyboard.SetTarget(animation, target);
            Storyboard.SetTargetProperty(animation, propertyName);

            return animation;
        }

        private static ObjectAnimationUsingKeyFrames CreateRectangleAnimation(Rect to, TimeSpan duration, RectangleGeometry rectangle, bool enableDependentAnimation)
        {
            ObjectAnimationUsingKeyFrames animation = new ObjectAnimationUsingKeyFrames
            {
                Duration = duration,
                EnableDependentAnimation = enableDependentAnimation
            };

            List<DiscreteObjectKeyFrame> frames = GetRectKeyframes(rectangle.Rect, to, duration);
            foreach (DiscreteObjectKeyFrame item in frames)
            {
                animation.KeyFrames.Add(item);
            }

            Storyboard.SetTarget(animation, rectangle);
            Storyboard.SetTargetProperty(animation, nameof(RectangleGeometry.Rect));

            return animation;
        }

        private static List<DiscreteObjectKeyFrame> GetRectKeyframes(Rect from, Rect to, TimeSpan duration)
        {
            List<DiscreteObjectKeyFrame> rectKeyframes = new List<DiscreteObjectKeyFrame>();
            TimeSpan step = TimeSpan.FromMilliseconds(10);
            Point startPointFrom = new Point(from.X, from.Y);
            Point endPointFrom = new Point(from.X + from.Width, from.Y + from.Height);
            Point startPointTo = new Point(to.X, to.Y);
            Point endPointTo = new Point(to.X + to.Width, to.Y + to.Height);
            for (TimeSpan time = default; time < duration; time += step)
            {
                double progress = time.TotalMilliseconds / duration.TotalMilliseconds;
                Point startPoint = new Point
                {
                    X = startPointFrom.X + (progress * (startPointTo.X - startPointFrom.X)),
                    Y = startPointFrom.Y + (progress * (startPointTo.Y - startPointFrom.Y)),
                };
                Point endPoint = new Point
                {
                    X = endPointFrom.X + (progress * (endPointTo.X - endPointFrom.X)),
                    Y = endPointFrom.Y + (progress * (endPointTo.Y - endPointFrom.Y)),
                };
                rectKeyframes.Add(new DiscreteObjectKeyFrame
                {
                    KeyTime = KeyTime.FromTimeSpan(time),
                    Value = startPoint.ToRect(endPoint)
                });
            }

            rectKeyframes.Add(new DiscreteObjectKeyFrame
            {
                KeyTime = duration,
                Value = to
            });
            return rectKeyframes;
        }

        private const string s_centerXPath = "(UIElement.RenderTransform).(CompositeTransform.CenterX)";
        private const string s_centerYPath = "(UIElement.RenderTransform).(CompositeTransform.CenterY)";
        private const string s_scaleXPath = "(UIElement.RenderTransform).(CompositeTransform.ScaleX)";
        private const string s_scaleYPath = "(UIElement.RenderTransform).(CompositeTransform.ScaleY)";
        private const string s_translateXPath = "(UIElement.RenderTransform).(CompositeTransform.TranslateX)";
        private const string s_translateYPath = "(UIElement.RenderTransform).(CompositeTransform.TranslateY)";
    }
}