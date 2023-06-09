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
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Brain.OpenAI.Schema;

namespace Brain.OpenAI
{
    public class CreateChatCompletionComponent : GH_Component_HTTPAsync
    {
        private const string ENDPOINT = "https://api.openai.com/v1/chat/completions";
        private const string contentType = "application/json";

        public CreateChatCompletionComponent() :
            base("Create chat completion", "Chat",
                "Creates a model response for the given chat conversation.",
                "Brain", "OpenAI")
        { }
        public override Guid ComponentGuid => new Guid("{42A4BC7B-FEDD-408F-AACB-B30F0045AEE2}");
        public override GH_Exposure Exposure => GH_Exposure.primary;
        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, "Advanced options", AdvancedClicked, true, _advanced);
        }
        readonly List<IGH_Param> advancedParams = new List<IGH_Param>() {
            new Param_String { Name = "Model", NickName = "Model", Description = "ID of the model to use. See the model endpoint compatibility table for details on which models work with the Chat API.", Optional =true },
            new Param_String { Name = "Role", NickName = "Role", Description = "The role of the author of this message. One of system, user, or assistant.", Optional =true },
            new Param_Number { Name = "Temperature", NickName = "Temp", Description = "What sampling temperature to use, between 0 and 2. Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic.\r\n\r\nWe generally recommend altering this or top_p but not both.", Optional=true },
            new Param_Number { Name = "Top_%", NickName = "Top%", Description = "An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are considered.\r\n\r\nWe generally recommend altering this or temperature but not both.", Optional = true },
            new Param_Integer { Name = "Count", NickName = "Count", Description = "The number of images to generate.", Optional=true},
            new Param_String { Name="Stop", NickName="Stop", Description="Up to 4 sequences where the API will stop generating further tokens. ", Optional=true},
            new Param_Integer { Name="Max tokens", NickName="Max tokens", Description="The maximum number of tokens to generate in the chat completion. The total length of input tokens and generated tokens is limited by the model's context length.", Optional=true},
            new Param_Number { Name="Presence penalty", NickName="Presence penalty", Description="Number between -2.0 and 2.0. Positive values penalize new tokens based on whether they appear in the text so far, increasing the model's likelihood to talk about new topics.", Optional=true},
            new Param_Number { Name="Frequency penalty", NickName="Frequency penalty", Description="Number between -2.0 and 2.0. Positive values penalize new tokens based on their existing frequency in the text so far, decreasing the model's likelihood to repeat the same line verbatim.", Optional=true},
            new Param_String { Name="User", NickName="User", Description="A unique identifier representing your end-user, which can help OpenAI to monitor and detect abuse.", Optional=true}
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
            pManager.AddTextParameter("Content", "C", "The contents of the message.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Response", "R", "Request response", GH_ParamAccess.item);
            pManager.AddTextParameter("Options", "O", "Available options", GH_ParamAccess.list);
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
                                choices.Add(choice.message.content);
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

            string model = "gpt-3.5-turbo", role = "user", content = null, user = null, stop = null;
            double? temperature=null, top_p=null, presence_penalty = null, frequency_penalty = null;
            int? n = null, max_tokens=null;
            DA.GetData("Content", ref content);
            if (_advanced)
            {
                DA.GetData("Model", ref model);
                DA.GetData("Role", ref role);
                DA.GetData("Temperature", ref temperature);
                DA.GetData("Top_%", ref top_p);
                DA.GetData("Count", ref n);
                DA.GetData("Stop", ref stop);
                DA.GetData("Max tokens", ref max_tokens);
                DA.GetData("Presence penalty", ref presence_penalty);
                DA.GetData("Frequency penalty", ref frequency_penalty);
                DA.GetData("User", ref user);
            }
            ReqSchema bodyJson = new ReqSchema()
            {
                model = model,
                messages = new Msg[] { new Msg(role, content) },
                temperature = temperature,
                top_p = top_p,
                n = n,
                stop = stop,
                max_tokens = max_tokens,
                presence_penalty = presence_penalty,
                frequency_penalty = frequency_penalty,
                user = user,
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
