﻿using System.Collections.Generic;
using TextReader.Extensions;
using TextReader.Helpers;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;

namespace TextReader.Controls
{
    public class DisplayRegionHelper
    {
        private static DisplayRegionHelper Instance;

        // TODO: Remove once ApplicationViewMode::Spanning is available in the SDK
        private const int c_ApplicationViewModeSpanning = 2;

        public static DisplayRegionHelper GetDisplayRegionHelperInstance()
        {
            if (Instance == null)
            {
                Instance = new DisplayRegionHelper();
            }
            return Instance;
        }

        public static DisplayRegionHelperInfo GetRegionInfo()
        {
            DisplayRegionHelper instance = GetDisplayRegionHelperInstance();

            DisplayRegionHelperInfo info = new DisplayRegionHelperInfo
            {
                Mode = TwoPaneViewMode.SinglePane
            };

            if (instance.m_simulateDisplayRegions)
            {
                // Create fake rectangles for test app
                if (instance.m_simulateMode == TwoPaneViewMode.Wide)
                {
                    info.Regions[0] = m_simulateWide0;
                    info.Regions[1] = m_simulateWide1;
                    info.Mode = TwoPaneViewMode.Wide;
                }
                else if (instance.m_simulateMode == TwoPaneViewMode.Tall)
                {
                    info.Regions[0] = m_simulateTall0;
                    info.Regions[1] = m_simulateTall1;
                    info.Mode = TwoPaneViewMode.Tall;
                }
                else
                {
                    info.Regions[0] = m_simulateWide0;
                }
            }
            else if (ApiInformation.IsPropertyPresent("Windows.UI.ViewManagement.ApplicationView", "ViewMode"))
            {
                // ApplicationView::GetForCurrentView throws on failure; in that case we just won't do anything.
                ApplicationView view = null;
                try
                {
                    view = ApplicationView.GetForCurrentView();
                }
                catch { }

                if (view != null && view.ViewMode == (ApplicationViewMode)c_ApplicationViewModeSpanning)
                {
                    if (ApiInformation.IsPropertyPresent("Windows.UI.ViewManagement.ApplicationView", "GetDisplayRegions"))
                    {
                        IReadOnlyList<DisplayRegion> rects = view.GetDisplayRegions();

                        if (rects.Count == 2)
                        {
                            info.Regions[0] = rects[0].WorkAreaSize.ToRect(rects[0].WorkAreaOffset);
                            info.Regions[1] = rects[1].WorkAreaSize.ToRect(rects[1].WorkAreaOffset);

                            // Determine orientation. If neither of these are true, default to doing nothing.
                            if (info.Regions[0].X < info.Regions[1].X && info.Regions[0].Y == info.Regions[1].Y)
                            {
                                // Double portrait
                                info.Mode = TwoPaneViewMode.Wide;
                            }
                            else if (info.Regions[0].X == info.Regions[1].X && info.Regions[0].Y < info.Regions[1].Y)
                            {
                                // Double landscape
                                info.Mode = TwoPaneViewMode.Tall;
                            }
                        }
                    }
                }
            }

            return info;
        }

        /* static */
        public static UIElement WindowElement(UIElement element)
        {
            DisplayRegionHelper instance = GetDisplayRegionHelperInstance();

            if (instance.m_simulateDisplayRegions)
            {
                // Instead of returning the actual window, find the SimulatedWindow element
                UIElement window = null;
                UIElement xamlRoot = element.GetXAMLRoot();

                if (xamlRoot is FrameworkElement fe)
                {
                    window = fe.FindDescendant("SimulatedWindow");
                }

                return window ?? xamlRoot;
            }
            else
            {
                return element.GetXAMLRoot();
            }
        }

        /* static */
        public static Rect WindowRect(UIElement element)
        {
            DisplayRegionHelper instance = GetDisplayRegionHelperInstance();

            if (instance.m_simulateDisplayRegions)
            {
                // Return the bounds of the simulated window
                FrameworkElement window = WindowElement(element) as FrameworkElement;
                Rect rc = new Rect(
                    0, 0,
                    window.ActualWidth,
                    window.ActualHeight);
                return rc;
            }
            else
            {
                return Window.Current.Bounds;
            }
        }

        /* static */
        public static bool SimulateDisplayRegions
        {
            get
            {
                DisplayRegionHelper instance = GetDisplayRegionHelperInstance();
                return instance.m_simulateDisplayRegions;
            }
            set
            {
                DisplayRegionHelper instance = GetDisplayRegionHelperInstance();
                instance.m_simulateDisplayRegions = value;
            }
        }

        /* static */
        public static TwoPaneViewMode SimulateMode
        {
            get
            {
                DisplayRegionHelper instance = GetDisplayRegionHelperInstance();
                return instance.m_simulateMode;
            }
            set
            {
                DisplayRegionHelper instance = GetDisplayRegionHelperInstance();
                instance.m_simulateMode = value;
            }
        }

        private bool m_simulateDisplayRegions = false;
        private TwoPaneViewMode m_simulateMode = TwoPaneViewMode.SinglePane;
        private static Rect m_simulateWide0 = new Rect(0, 0, 300, 400);
        private static Rect m_simulateWide1 = new Rect(312, 0, 300, 400);
        private static Rect m_simulateTall0 = new Rect(0, 0, 400, 300);
        private static Rect m_simulateTall1 = new Rect(0, 312, 400, 300);
    }
}
