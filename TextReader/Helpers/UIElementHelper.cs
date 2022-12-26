using Windows.Foundation.Metadata;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace TextReader.Helpers
{
    public static class UIElementHelper
    {
        #region CornerRadius

        /// <summary>
        /// Gets the radius for the corners of the control's border.
        /// </summary>
        /// <param name="control">The element from which to read the property value.</param>
        /// <returns>
        /// The degree to which the corners are rounded, expressed as values of the CornerRadius
        /// structure.
        /// </returns>
        public static CornerRadius GetCornerRadius(Control control)
        {
            return (CornerRadius)control.GetValue(CornerRadiusProperty);
        }

        /// <summary>
        /// Sets the radius for the corners of the control's border.
        /// </summary>
        /// <param name="control">The element on which to set the attached property.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetCornerRadius(Control control, CornerRadius value)
        {
            control.SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// Identifies the CornerRadius dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.RegisterAttached(
                "CornerRadius",
                typeof(CornerRadius),
                typeof(UIElementHelper),
                new PropertyMetadata(null, OnCornerRadiusChanged));

        private static void OnCornerRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Control element = (Control)d;
            if (e.NewValue is CornerRadius CornerRadius && ApiInformation.IsPropertyPresent("Windows.UI.Xaml.Controls.Control", "CornerRadius"))
            {
                element.CornerRadius = CornerRadius;
            }
        }

        #endregion

        #region ContextFlyout

        /// <summary>
        /// Gets the flyout associated with this element.
        /// </summary>
        /// <param name="element">The flyout associated with this element.</param>
        /// <returns>
        /// The flyout associated with this element, if any; otherwise, <see langword="null"/>. The default is <see langword="null"/>.
        /// </returns>
        public static FlyoutBase GetContextFlyout(UIElement element)
        {
            return (FlyoutBase)element.GetValue(ContextFlyoutProperty);
        }

        /// <summary>
        /// Sets the flyout associated with this element.
        /// </summary>
        /// <param name="element">The element on which to set the attached property.</param>
        /// <param name="value">The flyout associated with this element.</param>
        public static void SetContextFlyout(UIElement element, FlyoutBase value)
        {
            element.SetValue(ContextFlyoutProperty, value);
        }

        /// <summary>
        /// Identifies the ContextFlyout dependency property.
        /// </summary>
        public static readonly DependencyProperty ContextFlyoutProperty =
            DependencyProperty.RegisterAttached(
                "ContextFlyout",
                typeof(FlyoutBase),
                typeof(UIElementHelper),
                new PropertyMetadata(null, OnContextFlyoutChanged));

        private static void OnContextFlyoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement element = (UIElement)d;
            if (ApiInformation.IsPropertyPresent("Windows.UI.Xaml.UIElement", "ContextFlyout"))
            {
                element.ContextFlyout = e.NewValue as FlyoutBase;
            }
            else if (element is FrameworkElement frameworkElement)
            {
                FlyoutBase.SetAttachedFlyout(frameworkElement, e.NewValue as FlyoutBase);

                element.KeyDown -= OnKeyDown;
                element.Holding -= OnHolding;
                element.RightTapped -= OnRightTapped;

                if (element != null)
                {
                    element.KeyDown += OnKeyDown;
                    element.Holding += OnHolding;
                    element.RightTapped += OnRightTapped;
                }
            }
        }

        private static void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Menu)
            {
                FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
            }
        }

        private static void OnHolding(object sender, HoldingRoutedEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            FlyoutBase flyout = FlyoutBase.GetAttachedFlyout(element);
            if (flyout is MenuFlyout menu)
            {
                menu.ShowAt(element, e.GetPosition(element));
            }
            else
            {
                FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
            }
        }

        private static void OnRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            FlyoutBase flyout = FlyoutBase.GetAttachedFlyout(element);
            if (flyout is MenuFlyout menu)
            {
                menu.ShowAt(element, e.GetPosition(element));
            }
            else
            {
                FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
            }
        }

        #endregion

        #region Icon

        /// <summary>
        /// Gets the graphic content of the menu flyout item.
        /// </summary>
        /// <param name="control">The element from which to read the property value.</param>
        /// <returns>The graphic content of the menu flyout item.</returns>
        public static IconElement GetIcon(MenuFlyoutItem control)
        {
            return (IconElement)control.GetValue(IconProperty);
        }

        /// <summary>
        /// Sets the graphic content of the menu flyout item.
        /// </summary>
        /// <param name="control">The element on which to set the attached property.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetIcon(MenuFlyoutItem control, IconElement value)
        {
            control.SetValue(IconProperty, value);
        }

        /// <summary>
        /// Identifies the Icon dependency property.
        /// </summary>
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.RegisterAttached(
                "Icon",
                typeof(IconElement),
                typeof(UIElementHelper),
                new PropertyMetadata(null, OnIconChanged));

        private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MenuFlyoutItem element = (MenuFlyoutItem)d;
            if (e.NewValue is IconElement IconElement && ApiInformation.IsPropertyPresent("Windows.UI.Xaml.Controls.MenuFlyoutItem", "Icon"))
            {
                element.Icon = IconElement;
            }
        }

        #endregion
    }
}
