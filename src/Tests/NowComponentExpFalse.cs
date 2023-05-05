using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Eto.Threading;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;

namespace brain_ghplugin.Tests
{
    public class NowComponentExpFalse : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the NowComponent class.
        /// </summary>
        public NowComponentExpFalse()
          : base("NowComponent ExpFalse", "Now ExpT",
              "",
            "Brain", "Testing")
        {
            RecomputeSolution = () =>
            {
                //shouldExpire = true;
                RhinoApp.InvokeOnUiThread((Action)delegate { ExpireSolution(false); });
            };
        }
        public override GH_Exposure Exposure => GH_Exposure.hidden;

        bool outputResult = false;
        Action RecomputeSolution;
        string msg = "";

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Send", "S", "Perform the request?", GH_ParamAccess.item, false);
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
            if (outputResult)
            {
                DA.SetData(0, msg);
                outputResult = false;
                return;
            }


            bool active = false;
            DA.GetData("Send", ref active);

            if (active)
            {
                string now = DateTime.Now.ToString();
                outputResult = true;
                OutputHack(now);
            }
            else
            {
                //outputResult = true;
                //OutputHack("Inactive");
            }
        }




        protected override void ExpireDownStreamObjects()
        {
            // Prevents the flash of null data until the new solution is ready
            if (outputResult)
            {
                base.ExpireDownStreamObjects();
            }
        }


        private void OutputHack(string result)
        {
            Task.Run(() =>
            {
                msg = result;
                System.Threading.Thread.Sleep(1000);
                RecomputeSolution();
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
            get { return new Guid("0B3E91A1-01D8-4548-A7C0-432E5ACE0113"); }
        }
    }
}