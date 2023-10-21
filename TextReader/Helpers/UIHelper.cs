using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TextReader.Pages;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using TextReader.Common;
using TextReader.Extensions;

namespace TextReader.Helpers
{
    public static partial class UIHelper
    {
        public const int Duration = 3000;
        public static bool IsShowingProgressBar, IsShowingMessage;
        public static Queue<string> MessageQueue { get; } = new Queue<string>();
        public static bool HasTitleBar => !CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar;
        public static bool HasStatusBar => ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar");

        public static async Task<MainPage> GetMainPageAsync() =>
            Window.Current is Window window ? await window.GetMainPageAsync().ConfigureAwait(false) : null;

        public static async Task<MainPage> GetMainPageAsync(this Window window)
        {
            if (window.Dispatcher?.HasThreadAccess == false)
            {
                await window.Dispatcher.ResumeForegroundAsync();
            }
            return window.Content.FindDescendant<MainPage>();
        }

        public static async Task<MainPage> GetMainPageAsync(this CoreDispatcher dispatcher)
        {
            if (WindowHelper.ActiveWindows.TryGetValue(dispatcher, out Window window))
            {
                if (window.Dispatcher?.HasThreadAccess == false)
                {
                    await window.Dispatcher.ResumeForegroundAsync();
                }
                return window.Content.FindDescendant<MainPage>();
            }
            else
            {
                return null;
            }
        }

        public static async Task<MainPage> GetMainPageAsync(this DependencyObject element)
        {
            if (element is MainPage mainPage)
            {
                return mainPage;
            }

            if (element.Dispatcher?.HasThreadAccess == false)
            {
                await element.Dispatcher.ResumeForegroundAsync();
            }

            if (WindowHelper.IsXamlRootSupported
                && element is UIElement uiElement
                && uiElement.XamlRoot != null)
            {
                return uiElement.XamlRoot.Content.FindDescendant<MainPage>();
            }

            return element.FindAscendant<MainPage>()
                ?? await element.Dispatcher.GetMainPageAsync().ConfigureAwait(false);
        }

        public static Task ShowProgressBarAsync(this CoreDispatcher dispatcher) => dispatcher.GetMainPageAsync().ContinueWith(x => ShowProgressBarAsync(x.Result)).Unwrap();

        public static Task ShowProgressBarAsync(this DependencyObject element) => element.GetMainPageAsync().ContinueWith(x => ShowProgressBarAsync(x.Result)).Unwrap();

        public static async Task ShowProgressBarAsync(MainPage mainPage)
        {
            IsShowingProgressBar = true;
            if (mainPage.Dispatcher?.HasThreadAccess == false)
            {
                await mainPage.Dispatcher.ResumeForegroundAsync();
            }
            if (HasStatusBar)
            {
                await mainPage?.HideProgressBarAsync();
                StatusBar.GetForCurrentView().ProgressIndicator.ProgressValue = null;
                await StatusBar.GetForCurrentView().ProgressIndicator.ShowAsync();
            }
            else
            {
                await mainPage?.ShowProgressBarAsync();
            }
        }

        public static Task HideProgressBarAsync(this CoreDispatcher dispatcher) => dispatcher.GetMainPageAsync().ContinueWith(x => HideProgressBarAsync(x.Result)).Unwrap();

        public static Task HideProgressBarAsync(this DependencyObject element) => element.GetMainPageAsync().ContinueWith(x => HideProgressBarAsync(x.Result)).Unwrap();

        public static async Task HideProgressBarAsync(MainPage mainPage)
        {
            IsShowingProgressBar = false;
            if (mainPage.Dispatcher?.HasThreadAccess == false)
            {
                await mainPage.Dispatcher.ResumeForegroundAsync();
            }
            if (HasStatusBar)
            {
                await StatusBar.GetForCurrentView().ProgressIndicator.HideAsync();
            }
            await mainPage?.HideProgressBarAsync();
        }

        public static Task ShowMessageAsync(string message) => GetMainPageAsync().ContinueWith(x => ShowMessageAsync(x.Result, message)).Unwrap();

        public static Task ShowMessageAsync(this CoreDispatcher dispatcher, string message) => dispatcher.GetMainPageAsync().ContinueWith(x => ShowMessageAsync(x.Result, message)).Unwrap();

        public static Task ShowMessageAsync(this DependencyObject element, string message) => element.GetMainPageAsync().ContinueWith(x => ShowMessageAsync(x.Result, message)).Unwrap();

        public static async Task ShowMessageAsync(MainPage mainPage, string message)
        {
            if (mainPage == null) { return; }
            MessageQueue.Enqueue(message);
            if (!IsShowingMessage)
            {
                IsShowingMessage = true;
                if (mainPage.Dispatcher?.HasThreadAccess == false)
                {
                    await mainPage.Dispatcher.ResumeForegroundAsync();
                }
                if (HasStatusBar)
                {
                    StatusBar statusBar = StatusBar.GetForCurrentView();
                    while (MessageQueue.Count > 0)
                    {
                        message = MessageQueue.Dequeue();
                        if (!string.IsNullOrEmpty(message))
                        {
                            statusBar.ProgressIndicator.Text = $"[{MessageQueue.Count + 1}] {message.Replace("\n", " ")}";
                            statusBar.ProgressIndicator.ProgressValue = IsShowingProgressBar ? null : (double?)0;
                            await statusBar.ProgressIndicator.ShowAsync();
                            await Task.Delay(Duration);
                        }
                    }
                    if (!IsShowingProgressBar) { await statusBar.ProgressIndicator.HideAsync(); }
                    statusBar.ProgressIndicator.Text = string.Empty;
                }
                else if (mainPage != null)
                {
                    while (MessageQueue.Count > 0)
                    {
                        message = MessageQueue.Dequeue();
                        if (!string.IsNullOrEmpty(message))
                        {
                            string messages = $"[{MessageQueue.Count + 1}] {message.Replace("\n", " ")}";
                            await mainPage.ShowMessageAsync(messages);
                            await Task.Delay(Duration);
                        }
                    }
                    await mainPage.ShowMessageAsync();
                }
                IsShowingMessage = false;
            }
        }

        public static string ExceptionToMessage(this Exception ex)
        {
            StringBuilder builder = new StringBuilder().AppendLine();
            if (!string.IsNullOrWhiteSpace(ex.Message)) { _ = builder.AppendLine($"Message: {ex.Message}"); }
            _ = builder.AppendLine($"HResult: {ex.HResult} (0x{Convert.ToString(ex.HResult, 16).ToUpperInvariant()})");
            if (!string.IsNullOrWhiteSpace(ex.StackTrace)) { _ = builder.AppendLine(ex.StackTrace); }
            if (!string.IsNullOrWhiteSpace(ex.HelpLink)) { _ = builder.AppendLine($"HelperLink: {ex.HelpLink}"); }
            return builder.ToString();
        }
    }
}
