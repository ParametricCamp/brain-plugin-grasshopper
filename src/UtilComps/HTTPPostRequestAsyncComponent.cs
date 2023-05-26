using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Brain.Templates;
using Grasshopper.GUI.Script;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;

namespace Brain.UtilComps
{
    public class HTTPPostRequestAsyncComponent : GH_Component
    {
        private string _response = "";
        private bool _shouldExpire = false;
        private RequestState _currentState = RequestState.Off;

        /// <summary>
        /// Initializes a new instance of the HTTPPostRequestAsyncComponent class.
        /// </summary>
        public HTTPPostRequestAsyncComponent()
          : base("HTTP POST (async)", "POST Async",
              "Creates a generic HTTP POST request (asynchronous)",
              "Brain", "Utils")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            // active
            pManager.AddBooleanParameter("Send", "S", "Perform the request?", GH_ParamAccess.item, false);
            // url (endpoint)
            pManager.AddTextParameter("Url", "U", "Url for the request", GH_ParamAccess.item);
            // body
            pManager.AddTextParameter("Body", "B", "Body of the request", GH_ParamAccess.item);
            // context/type
            pManager.AddTextParameter("Content Type", "T", "Content type for the request, such as \"application/json\", \"text/html\", etc.", GH_ParamAccess.item, "application/json");

            // custom headers (future)
            // custom headers would be nice here: how to handle key-value pairs in GH? takes in a tree?

            // auth 
            int authId = pManager.AddTextParameter("Authorization", "A", "If this request requires authorization, input your formatted token as an Auth string, e.g. \"Bearer h1g23g1fdg3d1\"", GH_ParamAccess.item);
            // timeout
            pManager.AddIntegerParameter("Timeout", "T", "Timeout for the request in ms. If the request takes longer that this, it will fail.", GH_ParamAccess.item, 10000);

            pManager[authId].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Response", "R", "Request response", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (_shouldExpire)
            {
                switch(_currentState)
                {
                    case RequestState.Off:
                        this.Message = "Inactive";
                        _currentState = RequestState.Idle;
                        break;

                    case RequestState.Error:
                        this.Message = "ERROR";
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, _response);
                        _currentState = RequestState.Idle;
                        break;

                    case RequestState.Done:
                        this.Message = "Complete!";
                        _currentState = RequestState.Idle;
                        break;
                }
                // Output
                DA.SetData(0, _response);
                _shouldExpire = false;
                return;
            }

            bool active = false;
            string url = "";
            string body = "";
            string contentType = "";
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

            if (!DA.GetData("Url", ref url)) return;
            if (!DA.GetData("Body", ref body)) return;
            if (!DA.GetData("Content Type", ref contentType)) return;
            DA.GetData("Authorization", ref authToken);
            if (!DA.GetData("Timeout", ref timeout)) return;

            // Validity checks
            if (url == null || url.Length == 0)
            {
                _response = "Empty URL";
                _currentState = RequestState.Error;
                _shouldExpire = true;
                ExpireSolution(true);
                return;
            }
            if (contentType == null || contentType.Length == 0)
            {
                _response = "Empty content type";
                _currentState = RequestState.Error;
                _shouldExpire = true;
                ExpireSolution(true);
                return;
            }

            _currentState = RequestState.Requesting;
            this.Message = "Requesting...";

            AsyncPOST(url, body, contentType, authToken, timeout);  
        }

        protected override void ExpireDownStreamObjects()
        {
            if (_shouldExpire)
            { 
                base.ExpireDownStreamObjects();
            }
        }

        private void AsyncPOST(
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

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("97B12696-7321-423E-BCF6-56486645DE15"); }
        }
    }
}