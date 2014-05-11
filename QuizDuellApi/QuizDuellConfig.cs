using System;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QuizDuell
{
    public class QuizDuellConfig
    {
        // NOTE: Looks nicer then Host, Username, Password
        public string Host { get; set; }
        public string User { get; set; }
        public string Pass { get; set; }
        public string Mail { get; set; }

        // MD5-SALT - Different per Country/Plattform
        // qkgermany: SQ2zgOTmQc8KXmBP
        // TODO: Reverse the md5 salt for qkunited
        public string Salt { get; set; }

        // HMAC-KEYS - Different per Country/Plattform
        // IOS: 7GprrSCirEToJjG5
        // APK: irETGpoJjG57rrSC
        public string Key { get; set; }

        public string UserAgent { get; set; }
        public QuizDuellPlattform Plattform { get; set; }

        [JsonIgnore] // The NonSerializedAttribute can be used as substitute for JsonIgnoreAttribute.
        public CookieContainer Cookies { get; set; }
        public Cookie AuthCookie { get; set; }

        // The CookieContainer cannot be de/serialized therefore allways create a new instance and add the auth cookie to the container during de/serialization
        public QuizDuellConfig() { Cookies = new CookieContainer(); }

        // TODO: Add Exception Handling + using Statements
        public static QuizDuellConfig LoadFromFile(string configFile = "config.json")
        {
            string json = File.ReadAllText(configFile);
            var config = JsonConvert.DeserializeObject<QuizDuellConfig>(json);

            // HACK: For some reason the cookie is not sent on the first request even tough it has a default path set. FRAMEWORK BUG!!!
            // http://social.microsoft.com/Forums/en-US/1297afc1-12d4-4d75-8d3f-7563222d234c/httpwebrequest-incorrectly-works-with-cookiecontainer?forum=netfxnetcom
            // https://connect.microsoft.com/VisualStudio/feedback/details/478521/cookiecontainer-domain-handling-issue
            // http://stackoverflow.com/questions/1047669/cookiecontainer-bug
            // Add the Auth Cookie to the Collection before returning, since we cannot serialize the collection
            if (config.AuthCookie != null)
            {
                Uri cookieDomain = new Uri("https://" + config.Host);
                Cookie cookie = new Cookie(config.AuthCookie.Name, config.AuthCookie.Value);
                config.AuthCookie = cookie;
                config.Cookies.Add(cookieDomain, config.AuthCookie);
            }

            return config;
        }

        // TODO: Add Exception Handling + using Statements
        public static void SaveToFile(QuizDuellConfig config, string configFile = "config.json")
        {
            // Update the AuthCookie before serilization since we will lose all data in the CookieContainer during Serialization
            Uri cookieDomain = new Uri("https://" + config.Host);
            var cookieCollection = config.Cookies.GetCookies(cookieDomain);
            if (cookieCollection.Count > 0) { config.AuthCookie = cookieCollection[0]; }

            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(configFile, json);
        }
    }
}