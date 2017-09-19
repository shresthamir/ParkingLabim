using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ParkingManagement.Library
{
    public static class Imaging
    {
        public static BitmapImage BinaryToImage(byte[] array)
        {
            using (var ms = new System.IO.MemoryStream(array))
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad; // here
                image.StreamSource = ms;
                image.EndInit();
                return image;
            }
        }

        public static byte[] FileToBinary(string filePath)
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(stream))
            {   
                return reader.ReadBytes((int)stream.Length);
            }
        }

        public static BitmapImage FileToImage(string FilePath)
        {
            var BI = new System.Windows.Media.Imaging.BitmapImage();
            BI.BeginInit();
            BI.UriSource = new Uri(FilePath, UriKind.RelativeOrAbsolute);
            BI.EndInit();
            return BI;
        }
    }
}
