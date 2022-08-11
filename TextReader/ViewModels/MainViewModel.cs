using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Media.Capture;
using System.IO;
using TextReader.Controls;
using Windows.Storage.Streams;
using Windows.Globalization;
using Windows.ApplicationModel.DataTransfer;

namespace TextReader.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public static string[] ImageTypes = new string[] { ".jpg", ".jpeg", ".png", ".bmp" };
        public static IReadOnlyList<Language> Languages => OcrEngine.AvailableRecognizerLanguages;

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
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChangedEvent([System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            if (name != null) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); }
        }

        public async Task PickImage()
        {
            FileOpenPicker FileOpen = new();
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

        public async Task ReadFile(StorageFile file)
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
            var WriteableImage = new WriteableBitmap(1, 1);
            await WriteableImage.SetSourceAsync(stream);
            CropperImage = WriteableImage;
        }

        public async Task SetImage(SoftwareBitmap softwareBitmap)
        {
            if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 ||
                softwareBitmap.BitmapAlphaMode == BitmapAlphaMode.Straight)
            {
                softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            }

            var source = new SoftwareBitmapSource();
            await source.SetBitmapAsync(softwareBitmap);
            Image = source;
        }

        public async Task ReadText(SoftwareBitmap softwareBitmap)
        {
            StringBuilder text = new StringBuilder();

            var ocrEngine = OcrEngine.TryCreateFromLanguage(Languages[LanguageIndex]);
            var ocrResult = await ocrEngine.RecognizeAsync(softwareBitmap);

            foreach (var line in ocrResult.Lines) text.AppendLine(line.Text);

            Result = text.ToString();
        }
    }
}
