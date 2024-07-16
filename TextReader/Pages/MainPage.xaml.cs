using System;
using System.Threading.Tasks;
using TextReader.Common;
using TextReader.Controls;
using TextReader.Helpers;
using TextReader.Helpers.Converters;
using TextReader.ViewModels;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Foundation.Metadata;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.System;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using TwoPaneViewPriority = TextReader.Controls.TwoPaneViewPriority;
using TwoPaneViewTallModeConfiguration = TextReader.Controls.TwoPaneViewTallModeConfiguration;
using TwoPaneViewWideModeConfiguration = TextReader.Controls.TwoPaneViewWideModeConfiguration;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace TextReader.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private bool isClipboardChanged = true;
        private readonly MainViewModel Provider;

        public Frame MainFrame => Frame;

        public MainPage()
        {
            InitializeComponent();
            AppTitle.Text = ResourceLoader.GetForViewIndependentUse().GetString("AppName") ?? "文字识别";
            if (!(AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop"))
            { UpdateTitleBarVisible(false); }
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 14))
            { CommandBar.DefaultLabelPosition = CommandBarDefaultLabelPosition.Right; }
            Provider = new MainViewModel(this);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Window.Current.SetTitleBar(CustomTitleBar);
            CoreApplicationViewTitleBar TitleBar = CoreApplication.GetCurrentView().TitleBar;
            Dispatcher.AcceleratorKeyActivated += Dispatcher_AcceleratorKeyActivated;
            TitleBar.LayoutMetricsChanged += TitleBar_LayoutMetricsChanged;
            TitleBar.IsVisibleChanged += TitleBar_IsVisibleChanged;
            Clipboard.ContentChanged += Clipboard_ContentChanged;
            if (e.Parameter is IActivatedEventArgs args)
            { OpenActivatedEventArgs(args); }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            Window.Current.SetTitleBar(null);
            CoreApplicationViewTitleBar TitleBar = CoreApplication.GetCurrentView().TitleBar;
            Dispatcher.AcceleratorKeyActivated -= Dispatcher_AcceleratorKeyActivated;
            TitleBar.LayoutMetricsChanged -= TitleBar_LayoutMetricsChanged;
            TitleBar.IsVisibleChanged -= TitleBar_IsVisibleChanged;
            Clipboard.ContentChanged -= Clipboard_ContentChanged;
        }

        public void OpenActivatedEventArgs(IActivatedEventArgs args)
        {
            switch (args?.Kind)
            {
                case ActivationKind.File when args is IFileActivatedEventArgs fileEventArgs:
                    _ = Provider.DropFileAsync(fileEventArgs.Files);
                    break;
                case ActivationKind.ShareTarget when args is IShareTargetActivatedEventArgs shareTargetEventArgs:
                    _ = Provider.DropFileAsync(shareTargetEventArgs.ShareOperation.Data);
                    break;
                default:
                    break;
            }
        }

        private void ComboBox_Loaded(object sender, RoutedEventArgs e) => _ = Provider.SetIndexAsync(Language);

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement element)) { return; }
            switch (element.Tag?.ToString())
            {
                case "Save":
                    _ = Provider.SaveImageAsync();
                    break;
                case "Paste":
                    _ = Provider.DropFileAsync(Clipboard.GetContent());
                    break;
                case "Finnish":
                    _ = ClipFinnish();
                    break;
                case "Cancel":
                    ClipCancel();
                    break;
                case "ClipImage":
                    ClipImage();
                    break;
                case "PickImage":
                    _ = Provider?.PickImage();
                    break;
                case "TakePhoto":
                    _ = Provider?.TakePhoto();
                    break;
                case "Refresh":
                    _ = Provider.ReadTextAsync(Provider.SoftwareImage);
                    break;
                case "TwoPanel":
                    Provider.IsSinglePane = false;
                    Provider.PanePriority = TwoPaneViewPriority.Pane1;
                    break;
                case "NewWindow":
                    _ = WindowHelper.CreateWindowAsync(window =>
                    {
                        CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
                        Frame _frame = new Frame();
                        window.Content = _frame;
                        ThemeHelper.Initialize(window);
                        _ = _frame.Navigate(typeof(MainPage));
                    });
                    break;
                case "ImageOnly":
                    Provider.IsSinglePane = true;
                    Provider.PanePriority = TwoPaneViewPriority.Pane1;
                    break;
                case "ResultOnly":
                    Provider.IsSinglePane = true;
                    Provider.PanePriority = TwoPaneViewPriority.Pane2;
                    break;
                default:
                    break;
            }
        }

        public async Task ClipFinnish()
        {
            using (InMemoryRandomAccessStream random = new InMemoryRandomAccessStream())
            {
                //Saves the cropped image to a stream.
                await ImageCropper.SaveAsync(random, BitmapFileFormat.Png, false);
                BitmapDecoder ImageDecoder = await BitmapDecoder.CreateAsync(random);
                Provider.SoftwareImage = await ImageDecoder.GetSoftwareBitmapAsync();
            }
            Provider.ShowCropper = false;
        }

        public void ClipCancel() => Provider.ShowCropper = false;

        public void ClipImage()
        {
            if (Provider.CropperImage != null)
            {
                Provider.ShowCropper = true;
            }
        }

        private async void Grid_DragOver(object sender, DragEventArgs e)
        {
            DragOperationDeferral deferral = e.GetDeferral();
            e.AcceptedOperation = await Provider.CheckDataAsync(e.DataView) ? DataPackageOperation.Copy : DataPackageOperation.None;
            e.Handled = true;
            deferral.Complete();
        }

        private async void Grid_Drop(object sender, DragEventArgs e)
        {
            DragOperationDeferral deferral = e.GetDeferral();
            await Provider.DropFileAsync(e.DataView);
            e.Handled = true;
            deferral.Complete();
        }

        private void UpdateTitleBarLayout(CoreApplicationViewTitleBar TitleBar)
        {
            CustomTitleBar.Opacity = TitleBar.SystemOverlayLeftInset > 48 ? 0 : 1;
            LeftPaddingColumn.Width = new GridLength(TitleBar.SystemOverlayLeftInset);
            RightPaddingColumn.Width = new GridLength(TitleBar.SystemOverlayRightInset);
        }

        private void UpdateTitleBarVisible(bool IsVisible)
        {
            TopPaddingRow.Height = IsVisible && !UIHelper.HasStatusBar && !UIHelper.HasTitleBar ? new GridLength(32) : new GridLength(0);
            CustomTitleBar.Visibility = IsVisible && !UIHelper.HasStatusBar && !UIHelper.HasTitleBar ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Dispatcher_AcceleratorKeyActivated(CoreDispatcher sender, AcceleratorKeyEventArgs args)
        {
            if (args.EventType.HasFlag(CoreAcceleratorKeyEventType.KeyDown) || args.EventType.HasFlag(CoreAcceleratorKeyEventType.SystemKeyUp))
            {
                CoreVirtualKeyStates ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control);
                if (ctrl.HasFlag(CoreVirtualKeyStates.Down))
                {
                    switch (args.VirtualKey)
                    {
                        case VirtualKey.V when Provider.IsPasteEnabled:
                            _ = Provider.DropFileAsync(Clipboard.GetContent());
                            args.Handled = true;
                            break;
                    }
                }
            }
        }

        private void Page_GotFocus(object sender, RoutedEventArgs e)
        {
            if (isClipboardChanged)
            {
                _ = Provider.UpdatePasteEnabledAsync();
            }
            isClipboardChanged = false;
        }

        private void Clipboard_ContentChanged(object sender, object e)
        {
            if (FocusState == FocusState.Unfocused)
            {
                isClipboardChanged = true;
            }
            else
            {
                _ = Provider.UpdatePasteEnabledAsync();
                isClipboardChanged = false;
            }
        }

        private void Image_DragStarting(UIElement sender, DragStartingEventArgs args) => args.DragUI.SetContentFromSoftwareBitmap(Provider.SoftwareImage);

        private void TitleBar_IsVisibleChanged(CoreApplicationViewTitleBar sender, object args) => UpdateTitleBarVisible(sender.IsVisible);

        private void TitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args) => UpdateTitleBarLayout(sender);

        #region 进度条

        public async Task ShowProgressBarAsync()
        {
            await Dispatcher.ResumeForegroundAsync();
            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.IsIndeterminate = true;
            ProgressBar.ShowError = false;
            ProgressBar.ShowPaused = false;
        }

        public async Task HideProgressBarAsync()
        {
            await Dispatcher.ResumeForegroundAsync();
            ProgressBar.Visibility = Visibility.Collapsed;
            ProgressBar.IsIndeterminate = false;
            ProgressBar.ShowError = false;
            ProgressBar.ShowPaused = false;
            ProgressBar.Value = 0;
        }

        public async Task ShowMessageAsync(string message = null)
        {
            await Dispatcher.ResumeForegroundAsync();
            AppTitle.Text = message ?? ResourceLoader.GetForViewIndependentUse().GetString("AppName") ?? Package.Current.DisplayName;
            ApplicationView.GetForCurrentView().Title = message ?? string.Empty;
        }

        #endregion
    }

    public class BoolToTwoPaneViewTallModeConfigurationConverter : BoolToObjectConverter
    {
        public BoolToTwoPaneViewTallModeConfigurationConverter()
        {
            FalseValue = TwoPaneViewTallModeConfiguration.TopBottom;
            TrueValue = TwoPaneViewTallModeConfiguration.SinglePane;
        }
    }

    public class BoolToTwoPaneViewWideModeConfigurationConverter : BoolToObjectConverter
    {
        public BoolToTwoPaneViewWideModeConfigurationConverter()
        {
            FalseValue = TwoPaneViewWideModeConfiguration.LeftRight;
            TrueValue = TwoPaneViewWideModeConfiguration.SinglePane;
        }
    }
}
