using Brain.Templates;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Brain.UtilMethods;
using static Brain.OpenAI.Schema;

namespace Brain.OpenAI
{
    public class CreateImageVariationComponent : GH_Component_HTTPAsync_Form
    {
        private const string ENDPOINT = "https://api.openai.com/v1/images/variations";
        public string format = null;

        public CreateImageVariationComponent() : base("Create image variation", "ImageVariation", "Creates a variation of a given image.", "Brain", "OpenAI")
        {
        }

        public override Guid ComponentGuid => new Guid("{2A0F75F1-80F7-45DB-9414-DD064602A3AD}");
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, "Advanced options", AdvancedClicked, true, _advanced);
        }
        readonly List<IGH_Param> advancedParams = new List<IGH_Param>() {
            new Param_Integer { Name = "Count", NickName = "Count", Description = "The number of images to generate. Must be between 1 and 10.", Optional = true },
            new Param_Integer { Name = "Size", NickName = "Size", Description = "The size of the generated square images. Must be one of 256, 512, or 1024.", Optional = true },
            new Param_String { Name = "Format", NickName = "Format", Description = "The format in which the generated images are returned. Must be one of url or b64_json", Optional = true },
            new Param_String { Name = "User", NickName = "User", Description = "A unique identifier representing your end-user, which can help OpenAI to monitor and detect abuse.", Optional = true },
        };
        private void AdvancedClicked(object sender, EventArgs e)
        {
            RecordUndoEvent("Toggle Advanced options");
            _advanced = !_advanced;
            if (_advanced)
            {
                foreach (var param in advancedParams)
                    Params.RegisterInputParam(param);
            }
            else
            {
                foreach (var param in advancedParams)
                    Params.UnregisterInputParameter(param, false);
            }
            Params.OnParametersChanged();
            this.OnDisplayExpired(true);
        }
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            base.RegisterInputParams(pManager);
            pManager.AddTextParameter("Image", "I", "Bitmap object or file path\r\n\r\nThe image to use as the basis for the variation(s). Must be a valid PNG file, less than 4MB, and square.", GH_ParamAccess.item);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Response", "R", "Request response", GH_ParamAccess.item);
            pManager.AddTextParameter("URL", "U", "Image URL", GH_ParamAccess.list);
            pManager.AddTextParameter("Base64", "B", "Image encoded as Base64", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.DisableGapLogic();
            if (_shouldExpire)
            {
                _sw.Stop();
                List<string> url = new List<string>();
                List<string> b64 = new List<string>();
                switch (_currentState)
                {
                    case RequestState.Off:
                        this.Message = "Inactive";
                        _currentState = RequestState.Idle;
                        break;

                    case RequestState.Error:
                        this.Message = $"ERROR\r\n{_sw.Elapsed.ToShortString()}";
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, _response);
                        _currentState = RequestState.Idle;
                        break;

                    case RequestState.Done:
                        this.Message = $"Complete!\r\n{_sw.Elapsed.ToShortString()}";
                        _currentState = RequestState.Idle;

                        try
                        {
                            var resJson = JsonSerializer.Deserialize<DataSchema>(_response);
                            Data[] data = resJson.data;
                            switch (format)
                            {
                                case "b64_json":
                                    foreach (var imageData in data)
                                        b64.Add(imageData.b64_json);
                                    break;
                                case "url":
                                default:
                                    foreach (var imageData in data)
                                        url.Add(imageData.url);
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Something went wrong deserializing the response: " + ex.Message);
                        }

                        break;
                }
                // Output
                DA.SetData(0, _response);
                DA.SetDataList(1, url);
                DA.SetDataList(2, b64);
                _shouldExpire = false;
                return;
            }

            bool active = false;
            string authToken = null;
            int timeout = 0;

            DA.GetData("Send", ref active);
            if (!active)
            {
                _currentState = RequestState.Off;
                _shouldExpire = true;
                _response = "";
                ExpireSolution(true);
                return;
            }

            DA.GetData("Authorization", ref authToken);
            if (!DA.GetData("Timeout", ref timeout)) return;
            string user = null;
            format = null;
            int? size = 256, n = null;
            GH_ObjectWrapper image = new GH_ObjectWrapper();
            DA.GetData("Image", ref image);
            if (_advanced)
            {
                DA.GetData("Count", ref n);
                DA.GetData("Size", ref size);
                DA.GetData("Format", ref format);
                DA.GetData("User", ref user);
            }
            List<ReqContent> form = new List<ReqContent>();
            if (image.Value is Bitmap)
            {
                var imageBitmap = image.Value as Bitmap;
                form.Add(new ReqContent(Bitmap2Bytes(imageBitmap), "image", BitmapName(imageBitmap)));
            }
            else
            {
                var imagePath = (image.Value as GH_String).ToString();
                form.Add(new ReqContent(File.ReadAllBytes(imagePath), "image", Path.GetFileName(imagePath)));
            }
            if (n != null)
                form.Add(new ReqContent(Encoding.UTF8.GetBytes(n.ToString()), "n", null));
            if (size != null)
                form.Add(new ReqContent(Encoding.UTF8.GetBytes(String(size)), "size", null));
            if (format != null)
                form.Add(new ReqContent(Encoding.UTF8.GetBytes(format), "response_format", null));
            if (user != null)
                form.Add(new ReqContent(Encoding.UTF8.GetBytes(user), "user", null));

            _currentState = RequestState.Requesting;
            this.Message = "Requesting...";

            _sw.Restart();
            POSTAsync(ENDPOINT, form, authToken, timeout);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetBoolean("ShowAdvanced", _advanced);
            return base.Write(writer);
        }
        public override bool Read(GH_IReader reader)
        {
            _advanced = reader.GetBoolean("ShowAdvanced");
            return base.Read(reader);
        }
    }
}