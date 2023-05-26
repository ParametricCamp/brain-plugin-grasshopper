using Grasshopper.Kernel;
using Rhino;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Brain.Templates
{
    public abstract class GH_Component_POSTSync : GH_Component
    {

        public GH_Component_POSTSync(string name, string nickname, string description, string category, string subcategory)
            : base(name, nickname, description, category, subcategory)
        {
        }



        protected string POST(
            string url,
            string body,
            string contentType,
            string authorization,
            int timeout)
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
                var response = new StreamReader(res.GetResponseStream()).ReadToEnd();
                return response;
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Something went wrong: " + ex.Message);
                return "";
            }
        }
    }
}
