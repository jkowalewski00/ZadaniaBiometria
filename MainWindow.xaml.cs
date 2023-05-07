using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ZadaniaBiometria
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public BitmapImage Original;
        public BitmapImage Converted;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void OpenPhoto(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Select a picture";
            ofd.Filter = "JPEG Files (*.jpeg)|*.jpeg|PNG Files (*.png)|*.png|JPG Files (*.jpg)|*.jpg";
            if (ofd.ShowDialog() == true)
            {
                Original = new BitmapImage(new Uri(ofd.FileName));
                Converted = new BitmapImage(new Uri(ofd.FileName));
                Image.Source = Converted;
            }
        }

        public void SavePicture(object sender, RoutedEventArgs e)
        {
            if (Converted == null)
                return;
            Converted = (BitmapImage)Image.Source;
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Save image as";
            sfd.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.bmp)|*.jpg; *.jpeg; *.gif; *.bmp";
            if (sfd.ShowDialog() == true)
            {
                BitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(Converted));
                using (Stream strm = File.Create(sfd.FileName))
                {
                    encoder.Save(strm);
                }
            }
        }

        public void OriginalImage_ResetValue(object sender, RoutedEventArgs e)
        {
            Image.Source = Original;
            DisplayHistogram(Original);
        }

        private void DisplayHistogram(BitmapImage bitmapImage)
        {
            if (bitmapImage != null)
            {
                // Utworzenie nowego obiektu BitmapSource na podstawie BitmapImage
                FormatConvertedBitmap convertedBitmap = new FormatConvertedBitmap(bitmapImage, PixelFormats.Rgb24, null, 0);
                byte[] pixels = new byte[convertedBitmap.PixelWidth * convertedBitmap.PixelHeight * 3];
                convertedBitmap.CopyPixels(pixels, convertedBitmap.PixelWidth * 3, 0);

                // Utworzenie tablicy z wartościami histogramu
                int[] histogramValues = new int[256];

                // Obliczenie wartości histogramu dla średniej kanałów RGB
                for (int i = 0; i < pixels.Length; i += 3)
                {
                    int value = (pixels[i] + pixels[i + 1] + pixels[i + 2]) / 3;
                    histogramValues[value]++;
                }

                // Utworzenie nowego płótna dla histogramu
                Canvas histogramCanvas = new Canvas();
                histogramCanvas.Width = 300;
                histogramCanvas.Height = 200;

                // Dodanie podziałek na osi Y
                for (int i = 0; i <= 10; i++)
                {
                    double y = histogramCanvas.Height - i * histogramCanvas.Height / 10.0;
                    Line line = new Line();
                    line.X1 = 0;
                    line.X2 = histogramCanvas.Width;
                    line.Y1 = y;
                    line.Y2 = y;
                    line.Stroke = Brushes.LightGray;
                    histogramCanvas.Children.Add(line);
                }

                // Wyznaczenie maksymalnej wartości histogramu
                int maxValue = histogramValues.Max();

                // Rysowanie prostokątów dla każdej wartości histogramu
                for (int i = 0; i < histogramValues.Length; i++)
                {
                    double height = histogramValues[i] * histogramCanvas.Height / (double)maxValue;
                    Rectangle rectangle = new Rectangle();
                    rectangle.Width = histogramCanvas.Width / 256.0;
                    rectangle.Height = height;
                    rectangle.Fill = Brushes.Black;
                    Canvas.SetLeft(rectangle, i * histogramCanvas.Width / 256.0);
                    Canvas.SetTop(rectangle, histogramCanvas.Height - height);
                    histogramCanvas.Children.Add(rectangle);
                }

                // Dodanie etykiet na osi X
                for (int i = 0; i <= 255; i += 32)
                {
                    double x = i * histogramCanvas.Width / 256.0;
                    TextBlock label = new TextBlock();
                    label.Text = i.ToString();
                    label.FontSize = 8;
                    Canvas.SetLeft(label, x);
                    Canvas.SetTop(label, histogramCanvas.Height + 5);
                    histogramCanvas.Children.Add(label);
                }
                HistogramCanvas.Children.Clear();
                HistogramCanvas.Children.Add(histogramCanvas);
            }
        }

        private void DisplayHistogram(object sender, RoutedEventArgs e)
        {
            DisplayHistogram(Converted);
        }

        private BitmapImage ConvertBitmapSourceToBitmapImage(BitmapSource bitmapSource)
        {
            BitmapImage bitmapImage = new BitmapImage();

            using (MemoryStream memoryStream = new MemoryStream())
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(memoryStream);

                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.EndInit();
            }

            return bitmapImage;
        }


        private void StretchHistogram()
        {
            if (Image.Source is BitmapImage bitmapImage)
            {
                // Przetworzenie pikseli obrazu
                FormatConvertedBitmap convertedBitmap = new FormatConvertedBitmap(bitmapImage, PixelFormats.Gray8, null, 0);
                byte[] pixels = new byte[convertedBitmap.PixelWidth * convertedBitmap.PixelHeight];
                convertedBitmap.CopyPixels(pixels, convertedBitmap.PixelWidth, 0);

                // Znajdź najniższą i najwyższą wartość jasności w obrazie
                byte minValue = pixels.Min();
                byte maxValue = pixels.Max();

                // Odczytaj wartości z sliderów
                double newMinValue = MinStretch.Value;
                double newMaxValue = MaxStretch.Value;

                // Rozciąganie histogramu dla każdego piksela
                for (int i = 0; i < pixels.Length; i++)
                {
                    double normalizedValue = (pixels[i] - minValue) / (double)(maxValue - minValue);
                    byte stretchedValue = (byte)(normalizedValue * (newMaxValue - newMinValue) + newMinValue);
                    pixels[i] = stretchedValue;
                }

                // Utworzenie nowego obiektu BitmapImage na podstawie pikseli po rozciąganiu histogramu
                BitmapSource stretchedBitmapSource = BitmapSource.Create(convertedBitmap.PixelWidth, convertedBitmap.PixelHeight, convertedBitmap.DpiX, convertedBitmap.DpiY, PixelFormats.Gray8, null, pixels, convertedBitmap.PixelWidth);

                // Ustawienie zaktualizowanego obiektu BitmapImage jako źródło kontrolki Image
                Image.Source = ConvertBitmapSourceToBitmapImage(stretchedBitmapSource);

                // Wyświetlanie zaktualizowanego histogramu
                DisplayHistogram((BitmapImage)Image.Source);
            }
        }

        private void Stretch_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            MinStretchValue.Content = "Min stretch value: " + MinStretch.Value;
            MaxStretchValue.Content = "Max stretch value: " + MaxStretch.Value;
            StretchHistogram();
            
        }

        private void EqualizeHistogram(BitmapImage bitmapImage)
        {
            // Konwersja BitmapImage na BitmapSource
            BitmapSource bitmapSource = bitmapImage as BitmapSource;

            // Konwersja obrazu na odcienie szarości
            FormatConvertedBitmap grayBitmap = new FormatConvertedBitmap(bitmapSource, PixelFormats.Gray8, null, 0);

            // Obliczenie histogramu
            int[] histogram = new int[256];
            byte[] pixels = new byte[grayBitmap.PixelWidth * grayBitmap.PixelHeight];
            grayBitmap.CopyPixels(pixels, grayBitmap.PixelWidth, 0);
            for (int i = 0; i < pixels.Length; i++)
            {
                histogram[pixels[i]]++;
            }

            // Obliczenie funkcji dystrybuanty
            int totalPixels = grayBitmap.PixelWidth * grayBitmap.PixelHeight;
            double[] cumulativeHistogram = new double[256];
            cumulativeHistogram[0] = histogram[0] / (double)totalPixels;
            for (int i = 1; i < 256; i++)
            {
                cumulativeHistogram[i] = cumulativeHistogram[i - 1] + histogram[i] / (double)totalPixels;
            }

            // Przekształcenie pikseli obrazu
            byte[] equalizedPixels = new byte[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                int pixelValue = pixels[i];
                byte equalizedValue = (byte)Math.Round(cumulativeHistogram[pixelValue] * 255);
                equalizedPixels[i] = equalizedValue;
            }

            // Utworzenie nowego BitmapImage na podstawie pikseli po wyrównaniu histogramu
            BitmapSource equalizedBitmapSource = BitmapSource.Create(grayBitmap.PixelWidth, grayBitmap.PixelHeight, grayBitmap.DpiX, grayBitmap.DpiY, PixelFormats.Gray8, null, equalizedPixels, grayBitmap.PixelWidth);
            BitmapImage equalizedBitmapImage = new BitmapImage();
            using (MemoryStream stream = new MemoryStream())
            {
                // Konwersja z BitmapSource na BitmapImage
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(equalizedBitmapSource));
                encoder.Save(stream);
                equalizedBitmapImage.BeginInit();
                equalizedBitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                equalizedBitmapImage.StreamSource = stream;
                equalizedBitmapImage.EndInit();
            }

            Image.Source = equalizedBitmapImage;

            DisplayHistogram((BitmapImage)Image.Source);
        }

        private void EqualHistogram(object sender, RoutedEventArgs e)
        {
            EqualizeHistogram(Original);
        }

        private void OtsuThresholding()
        {
            if (Image.Source != null)
            {
                // Pobierz obraz z kontrolki Image jako BitmapImage
                BitmapImage bitmapImage = (BitmapImage)Image.Source;

                // Utwórz nowy obiekt BitmapImage na podstawie oryginalnego
                BitmapImage processedBitmapImage = new BitmapImage();

                // Ustawienie odpowiednich właściwości obiektu BitmapImage
                processedBitmapImage.BeginInit();
                processedBitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                processedBitmapImage.CreateOptions = BitmapCreateOptions.None;
                processedBitmapImage.UriSource = bitmapImage.UriSource;
                processedBitmapImage.EndInit();

                // Przetworzenie pikseli obrazu
                FormatConvertedBitmap convertedBitmap = new FormatConvertedBitmap(processedBitmapImage, PixelFormats.Gray8, null, 0);
                byte[] pixels = new byte[convertedBitmap.PixelWidth * convertedBitmap.PixelHeight];
                convertedBitmap.CopyPixels(pixels, convertedBitmap.PixelWidth, 0);

                // Obliczenie histogramu
                int[] histogram = CalculateHistogram(pixels);

                // Obliczenie prawdopodobieństwa wystąpienia każdego piksela
                double[] probabilities = new double[256];
                int numPixels = pixels.Length;
                for (int i = 0; i < histogram.Length; i++)
                {
                    probabilities[i] = (double)histogram[i] / numPixels;
                }

                // Obliczenie kumulatywnego histogramu
                double[] cumulativeHistogram = new double[256];
                cumulativeHistogram[0] = probabilities[0];
                for (int i = 1; i < 256; i++)
                {
                    cumulativeHistogram[i] = cumulativeHistogram[i - 1] + probabilities[i];
                }

                // Obliczenie międzyklasowej wariancji dla każdego progu
                double[] variances = new double[256];
                for (int i = 0; i < 256; i++)
                {
                    double meanBackground = 0;
                    double meanForeground = 0;
                    double varianceBackground = 0;
                    double varianceForeground = 0;

                    if (cumulativeHistogram[i] > 0)
                    {
                        meanBackground = CalculateMean(0, i, probabilities);
                        varianceBackground = CalculateVariance(0, i, meanBackground, probabilities);
                    }

                    if (cumulativeHistogram[i] < 1)
                    {
                        meanForeground = CalculateMean(i + 1, 255, probabilities);
                        varianceForeground = CalculateVariance(i + 1, 255, meanForeground, probabilities);
                    }

                    variances[i] = cumulativeHistogram[i] * (1 - cumulativeHistogram[i]) * (meanBackground - meanForeground) * (meanBackground - meanForeground);
                }

                // Znalezienie progu, który maksymalizuje międzyklasową wariancję
                double maxVariance = variances.Max();
                int threshold = Array.IndexOf(variances, maxVariance);

                // Binaryzacja obrazu na podstawie progu
                byte[] binaryPixels = new byte[pixels.Length];
                for (int i = 0; i < pixels.Length; i++)
                {
                    binaryPixels[i] = pixels[i] > threshold ? (byte)255 : (byte)0;
                }

                // Utworzenie nowego obiektu BitmapImage na podstawie pikseli po binaryzacji
                BitmapSource binaryBitmapSource = BitmapSource.Create(convertedBitmap.PixelWidth, convertedBitmap.PixelHeight, convertedBitmap.DpiX, convertedBitmap.DpiY, PixelFormats.Gray8, null, binaryPixels, convertedBitmap.PixelWidth);
                processedBitmapImage = new BitmapImage();
                using (MemoryStream stream = new MemoryStream())
                {
                    // Konwersja z BitmapSource na BitmapImage
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(binaryBitmapSource));
                    encoder.Save(stream);
                    processedBitmapImage.BeginInit();
                    processedBitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    processedBitmapImage.StreamSource = stream;
                    processedBitmapImage.EndInit();
                }

                // Ustawienie obiektu BitmapImage jako źródło kontrolki Image
                Image.Source = processedBitmapImage;

                // Wyświetlanie zaktualizowanego histogramu
                DisplayHistogram(processedBitmapImage);
            }
        }

        private int[] CalculateHistogram(byte[] pixels)
        {
            int[] histogram = new int[256];
            for (int i = 0; i < pixels.Length; i++)
            {
                histogram[pixels[i]]++;
            }
            return histogram;
        }

        private double CalculateMean(int start, int end, double[] probabilities)
        {
            double mean = 0;
            for (int i = start; i <= end; i++)
            {
                mean += i * probabilities[i];
            }
            return mean;
        }

        private double CalculateVariance(int start, int end, double mean, double[] probabilities)
        {
            double variance = 0;
            for (int i = start; i <= end; i++)
            {
                variance += Math.Pow(i - mean, 2) * probabilities[i];
            }
            return variance;
        }

        private void OtsuThereshold(object sender, RoutedEventArgs e)
        {
            OtsuThresholding();
        }


    }
}
