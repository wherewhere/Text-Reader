using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TextReader.Common;
using TextReader.Pages;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Metadata;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace TextReader.Helpers
{
    public static partial class UIHelper
    {
        public const int Duration = 3000;
        public static bool IsShowingProgressBar, IsShowingMessage;
        public static Queue<string> MessageQueue { get; } = new Queue<string>();
        public static bool HasTitleBar => !CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar;
        public static bool HasStatusBar => ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar");

        public static async Task ShowProgressBarAsync(MainPage mainPage)
        {
            IsShowingProgressBar = true;
            await mainPage.Dispatcher.ResumeForegroundAsync();
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

        public static async Task HideProgressBarAsync(MainPage mainPage)
        {
            IsShowingProgressBar = false;
            await mainPage.Dispatcher.ResumeForegroundAsync();
            if (HasStatusBar)
            {
                await StatusBar.GetForCurrentView().ProgressIndicator.HideAsync();
            }
            await mainPage?.HideProgressBarAsync();
        }

        public static async Task ShowMessageAsync(MainPage mainPage, string message)
        {
            if (mainPage == null) { return; }
            MessageQueue.Enqueue(message);
            if (!IsShowingMessage)
            {
                IsShowingMessage = true;
                await mainPage.Dispatcher.ResumeForegroundAsync();
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

        public static async Task SetValueAsync<T>(this DependencyObject element, DependencyProperty dp, T value)
        {
            await element.Dispatcher.ResumeForegroundAsync();
            element.SetValue(dp, value);
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
