using System;
using System.Collections.Generic;
using System.IO;
using mosair.Models;

namespace mosair.Services
{
    public static class ColorCatalogService
    {
        public static void LoadCatalog(string path)
        {
            MosaicData.arRGBAll.Clear();

            using var reader = new StreamReader(path);
            string? s;
            int id = 0;

            while ((s = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(s)) continue;
                id++;

                string[] u = s.Split(' ');
                byte r = Convert.ToByte(u[0]);
                byte g = Convert.ToByte(u[1]);
                byte b = Convert.ToByte(u[2]);
                string codeName = u[3];
                string name = u[4] + " " + u[5];
                if (u.Length >= 7)
                    name += " " + u[6];

                var re = new rgb(r, g, b, (r + g + b) / 3.0, id, codeName, name);
                MosaicData.arRGBAll.Add(re);
            }

            InitArcs();
            SetActiveColors();
        }

        public static void LoadDefaultCatalog()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Assets", "colorsBas.txt");
            if (File.Exists(path))
                LoadCatalog(path);
        }

        public static void SetLeaveOut(int index, bool leaveOut)
        {
            if (index >= 0 && index < MosaicData.arRGBAll.Count)
            {
                MosaicData.arRGBAll[index].boolLeaveOut = leaveOut;
                if (index < MosaicData.arcs.Count)
                    MosaicData.arcs[index] = leaveOut;
            }
            SetActiveColors();
        }

        public static void SetActiveColors()
        {
            MosaicData.arRGB.Clear();
            for (int i = 0; i < MosaicData.arRGBAll.Count; i++)
            {
                rgb r = MosaicData.arRGBAll[i];
                if (!r.boolLeaveOut)
                    MosaicData.arRGB.Add(r);
            }
        }

        private static void InitArcs()
        {
            if (MosaicData.arcs.Count == 0 || MosaicData.arcs.Count != MosaicData.arRGBAll.Count)
            {
                MosaicData.arcs.Clear();
                for (int i = 0; i < MosaicData.arRGBAll.Count; i++)
                    MosaicData.arcs.Add(false);
            }
            else
            {
                for (int i = 0; i < MosaicData.arcs.Count; i++)
                    MosaicData.arRGBAll[i].boolLeaveOut = MosaicData.arcs[i];
            }
        }

        public static List<CatalogColorInfo> GetCatalogList()
        {
            var list = new List<CatalogColorInfo>();
            for (int i = 0; i < MosaicData.arRGBAll.Count; i++)
            {
                var c = MosaicData.arRGBAll[i];
                list.Add(new CatalogColorInfo
                {
                    Index = i,
                    R = (byte)c.r,
                    G = (byte)c.g,
                    B = (byte)c.b,
                    CodeName = c.codeName,
                    Name = c.name,
                    ID = c.ID,
                    IsExcluded = c.boolLeaveOut
                });
            }
            return list;
        }
    }

    public class CatalogColorInfo
    {
        public int Index { get; set; }
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public string CodeName { get; set; } = "";
        public string Name { get; set; } = "";
        public int ID { get; set; }
        public bool IsExcluded { get; set; }
    }
}
