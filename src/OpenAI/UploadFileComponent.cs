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

namespace Brain.OpenAI
{
    public class UploadFileComponent : GH_Component_HTTPAsync_Form
    {
        private const string ENDPOINT = "https://api.openai.com/v1/files";

        public UploadFileComponent() :
            base("Upload file", "Upload",
                "Upload a file that contains document(s) to be used across various endpoints/features. Currently, the size of all the files uploaded by one organization can be up to 1 GB. Please contact us if you need to increase the storage limit.",
                "Brain", "OpenAI")
        { }
        public override Guid ComponentGuid => new Guid("{00DB5AED-0451-4430-AAA7-F77E24A89E03}");
        public override GH_Exposure Exposure => GH_Exposure.quarternary;


        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            base.RegisterInputParams(pManager);
            pManager.AddGenericParameter("File Path", "F", "Name of the JSON Lines file to be uploaded.\r\n\r\nIf the purpose is set to \"fine-tune\", each line is a JSON record with \"prompt\" and \"completion\" fields representing your training examples.", GH_ParamAccess.item);
            pManager.AddTextParameter("Purpose", "P", "The intended purpose of the uploaded documents.\r\n\r\nUse \"fine-tune\" for Fine-tuning. This allows us to validate the format of the uploaded file.", GH_ParamAccess.item);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Response", "R", "Request response", GH_ParamAccess.item);
            pManager.AddTextParameter("ID", "ID", "ID of uploaded file", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (_shouldExpire)
            {
                _sw.Stop();
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
                            var resJson = JsonSerializer.Deserialize<Data>(_response);
                            DA.SetData(1, resJson.id);
                        }
                        catch (Exception ex)
                        {
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Something went wrong deserializing the response: " + ex.Message);
                        }

                        break;
                }
                // Output
                DA.SetData(0, _response);
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
            string file = null, purpose = null;
            DA.GetData("File Path", ref file);
            DA.GetData("Purpose", ref purpose);
            List<ReqContent> form = new List<ReqContent>
            {
                new ReqContent(File.ReadAllBytes(file), "file", Path.GetFileName(file)),
                new ReqContent(Encoding.UTF8.GetBytes(purpose), "purpose", null)
            };
            
            _currentState = RequestState.Requesting;
            this.Message = "Requesting...";

            _sw.Restart();
            POSTAsync(ENDPOINT, form, authToken, timeout);
        }
    }
}
