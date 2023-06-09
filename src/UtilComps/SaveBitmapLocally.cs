using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Brain.UtilMethods;

namespace Brain.UtilComps
{
    public class SaveBitmapLocally : GH_Component
    {
        public SaveBitmapLocally() : base("Save Bitmap Locally", "SaveBitmap", "Saves Bitmap object to local disk.", "Brain", "Utils")
        { }
        public override Guid ComponentGuid => new Guid("{34625A3A-893C-4DFF-8C53-8B950CCBF8B9}");

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Dir", "D", "Directory to the saved images", GH_ParamAccess.item);
            pManager.AddGenericParameter("Bitmap", "BMP", "Bitmap object", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Save", "S", "Save the image", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Path", "P", "Path of the saved images", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string dir = "";
            List<Bitmap> bmp = new List<Bitmap>();
            bool save = false;
            List<string> paths = new List<string>();

            DA.GetData(0, ref dir);
            DA.GetDataList(1, bmp);
            DA.GetData(2, ref save);

            if (save)
            {
                foreach (var b in bmp)
                {
                    string path = Path.Combine(dir, BitmapName(b));
                    b.Save(path);
                    paths.Add(path);
                }
            }

            DA.SetDataList(0, paths);
        }
    }
}
