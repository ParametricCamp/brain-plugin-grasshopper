using Brain.Templates;
using GH_IO.Serialization;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using static Brain.OpenAI.Schema;
using static Brain.UtilMethods;
using Grasshopper.Kernel.Types;

// REF: https://github.com/DavidDeSimone/AIAssetGeneration/blob/main/Packages/com.recursiverhapsody.aiassetgenerator/Editor/Scripts/ImageEditOpenAIRequest.cs
// REF: https://github.com/Unity-Technologies/UnityCsReference/blob/916398adce75c6d575b0be1b00c1801599a0733f/Modules/UnityWebRequest/Public/WebRequestUtils.cs#L18
// REF: https://github.com/Unity-Technologies/UnityCsReference/blob/916398adce75c6d575b0be1b00c1801599a0733f/Modules/UnityWebRequest/Public/WebRequestExtensions.cs#L15

namespace Brain.OpenAI
{
    public class CreateImageEditComponent : GH_Component_HTTPAsync_Form
    {
        private const string ENDPOINT = "https://api.openai.com/v1/images/edits";
        public string format = null;

        public CreateImageEditComponent() :
            base("Create image edit", "ImageEdit",
                "Creates an edited or extended image given an original image and a prompt.",
                "Brain", "OpenAI")
        { }
        public override Guid ComponentGuid => new Guid("{A7AC1FEA-546E-4BE4-AF67-609C3170617E}");
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
            pManager.AddGenericParameter("Image", "I", "bitmap object or file path\r\n\r\nThe image to edit. Must be a valid PNG file, less than 4MB, and square. If mask is not provided, image must have transparency, which will be used as the mask.", GH_ParamAccess.item);
            int i = pManager.AddGenericParameter("Mask", "M", "bitmap object or file path\r\n\r\nAn additional image whose fully transparent areas (e.g. where alpha is zero) indicate where image should be edited. Must be a valid PNG file, less than 4MB, and have the same dimensions as image", GH_ParamAccess.item);
            pManager.AddTextParameter("Prompt", "P", "A text description of the desired image(s). The maximum length is 1000 characters.", GH_ParamAccess.item);
            pManager[i].Optional = true;
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
                                default:
                                case "url":
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
            string prompt = null, user = null;
            format = null;
            int? size = 256, n = null;
            GH_ObjectWrapper image, mask;
            image = mask = new GH_ObjectWrapper();
            DA.GetData("Image", ref image);
            DA.GetData("Mask", ref mask);
            DA.GetData("Prompt", ref prompt);
            if (_advanced)
            {
                DA.GetData("Count", ref n);
                DA.GetData("Size", ref size);
                DA.GetData("Format", ref format);
                DA.GetData("User", ref user);
            }
            List<ReqContent> form = new List<ReqContent>
            {
                new ReqContent(Encoding.UTF8.GetBytes(prompt), "prompt", null)
            };
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
            if (mask != null)
            {
                if (mask.Value is Bitmap)
                {
                    var maskBitmap = mask.Value as Bitmap;
                    form.Add(new ReqContent(Bitmap2Bytes(maskBitmap), "mask", BitmapName(maskBitmap)));
                }
                else
                {
                    var maskPath = (mask.Value as GH_String).ToString();
                    form.Add(new ReqContent(File.ReadAllBytes(maskPath), "mask", Path.GetFileName(maskPath)));
                }
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
