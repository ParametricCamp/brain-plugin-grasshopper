using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Brain.Templates;

namespace Brain.UtilComps
{
    public class HTTPPostRequestComponent : GH_Component_POSTSync
    {
        /// <summary>
        /// Initializes a new instance of the HTTPPostRequestComponent class.
        /// </summary>
        public HTTPPostRequestComponent()
          : base("HTTP POST", 
                "POST",
                "Creates a generic HTTP POST request (synchronous)",
                "Brain", 
                "Utils")
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
            bool active = false;
            string url = "";
            string body = "";
            string contentType = "";
            string authToken = "";
            int timeout = 0;

            DA.GetData("Send", ref active);

            if (!active)
            {
                DA.SetData("Response", "");
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
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Empty URL");
                return;
            }
            if (contentType == null || contentType.Length == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Empty content type");
                return;
            }

            string response = POST(url, body, contentType, authToken, timeout);

            // Output
            DA.SetData(0, response);
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
            get { return new Guid("CC83DAAC-80BA-4880-B7E3-24D8B53C0CAD"); }
        }
    }
}