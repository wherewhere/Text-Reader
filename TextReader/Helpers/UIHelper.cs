using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TextReader.Pages;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace TextReader.Helpers
{
    internal static partial class UIHelper
    {
        public static IHaveTitleBar AppTitle;
        public static bool IsShowingProgressBar, IsShowingMessage;
        public static bool HasTitleBar => !CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar;
        public static bool HasStatusBar => ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar");

        public static double TitleBarHeight = 32;
        public static Thickness StackPanelMargin => new Thickness(0, HasStatusBar || HasTitleBar ? 0 : TitleBarHeight, 0, 0);

        private static readonly List<string> MessageList = new List<string>();

        private static CoreDispatcher shellDispatcher;
        public static CoreDispatcher ShellDispatcher
        {
            get => shellDispatcher;
            set
            {
                if (shellDispatcher == null)
                {
                    shellDispatcher = value;
                }
            }
        }

        public static async void ShowProgressBar()
        {
            IsShowingProgressBar = true;
            if (HasStatusBar)
            {
                StatusBar.GetForCurrentView().ProgressIndicator.ProgressValue = null;
                await StatusBar.GetForCurrentView().ProgressIndicator.ShowAsync();
            }
            else
            {
                AppTitle?.ShowProgressBar();
            }
        }

        public static async void ShowProgressBar(double value = 0)
        {
            IsShowingProgressBar = true;
            if (HasStatusBar)
            {
                StatusBar.GetForCurrentView().ProgressIndicator.ProgressValue = value * 0.01;
                await StatusBar.GetForCurrentView().ProgressIndicator.ShowAsync();
            }
            else
            {
                AppTitle?.ShowProgressBar(value);
            }
        }

        public static async void PausedProgressBar()
        {
            IsShowingProgressBar = true;
            if (HasStatusBar)
            {
                if (IsShowingMessage)
                {
                    StatusBar.GetForCurrentView().ProgressIndicator.ProgressValue = 0;
                }
                else
                {
                    await StatusBar.GetForCurrentView().ProgressIndicator.HideAsync();
                }
            }
            AppTitle?.PausedProgressBar();
        }

        public static async void ErrorProgressBar()
        {
            IsShowingProgressBar = true;
            if (HasStatusBar)
            {
                if (IsShowingMessage)
                {
                    StatusBar.GetForCurrentView().ProgressIndicator.ProgressValue = 0;
                }
                else
                {
                    await StatusBar.GetForCurrentView().ProgressIndicator.HideAsync();
                }
            }
            AppTitle?.ErrorProgressBar();
        }

        public static async void HideProgressBar()
        {
            IsShowingProgressBar = false;
            if (HasStatusBar)
            {
                if (IsShowingMessage)
                {
                    StatusBar.GetForCurrentView().ProgressIndicator.ProgressValue = 0;
                }
                else
                {
                    await StatusBar.GetForCurrentView().ProgressIndicator.HideAsync();
                }
            }
            AppTitle?.HideProgressBar();
        }

        public static async void ShowMessage(string message)
        {
            MessageList.Add(message);
            if (!IsShowingMessage)
            {
                IsShowingMessage = true;
                while (MessageList.Count > 0)
                {
                    if (HasStatusBar)
                    {
                        StatusBar statusBar = StatusBar.GetForCurrentView();
                        if (!string.IsNullOrEmpty(MessageList[0]))
                        {
                            statusBar.ProgressIndicator.Text = $"[{MessageList.Count}] {MessageList[0].Replace("\n", " ")}";
                            statusBar.ProgressIndicator.ProgressValue = IsShowingProgressBar ? null : (double?)0;
                            await statusBar.ProgressIndicator.ShowAsync();
                            await Task.Delay(3000);
                        }
                        MessageList.RemoveAt(0);
                        if (MessageList.Count == 0 && !IsShowingProgressBar)
                        {
                            IsShowingMessage = false;
                            await statusBar.ProgressIndicator.HideAsync();
                        }
                        statusBar.ProgressIndicator.Text = string.Empty;
                    }
                    else if (AppTitle != null)
                    {
                        if (!string.IsNullOrEmpty(MessageList[0]))
                        {
                            string messages = $"[{MessageList.Count}] {MessageList[0].Replace("\n", " ")}";
                            AppTitle.ShowMessage(messages);
                            await Task.Delay(3000);
                        }
                        MessageList.RemoveAt(0);
                        if (MessageList.Count == 0)
                        {
                            IsShowingMessage = false;
                            AppTitle.ShowMessage();
                        }
                    }
                }
                IsShowingMessage = false;
            }
        }
    }
}
