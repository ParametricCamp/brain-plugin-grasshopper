using Brain.Templates;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Brain.OpenAI
{
    public class OAICreateImage : GH_Component_HTTPAsync
    {
        public class CreateImage
        {
            public Data[] data { get; set; }
        }
        public class Data
        {
            public string url { get; set; }
            public string b64_json { get; set; }
        }
        public class ReqBody
        {
            public string prompt { get; set; }
            public int? n { get; set; }
            public string size { get; set; }
            public string response_format { get; set; }
            public string user { get; set; }
        }
        private const string ENDPOINT = "https://api.openai.com/v1/images/generations";
        private const string contentType = "application/json";
        public string format = null;
        public bool advanced = false;
        Stopwatch sw = new Stopwatch();

        public OAICreateImage() :
            base("Create image", "Image",
                "Creates an image given a prompt.",
                "Brain", "OpenAI")
        { }
        public override Guid ComponentGuid => new Guid("{D3E4C347-2D1C-4BEF-A249-54BBC2D08109}");

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, "Advanced options", AdvancedClicked, true, advanced);
        }
        readonly List<IGH_Param> advancedParams = new List<IGH_Param>() {
            new Param_Integer { Name = "Size", NickName = "Size", Description = "The size of the generated square images. Must be one of 256, 512, or 1024.", Optional = true },
            new Param_String { Name = "Format", NickName = "Format", Description = "The format in which the generated images are returned. Must be one of url or b64_json", Optional = true },
            new Param_Integer { Name = "Count", NickName = "Count", Description = "The number of images to generate. Must be between 1 and 10.", Optional = true },
            new Param_String { Name = "User", NickName = "User", Description = "A unique identifier representing your end-user, which can help OpenAI to monitor and detect abuse.", Optional = true },
        };
        private void AdvancedClicked(object sender, EventArgs e)
        {
            RecordUndoEvent("Toggle Advanced options");
            advanced = !advanced;
            if (advanced)
            {
                foreach(var param in advancedParams)
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
            pManager.AddTextParameter("Prompt", "P", "A text description of the desired image(s). The maximum length is 1000 characters.", GH_ParamAccess.item);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Response", "R", "Request response", GH_ParamAccess.item);
            pManager.AddTextParameter("URL", "U", "Image URL", GH_ParamAccess.list);
            pManager.AddTextParameter("Base64", "B", "Image encoded as Base64", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (_shouldExpire)
            {
                sw.Stop();
                List<string> url = new List<string>();
                List<string> b64 = new List<string>();
                switch (_currentState)
                {
                    case RequestState.Off:
                        this.Message = "Inactive";
                        _currentState = RequestState.Idle;
                        break;

                    case RequestState.Error:
                        this.Message = $"ERROR\r\n{sw.Elapsed.ToShortString()}";
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, _response);
                        _currentState = RequestState.Idle;
                        break;

                    case RequestState.Done:
                        this.Message = $"Complete!\r\n{sw.Elapsed.ToShortString()}";
                        _currentState = RequestState.Idle;

                        try
                        {
                            var resJson = JsonSerializer.Deserialize<CreateImage>(_response);
                            Data[] data = resJson.data;
                            switch (format)
                            {                                
                                case "b64_json":
                                    foreach (var image in data)
                                        b64.Add(image.b64_json);
                                    break;
                                case "url":
                                default:
                                    WebClient client = new WebClient();
                                    foreach (var image in data)
                                        url.Add(image.url);
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
            string prompt = null;
            format = null;
            int? size = 256, n = null;
            string user = null;
            bool advanced = false;
            DA.GetData("Prompt", ref prompt);
            if (advanced)
            {
                DA.GetData("Size", ref size);
                DA.GetData("Format", ref format);
                DA.GetData("Count", ref n);
                DA.GetData("User", ref user);
            }
            ReqBody bodyJson = new ReqBody()
            {
                prompt = prompt,
                n = n,
                size = size == null ? null : String(size),
                response_format = format,
                user = user,
            };

            _currentState = RequestState.Requesting;
            this.Message = "Requesting...";

            string body = JsonSerializer.Serialize(bodyJson, new JsonSerializerOptions() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });

            sw.Restart();
            POSTAsync(ENDPOINT, body, contentType, authToken, timeout);
        }
        public override bool Write(GH_IWriter writer)
        {
            writer.SetBoolean("ShowAdvanced", advanced);
            return base.Write(writer);
        }
        public override bool Read(GH_IReader reader)
        {
            advanced = reader.GetBoolean("ShowAdvanced");
            return base.Read(reader);
        }
        private string String(int? size) => string.Concat(size,'x',size);
    }
}
