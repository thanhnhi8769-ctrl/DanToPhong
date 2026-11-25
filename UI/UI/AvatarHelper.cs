using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace UI.Services
{
    public static class AvatarHelper
    {
        // Cắt vuông giữa ảnh, thu nhỏ 128x128, nén JPEG (khoảng 5-15KB, server cho tối đa 40KB).
        public static byte[]? ToJpeg128(string path)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var src = Image.FromStream(fs);

            int side = Math.Min(src.Width, src.Height);
            var crop = new Rectangle((src.Width - side) / 2, (src.Height - side) / 2, side, side);

            using var bmp = new Bitmap(128, 128);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(src, new Rectangle(0, 0, 128, 128), crop, GraphicsUnit.Pixel);
            }

            ImageCodecInfo jpeg = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
            using var ep = new EncoderParameters(1);
            ep.Param[0] = new EncoderParameter(Encoder.Quality, 85L);

            using var ms = new MemoryStream();
            bmp.Save(ms, jpeg, ep);
            byte[] data = ms.ToArray();
            return data.Length <= 40 * 1024 ? data : null;
        }
    }
}