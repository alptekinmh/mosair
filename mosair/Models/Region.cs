using System.Collections.Generic;

namespace mosair.Models
{
    public class dr
    {
        public int x, y;
        public double L { get; set; }

        public dr() { }

        public dr(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }

    public class drl
    {
        public static List<drl> arar = new();
        public List<dr> ar = new();
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
