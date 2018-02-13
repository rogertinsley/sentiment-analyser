using System;
using System.Net;

namespace sentiment
{
     public class WebProxy : IWebProxy
    {
        private readonly Uri uri;

        public WebProxy()
        {
            uri = new Uri(Environment.GetEnvironmentVariable("HTTPS_PROXY", EnvironmentVariableTarget.Process));
        }
        
        public Uri GetProxy(Uri destination) => uri;

        public bool IsBypassed(Uri host) => false;

        public ICredentials Credentials { get; set; }
    }
}
