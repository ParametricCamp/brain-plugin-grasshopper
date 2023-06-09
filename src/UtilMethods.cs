using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;

namespace Brain
{
    public class UtilMethods
    {
        public static string BitmapName(Bitmap bitmap) => (string)bitmap.Tag ?? (DateTime.Now.ToString("yyyyMMdd_HHmmssfff") + ".png");
        public static string String(int? size) => string.Concat(size, 'x', size);
        public static byte[] Bitmap2Bytes(Bitmap bitmap)
        {
            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }
        }
        public static byte[] File2Bytes(string filepath) => File.ReadAllBytes(filepath);
        
    }
}
