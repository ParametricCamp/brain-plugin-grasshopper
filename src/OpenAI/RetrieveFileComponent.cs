using Brain.Templates;
using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static Brain.OpenAI.Schema;

namespace Brain.OpenAI
{
    public class RetrieveFileComponent : GH_Component_HTTPAsync
    {//
        private const string ENDPOINT = "https://api.openai.com/v1/files";

        public RetrieveFileComponent() :
            base("Retrieve Files", "File",
                "Returns information about a specific file.",
                "Brain", "OpenAI")
        { }
        public override Guid ComponentGuid => new Guid("{3D942897-E56B-489A-8258-96A727A3A4B7}");
        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            base.RegisterInputParams(pManager);
            pManager.AddTextParameter("ID", "ID", "The ID of the file to use for this request", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Response", "R", "Request response", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (_shouldExpire)
            {
                _sw.Stop();
                List<string> files = new List<string>();
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
                        break;
                }

                // Output...
                DA.SetData(0, _response);
                _shouldExpire = false;
                return;
            }

            bool active = false;
            string authToken = "";
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

            _currentState = RequestState.Requesting;
            this.Message = "Requesting...";

            _sw.Restart();
            GETAsync(ENDPOINT+id, authToken, timeout);
        }
    }
}
