using System;
using System.IO;
using System.Net;
using System.Text;

namespace Brain.HTTP
{
    public class BrainHttp
    {

        internal string GetResponseFromGet(string url, string authorization, int timeout)
        {
            var request = CreateBaseRequest(url, "GET", authorization, timeout);

            return GetResponse(request);
        }
        internal string GetResponseFromPost(string body, string url, string contentType, string authorization, int timeout)
        {
            // Compose the request
            byte[] data = Encoding.ASCII.GetBytes(body);

            var request = CreateBaseRequest(url, "POST", authorization, timeout);

            request.ContentType = contentType;
            request.ContentLength = data.Length;

            return GetResponse(request);
        }
        private string GetResponse(HttpWebRequest request)
        {
            return new StreamReader(request.GetResponse().GetResponseStream()).ReadToEnd();
        }

        private HttpWebRequest CreateBaseRequest(string url, string method, string authorization, int timeout)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            request.Method = method;
            request.Timeout = timeout;

            // Handle authorization
            if (authorization != null && authorization.Length > 0)
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12; //the auth type

                request.PreAuthenticate = true;
                request.Headers.Add("Authorization", authorization);
            }
            else
            {
                request.Credentials = CredentialCache.DefaultCredentials;
            }

            return request;
        }


    }
}
