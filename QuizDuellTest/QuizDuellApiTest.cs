using QuizDuell;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace QuizDuellTest
{
    /// <summary>
    ///This is a test class for QuizDuellApiTest and is intended
    ///to contain all QuizDuellApiTest Unit Tests
    ///</summary>
    [TestClass()]
    public class QuizDuellApiTest
    {
        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get { return testContextInstance; }
            set { testContextInstance = value; }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        // Class Members
        QuizDuellConfig config;
        QuizDuellApi target;

        //Use TestInitialize to run code before running each test
        [TestInitialize()]
        public void Initialize()
        {
            // Initalize the base config
            config = new QuizDuellConfig();
            config.Host = "qkgermany.feomedia.se";
            config.Plattform = QuizDuellPlattform.ANDROID;
            config.UserAgent = "Quizduell A 1.3.2";
            config.Cookies = new CookieContainer();

            // MD5-SALT - Different per Country/Plattform
            // qkgermany: SQ2zgOTmQc8KXmBP
            config.Salt = "SQ2zgOTmQc8KXmBP";

            // NOTE: Quizduell uses different HMAC Keys for different locales.
            // IOS: 7GprrSCirEToJjG5
            // APK: irETGpoJjG57rrSC
            if (config.Plattform == QuizDuellPlattform.ANDROID) { config.Key = "irETGpoJjG57rrSC"; }
            if (config.Plattform == QuizDuellPlattform.IOS) { config.Key = "7GprrSCirEToJjG5"; }

            target = new QuizDuellApi(config);
        }

        //Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void Cleanup()
        {

        }

        /* TODO: Think about using one of the other methods for testing private Methods.
         * 1 - using a Mock Class
         * 2 - using PrivateObject Class
         * 3 - using Reflection
         * 4 - using Internals Visible to Attribute and changing the private methods to internal
         * TODO: Think about refactoring the private methods by moving them into a separate class
         */

        /// <summary>
        ///A test for GetAuthCode
        ///</summary>
        [TestMethod()]
        public void GetAuthCodeTest()
        {
            /* POST https://qkunited.feomedia.se/users/create HTTP/1.1
            * Host: qkunited.feomedia.se
            * Authorization: X3LXDMWNpnw3YP4CLsrg0jvl4h6HgpH7R0cYZzRHLHI=
            * clientdate: 2014-04-21 13:41:03
            * 
            * pwd=53c63b8c469ebafde197f3ca8507672f&name=abot
            **/
            // curl -k -s -c cookies.txt -b cookies.txt https://qkgermany.feomedia.se/users/login -H 'Authorization: CzG3OsUIj4/FzYlY+DQZ7Ec7hfXTvWSazHK4VVWML6Q=' -H 'clientdate: 2014-04-18 01:05:33' --data 'name=clearlynotabot&pwd=1a319704bbc7bf9518157d11fb413ca9' -A 'Quizduell A 1.3.2' -H 'dt: a'
            // TODO: Replace these with more sane values
            target.Key = "irETGpoJjG57rrSC";
            string host = "qkgermany.feomedia.se";
            string relativeUrl = "/users/login";
            string clientdate = "2014-04-18 01:05:33";
            string username = "clearlynotabot";
            string md5pass = "1a319704bbc7bf9518157d11fb413ca9";
            IDictionary<string, string> post_params = new Dictionary<string, string>() { { "name", username }, { "pwd", md5pass } };

            var obj = new PrivateObject(target);
            string expected = "CzG3OsUIj4/FzYlY+DQZ7Ec7hfXTvWSazHK4VVWML6Q=";
            string actual = (string)obj.Invoke("GetAuthCode", host, relativeUrl, clientdate, post_params);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void GetAuthCodeWithArrayTest()
        {
            // Different Request with Array as Post Data
            // IOS Version: 7GprrSCirEToJjG5
            /* POST https://qkgermany.feomedia.se/games/5568400443047936/upload_round_answers HTTP/1.1
            clientdate: 2014-04-25 20:11:01
            Authorization: MsF9X/RlJPfQYN+wIgsbRubr+sliZY7a/qFYnghc184=

            cat_choice=0&answers=%5B2,0,1,2,2,1%5D*/

            target.Key = "7GprrSCirEToJjG5";
            string host = "qkgermany.feomedia.se";
            string relativeUrl = "/games/5568400443047936/upload_round_answers";
            string clientdate = "2014-04-25 20:11:01";
            IDictionary<string, string> post_params = new Dictionary<string, string>() { { "answers", "[2,0,1,2,2,1]" }, { "cat_choice", "0" } };

            var obj = new PrivateObject(target);
            string expected = "MsF9X/RlJPfQYN+wIgsbRubr+sliZY7a/qFYnghc184=";
            string actual = (string)obj.Invoke("GetAuthCode", host, relativeUrl, clientdate, post_params);

            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for EncodePassword
        ///</summary>
        [TestMethod()]
        public void EncodePasswordTest()
        {
            // string userAgent = "Quizduell A 1.3.2", string salt = "SQ2zgOTmQc8KXmBP", string key = "irETGpoJjG57rrSC"
            // NOTE: The password_salt is depending on region/platform. this one is for Germany and Android.
            string password_salt = "SQ2zgOTmQc8KXmBP";
            string password = "penis123";

            var obj = new PrivateObject(target);
            string expected = "1a319704bbc7bf9518157d11fb413ca9";
            var actual = (string)obj.Invoke("EncodePassword", password, password_salt);
            Console.WriteLine("MD5 EXPECTED: " + expected);
            Console.WriteLine("MD5 RESULTED: " + actual);

            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for EncodeHMAC
        ///</summary>
        [TestMethod()]
        public void EncodeHMACTest()
        {
            /* POST https://qkgermany.feomedia.se/games/create_game HTTP/1.1
            * clientdate: 2014-04-25 20:09:25
            * Authorization: xNEA+6AljV3A7p7m9pE/FGB9bfD/2Q29defuSSrwei8=
            * opponent_id=6680037336023040*/

            // NOTE: Quizduell uses different HMAC Keys for different locales.
            // IOS: 7GprrSCirEToJjG5
            // APK: irETGpoJjG57rrSC
            string key = "7GprrSCirEToJjG5";
            string input = "https://qkgermany.feomedia.se/games/create_game2014-04-25 20:09:256680037336023040";

            var obj = new PrivateObject(target);
            string expected = "xNEA+6AljV3A7p7m9pE/FGB9bfD/2Q29defuSSrwei8=";
            var actual = (string)obj.Invoke("EncodeHMAC", input, key);

            Console.WriteLine("HMAC EXPECTED: " + expected);
            Console.WriteLine("HMAC RESULTED: " + actual);

            Assert.AreEqual(expected, actual);
        }
    }
}
