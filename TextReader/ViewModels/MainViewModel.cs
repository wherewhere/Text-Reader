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
using TwoPaneViewPriority = TextReader.Controls.TwoPaneViewPriority;
using Windows.UI.Xaml;

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

        public async Task CopyImage()
        {
            using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
            {
                // Create an encoder with the desired format
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);

                // Set the software bitmap
                encoder.SetSoftwareBitmap(SoftwareImage);
                await encoder.FlushAsync();

                RandomAccessStreamReference bitmap = RandomAccessStreamReference.CreateFromStream(stream);
                DataPackage dataPackage = new DataPackage();
                dataPackage.SetBitmap(bitmap);
                Clipboard.SetContentWithOptions(dataPackage, null);
                await Task.Delay(1000);
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
            try
            {
                await WriteableImage.SetSourceAsync(stream);
                CropperImage = WriteableImage;
            }
            catch { CropperImage = null; }
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

            var ocrEngine = ProfileLanguage ? OcrEngine.TryCreateFromUserProfileLanguages() : OcrEngine.TryCreateFromLanguage(Languages[LanguageIndex]);
            var ocrResult = await ocrEngine.RecognizeAsync(softwareBitmap);

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
    }
}
