using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace QuizDuell
{
    public class RestClient
    {
        public enum HttpVerb { GET, POST, PUT, DELETE }

        // Basic Data NOTE: decided to set these for each request instead of at class initalization
        //public string EndPoint { get; set; }
        //public HttpVerb Method { get; set; }

        // Headers/Authentication/Cookies
        // TODO: Think about using System.Net.Mime.ContentType --> contentType = new ContentType("application/x-www-form-urlencoded; charset=utf-8");
        public string ContentType { get; set; }
        public string UserAgent { get; set; }
        public CookieContainer Cookies { get; set; }

        #region Ctors
        public RestClient()
        {
            // Default Post Data ContentType
            ContentType = "application/x-www-form-urlencoded; charset=utf-8";
            Cookies = new CookieContainer();
            SetSecurityContext();
        }
        public RestClient(string userAgent, CookieContainer cookies = null, string contentType = "application/x-www-form-urlencoded; charset=utf-8")
        {
            UserAgent = userAgent;
            ContentType = contentType;

            // Create a new Cookie Container if non was passed.
            if (cookies == null)
            {
                cookies = new CookieContainer();
                Console.WriteLine("No Cookie Container Passed, creating a new one");
            }

            Cookies = cookies;
            SetSecurityContext();
        }
        #endregion

        // Authentication using a Credential Cache to store the authentication
        CredentialCache GetCredential(string uri, string username, string password)
        {
            // ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
            var credentialCache = new CredentialCache();
            credentialCache.Add(new System.Uri(uri), "Basic", new NetworkCredential(username, password));
            return credentialCache;
        }

        // Sets the SSL Certificate Validation Method --> ACCEPT ALL
        private void SetSecurityContext()
        {
            //Trust all certificates
            System.Net.ServicePointManager.ServerCertificateValidationCallback = ((sender, certificate, chain, sslPolicyErrors) => true);

            // trust sender
            // System.Net.ServicePointManager.ServerCertificateValidationCallback = ((sender, cert, chain, errors) => cert.Subject.Contains("YourServerName"));

            // validate cert by calling a function
            // ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateRemoteCertificate);

            // callback used to validate the certificate in an SSL conversation
            /*private static bool ValidateRemoteCertificate(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors policyErrors)
            {
                bool result = cert.Subject.ToUpper().Contains("YourServerName");
                return result;
            }*/
        }

        // HACK: For the Moment I only need these overloads, and since I am not 100% sure yet on the parameter order I will only create the ones I need.
        public string MakeRequest(string uri) { return MakeRequest(uri, String.Empty, HttpVerb.GET, String.Empty, Cookies, UserAgent, ContentType, null); }
        public string MakeRequest(string uri, string parameters) { return MakeRequest(uri, parameters, HttpVerb.GET, String.Empty, Cookies, UserAgent, ContentType, null); }
        public string MakeRequest(string uri, string parameters, string postData) { return MakeRequest(uri, parameters, HttpVerb.POST, postData, Cookies, UserAgent, ContentType, null); }
        public string MakeRequest(string uri, string parameters, HttpVerb method, string postData) { return MakeRequest(uri, parameters, method, postData, Cookies, UserAgent, ContentType, null); }

        // Includes CustomHeaders
        public string MakeRequest(string uri, WebHeaderCollection customHeaders) { return MakeRequest(uri, String.Empty, HttpVerb.GET, String.Empty, Cookies, UserAgent, ContentType, customHeaders); }
        public string MakeRequest(string uri, string parameters, WebHeaderCollection customHeaders) { return MakeRequest(uri, parameters, HttpVerb.GET, String.Empty, Cookies, UserAgent, ContentType, customHeaders); }
        public string MakeRequest(string uri, string parameters, string postData, WebHeaderCollection customHeaders) { return MakeRequest(uri, parameters, HttpVerb.POST, postData, Cookies, UserAgent, ContentType, customHeaders); }
        public string MakeRequest(string uri, string parameters, HttpVerb method, string postData, WebHeaderCollection customHeaders) { return MakeRequest(uri, parameters, method, postData, Cookies, UserAgent, ContentType, customHeaders); }

        // The Actual Request Method
        public static string MakeRequest(string uri, string parameters, HttpVerb method, string postData, CookieContainer cookieContainer, string userAgent, string contentType, WebHeaderCollection customHeaders)
        {
            try
            {
                // Create request and setup Headers
                var request = WebRequest.Create(uri + parameters) as HttpWebRequest;

                request.Method = method.ToString();
                request.UserAgent = userAgent;
                request.CookieContainer = cookieContainer;
                //request.Accept = "*/*";

                // Add Custom Headers if we have some, this includes our Authentication Header
                if (customHeaders != null && customHeaders.Count > 0)
                {
                    // NOTE: You cannot add the following Special Headers - http://msdn.microsoft.com/en-us/library/system.net.httpwebrequest.headers.aspx
                    request.Headers.Add(customHeaders);
                }

                #region HTTP Authentication Methods
                // Add a Custom Auth Header if we have one.
                // if (!string.IsNullOrEmpty(authHeader)) { request.Headers.Add(HttpRequestHeader.Authorization, "Basic " + authHeader); }

                // Authentication using a Credential Cache
                // request.Credentials = GetCredential(uri, "username", "password");

                // Manual Basic Authentication
                // string username = "username";
                // string password = "password";
                // string encoded = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(username + ":" + password));
                // request.Headers.Add("Authorization", "Basic " + encoded);
                #endregion

                // Post Data
                if (method == HttpVerb.POST)
                {
                    // HACK: The Android Version Requires a Content Length Header even if the post data is null
                    request.ContentLength = 0;

                    if (!string.IsNullOrEmpty(postData))
                    {
                        var bytes = Encoding.UTF8.GetBytes(postData);
                        request.ContentType = contentType;
                        request.ContentLength = bytes.Length;

                        using (var requestStream = request.GetRequestStream())
                        {
                            requestStream.Write(bytes, 0, bytes.Length);
                        }
                    }
                }


                using (var response = (HttpWebResponse)request.GetResponse())
                using (var responseStream = response.GetResponseStream())
                {
                    // Throw an Exception on Error
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        throw new Exception(String.Format("Server error (HTTP {0}: {1}).", response.StatusCode, response.StatusDescription));
                    }

                    // TODO: Is this check necessary? let it throw an exception if there is no response Stream
                    string responseText = string.Empty;
                    if (responseStream != null)
                    {
                        using (var sr = new StreamReader(responseStream)) { responseText = sr.ReadToEnd(); }
                    }

                    // Done, return the response as Text for further processing.
                    return responseText;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

    }

}
