using System.Collections.Generic;

namespace mosair.Models
{
    // A region of the mosaic (WPF's multi-region model; mosair always uses one region covering the image).
    // WPF also kept a per-stone list of positions (dr objects) here; nothing read it, and at 20 m it took
    // about 0.2-0.3 GB and up to 0.6 s per Mos, so it was removed. Project files never contained it.
    public class drl
    {
        public static List<drl> arar = new();
        public static int[,,] dat = new int[1, 1, 4];
        public int x1, y1, x2, y2;
        public int rgbM = 3;

        public drl() { }

        public drl(int x1, int y1, int x2, int y2)
        {
            this.x1 = x1; this.y1 = y1;
            this.x2 = x2; this.y2 = y2;
        }
    }
}
