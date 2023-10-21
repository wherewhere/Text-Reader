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
        #region ContextFlyout

        /// <summary>
        /// Gets the flyout associated with this element.
        /// </summary>
        /// <param name="element">The flyout associated with this element.</param>
        /// <returns>The flyout associated with this element, if any; otherwise, <see langword="null"/>. The default is <see langword="null"/>.</returns>
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
                element.ContextFlyout = GetContextFlyout(element);
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
            if (e?.Handled == true) { return; }
            if (e.Key == VirtualKey.Menu)
            {
                FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
                if (e != null) { e.Handled = true; }
            }
        }

        private static void OnHolding(object sender, HoldingRoutedEventArgs e)
        {
            if (e?.Handled == true || !(sender is FrameworkElement element)) { return; }
            FlyoutBase flyout = FlyoutBase.GetAttachedFlyout(element);
            if (flyout is MenuFlyout menu)
            {
                menu.ShowAt(element, e.GetPosition(element));
            }
            else
            {
                FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
            }
            if (e != null) { e.Handled = true; }
        }

        private static void OnRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (e?.Handled == true || !(sender is FrameworkElement element)) { return; }
            FlyoutBase flyout = FlyoutBase.GetAttachedFlyout(element);
            if (flyout is MenuFlyout menu)
            {
                menu.ShowAt(element, e.GetPosition(element));
            }
            else
            {
                FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
            }
            if (e != null) { e.Handled = true; }
        }

        #endregion

        #region ShouldConstrainToRootBounds

        /// <summary>
        /// Identifies the ShouldConstrainToRootBounds dependency property.
        /// </summary>
        public static readonly DependencyProperty ShouldConstrainToRootBoundsProperty =
            DependencyProperty.RegisterAttached(
                "ShouldConstrainToRootBounds",
                typeof(bool),
                typeof(UIElementHelper),
                new PropertyMetadata(true, OnShouldConstrainToRootBoundsChanged));

        /// <summary>
        /// Gets a value that indicates whether the flyout should be shown within the bounds of the XAML root.
        /// </summary>
        /// <param name="control">The element from which to read the property value.</param>
        /// <returns>A value that indicates whether the flyout should be shown within the bounds of the XAML root.</returns>
        public static bool GetShouldConstrainToRootBounds(FlyoutBase control)
        {
            return (bool)control.GetValue(ShouldConstrainToRootBoundsProperty);
        }

        /// <summary>
        /// Sets a value that indicates whether the flyout should be shown within the bounds of the XAML root.
        /// </summary>
        /// <param name="control">The element on which to set the attached property.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetShouldConstrainToRootBounds(FlyoutBase control, bool value)
        {
            control.SetValue(ShouldConstrainToRootBoundsProperty, value);
        }

        private static void OnShouldConstrainToRootBoundsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlyoutBase flyout)
            {
                if (e.NewValue is bool ShouldConstrainToRootBounds && ApiInformation.IsPropertyPresent("Windows.UI.Xaml.Controls.Primitives.FlyoutBase", "ShouldConstrainToRootBounds"))
                {
                    flyout.ShouldConstrainToRootBounds = ShouldConstrainToRootBounds;
                }
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
            if (d is MenuFlyoutItem item)
            {
                if (e.NewValue is IconElement IconElement && ApiInformation.IsPropertyPresent("Windows.UI.Xaml.Controls.MenuFlyoutItem", "Icon"))
                {
                    item.Icon = IconElement;
                }
            }
            else if (d is MenuFlyoutSubItem subitem)
            {
                if (e.NewValue is IconElement IconElement && ApiInformation.IsPropertyPresent("Windows.UI.Xaml.Controls.MenuFlyoutSubItem", "Icon"))
                {
                    subitem.Icon = IconElement;
                }
            }
            else if (d is ToggleMenuFlyoutItem toggle)
            {
                if (e.NewValue is IconElement IconElement && ApiInformation.IsPropertyPresent("Windows.UI.Xaml.Controls.ToggleMenuFlyoutItem", "Icon"))
                {
                    toggle.Icon = IconElement;
                }
            }
        }

        #endregion
    }
}
