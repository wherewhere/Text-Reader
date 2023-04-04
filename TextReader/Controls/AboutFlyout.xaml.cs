using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace TextReader.Controls
{
    public sealed partial class AboutFlyout : UserControl
    {
        // CSHARP_MIGRATION: TODO:
        // BUILD_YEAR was a C++/CX macro and may update the value from the pipeline
        private const string BUILD_YEAR = "2023";

        public AboutFlyout()
        {
            ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView("AboutFlyout");

            this.InitializeComponent();

            SetVersionString();

            Header.Text = resourceLoader.GetString("AboutButton/Content");

            string copyrightText = string.Format(resourceLoader.GetString("AboutControlCopyright"), BUILD_YEAR);
            AboutControlCopyrightRun.Text = copyrightText;

            InitializeContributeTextBlock();
        }

        private void FeedbackButton_Click(object sender, RoutedEventArgs e)
        {
            _ = Launcher.LaunchUriAsync(new Uri("https://github.com/wherewhere/Text-Reader/issues/new"));
        }

        private async void AboutFlyoutLog_Click(object sender, RoutedEventArgs e)
        {
            _ = Launcher.LaunchFolderAsync(await ApplicationData.Current.LocalFolder.CreateFolderAsync("MetroLogs", CreationCollisionOption.OpenIfExists));
        }

        private void SetVersionString()
        {
            PackageVersion version = Package.Current.Id.Version;
            string appName = ResourceLoader.GetForViewIndependentUse().GetString("AppName") ?? "Text Reader";
            AboutFlyoutVersion.Text = appName + " " + version.Major + "." + version.Minor + "." + version.Build + "." + version.Revision;
        }

        private void SetDefaultFocus()
        {
            AboutFlyoutEULA.Focus(FocusState.Programmatic);
        }

        private void InitializeContributeTextBlock()
        {
            ResourceLoader resProvider = ResourceLoader.GetForCurrentView("AboutFlyout");
            string appName = ResourceLoader.GetForViewIndependentUse().GetString("AppName") ?? "Text Reader";
            string contributeHyperlinkText = string.Format(resProvider.GetString("AboutFlyoutContribute"), appName);

            // The resource string has the 'GitHub' hyperlink wrapped with '%HL%'.
            // Break the string and assign pieces appropriately.
            string delimiter = "%HL%";
            int delimiterLength = delimiter.Length;

            // Find the delimiters.
            int firstSplitPosition = contributeHyperlinkText.IndexOf(delimiter, 0);
            int secondSplitPosition = contributeHyperlinkText.IndexOf(delimiter, firstSplitPosition + 1);
            int hyperlinkTextLength = secondSplitPosition - (firstSplitPosition + delimiterLength);

            // Assign pieces.
            string contributeTextBeforeHyperlink = contributeHyperlinkText.Substring(0, firstSplitPosition);
            string contributeTextLink = contributeHyperlinkText.Substring(firstSplitPosition + delimiterLength, hyperlinkTextLength);
            string contributeTextAfterHyperlink = contributeHyperlinkText.Substring(secondSplitPosition + delimiterLength);

            ContributeRunBeforeLink.Text = contributeTextBeforeHyperlink;
            ContributeRunLink.Text = contributeTextLink;
            ContributeRunAfterLink.Text = contributeTextAfterHyperlink;
        }
    }
}
