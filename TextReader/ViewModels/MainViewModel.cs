using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TextReader.Helpers;
using TextReader.Pages;
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
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using TwoPaneViewPriority = TextReader.Controls.TwoPaneViewPriority;

namespace TextReader.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private MainPage page;

        public static string[] ImageTypes = new string[] { ".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".tif", ".heif", ".heic" };
        public static IReadOnlyList<Language> Languages => OcrEngine.AvailableRecognizerLanguages;

        public bool IsSupportCompactOverlay { get; } = ApiInformation.IsMethodPresent("Windows.UI.ViewManagement.ApplicationView", "IsViewModeSupported")
                                                    && ApplicationView.GetForCurrentView().IsViewModeSupported(ApplicationViewMode.CompactOverlay);

        public bool? IsCompactOverlay
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
            set
            {
                if (result != value)
                {
                    result = value;
                    RaisePropertyChangedEvent();
                }
            }
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
            set
            {
                if (cropperImage != value)
                {
                    cropperImage = value;
                    RaisePropertyChangedEvent();
                }
            }
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
            set
            {
                if (isImageOnlyEnable != value)
                {
                    isImageOnlyEnable = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private bool isResultOnlyEnable = true;
        public bool IsResultOnlyEnable
        {
            get => isResultOnlyEnable;
            set
            {
                if (isResultOnlyEnable != value)
                {
                    isResultOnlyEnable = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        private GeometryGroup resultGeometry;
        public GeometryGroup ResultGeometry
        {
            get => resultGeometry;
            set
            {
                if (resultGeometry != value)
                {
                    resultGeometry = value;
                    RaisePropertyChangedEvent();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChangedEvent([System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            if (name != null) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); }
        }

        public MainViewModel(MainPage page) => this.page = page;

        public async Task SetIndex(string Language)
        {
            await Task.Run(async () =>
            {
                Language LocalLanguage = new Language(Language);
                for (int i = 0; i < Languages.Count; i++)
                {
                    Language language = Languages[i];
                    if (language.DisplayName == LocalLanguage.DisplayName)
                    {
                        await DispatcherHelper.ExecuteOnUIThreadAsync(() => LanguageIndex = i);
                        return;
                    }
                }
            });
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
            ResourceLoader loader = ResourceLoader.GetForCurrentView("MainPage");

            FileSavePicker fileSavePicker = new FileSavePicker();
            fileSavePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            fileSavePicker.FileTypeChoices.Add($"JPEG {loader.GetString("Files")}", new List<string> { ".jpeg", ".jpg" });
            fileSavePicker.FileTypeChoices.Add($"PNG {loader.GetString("Files")}", new List<string> { ".png" });
            fileSavePicker.FileTypeChoices.Add($"BMP {loader.GetString("Files")}", new List<string> { ".bmp" });
            fileSavePicker.FileTypeChoices.Add($"TIFF {loader.GetString("Files")}", new List<string> { ".tiff", ".tif" });
            fileSavePicker.FileTypeChoices.Add($"GIF {loader.GetString("Files")}", new List<string> { ".gif" });
            fileSavePicker.FileTypeChoices.Add($"HEIF {loader.GetString("Files")}", new List<string> { ".heif", ".heic" });
            fileSavePicker.SuggestedFileName = $"TextReader_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";

            StorageFile outputFile = await fileSavePicker.PickSaveFileAsync();

            if (outputFile == null)
            {
                // The user cancelled the picking operation
                return;
            }

            page.ShowProgressBar();
            try
            {
                await SaveSoftwareBitmapToFile(SoftwareImage, outputFile);
                page.ShowMessage(string.Format(loader.GetString("ImageSaved"), outputFile.Path));
            }
            catch (Exception ex)
            {
                SettingsHelper.LogManager.GetLogger(nameof(MainViewModel)).Error(ex.ExceptionToMessage(), ex);
                page.ShowMessage(string.Format(loader.GetString("ImageSaveError"), ex.Message));
                await outputFile.DeleteAsync();
            }
            page.HideProgressBar();
        }

        private async Task SaveSoftwareBitmapToFile(SoftwareBitmap softwareBitmap, StorageFile outputFile)
        {
            using (IRandomAccessStream stream = await outputFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                Guid bitmapEncoderGuid;
                switch (outputFile.FileType.ToLower())
                {
                    case "jpeg":
                    case "jpg":
                        bitmapEncoderGuid = BitmapEncoder.JpegEncoderId;
                        break;
                    case "png":
                        bitmapEncoderGuid = BitmapEncoder.PngEncoderId;
                        break;
                    case "bmp":
                        bitmapEncoderGuid = BitmapEncoder.BmpEncoderId;
                        break;
                    case "tiff":
                    case "tif":
                        bitmapEncoderGuid = BitmapEncoder.TiffEncoderId;
                        break;
                    case "gif":
                        bitmapEncoderGuid = BitmapEncoder.GifEncoderId;
                        break;
                    case "heif":
                    case "heic":
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
            catch(Exception e)
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
            page.ShowProgressBar();
            StringBuilder text = new StringBuilder();

            OcrEngine ocrEngine = ProfileLanguage ? OcrEngine.TryCreateFromUserProfileLanguages() : OcrEngine.TryCreateFromLanguage(Languages[LanguageIndex]);

            if (ocrEngine == null)
            {
                Result = string.Empty;
                page.ShowMessage(ResourceLoader.GetForViewIndependentUse().GetString("LanguageError"));
                page.HideProgressBar();
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
            page.HideProgressBar();
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
