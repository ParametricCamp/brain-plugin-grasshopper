using Brain.Templates;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Brain.OpenAI.Schema;

namespace Brain.OpenAI
{
    public class CreateTranscriptionComponent : GH_Component_HTTPAsync_Form
    {
        const string ENDPOINT = "https://api.openai.com/v1/audio/transcriptions";
        public string format = null;
        public CreateTranscriptionComponent() : base("Create Transcription", "Transcribes", "Transcribes audio into the input language.", "Brain", "OpenAI")
        {
        }

        public override Guid ComponentGuid => new Guid("{FA9C0AD7-AD05-4910-A01A-42FDE0856D48}");
        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, "Advanced options", AdvancedClicked, true, _advanced);
        }
        readonly List<IGH_Param> advancedParams = new List<IGH_Param>() {
            new Param_String { Name = "Model", NickName = "Model", Description = "ID of the model to use. As of June.9th, 2023, only whisper-1 is currently available.", Optional =true },
            new Param_String { Name = "Prompt", NickName = "Prompt", Description = "An optional text to guide the model's style or continue a previous audio segment. The prompt should match the audio language.", Optional =true },
            new Param_String { Name = "Format", NickName = "Format", Description = "The format of the transcript output, in one of these options: json, text, srt, verbose_json, or vtt.", Optional =true },
            new Param_Number { Name = "Temperature", NickName = "Temperature", Description = "The sampling temperature, between 0 and 1. Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic. If set to 0, the model will use log probability to automatically increase the temperature until certain thresholds are hit.", Optional =true },
            new Param_String { Name = "Language", NickName = "Language", Description = "The language of the input audio. Supplying the input language in ISO-639-1 format will improve accuracy and latency.", Optional = true },
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
            pManager.AddTextParameter("File Path", "F", "The audio file to transcribe, in one of these formats: mp3, mp4, mpeg, mpga, m4a, wav, or webm.", GH_ParamAccess.item);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Response", "R", "Request response", GH_ParamAccess.item);
            pManager.AddTextParameter("JSON", "J", "Transcription in JSON format", GH_ParamAccess.item);
            pManager.AddTextParameter("Text", "T", "Transcription in text format", GH_ParamAccess.item);
            pManager.AddTextParameter("SRT", "S", "Transcription in srt format", GH_ParamAccess.item);
            pManager.AddTextParameter("vJSON", "VJ", "Transcription in verbose JSON format", GH_ParamAccess.item);
            pManager.AddTextParameter("VTT", "V", "Transcription in vtt format", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.DisableGapLogic();
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
                            switch (format)
                            {
                                case "json":
                                    DA.SetData(1, _response);
                                    break;
                                case "srt":
                                    DA.SetData(3, _response);
                                    break;
                                case "verbose_json":
                                    DA.SetData(4, _response);
                                    break;
                                case "vtt":
                                    DA.SetData(5, _response);
                                    break;
                                case "text":
                                default:
                                    DA.SetData(2, _response);
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
            string filePath = null, model = "whisper-1", prompt = null, language = null;
            double? temperature = null;
            DA.GetData("File Path", ref filePath);
            if (_advanced)
            {
                DA.GetData("Model", ref model);
                DA.GetData("Prompt", ref prompt);
                DA.GetData("Format", ref format);
                DA.GetData("Temperature", ref temperature);
                DA.GetData("Language", ref language);
            }
            List<ReqContent> form = new List<ReqContent>
            {
                new ReqContent(File.ReadAllBytes(filePath), "file", Path.GetFileName(filePath)),
                new ReqContent(Encoding.UTF8.GetBytes(model), "model", null),
            };
            if (prompt != null)
                form.Add(new ReqContent(Encoding.UTF8.GetBytes(prompt), "prompt", null));
            if (format != null)
                form.Add(new ReqContent(Encoding.UTF8.GetBytes(format), "response_format", null));
            if (temperature != null)
                form.Add(new ReqContent(Encoding.UTF8.GetBytes(temperature.ToString()), "temperature", null));
            if (language != null)
                form.Add(new ReqContent(Encoding.UTF8.GetBytes(language), "language", null));

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
