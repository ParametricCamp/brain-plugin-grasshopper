using Brain.HTTP;
using Grasshopper.Kernel;
using Rhino;
using System;
using System.Threading.Tasks;

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

        protected void GETAsync(
            string url,
            string authorization,
            int timeout)
        {
            GetResponse(() => BrainHttp.GetResponseFromGet(url, authorization, timeout));
        }

        protected void POSTAsync(
            string url,
            string body,
            string contentType,
            string authorization,
            int timeout)
        {
            GetResponse(() => BrainHttp.GetResponseFromPost(body, url, contentType, authorization, timeout));
        }

        private void GetResponse(Func<string> getAsyncWebResponse)
        {
            Task.Run(() =>
            {
                try
                {
                    _response =  getAsyncWebResponse.Invoke();
                    _currentState = RequestState.Done;
                }
                catch (Exception ex)
                {
                    _response = ex.Message;
                    _currentState = RequestState.Error;
                }
                finally
                {
                    _shouldExpire = true;
                    RhinoApp.InvokeOnUiThread((Action)delegate { ExpireSolution(true); });
                }
            });
        }

        protected override void ExpireDownStreamObjects()
        {
            if (_shouldExpire)
            {
                base.ExpireDownStreamObjects();
            }
        }

    }
}
