﻿using System;
using TextReader.Helpers;
using Windows.ApplicationModel.Resources;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.System;
using Windows.UI.ApplicationSettings;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace TextReader.Common
{
    public static class SettingsPaneRegister
    {
        public static bool IsSettingsPaneSupported { get; } = ApiInformation.IsTypePresent("Windows.UI.ApplicationSettings.SettingsPane");

        public static void Register(Window window)
        {
            try
            {
                if (IsSettingsPaneSupported)
                {
                    SettingsPane searchPane = SettingsPane.GetForCurrentView();
                    searchPane.CommandsRequested -= OnCommandsRequested;
                    searchPane.CommandsRequested += OnCommandsRequested;
                    window.Dispatcher.AcceleratorKeyActivated -= Dispatcher_AcceleratorKeyActivated;
                    window.Dispatcher.AcceleratorKeyActivated += Dispatcher_AcceleratorKeyActivated;
                }
            }
            catch (Exception ex)
            {
                SettingsHelper.LogManager.GetLogger(nameof(SettingsPaneRegister)).Error(ex.ExceptionToMessage(), ex);
            }
        }

        public static void Unregister(Window window)
        {
            try
            {
                if (IsSettingsPaneSupported)
                {
                    SettingsPane.GetForCurrentView().CommandsRequested -= OnCommandsRequested;
                    window.Dispatcher.AcceleratorKeyActivated -= Dispatcher_AcceleratorKeyActivated;
                }
            }
            catch (Exception ex)
            {
                SettingsHelper.LogManager.GetLogger(nameof(SettingsPaneRegister)).Error(ex.ExceptionToMessage(), ex);
            }
        }

        private static void OnCommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            ResourceLoader loader = ResourceLoader.GetForViewIndependentUse("SettingsPane");
            args.Request.ApplicationCommands.Add(
                new SettingsCommand(
                    "Feedback",
                    loader.GetString("Feedback"),
                    handler => _ = Launcher.LaunchUriAsync(new Uri("https://github.com/wherewhere/Text-Reader/issues"))));
            args.Request.ApplicationCommands.Add(
                new SettingsCommand(
                    "LogFolder",
                    loader.GetString("LogFolder"),
                    async handler => _ = Launcher.LaunchFolderAsync(await ApplicationData.Current.LocalFolder.CreateFolderAsync("MetroLogs", CreationCollisionOption.OpenIfExists))));
            args.Request.ApplicationCommands.Add(
                new SettingsCommand(
                    "Repository",
                    loader.GetString("Repository"),
                    handler => _ = Launcher.LaunchUriAsync(new Uri("https://github.com/wherewhere/Text-Reader"))));
            args.Request.ApplicationCommands.Add(
                new SettingsCommand(
                    "StoreReview",
                    loader.GetString("StoreReview"),
                    handler => _ = Launcher.LaunchUriAsync(new Uri("ms-windows-store://review/?ProductId=9P8NKQW6NCNC"))));
        }

        private static void Dispatcher_AcceleratorKeyActivated(CoreDispatcher sender, AcceleratorKeyEventArgs args)
        {
            if (args.EventType.HasFlag(CoreAcceleratorKeyEventType.KeyDown) || args.EventType.HasFlag(CoreAcceleratorKeyEventType.SystemKeyUp))
            {
                CoreWindow window = CoreWindow.GetForCurrentThread();
                CoreVirtualKeyStates ctrl = window.GetKeyState(VirtualKey.Control);
                if (ctrl.HasFlag(CoreVirtualKeyStates.Down))
                {
                    CoreVirtualKeyStates shift = window.GetKeyState(VirtualKey.Shift);
                    if (shift.HasFlag(CoreVirtualKeyStates.Down))
                    {
                        switch (args.VirtualKey)
                        {
                            case VirtualKey.X when IsSettingsPaneSupported:
                                SettingsPane.Show();
                                args.Handled = true;
                                break;
                        }
                    }
                }
            }
        }
    }
}
