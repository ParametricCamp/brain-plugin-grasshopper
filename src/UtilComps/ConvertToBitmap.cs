using Grasshopper.Kernel;
using Rhino;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static Brain.OpenAI.OAICreateImage;

namespace Brain.UtilComps
{
    public class ConvertToBitmap : GH_Component
    {
        List<Bitmap> bmp = new List<Bitmap>();
        bool output = false;
        public ConvertToBitmap() : base("Convert to Bitmap", "ToBitmap", "Converts URL or Base64 string to Bitmap object.", "Brain", "Utils")
        { }
        public override Guid ComponentGuid => new Guid("{50A4FB77-C437-47A5-94EA-B46A9849819D}");

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("URL", "URL", "URL of the image.", GH_ParamAccess.list);
            pManager.AddTextParameter("Base64", "B64", "Base64 string of the image.", GH_ParamAccess.list);
            pManager[0].Optional = pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Bitmap", "BMP", "Bitmap object", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (output)
            {
                DA.SetDataList(0, bmp);
                output = false;
                return;
            }
            List<string> urls = new List<string>();
            List<string> b64s = new List<string>();
            bmp = new List<Bitmap>();
            DA.GetDataList(0, urls);
            DA.GetDataList(1, b64s);
            Task.Run(() =>
            {
                using (WebClient webClient = new WebClient())
                {
                    foreach (var url in urls)
                    {
                        byte[] imageBytes = webClient.DownloadData(url);
                        using (MemoryStream ms = new MemoryStream(imageBytes))
                        {
                            var image = new Bitmap(ms);
                            image.Tag = GetFileName(url);
                            bmp.Add(image);
                        }
                    }
                }
                DA.GetDataList(1, b64s);
                foreach (var b64 in b64s)
                {
                    byte[] imageBytes = Convert.FromBase64String(b64);
                    using (MemoryStream ms = new MemoryStream(imageBytes))
                    {
                        var image = new Bitmap(ms);
                        bmp.Add(new Bitmap(ms));
                    }
                }
                output = true;
                RhinoApp.InvokeOnUiThread((Action)delegate { ExpireSolution(true); });
            });            
        }
        protected override void ExpireDownStreamObjects()
        {
            if (output)
            {
                base.ExpireDownStreamObjects();
            }
        }

        private static string GetFileName(string url)
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath;
            var index = path.LastIndexOf("/");
            if (index == -1)
            {
                return path;
            }
            else
            {
                return path.Substring(index + 1);
            }
        }
    }
}
