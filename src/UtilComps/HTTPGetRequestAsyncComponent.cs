using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Brain.Templates;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace Brain.UtilComps
{
    public class HTTPGetRequestAsyncComponent : GH_Component_HTTPAsync
    {
        /// <summary>
        /// Initializes a new instance of the HTTPGetRequestAsyncComponent class.
        /// </summary>
        public HTTPGetRequestAsyncComponent()
          : base("HTTP GET (Async)", "GET Async",
              "Creates a generic HTTP GET request (asynchronous)",
              "Brain", "Utils")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {// active
            pManager.AddBooleanParameter("Send", "S", "Perform the request?", GH_ParamAccess.item, false);
            // url (endpoint)
            pManager.AddTextParameter("Url", "U", "Url for the request", GH_ParamAccess.item);

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
                switch (_currentState)
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
                // Output...
                DA.SetData(0, _response);
                _shouldExpire = false;
                return;
            }

            bool active = false;
            string url = "";
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

            _currentState = RequestState.Requesting;
            this.Message = "Requesting...";

            GETAsync(url, authToken, timeout);
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
            get { return new Guid("27F15667-131B-433C-9605-CEED238B11DD"); }
        }
    }
}