using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    public class Material
    {
        public String Name { get; set; }
        public Bitmap DiffuseMap { get; set; }
        public Bitmap NormalMap { get; set; }
        public int DiffuseMapRenderID { get; set; }
        public int NormalMapRenderID { get; set; }
    }
}
