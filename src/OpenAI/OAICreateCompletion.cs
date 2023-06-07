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
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Brain.OpenAI
{
    public class OAICreateCompletion : GH_Component_HTTPAsync
    {
        public class CreateCompletion
        {
            public Choice[] choices { get; set; }
        }
        public class Choice
        {
            public string text { get; set; }
        }
        public class ReqBody
        {
            public string model { get; set; }
            public string prompt { get; set; }
            public string suffix { get; set; }
            public int? max_tokens { get; set; }
            public double? temperature { get; set; }
            public double? top_p { get; set; }
            public int? n { get; set; }
            public int? logprobs { get; set; }
            public bool? echo { get; set; }
            public string stop { get; set; }
            public double? presence_penalty { get; set; }
            public double? frequency_penalty { get; set; }
            public int? best_of { get; set; }
            public string user { get; set; }
        }

        private const string ENDPOINT = "https://api.openai.com/v1/completions";
        private const string contentType = "application/json";
        public bool advanced = false;
        Stopwatch sw = new Stopwatch();

        public OAICreateCompletion() :
            base("Create completion", "Completion",
                "Creates a completion for the provided prompt and parameters.",
                "Brain", "OpenAI")
        { }
        public override Guid ComponentGuid => new Guid("{323C02ED-AEE0-4665-9981-21AFC439E2F6}");
        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, "Advanced options", AdvancedClicked, true, advanced);
        }
        readonly List<IGH_Param> advancedParams = new List<IGH_Param>() {
            new Param_String { Name = "Model", NickName = "Model", Description = "ID of the model to use. You can use the List models API to see all of your available models, or see our Model overview for descriptions of them.", Optional =true },
            new Param_String { Name = "Suffix", NickName = "Suffix", Description = "The suffix that comes after a completion of inserted text.", Optional =true },
            new Param_Integer { Name="Max tokens", NickName="Max tokens", Description="The maximum number of tokens to generate in the completion. The token count of your prompt plus max_tokens cannot exceed the model's context length.", Optional=true},
            new Param_Number { Name = "Temperature", NickName = "Temp", Description = "What sampling temperature to use, between 0 and 2. Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic.\r\n\r\nWe generally recommend altering this or top_p but not both.", Optional=true },
            new Param_Number { Name = "Top_%", NickName = "Top%", Description = "An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are considered.\r\n\r\nWe generally recommend altering this or temperature but not both.", Optional = true },
            new Param_Integer { Name = "Count", NickName = "Count", Description = "How many completions to generate for each prompt.\r\n\r\nNote: Because this parameter generates many completions, it can quickly consume your token quota. Use carefully and ensure that you have reasonable settings for max_tokens and stop.", Optional=true},
            new Param_Integer { Name = "Logprobs", NickName = "Logprobs", Description = "Include the log probabilities on the logprobs most likely tokens, as well the chosen tokens. For example, if logprobs is 5, the API will return a list of the 5 most likely tokens. The API will always return the logprob of the sampled token, so there may be up to logprobs+1 elements in the response.\r\n\r\nThe maximum value for logprobs is 5.", Optional=true},
            new Param_Boolean { Name = "Echo", NickName = "Echo", Description = "Echo back the prompt in addition to the completion", Optional=true},
            new Param_String { Name="Stop", NickName="Stop", Description="Up to 4 sequences where the API will stop generating further tokens. ", Optional=true},
            new Param_Number { Name="Presence penalty", NickName="Presence penalty", Description="Number between -2.0 and 2.0. Positive values penalize new tokens based on whether they appear in the text so far, increasing the model's likelihood to talk about new topics.", Optional=true},
            new Param_Number { Name="Frequency penalty", NickName="Frequency penalty", Description="Number between -2.0 and 2.0. Positive values penalize new tokens based on their existing frequency in the text so far, decreasing the model's likelihood to repeat the same line verbatim.", Optional=true},
            new Param_Integer { Name="Best of", NickName="Best of", Description="Generates best_of completions server-side and returns the \"best\" (the one with the highest log probability per token). Results cannot be streamed.\r\n\r\nWhen used with n, best_of controls the number of candidate completions and n specifies how many to return – best_of must be greater than n.\r\n\r\nNote: Because this parameter generates many completions, it can quickly consume your token quota. Use carefully and ensure that you have reasonable settings for max_tokens and stop.", Optional=true},
            new Param_String { Name="User", NickName="User", Description="A unique identifier representing your end-user, which can help OpenAI to monitor and detect abuse.", Optional=true}
        };
        private void AdvancedClicked(object sender, EventArgs e)
        {
            RecordUndoEvent("Toggle Advanced options");
            advanced = !advanced;
            if (advanced)
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
            pManager.AddTextParameter("Prompt", "P", "The prompt to generate completions for, encoded as a string", GH_ParamAccess.item);
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
                sw.Stop();
                List<string> choices = new List<string>();
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
                            var resJson = JsonSerializer.Deserialize<CreateCompletion>(_response);
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
            string model = "text-davinci-003", prompt = null, suffix = null, stop=null, user=null;
            int? n = null, max_tokens=null, logprobs = null, best_of=null;
            double? temperature = null, top_p = null, presence_penalty = null, frequency_penalty = null;
            bool? echo = null;
            DA.GetData("Prompt", ref prompt);
            if (advanced)
            {
                DA.GetData("Model", ref model);
                DA.GetData("Suffix", ref suffix);
                DA.GetData("Max tokens", ref max_tokens);
                DA.GetData("Temperature", ref temperature);
                DA.GetData("Top_%", ref top_p);
                DA.GetData("Count", ref n);
                DA.GetData("Logprobs", ref logprobs);
                DA.GetData("Echo", ref echo);
                DA.GetData("Stop", ref stop);
                DA.GetData("Presence penalty", ref presence_penalty);
                DA.GetData("Frequency penalty", ref frequency_penalty);
                DA.GetData("Best of", ref best_of);
                DA.GetData("User", ref user);
            }
            ReqBody bodyJson = new ReqBody()
            {
                model = model,
                prompt = prompt,
                suffix = suffix,
                max_tokens  = max_tokens,
                temperature = temperature,
                top_p = top_p,
                n = n,
                logprobs = logprobs,
                echo = echo,
                stop = stop,
                presence_penalty = presence_penalty,
                frequency_penalty = frequency_penalty,
                best_of = best_of,
                user = user
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
    }
}
