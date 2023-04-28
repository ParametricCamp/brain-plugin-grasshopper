using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace Brain
{
    public class brain_ghpluginInfo : GH_AssemblyInfo
    {
        public override string Name => "brain_ghplugin";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "";

        public override Guid Id => new Guid("AC0D367B-B469-4920-A3CB-3DE02C5D2170");

        //Return a string identifying you or your company.
        public override string AuthorName => "";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "";
    }
}