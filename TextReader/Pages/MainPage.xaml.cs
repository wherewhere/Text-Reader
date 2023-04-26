using Microsoft.Toolkit.Uwp.Helpers;
using Microsoft.Toolkit.Uwp.UI.Converters;
using System;
using System.Linq;
using System.Threading.Tasks;
using TextReader.Controls;
using TextReader.Helpers;
using TextReader.ViewModels;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Graphics.Imaging;
using Windows.Storage;
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
        private readonly MainViewModel Provider;
        public Frame MainFrame => Frame;

        public MainPage()
        {
            InitializeComponent();
            AppTitle.Text = ResourceLoader.GetForViewIndependentUse().GetString("AppName") ?? "文字识别";
            if (!(AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Desktop"))
            { UpdateTitleBarLayout(false); }
            if (SystemInformation.OperatingSystemVersion.Build >= 22000)
            { CommandBar.DefaultLabelPosition = CommandBarDefaultLabelPosition.Right; }
            Provider = new MainViewModel(this);
            DataContext = Provider;
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
            Clipboard_ContentChanged(null, null);
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
                case ActivationKind.File:
                    IFileActivatedEventArgs FileEventArgs = args as IFileActivatedEventArgs;
                    foreach (IStorageItem file in FileEventArgs.Files)
                    {
                        if (file is IStorageFile storageFile && MainViewModel.ImageTypes.Contains($".{storageFile.FileType.ToLowerInvariant()}"))
                        {
                            _ = Provider.ReadFile(storageFile);
                            break;
                        }
                    }
                    break;
                case ActivationKind.ShareTarget:
                    IShareTargetActivatedEventArgs ShareTargetEventArgs = args as IShareTargetActivatedEventArgs;
                    _ = Provider.DropFile(ShareTargetEventArgs.ShareOperation.Data);
                    break;
                default:
                    break;
            }
        }

        private void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            _ = Provider.SetIndex(Language);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            switch ((sender as FrameworkElement).Tag as string)
            {
                case "Save":
                    _ = Provider.SaveImage();
                    break;
                case "Paste":
                    _ = Provider.DropFile(Clipboard.GetContent());
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
                    _ = Provider.ReadText(Provider.SoftwareImage);
                    break;
                case "TwoPanel":
                    Provider.IsSinglePane = false;
                    Provider.PanePriority = TwoPaneViewPriority.Pane1;
                    break;
                case "ImageOnly":
                    Provider.IsSinglePane = true;
                    Provider.PanePriority = TwoPaneViewPriority.Pane1;
                    break;
                case "ResultOnly":
                    Provider.IsSinglePane = true;
                    Provider.PanePriority = TwoPaneViewPriority.Pane2;
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

        public void ClipCancel()
        {
            Provider.ShowCropper = false;
        }

        public void ClipImage()
        {
            if (Provider.CropperImage == null) { return; }
            Provider.ShowCropper = true;
        }

        private void Grid_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.Handled = true;
        }

        private void Grid_Drop(object sender, DragEventArgs e)
        {
            _ = Provider.DropFile(e.DataView);
            e.Handled = true;
        }

        private void UpdateTitleBarLayout(CoreApplicationViewTitleBar TitleBar)
        {
            CustomTitleBar.Height = TitleBar.Height;
            LeftPaddingColumn.Width = new GridLength(TitleBar.SystemOverlayLeftInset);
            RightPaddingColumn.Width = new GridLength(TitleBar.SystemOverlayRightInset);
        }

        private void UpdateTitleBarLayout(bool IsVisible)
        {
            CustomTitleBar.Visibility = IsVisible && !UIHelper.HasStatusBar && !UIHelper.HasTitleBar ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Dispatcher_AcceleratorKeyActivated(CoreDispatcher sender, AcceleratorKeyEventArgs args)
        {
            if (args.EventType.ToString().Contains("Down"))
            {
                CoreVirtualKeyStates ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control);
                if (ctrl.HasFlag(CoreVirtualKeyStates.Down))
                {
                    switch (args.VirtualKey)
                    {
                        case VirtualKey.V:
                            if (Paste.IsEnabled)
                            {
                                _ = Provider.DropFile(Clipboard.GetContent());
                                args.Handled = true;
                            }
                            break;
                    }
                }
            }
        }

        private void Clipboard_ContentChanged(object sender, object e) => _ = Dispatcher.AwaitableRunAsync(async () => Paste.IsEnabled = await Provider.CheckData(Clipboard.GetContent()));

        private void TitleBar_IsVisibleChanged(CoreApplicationViewTitleBar sender, object args) => UpdateTitleBarLayout(sender.IsVisible);

        private void TitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args) => UpdateTitleBarLayout(sender);

        #region 进度条

        public void ShowProgressBar()
        {
            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.IsIndeterminate = true;
            ProgressBar.ShowError = false;
            ProgressBar.ShowPaused = false;
        }

        public void ShowProgressBar(double value)
        {
            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.IsIndeterminate = false;
            ProgressBar.ShowError = false;
            ProgressBar.ShowPaused = false;
            ProgressBar.Value = value;
        }

        public void PausedProgressBar()
        {
            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.IsIndeterminate = true;
            ProgressBar.ShowError = false;
            ProgressBar.ShowPaused = true;
        }

        public void ErrorProgressBar()
        {
            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.IsIndeterminate = true;
            ProgressBar.ShowPaused = false;
            ProgressBar.ShowError = true;
        }

        public void HideProgressBar()
        {
            ProgressBar.Visibility = Visibility.Collapsed;
            ProgressBar.IsIndeterminate = false;
            ProgressBar.ShowError = false;
            ProgressBar.ShowPaused = false;
            ProgressBar.Value = 0;
        }

        public void ShowMessage(string message = null)
        {
            if (CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar)
            {
                AppTitle.Text = message ?? ResourceLoader.GetForViewIndependentUse().GetString("AppName") ?? "文字识别";
            }
            else
            {
                ApplicationView.GetForCurrentView().Title = message ?? string.Empty;
            }
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
