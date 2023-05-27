using Brain.HTTP;
using Grasshopper.Kernel;
using System;

namespace Brain.Templates
{
    public abstract class GH_Component_HTTPSync : GH_Component
    { 
        public GH_Component_HTTPSync(string name, string nickname, string description, string category, string subcategory)
            : base(name, nickname, description, category, subcategory)
        {
        }

        protected string GET(
            string url,
            string authorization,
            int timeout)
        {
            return GetResponse(() => BrainHttp.GetResponseFromGet(url, authorization, timeout));
        }

        protected string POST(
            string url,
            string body,
            string contentType,
            string authorization,
            int timeout)
        {

            return GetResponse(() => BrainHttp.GetResponseFromPost(body, url, contentType, authorization, timeout));
        }

        private string GetResponse(Func<string> getAsyncWebResponse)
        {
            try
            {
                return getAsyncWebResponse.Invoke();
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Something went wrong: " + ex.Message);
                return "";
            }
        }
    }
}
