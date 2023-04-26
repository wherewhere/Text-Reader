using System;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;

namespace TextReader.Common
{
    public class CustomResource : ResourceDictionary
    {
        public CustomResource() => AddResource();

        private void AddResource()
        {
            if (ApiInformation.IsMethodPresent("Windows.UI.Composition.Compositor", "TryCreateBlurredWallpaperBackdropBrush"))
            {
                AddResourceDictionary("ms-appx:///Styles/Brushes/ThemeResources.21H2.xaml");
            }
            else if (ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.AcrylicBrush"))
            {
                AddResourceDictionary("ms-appx:///Styles/Brushes/ThemeResources.RS3.xaml");
            }
            else if (ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.XamlCompositionBrushBase"))
            {
                AddResourceDictionary("ms-appx:///Styles/Brushes/ThemeResources.RS2.xaml");
            }
            else
            {
                AddResourceDictionary("ms-appx:///Styles/Brushes/ThemeResources.RS1.xaml");
            }

            void AddResourceDictionary(string Source)
            {
                MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(Source) });
            }
        }
    }
}
