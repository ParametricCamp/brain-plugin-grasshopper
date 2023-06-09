using Brain.Templates;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Brain.OpenAI.Schema;

namespace Brain.OpenAI
{
    public class CreateEditComponent : GH_Component_HTTPAsync
    {
        private const string ENDPOINT = "https://api.openai.com/v1/edits";
        private const string contentType = "application/json";

        public CreateEditComponent() :
            base("Create Edit", "Edit",
                "Creates a new edit for the provided input, instruction, and parameters.",
                "Brain", "OpenAI")
        { }
        public override Guid ComponentGuid => new Guid("{F40FAEB4-6A38-43E8-863C-E504B31230FA}");
        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, "Advanced options", AdvancedClicked, true, _advanced);
        }
        readonly List<IGH_Param> advancedParams = new List<IGH_Param>() {
            new Param_String { Name = "Model", NickName = "Model", Description = "ID of the model to use. You can use the text-davinci-edit-001 or code-davinci-edit-001 model with this endpoint.", Optional =true },
            new Param_Integer { Name = "Count", NickName = "Count", Description = "The number of images to generate.", Optional=true},
            new Param_Number { Name = "Temperature", NickName = "Temp", Description = "What sampling temperature to use, between 0 and 2. Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic.\r\n\r\nWe generally recommend altering this or top_p but not both.", Optional=true },
            new Param_Number { Name = "Top_%", NickName = "Top%", Description = "An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are considered.\r\n\r\nWe generally recommend altering this or temperature but not both.", Optional = true },
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
            int i  = pManager.AddTextParameter("Input", "in", "The input text to use as a starting point for the edit.", GH_ParamAccess.item);
            pManager.AddTextParameter("Instruction", "i", "The instruction that tells the model how to edit the prompt.", GH_ParamAccess.item);
            pManager[i].Optional = true;
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Response", "R", "Request response", GH_ParamAccess.item);
            pManager.AddTextParameter("Result", "r", "Result", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (_shouldExpire)
            {
                _sw.Stop();
                List<string> choices = new List<string>();
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
                            var resJson = JsonSerializer.Deserialize<ChoicesSchema>(_response);
                            Choice[] choiceList = resJson.choices;
                            foreach (var choice in choiceList)
                                choices.Add(choice.text);
                        }
                        catch (Exception ex)
                        {
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Something went wrong deserializing the response: " + ex.Message);
                        }

                        break;
                }
                // Output
                DA.SetData(0, _response);
                DA.SetDataList(1, choices);
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

            string model = "text-davinci-edit-001";
            string input = null;
            string instruction = null;
            int? n = null;
            double? temperature = null, top_p = null;
            DA.GetData("Input", ref input);
            DA.GetData("Instruction", ref instruction);
            if (_advanced)
            {
                DA.GetData("Model", ref model);
                DA.GetData("Count", ref n);
                DA.GetData("Temperature", ref temperature);
                DA.GetData("Top_%", ref top_p);
            }
            ReqSchema bodyJson = new ReqSchema()
            {
                model = model,
                input = input,
                instruction = instruction,
                n = n,
                temperature = temperature,
                top_p = top_p,
            };

            _currentState = RequestState.Requesting;
            this.Message = "Requesting...";

            string body = JsonSerializer.Serialize(bodyJson, new JsonSerializerOptions() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });

            _sw.Restart();
            POSTAsync(ENDPOINT, body, contentType, authToken, timeout);
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
