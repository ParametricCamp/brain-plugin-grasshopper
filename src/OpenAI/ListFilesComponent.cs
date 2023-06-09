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
    public class ListFilesComponent : GH_Component_HTTPAsync
    {//
        private const string ENDPOINT = "https://api.openai.com/v1/files";

        public ListFilesComponent() :
            base("List Files", "Files",
                "Returns a list of files that belong to the user's organization.",
                "Brain", "OpenAI")
        { }
        public override Guid ComponentGuid => new Guid("{AB609A22-E21F-4F9C-989C-375D46DF4810}");
        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        // Basic inputs implemented in Base

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Response", "R", "Request response", GH_ParamAccess.item);
            pManager.AddTextParameter("Files", "F", "Files that belong to the user's organization.", GH_ParamAccess.list);
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

                        try
                        {
                            var resJson = JsonSerializer.Deserialize<DataSchema>(_response);
                            Data[] fileList = resJson.data;
                            foreach (var file in fileList)
                                files.Add(file.filename);
                        }
                        catch (Exception ex)
                        {
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Something went wrong deserializing the response: " + ex.Message);
                        }

                        break;
                }

                // Output...
                DA.SetData(0, _response);
                DA.SetDataList(1, files);
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

            _currentState = RequestState.Requesting;
            this.Message = "Requesting...";

            _sw.Restart();
            GETAsync(ENDPOINT, authToken, timeout);
        }
    }
}
