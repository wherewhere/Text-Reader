using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TextReader.Common;
using TextReader.Helpers;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Ocr;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using TwoPaneViewPriority = TextReader.Controls.TwoPaneViewPriority;

namespace TextReader.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public static string[] ImageTypes = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".tif", ".heif", ".heic" };
        public static IReadOnlyList<Language> Languages => OcrEngine.AvailableRecognizerLanguages;

        public CoreDispatcher Dispatcher { get; }

        public bool IsSupportCompactOverlay { get; } = ApiInformation.IsMethodPresent("Windows.UI.ViewManagement.ApplicationView", "IsViewModeSupported")
                                                    && ApplicationView.GetForCurrentView().IsViewModeSupported(ApplicationViewMode.CompactOverlay);

        public bool IsCompactOverlay
        {
            get => IsSupportCompactOverlay && ApplicationView.GetForCurrentView().ViewMode == ApplicationViewMode.CompactOverlay;
            set
            {
                if (IsSupportCompactOverlay)
                {
                    if (value == true)
                    {
                        if (ApplicationView.GetForCurrentView().IsViewModeSupported(ApplicationViewMode.CompactOverlay))
                        {
                            ViewModePreferences preferences = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
                            preferences.CustomSize = new Size(500, 500);
                            _ = ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, preferences);
                        }
                    }
                    else
                    {
                        if (ApplicationView.GetForCurrentView().IsViewModeSupported(ApplicationViewMode.Default))
                        {
                            _ = ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.Default);
                        }
                    }
                }
                RaisePropertyChangedEvent();
            }
        }

        private string result;
        public string Result
        {
            get => result;
            set => SetProperty(ref result, value);
        }

        private ImageSource image;
        public ImageSource Image
        {
            get => image;
            set
            {
                if (image != value)
                {
                    image = value;
                    RaisePropertyChangedEvent();
                    if (image != null) { IsSinglePane = false; }
                }
            }
        }

        private SoftwareBitmap softwareImage;
        public SoftwareBitmap SoftwareImage
        {
            get => softwareImage;
            set
            {
                if (softwareImage != value)
                {
                    softwareImage = value;
                    RaisePropertyChangedEvent();
                    if (value != null)
                    {
                        _ = SetImage(value);
                        _ = ReadText(value);
                    }
                }
            }
        }

        private int languageIndex;
        public int LanguageIndex
        {
            get => languageIndex;
            set
            {
                if (languageIndex != value)
                {
                    languageIndex = value;
                    RaisePropertyChangedEvent();
                    if (SoftwareImage != null)
                    {
                        _ = ReadText(SoftwareImage);
                    }
                }
            }
        }

        private WriteableBitmap cropperImage;
        public WriteableBitmap CropperImage
        {
            get => cropperImage;
            set => SetProperty(ref cropperImage, value);
        }

        private TwoPaneViewPriority panePriority = TwoPaneViewPriority.Pane1;
        public TwoPaneViewPriority PanePriority
        {
            get => panePriority;
            set
            {
                if (panePriority != value)
                {
                    panePriority = value;
                    RaisePropertyChangedEvent();
                    SetEnable();
                }
            }
        }

        private bool profileLanguage;
        public bool ProfileLanguage
        {
            get => profileLanguage;
            set
            {
                if (profileLanguage != value)
                {
                    profileLanguage = value;
                    RaisePropertyChangedEvent();
                    if (SoftwareImage != null)
                    {
                        _ = ReadText(SoftwareImage);
                    }
                }
            }
        }

        private bool showCropper;
        public bool ShowCropper
        {
            get => showCropper;
            set
            {
                if (showCropper != value)
                {
                    showCropper = value;
                    IsSinglePane = value;
                    RaisePropertyChangedEvent();
                    if (value) { PanePriority = TwoPaneViewPriority.Pane1; }
                }
            }
        }

        private bool isSinglePane = true;
        public bool IsSinglePane
        {
            get => isSinglePane;
            set
            {
                if (isSinglePane != value)
                {
                    isSinglePane = value;
                    RaisePropertyChangedEvent();
                    SetEnable();
                }
            }
        }

        private bool isImageOnlyEnable = false;
        public bool IsImageOnlyEnable
        {
            get => isImageOnlyEnable;
            set => SetProperty(ref isImageOnlyEnable, value);
        }

        private bool isResultOnlyEnable = true;
        public bool IsResultOnlyEnable
        {
            get => isResultOnlyEnable;
            set => SetProperty(ref isResultOnlyEnable, value);
        }

        private GeometryGroup resultGeometry;
        public GeometryGroup ResultGeometry
        {
            get => resultGeometry;
            set => SetProperty(ref resultGeometry, value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected async void RaisePropertyChangedEvent([CallerMemberName] string name = null)
        {
            if (name != null)
            {
                await Dispatcher.ResumeForegroundAsync();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        protected void SetProperty<TProperty>(ref TProperty property, TProperty value, [CallerMemberName] string name = null)
        {
            if (property == null ? value != null : !property.Equals(value))
            {
                property = value;
                RaisePropertyChangedEvent(name);
            }
        }

        public MainViewModel(CoreDispatcher dispatcher) => Dispatcher = dispatcher;

        public async Task SetIndex(string Language)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();
            Language LocalLanguage = new Language(Language);
            for (int i = 0; i < Languages.Count; i++)
            {
                Language language = Languages[i];
                if (language.DisplayName == LocalLanguage.DisplayName)
                {
                    LanguageIndex = i;
                    return;
                }
            }
        }

        public async Task PickImage()
        {
            FileOpenPicker FileOpen = new FileOpenPicker();
            FileOpen.FileTypeFilter.Add(".jpg");
            FileOpen.FileTypeFilter.Add(".jpeg");
            FileOpen.FileTypeFilter.Add(".png");
            FileOpen.FileTypeFilter.Add(".bmp");
            FileOpen.SuggestedStartLocation = PickerLocationId.ComputerFolder;

            StorageFile file = await FileOpen.PickSingleFileAsync();
            if (file != null) { await ReadFile(file); }
        }

        public async Task TakePhoto()
        {
            CameraCaptureUI captureUI = new CameraCaptureUI();
            captureUI.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Png;
            StorageFile file = await captureUI.CaptureFileAsync(CameraCaptureUIMode.Photo);
            if (file != null) { await ReadFile(file); }
        }

        public async Task DropFile(DataPackageView data)
        {
            if (data.Contains(StandardDataFormats.Bitmap))
            {
                RandomAccessStreamReference bitmap = await data.GetBitmapAsync();
                using (IRandomAccessStreamWithContentType random = await bitmap.OpenReadAsync())
                {
                    await ReadStream(random);
                }
            }
            else if (data.Contains(StandardDataFormats.StorageItems))
            {
                IReadOnlyList<IStorageItem> items = await data.GetStorageItemsAsync();
                IEnumerable<IStorageItem> images = items.Where(i => i is StorageFile).Where(i =>
                {
                    foreach (string type in ImageTypes)
                    {
                        if (i.Name.EndsWith(type, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                    return false;
                });
                if (images.Any()) { await ReadFile(images.FirstOrDefault() as StorageFile); }
            }
        }

        public async Task<bool> CheckData(DataPackageView data)
        {
            if (data.Contains(StandardDataFormats.Bitmap))
            {
                return true;
            }
            else if (data.Contains(StandardDataFormats.StorageItems))
            {
                IReadOnlyList<IStorageItem> items = await data.GetStorageItemsAsync();
                IEnumerable<IStorageItem> images = items.Where(i => i is StorageFile).Where(i =>
                {
                    foreach (string type in ImageTypes)
                    {
                        if (i.Name.EndsWith(type, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                    return false;
                });
                if (images.Any()) { return true; }
            }
            return false;
        }

        public async Task SaveImage()
        {
            ResourceLoader loader = ResourceLoader.GetForViewIndependentUse("MainPage");

            FileSavePicker fileSavePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };
            fileSavePicker.FileTypeChoices.Add($"JPEG {loader.GetString("Files")}", new[] { ".jpeg", ".jpg" });
            fileSavePicker.FileTypeChoices.Add($"PNG {loader.GetString("Files")}", new[] { ".png" });
            fileSavePicker.FileTypeChoices.Add($"BMP {loader.GetString("Files")}", new[] { ".bmp" });
            fileSavePicker.FileTypeChoices.Add($"TIFF {loader.GetString("Files")}", new[] { ".tiff", ".tif" });
            fileSavePicker.FileTypeChoices.Add($"GIF {loader.GetString("Files")}", new[] { ".gif" });
            fileSavePicker.FileTypeChoices.Add($"HEIF {loader.GetString("Files")}", new[] { ".heif", ".heic" });
            fileSavePicker.SuggestedFileName = $"TextReader_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";

            StorageFile outputFile = await fileSavePicker.PickSaveFileAsync();

            if (outputFile == null)
            {
                // The user cancelled the picking operation
                return;
            }

            _ = Dispatcher.ShowProgressBarAsync();
            try
            {
                await SaveSoftwareBitmapToFile(SoftwareImage, outputFile);
                _ = Dispatcher.ShowMessageAsync(string.Format(loader.GetString("ImageSaved"), outputFile.Path));
            }
            catch (Exception ex)
            {
                SettingsHelper.LogManager.GetLogger(nameof(MainViewModel)).Error(ex.ExceptionToMessage(), ex);
                _ = Dispatcher.ShowMessageAsync(string.Format(loader.GetString("ImageSaveError"), ex.Message));
                await outputFile.DeleteAsync();
            }
            finally
            {
                _ = Dispatcher.HideProgressBarAsync();
            }
        }

        private async Task SaveSoftwareBitmapToFile(SoftwareBitmap softwareBitmap, StorageFile outputFile)
        {
            using (IRandomAccessStream stream = await outputFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                Guid bitmapEncoderGuid;
                switch (outputFile.FileType.ToLower())
                {
                    case ".jpeg":
                    case ".jpg":
                        bitmapEncoderGuid = BitmapEncoder.JpegEncoderId;
                        break;
                    case ".png":
                        bitmapEncoderGuid = BitmapEncoder.PngEncoderId;
                        break;
                    case ".bmp":
                        bitmapEncoderGuid = BitmapEncoder.BmpEncoderId;
                        break;
                    case ".tiff":
                    case ".tif":
                        bitmapEncoderGuid = BitmapEncoder.TiffEncoderId;
                        break;
                    case ".gif":
                        bitmapEncoderGuid = BitmapEncoder.GifEncoderId;
                        break;
                    case ".heif":
                    case ".heic":
                        bitmapEncoderGuid = ApiInformation.IsPropertyPresent("Windows.Graphics.Imaging.BitmapEncoder", "HeifEncoderId")
                            ? BitmapEncoder.HeifEncoderId
                            : BitmapEncoder.JpegEncoderId;
                        break;
                    default:
                        bitmapEncoderGuid = BitmapEncoder.JpegEncoderId;
                        break;
                }

                // Create an encoder with the desired format
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(bitmapEncoderGuid, stream);

                // Set the software bitmap
                encoder.SetSoftwareBitmap(softwareBitmap);


                // Set additional encoding parameters, if needed
                encoder.IsThumbnailGenerated = true;

                try
                {
                    await encoder.FlushAsync();
                }
                catch (Exception err)
                {
                    SettingsHelper.LogManager.GetLogger(nameof(MainViewModel)).Warn(err.ExceptionToMessage(), err);
                    const int WINCODEC_ERR_UNSUPPORTEDOPERATION = unchecked((int)0x88982F81);
                    switch (err.HResult)
                    {
                        case WINCODEC_ERR_UNSUPPORTEDOPERATION:
                            // If the encoder does not support writing a thumbnail, then try again
                            // but disable thumbnail generation.
                            encoder.IsThumbnailGenerated = false;
                            break;
                        default:
                            throw;
                    }
                }

                if (encoder.IsThumbnailGenerated == false)
                {
                    await encoder.FlushAsync();
                }
            }
        }

        public async Task ReadFile(IStorageFile file)
        {
            using (IRandomAccessStreamWithContentType stream = await file.OpenReadAsync())
            {
                await ReadStream(stream);
            }
        }

        public async Task ReadStream(IRandomAccessStream stream)
        {
            BitmapDecoder ImageDecoder = await BitmapDecoder.CreateAsync(stream);
            SoftwareImage = await ImageDecoder.GetSoftwareBitmapAsync();
            CropperImage = null;
            try
            {
                WriteableBitmap WriteableImage = new WriteableBitmap((int)ImageDecoder.PixelWidth, (int)ImageDecoder.PixelHeight);
                await WriteableImage.SetSourceAsync(stream);
                CropperImage = WriteableImage;
            }
            catch (Exception e)
            {
                SettingsHelper.LogManager.GetLogger(nameof(MainViewModel)).Warn(e.ExceptionToMessage(), e);
                try
                {
                    using (InMemoryRandomAccessStream random = new InMemoryRandomAccessStream())
                    {
                        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, random);
                        encoder.SetSoftwareBitmap(SoftwareImage);
                        await encoder.FlushAsync();
                        WriteableBitmap WriteableImage = new WriteableBitmap((int)ImageDecoder.PixelWidth, (int)ImageDecoder.PixelHeight);
                        await WriteableImage.SetSourceAsync(random);
                        CropperImage = WriteableImage;
                    }
                }
                catch (Exception ex)
                {
                    SettingsHelper.LogManager.GetLogger(nameof(MainViewModel)).Error(ex.ExceptionToMessage(), ex);
                    CropperImage = null;
                }
            }
        }

        public async Task SetImage(SoftwareBitmap softwareBitmap)
        {
            if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 ||
                softwareBitmap.BitmapAlphaMode == BitmapAlphaMode.Straight)
            {
                softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            }

            SoftwareBitmapSource source = new SoftwareBitmapSource();
            await source.SetBitmapAsync(softwareBitmap);
            Image = source;
        }

        public async Task ReadText(SoftwareBitmap softwareBitmap)
        {
            _ = Dispatcher.ShowProgressBarAsync();
            try
            {
                StringBuilder text = new StringBuilder();

                OcrEngine ocrEngine = ProfileLanguage ? OcrEngine.TryCreateFromUserProfileLanguages() : OcrEngine.TryCreateFromLanguage(Languages[LanguageIndex]);

                if (ocrEngine == null)
                {
                    Result = string.Empty;
                    _ = Dispatcher.ShowMessageAsync(ResourceLoader.GetForViewIndependentUse().GetString("LanguageError"));
                    _ = Dispatcher.HideProgressBarAsync();
                    return;
                }

                OcrResult ocrResult = await ocrEngine.RecognizeAsync(softwareBitmap);

                GeometryGroup GeometryGroup = new GeometryGroup();
                foreach (OcrLine line in ocrResult.Lines)
                {
                    text.AppendLine(line.Text);
                    foreach (OcrWord word in line.Words)
                    {
                        GeometryGroup.Children.Add(new RectangleGeometry { Rect = word.BoundingRect });
                    }
                }

                Result = text.ToString();
                ResultGeometry = GeometryGroup;
            }
            finally
            {
                _ = Dispatcher.HideProgressBarAsync();
            }
        }

        private void SetEnable()
        {
            if (!IsSinglePane)
            {
                IsImageOnlyEnable = IsResultOnlyEnable = true;
            }
            else if (PanePriority == TwoPaneViewPriority.Pane1)
            {
                IsImageOnlyEnable = false;
                IsResultOnlyEnable = true;
            }
            else if (PanePriority == TwoPaneViewPriority.Pane2)
            {
                IsImageOnlyEnable = true;
                IsResultOnlyEnable = false;
            }
        }
    }
}
