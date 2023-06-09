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
    public class DeleteFileComponent : GH_Component_HTTPAsync_Form
    {
        private const string ENDPOINT = "https://api.openai.com/v1/files/";

        public DeleteFileComponent() :
            base("Delete file", "Delete",
                "Delete a file.",
                "Brain", "OpenAI")
        { }
        public override Guid ComponentGuid => new Guid("{366277B3-31DE-4BBB-8BE9-8FD1CEB026A2}");
        public override GH_Exposure Exposure => GH_Exposure.quarternary;


        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            base.RegisterInputParams(pManager);
            pManager.AddTextParameter("ID", "ID", "The ID of the file to use for this request", GH_ParamAccess.item);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Response", "R", "Request response", GH_ParamAccess.item);
            pManager.AddTextParameter("Success", "S", "Success of operation", GH_ParamAccess.item);
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
                            DA.SetData(1, resJson.deleted);
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
            string id = null;
            DA.GetData("ID", ref id);
            List<ReqContent> form = new List<ReqContent> {};
            
            _currentState = RequestState.Requesting;
            this.Message = "Requesting...";

            _sw.Restart();
            POSTAsync(ENDPOINT+id, form, authToken, timeout,true);
        }
    }
}
