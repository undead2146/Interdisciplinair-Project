using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace InterdisciplinairProject.Fixtures.Converters
{
    public static class ImageCompressionHelpers
    {
        public static string CompressBase64(string base64)
        {
            byte[] bytes = Convert.FromBase64String(base64);
            using var inputStream = new MemoryStream(bytes);
            using var outputStream = new MemoryStream();
            using (var gzip = new GZipStream(outputStream, CompressionLevel.Optimal))
            {
                inputStream.CopyTo(gzip);
            }
            return Convert.ToBase64String(outputStream.ToArray());
        }

        public static string DecompressBase64(string compressedBase64)
        {
            byte[] compressedBytes = Convert.FromBase64String(compressedBase64);
            using var inputStream = new MemoryStream(compressedBytes);
            using var gzip = new GZipStream(inputStream, CompressionMode.Decompress);
            using var outputStream = new MemoryStream();
            gzip.CopyTo(outputStream);
            return Convert.ToBase64String(outputStream.ToArray());
        }
    }

    public class Base64ToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            BitmapImage image;

            if (value is string base64 && !string.IsNullOrEmpty(base64))
            {
                try
                {
                    byte[] bytes = System.Convert.FromBase64String(base64);
                    using var stream = new MemoryStream(bytes);
                    image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.EndInit();
                    return image;
                }
                catch
                {
                    // fallback
                }
            }

            // fallback naar default image
            image = new BitmapImage(new Uri(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixtures", "Views", "defaultFixturePng.png")));
            image.Freeze();
            return image;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
