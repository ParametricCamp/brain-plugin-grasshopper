using Grasshopper.Kernel;
using Rhino;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static Brain.OpenAI.Schema;

namespace Brain.Templates
{
    public abstract class GH_Component_HTTPAsync_Form : GH_Component_HTTPAsync
    {
        public GH_Component_HTTPAsync_Form(string name, string nickname, string description, string category, string subcategory)
    : base(name, nickname, description, category, subcategory)
        {
        }

        protected void POSTAsync(
            string url,
            List<ReqContent> form,
            string authorization,
            int timeout,
            [Optional]bool X)
        {
            Task.Run(async () =>
            {
                try
                {
                    var formData = new MultipartFormDataContent() { };
                    foreach(var obj in  form)
                    {
                        var content = new ByteArrayContent(obj.byteData);
                        if (obj.fileName==null)
                            formData.Add(content, obj.fieldName);
                        else
                            formData.Add(content, obj.fieldName, obj.fileName);
                    }
                    //byte[] binaryData = File.ReadAllBytes(form.image);
                    //var binaryContent = new ByteArrayContent(binaryData);
                    //formData.Add(binaryContent, "image", Path.GetFileName(form.image));
                    //byte[] bd = Encoding.UTF8.GetBytes(form.prompt);
                    //var promptContent = new ByteArrayContent(bd);
                    //formData.Add(promptContent, "prompt");

                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Content = formData;

                    //request.Headers
                    using (var client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("Authorization", authorization);
                        HttpResponseMessage response = null;
                        if (!X)
                            response = await client.SendAsync(request);
                        else
                        {
                            response = await client.DeleteAsync(url);
                        }
                        _response = await response.Content.ReadAsStringAsync();
                        if (response.StatusCode == HttpStatusCode.OK)
                            _currentState = RequestState.Done;
                        else
                        {
                            RootObject root = JsonSerializer.Deserialize<RootObject>(_response);
                            _response = root.error.message;
                            _currentState = RequestState.Error;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _response = ex.Message;
                    _currentState = RequestState.Error;
                }
                _shouldExpire = true;
                RhinoApp.InvokeOnUiThread((Action)delegate { ExpireSolution(true); });

                return;
            });
        }
        public class Error
        {
            public string message { get; set; }
        }

        public class RootObject
        {
            public Error error { get; set; }
        }
    }
}
