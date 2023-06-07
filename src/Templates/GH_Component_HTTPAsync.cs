using Grasshopper.Kernel;
using Rhino;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace Brain.Templates
{
    public abstract class GH_Component_HTTPAsync : GH_Component
    {
        protected string _response = "";
        protected bool _shouldExpire = false;
        protected RequestState _currentState = RequestState.Off;

        public GH_Component_HTTPAsync(string name, string nickname, string description, string category, string subcategory)
    : base(name, nickname, description, category, subcategory)
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Send", "S", "Perform the request?", GH_ParamAccess.item, false);
            pManager.AddTextParameter("Authorization", "A", "If this request requires authorization, input your formatted token as an Auth string, e.g. \"Bearer h1g23g1fdg3d1\"", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Timeout", "T", "Timeout for the request in ms. If the request takes longer that this, it will fail.", GH_ParamAccess.item, 10000);
        }

        protected override void ExpireDownStreamObjects()
        {
            if (_shouldExpire)
            {
                base.ExpireDownStreamObjects();
            }
        }

        protected void POSTAsync(
            string url,
            string body,
            string contentType,
            string authorization,
            int timeout)
        {
            Task.Run(() =>
            {
                try
                {
                    // Compose the request
                    byte[] data = Encoding.ASCII.GetBytes(body);

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.Method = "POST";
                    request.ContentType = contentType;
                    request.ContentLength = data.Length;
                    request.Timeout = timeout;

                    // Handle authorization
                    if (authorization != null && authorization.Length > 0)
                    {
                        System.Net.ServicePointManager.Expect100Continue = true;
                        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12; //the auth type

                        request.PreAuthenticate = true;
                        request.Headers.Add("Authorization", authorization);
                    }
                    else
                    {
                        request.Credentials = CredentialCache.DefaultCredentials;
                    }

                    using (var stream = request.GetRequestStream())
                    {
                        stream.Write(data, 0, data.Length);
                    }
                    var res = request.GetResponse();
                    _response = new StreamReader(res.GetResponseStream()).ReadToEnd();

                    _currentState = RequestState.Done;

                    _shouldExpire = true;
                    RhinoApp.InvokeOnUiThread((Action)delegate { ExpireSolution(true); });
                }
                catch (Exception ex)
                {
                    _response = ex.Message;

                    _currentState = RequestState.Error;

                    _shouldExpire = true;
                    RhinoApp.InvokeOnUiThread((Action)delegate { ExpireSolution(true); });

                    return;
                }
            });
        }


        protected void GETAsync(
            string url,
            string authorization,
            int timeout)
        {
            Task.Run(() =>
            {
                try
                {
                    // Compose the request
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.Method = "GET";
                    request.Timeout = timeout;

                    // Handle authorization
                    if (authorization != null && authorization.Length > 0)
                    {
                        System.Net.ServicePointManager.Expect100Continue = true;
                        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12; //the auth type

                        request.PreAuthenticate = true;
                        request.Headers.Add("Authorization", authorization);
                    }
                    else
                    {
                        request.Credentials = CredentialCache.DefaultCredentials;
                    }

                    var res = request.GetResponse();
                    _response = new StreamReader(res.GetResponseStream()).ReadToEnd();

                    _currentState = RequestState.Done;

                    _shouldExpire = true;
                    RhinoApp.InvokeOnUiThread((Action)delegate { ExpireSolution(true); });
                }
                catch (Exception ex)
                {
                    _response = ex.Message;

                    _currentState = RequestState.Error;

                    _shouldExpire = true;
                    RhinoApp.InvokeOnUiThread((Action)delegate { ExpireSolution(true); });

                    return;
                }
            });
        }
    }
}
