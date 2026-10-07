using System.Collections.Generic;
using SkiaSharp;

namespace mosair.Models
{
    public static class MosaicData
    {
        // Renk kataloglari
        public static List<rgb> arRGBAll = new();
        public static List<bool> arcs = new();
        public static List<rgb> arRGB = new();

        // M1 verileri
        public static byte[,,] dataM1 = new byte[3, 3, 3];

        // M3 verileri
        public static byte[,,] dataM3 = new byte[3, 3, 3];
        public static byte[,,] dataM3F = new byte[3, 3, 3];
        public static byte[,,] dataM3Backup = new byte[3, 3, 3];

        // M3 bolge bazli renk listeleri
        public static List<List<rgb>> arMA = new();
        public static List<List<rgb>> arMB = new();
        public static List<List<rgb>> arMBR = new();

        // Bitmap'ler (Mat yerine SKBitmap)
        public static SKBitmap? reducedBitmap;
        public static SKBitmap? exportBitmap;
        // The image Mos works from: the loaded image with the Görsel Ayarları adjustments applied (the same
        // bitmap as sourceBitmap while nothing is adjusted).
        public static SKBitmap? inputBitmap;
        // The loaded image exactly as read from the file.
        public static SKBitmap? sourceBitmap;
        // The stone-texture image is never held whole: MosaicView draws it in tiles (MosaicRenderSource).

        // Sabitler
        public static int N = 40;
        public static int[] arn = new int[3];
    }
}
