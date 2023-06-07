using Brain.Templates;
using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Brain.OpenAI
{
    public class OAIListModelsComponent : GH_Component_HTTPAsync
    {
        public class ListModels
        {
            public Model[] data { get; set; }
        }
        public class Model
        {
            public string id { get; set; }
        }

        private const string ENDPOINT = "https://api.openai.com/v1/models";
        Stopwatch sw = new Stopwatch();
        public OAIListModelsComponent() :
            base("List Models", "Models",
                "Lists the currently available models, and provides basic information about each one such as the owner and availability.",
                "Brain", "OpenAI")
        { }
        public override Guid ComponentGuid => new Guid("{11844CDA-FDDC-4FF1-B9CD-ABA68480427F}");

        // Basic inputs implemented in Base

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Response", "R", "Request response", GH_ParamAccess.item);
            pManager.AddTextParameter("Models", "M", "Available models", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (_shouldExpire)
            {
                sw.Stop();
                List<string> models = new List<string>();
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
                            var resJson = JsonSerializer.Deserialize<ListModels>(_response);
                            Model[] modelList = resJson.data;
                            foreach (var model in modelList)
                                models.Add(model.id);
                        }
                        catch (Exception ex)
                        {
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Something went wrong deserializing the response: " + ex.Message);
                        }

                        break;
                }

                // Output...
                DA.SetData(0, _response);
                DA.SetDataList(1, models);
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

            sw.Restart();
            GETAsync(ENDPOINT, authToken, timeout);
        }
    }
}
