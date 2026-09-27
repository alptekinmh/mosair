namespace mosair.Models
{
    public class rgb
    {
        public double r, g, b, ri, gi, bi, dis, L, A, B;
        public double unitPrice = 0, price = 0, area = 0;
        public bool boolUseOnce = false, boolUCDone = false, boolLeaveOut = false;
        public bool stokYetersiz = false;
        public int n = 0, numOfPixel = 0, reg = 0;
        public int u = 0, uc = 1, ID = 0;
        public string codeName = "", name = "";

        public rgb() { }

        public rgb(double r, double g, double b)
        {
            this.r = r; this.g = g; this.b = b;
        }

        public rgb(double r, double g, double b, string codeName, string name, int ID)
        {
            this.r = r; this.g = g; this.b = b;
            this.codeName = codeName; this.name = name; this.ID = ID;
        }

        public rgb(double r, double g, double b, double dis, int ID, string codeName, string name)
        {
            this.r = r; this.g = g; this.b = b;
            this.dis = dis; this.ID = ID;
            this.codeName = codeName; this.name = name;
        }

        public rgb(double r, double g, double b, string codeName, string name, int ID, double unitPrice)
        {
            this.r = r; this.g = g; this.b = b;
            this.codeName = codeName; this.name = name;
            this.ID = ID; this.unitPrice = unitPrice;
        }
    }

    public class cooo
    {
        public int n;
        public double av = 0;

        public cooo() { }

        public cooo(double av, int n)
        {
            this.av = av;
            this.n = n;
        }
    }
}
