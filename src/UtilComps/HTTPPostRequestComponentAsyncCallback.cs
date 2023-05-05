using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;

namespace Brain.Utilities
{
    public class HTTPPostRequestComponentAsyncCallback : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the HTTPPostRequestComponent class.
        /// </summary>
        public HTTPPostRequestComponentAsyncCallback()
          : base("HTTPPostRequestComponentAsync", "POST Async",
              "Create an async generic HTTP POST request (Callback)",
              "Brain", "Utils")
        {
            ExpireSolutionWrapper = () =>
            {
                RhinoApp.InvokeOnUiThread((Action)delegate { ExpireSolution(true); });
            };
        }
        enum RequestState
        {
            Off,
            Idle,
            Requesting,
            Done,
            Error
        }
        RequestState currentState = RequestState.Idle;
        string response = "";

        bool outputResult = false;
        Action ExpireSolutionWrapper;


        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            // url
            // body
            // content/type
            // custom headers (not jet)
            // auth --> 
            // timeout

            pManager.AddBooleanParameter("Send", "S", "Perform the request?", GH_ParamAccess.item, false);
            pManager.AddTextParameter("Url", "U", "Url for the request", GH_ParamAccess.item);
            pManager.AddTextParameter("Body", "B", "Body of the request", GH_ParamAccess.item);
            pManager.AddTextParameter("Content Type", "T", "Content type for the request, such as \"application/json\", \"text/html\", etc.", GH_ParamAccess.item, "application/json");
            // custom headers would be nice here: how to handle key-value pairs in GH? takes in a tree?
            int authId = pManager.AddTextParameter("Authorization", "A", "If this request requires authorization, input your formatted token as an Auth string, e.g. \"Bearer h1g23g1fdg3d1\"", GH_ParamAccess.item);
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
            // If true, this should be the second time `SolveInstance` was invoked, 
            // after downstream objects were expired. If so, set buffered output
            // and flag everything back to off/idle.
            if (outputResult)
            {
                switch (currentState)
                {
                    case RequestState.Off:
                        this.Message = "Inactive";
                        DA.SetData(0, "");
                        currentState = RequestState.Idle;
                        break;
                    case RequestState.Done:
                        this.Message = "Complete";
                        DA.SetData(0, response);
                        currentState = RequestState.Idle;
                        break;
                    case RequestState.Error:
                        this.Message = "ERROR";
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, response);
                        currentState = RequestState.Idle;
                        //DA.SetData(0, "");  // no empty string, output null on error
                        break;
                }

                // Do not expire downstream again after this... 
                outputResult = false;

                return;
            }

            // If here, first instance of `SolveInstance`
            bool active = false;
            DA.GetData("Send", ref active);
            if (!active)
            {
                currentState = RequestState.Off;
                outputResult = true;

                //ExpireSolution(false);  // this will invoke `ExpireDownStreamObjects` right away, causing crashing solutions.
                ExpireSolution(true);  // this will invoke `ExpireDownStreamObjects` after `SolveInstance` is complete
                
                return;
            }

            // Fetch inputs and create request if applicable 
            string url = "";
            string body = "";
            string contentType = "";
            string authToken = "";
            int timeout = 0;

            if (!DA.GetData("Url", ref url)) return;
            if (!DA.GetData("Body", ref body)) return;
            if (!DA.GetData("Content Type", ref contentType)) return;
            DA.GetData("Authorization", ref authToken);
            if (!DA.GetData("Timeout", ref timeout)) return;

            // Validity checks
            if (url == null || url.Length == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Empty URL");

                currentState = RequestState.Off;
                outputResult = true;
                ExpireSolution(true);

                return;
            }

            // Perform the request
            this.Message = "Requesting...";
            currentState = RequestState.Requesting;
            
            Post(url, body, contentType, authToken, timeout);
        }


        protected override void ExpireDownStreamObjects()
        {
            // Prevents the flash of null data until the new solution is ready
            // Note that, during the GH update cycle, this function will be
            // invoked BEFORE `SolveInstance`. 
            // This is why `outputResult` should always start as `false` to
            // avoid downstream updates, and if during `SolveInstance` it is
            // determined that some outputs need updates, then
            // set there `outputResult` to true and force a `ExpireSolution` to 
            // expire again downstream objects. 
            if (outputResult)
            {
                base.ExpireDownStreamObjects();
            }
        }

        private void Post(string url, 
            string body,
            string contentType,
            string authorization,
            int timeout)
        {
            outputResult = true;

            Task.Run(() =>
            {
                try
                {
                    var data = Encoding.ASCII.GetBytes(body);

                    var request = (HttpWebRequest)WebRequest.Create(url);
                    request.Method = "POST";
                    request.ContentType = contentType;
                    request.ContentLength = body.Length;
                    request.Timeout = timeout;

                    // If required by the server, set the credentials.
                    if (authorization != null && authorization.Length > 0)
                    {
                        System.Net.ServicePointManager.Expect100Continue = true;
                        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12; //the auth type

                        request.PreAuthenticate = true;
                        request.Headers.Add("Authorization", authorization);
                    }
                    else
                    {
                        request.Credentials = System.Net.CredentialCache.DefaultCredentials;
                    }

                    using (var stream = request.GetRequestStream())
                    {
                        stream.Write(data, 0, data.Length);
                    }
                    using (var res = request.GetResponse())
                    using (var reader = new StreamReader(res.GetResponseStream()))
                        response = reader.ReadToEnd();
                    currentState = RequestState.Done;

                    //ExpireSolution(true);  // not working, thread problems

                    //ExpireSolutionWrapper();
                    RhinoApp.InvokeOnUiThread((Action)delegate { ExpireSolution(true); });
                }
                catch (Exception ex)
                {
                    response = ex.ToString();
                    currentState = RequestState.Error;

                    //ExpireSolution(true);  // not working, thread problems

                    //ExpireSolutionWrapper();
                    RhinoApp.InvokeOnUiThread((Action)delegate { ExpireSolution(true); });
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
            get { return new Guid("{609376E9-B753-4E6F-B8C8-C99261E197A2}"); }
        }
    }
}