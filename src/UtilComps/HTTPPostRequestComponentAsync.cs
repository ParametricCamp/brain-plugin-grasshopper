using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;
using Brain.GrasshopperAsyncComponent;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Brain.Utilities
{
    public class HTTPPostRequestComponentAsync : GH_AsyncComponentStringProgress
    {
        /// <summary>
        /// Initializes a new instance of the HTTPPostRequestComponent class.
        /// </summary>
        public HTTPPostRequestComponentAsync()
          : base("HTTPPostRequestComponentAsync", "POST Async",
              "Create an async generic HTTP POST request",
              "Brain", "Utils")
        {
            BaseWorker = new HttpPostWorker();
        }

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

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);
            Menu_AppendItem(menu, "Cancel", (s, e) =>
            {
                RequestCancellation();
            });
        }

        ///// <summary>
        ///// This is the method that actually does the work.
        ///// </summary>
        ///// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        //protected override void SolveInstance(IGH_DataAccess DA)
        //{
        //    bool active = false;
        //    string url = "";
        //    string body = "";
        //    string contentType = "";
        //    string authToken = "";
        //    int timeout = 0;

        //    DA.GetData("Send", ref active);

        //    if (!active)
        //    {
        //        DA.SetData("Response", "");
        //        return;
        //    }

        //    if (!DA.GetData("Url", ref url)) return;
        //    if (!DA.GetData("Body", ref body)) return;
        //    if (!DA.GetData("Content Type", ref contentType)) return;
        //    DA.GetData("Authorization", ref authToken);
        //    if (!DA.GetData("Timeout", ref timeout)) return;

        //    // Validity checks
        //    if (url == null || url.Length == 0)
        //    {
        //        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Empty URL");
        //        return;
        //    }

        //    // Perform the request
        //    if (url == null) return;

        //    var data = Encoding.ASCII.GetBytes(body);

        //    var request = (HttpWebRequest)WebRequest.Create(url);
        //    request.Method = "POST";
        //    request.ContentType = contentType;
        //    request.ContentLength = body.Length;
        //    request.Timeout = timeout;

        //    // If required by the server, set the credentials.
        //    if (authToken != null && authToken.Length > 0)
        //    {
        //        System.Net.ServicePointManager.Expect100Continue = true;
        //        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12; //the auth type

        //        request.PreAuthenticate = true;
        //        request.Headers.Add("Authorization", authToken);
        //    }
        //    else
        //    {
        //        request.Credentials = System.Net.CredentialCache.DefaultCredentials;
        //    }

        //    using (var stream = request.GetRequestStream())
        //    {
        //        stream.Write(data, 0, data.Length);
        //    }


        //    string response = "";
        //    try
        //    {
        //        HttpWebResponse res = (HttpWebResponse)request.GetResponse();

        //        string responseString = new StreamReader(res.GetResponseStream()).ReadToEnd();

        //        response = responseString;
        //    }
        //    catch (Exception ex)
        //    {
        //        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.ToString());
        //    }

        //    // Outputs
        //    DA.SetData(0, response);
        //}

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
            get { return new Guid("05b144d4-25be-420d-95d5-060571286dfe"); }
        }





        private class HttpPostWorker : WorkerInstance<string>
        {
            bool active = false;
            string url = "";
            string body = "";
            string contentType = "";
            string authToken = "";
            int timeout = 0;

            string response = "";

            public HttpPostWorker() : base(null) { }

            public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
            {
                DA.GetData("Send", ref active);
                if (!DA.GetData("Url", ref url)) return;
                if (!DA.GetData("Body", ref body)) return;
                if (!DA.GetData("Content Type", ref contentType)) return;
                DA.GetData("Authorization", ref authToken);
                if (!DA.GetData("Timeout", ref timeout)) return;
            }

            public override void DoWork(Action<string, string> ReportProgress, Action Done, GH_Component component)
            {
                // 👉 Checking for cancellation!
                if (CancellationToken.IsCancellationRequested) { return; }

                if (!active)
                {
                    Done();
                    return;
                }

                // Validity checks
                if (url == null || url.Length == 0)
                {
                    component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Empty URL");
                    Done();
                    return;
                }

                // Perform the request
                if (url == null) return;

                var data = Encoding.ASCII.GetBytes(body);

                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = contentType;
                request.ContentLength = body.Length;
                request.Timeout = timeout;

                // If required by the server, set the credentials.
                if (authToken != null && authToken.Length > 0)
                {
                    System.Net.ServicePointManager.Expect100Continue = true;
                    System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12; //the auth type

                    request.PreAuthenticate = true;
                    request.Headers.Add("Authorization", authToken);
                }
                else
                {
                    request.Credentials = System.Net.CredentialCache.DefaultCredentials;
                }

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                ReportProgress(Id, "Requesting...");

                try
                {
                    HttpWebResponse res = (HttpWebResponse)request.GetResponse();

                    string responseString = new StreamReader(res.GetResponseStream()).ReadToEnd();

                    response = responseString;
                }
                catch (Exception ex)
                {
                    component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.ToString());
                }

                Done();
            }


            public override void SetData(IGH_DataAccess DA)
            {
                DA.SetData("Response", response);
            }

            public override WorkerInstance<string> Duplicate() => new HttpPostWorker();

        }
    }
}