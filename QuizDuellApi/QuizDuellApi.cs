using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QuizDuell
{
    public enum QuizDuellPlattform { IOS, ANDROID }
    public enum GameState
    {
        Waiting = 0,
        Active = 1,
        Finished = 2,
        GiveUp = 5,
        TimedOut = 6,
        Started = 10
    }

    /// <summary>
    /// Inofficial Quizduell API interface.
    /// </summary>
    public class QuizDuellApi
    {
        public string Host { get; set; }

        // MD5-SALT - Different per Country/Plattform
        // qkgermany: SQ2zgOTmQc8KXmBP
        // TODO: Reverse the md5 salt for qkunited
        public string Salt { get; set; }

        // HMAC-KEYS - Different per Country/Plattform
        // IOS: 7GprrSCirEToJjG5
        // APK: irETGpoJjG57rrSC
        public string Key { get; set; }

        // Rest Client
        public RestClient Client { get; set; }
        public CookieContainer Cookies { get; set; }
        public string UserAgent { get; set; }
        public QuizDuellPlattform Plattform { get; set; } // TODO: think about only maintaining one api Platform instead of the two different versions
        private Random random = new Random();

        /// <summary>
        /// Creates the API interface. Expects either an authentication cookie within
        /// the user supplied cookie jar or a subsequent call to
        /// QuizduellApi.LoginUser() or QuizduellApi.CreateUser().
        /// </summary>
        /// <param name="host"></param>
        /// <param name="cookies">Stores authentication tokens with each request made to the API</param>
        /// <param name="userAgent"></param>
        /// <param name="salt"></param>
        /// <param name="key"></param>
        public QuizDuellApi(string host, CookieContainer cookies = null, QuizDuellPlattform plattform = QuizDuellPlattform.ANDROID, string userAgent = "Quizduell A 1.3.2", string salt = "SQ2zgOTmQc8KXmBP", string key = "irETGpoJjG57rrSC")
        {
            // TODO: Create an instance of the config class, so we can deseriliaze the config data to a file at the end of the program run.
            Host = host;
            Plattform = plattform;
            Salt = salt;
            Key = key;
            UserAgent = userAgent;
            random = new Random();

            if (cookies != null)
            {
                Cookies = cookies;
                Client = new RestClient(userAgent, cookies);
            }
            else
            {
                Client = new RestClient(userAgent);
            }
        }
        public QuizDuellApi(QuizDuellConfig config)
        {
            // TODO: Refactor the Properties so that they just wrap an instance of the config class.
            // NOTE: This way we can deserialize the config file at the end of program run and the user of the api does not have to deal with that.  --- Think about this first, does it make sense that the api deals with deserializisation of the config file?
            Host = config.Host;
            Salt = config.Salt;
            Key = config.Key;
            UserAgent = config.UserAgent;
            Plattform = config.Plattform;
            Cookies = config.Cookies;
            Client = new RestClient(UserAgent, Cookies);
            random = new Random();
        }

        // This method will generate an authcode, post_params will be sorted by name and then concatined to the other parameters
        private string GetAuthCode(string host, string relativeUrl, string client_date, IDictionary<string, string> post_params = null)
        {
            string auth = String.Format("https://{0}{1}{2}", host, relativeUrl, client_date);

            // Sort the post_params array and join it as a string
            if (post_params != null)
            {
                // NOTE: For HMAC post_params should not be urlencoded
                var list = new List<string>(post_params.Values);
                list.Sort(); // NOTE: Why would they use different sort orders on different platforms ?_?

                if (Plattform == QuizDuellPlattform.ANDROID)
                {
                    // HACK: Default Comparer Sorts Special Chars, Numbers, Characters while ANDROID requires Numbers, Special Chars, Characters
                    list.Sort((x, y) =>
                    {
                        if (!Char.IsLetterOrDigit(x[0]))
                        {
                            if (Char.IsLetter(y[0]))
                                return -1;
                            else if (Char.IsDigit(y[0]))
                                return 1;
                        }
                        // return x.CompareTo(y); // Does not work since it's using Culture Specific information
                        return System.String.Compare(x, y, System.StringComparison.Ordinal);
                    });
                }

                auth += String.Join("", list);
            }

            return EncodeHMAC(auth, Key); ;
        }

        // This method will return the base 64 encoded string using the given input and key.
        private string EncodeHMAC(string input, string key)
        {
            Debug.WriteLine("HMAC-INPUT:   " + input);
            var encoding = new UTF8Encoding();
            var hmac = new HMACSHA256(encoding.GetBytes(key));
            byte[] messageBytes = encoding.GetBytes(input);
            byte[] hashedValue = hmac.ComputeHash(messageBytes);

            // Convert to base64 string
            string base64 = Convert.ToBase64String(hashedValue);
            return base64;
        }

        // This Hashes the Salt + Password as MD5 and then returns it in hex representation.
        private string EncodePassword(string password, string password_salt)
        {
            byte[] md5 = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(password_salt + password));

            // Turn the MD5 Bytes into their hex representation
            // string hashPassword = BitConverter.ToString(byteHashedPassword).Replace("-","").ToLower();
            var hex = new StringBuilder();
            foreach (var b in md5) { hex.AppendFormat("{0:x2}", b); } // lowercase: x2, use X2 for uppercase
            Debug.WriteLine("MD5_STRING: " + hex.ToString());

            return hex.ToString();
        }

        // Turns a Collection of KeyValuePairs to a url-encoded post_data string
        private string DictionaryToPostDataString(IDictionary<string, string> data)
        {
            string delimiter = String.Empty;
            var values = new StringBuilder();

            // Alternative to force the sort Order:  
            //foreach (KeyValuePair<string, string> kvp in data.OrderBy(k => k.Value)) // NOTE: Sorted Params?
            foreach (KeyValuePair<string, string> kvp in data)
            {
                // Alternative
                //System.Uri.EscapeDataString() 
                //System.Net.WebUtility.UrlEncode()
                // EscapeUriString only escapes these characters:
                // ` % ^ [ ] \ { } | " < > space // NOTE: Why are [ and ] not escaped?
                // EscapeDataString escapes all of these:
                // = ` @ # $ % ^ & + [ ] \ { } | ; : " , / < > ? space

                values.Append(delimiter);
                values.Append(Uri.EscapeUriString(kvp.Key));
                values.Append("=");
                //values.Append(Uri.EscapeUriString(kvp.Value).Replace("[", "%5B").Replace("]", "%5D"));
                //values.Append(System.Net.WebUtility.UrlEncode(kvp.Value).Replace("%40", "@"));
                values.Append(Uri.EscapeDataString(kvp.Value).Replace("%20", "+").Replace("%40", "@")); // HACK: Fix this UrlEncoding Mess, this breaks create user since mail contains @ which is not supposed to be urlencoded
                delimiter = "&";
            }

            return values.ToString();
        }

        [Obsolete] // TODO: Change all the Api Methods to use IDictionarys for the post_params instead of JObject
        private JObject Request(string relativeUrl, JObject post_params)
        {
            IDictionary<string, JToken> d = (JObject)post_params;
            Dictionary<string, string> dictionary = d.ToDictionary(pair => pair.Key, pair => (string)pair.Value);

            return Request(relativeUrl, dictionary);
        }
        private JObject Request(string relativeUrl, IDictionary<string, string> post_params = null) { return Request(Host, relativeUrl, post_params); }
        private JObject Request(string host, string relativeUrl, IDictionary<string, string> post_params)
        {
            string uri = String.Format("https://{0}{1}", host, relativeUrl);
            string clientDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string auth = GetAuthCode(host, relativeUrl, clientDate, post_params);

            // Custom Headers
            var customHeaders = new WebHeaderCollection();
            if (Plattform == QuizDuellPlattform.ANDROID) { customHeaders.Set("dt", "a"); }
            if (Plattform == QuizDuellPlattform.ANDROID) { customHeaders.Set("Accept-Encoding", "identity"); }
            customHeaders.Set("Authorization", auth);
            customHeaders.Set("clientdate", clientDate);

            string responseText = "{}";
            if (post_params != null)
            {
                // Post Request
                string postData = DictionaryToPostDataString(post_params);
                responseText = Client.MakeRequest(uri, "", postData, customHeaders);
            }
            else
            {
                // Get Request
                responseText = Client.MakeRequest(uri, "", customHeaders);
            }

            // Make sure that we have a valid JSON Object
            if (responseText == null || responseText.Contains("ACCESS NOT OK") || responseText.Contains("Error"))
            {
                // responseText = "{\"error\": \"ACCESS NOT OK\"}";
                responseText = "{\"error\": \"" + responseText + "\"}";
            }

            var jObject = JObject.Parse(responseText);
            return jObject;
        }

        #region /users/

        #region User
        /// <summary>
        /// Creates a new Quizduell user. The user will automatically be logged in. 
        /// </summary>
        /// <param name="name">the username for the quizduell account</param>
        /// <param name="password">the password for the new account</param>
        /// <param name="email">optional email parameter</param>
        /// <returns>
        /// Returns the following JSON structure on success:
        /// {
        /// "logged_in": true,
        /// "settings": {...},
        /// "user": {...}
        /// }
        /// </returns>
        public JObject CreateUser(string name, string password, string email = "") { return CreateUser(name, password, Salt, email); }
        private JObject CreateUser(string name, string password, string password_salt, string email = "")
        {
            /* data = {
                'name': name,
                'email': email,
                'pwd': hashlib.md5(self.password_salt + password).hexdigest()
            }*/

            dynamic data = new JObject();
            data.name = name;
            if (!String.IsNullOrEmpty(email)) { data.email = email; } // TODO: Email Creation no longer works, since changing sort order and encoding scheme.
            data.pwd = EncodePassword(password, password_salt);

            return Request("/users/create", data);
        }

        /// <summary>
        /// Authenticates an existing Quizduell user. 
        /// @attention: Any user can only log in 10 times every 24 hours!
        /// </summary>
        /// <param name="name">the username for the quizduell account</param>
        /// <param name="password">the password for the quizduell account</param>
        /// <returns>
        /// Returns the following JSON structure on
        /// success:
        /// {
        /// "logged_in": true,
        /// "settings": {...},
        /// "user": {...}
        /// }
        /// </returns>
        public JObject LoginUser(string name, string password) { return LoginUser(name, password, Salt); }
        private JObject LoginUser(string name, string password, string password_salt)
        {
            /* data = {
                'name': name,
                'pwd': hashlib.md5(self.password_salt + password).hexdigest()
            }*/

            dynamic data = new JObject();
            data.name = name;
            data.pwd = EncodePassword(password, password_salt);

            return Request("/users/login", data);
        }


        /// <summary>
        /// Updates an existing Quizduell user. The user will automatically be logged in.
        /// </summary>
        /// <param name="name">the current username</param>
        /// <param name="new_password">the new password to set</param>
        /// <param name="new_email">the new email to set</param>
        /// <returns>
        /// Returns the following JSON structure on success:
        /// {
        /// "logged_in": true,
        /// "settings": {...},
        /// "user": {...}
        /// }
        /// </returns>
        public JObject UpdateUser(string name, string new_password, string new_email = "") { return UpdateUser(name, new_password, Salt, new_email); }
        private JObject UpdateUser(string name, string new_password, string password_salt, string new_email = "")
        {
            /* data = {
                'name': name,
                'email': email,
                'pwd': hashlib.md5(self.password_salt + password).hexdigest()
            }*/

            dynamic data = new JObject();
            data.name = name;
            if (!String.IsNullOrEmpty(new_email)) { data.email = new_email; }
            data.pwd = EncodePassword(new_password, password_salt);

            return Request("/users/update_user", data);
        }

        /// <summary>
        /// Looks for a Quizduell user with the given name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns>
        /// Returns the following
        /// JSON structure on success:
        /// {
        /// "u": {
        ///        "avatar_code": "...",
        ///        "name": "...",
        ///        "user_id": "..."
        ///        }
        /// } 
        /// </returns>
        public JObject FindUser(string name)
        {
            /*data = {
                'opponent_name': name
            }*/

            dynamic data = new JObject();
            data.opponent_name = name;

            return Request("/users/find_user", data);
        }


        /// <summary>
        /// Send a mail with a password restore link.
        /// </summary>
        /// <param name="email">the email where the password reset link will be sent.</param>
        /// <returns>
        /// Returns the following JSON structure on success:
        /// {
        /// "popup_mess": "Eine E-Mail ... wurde an deine E-Mail gesendet",
        /// "popup_title": "E-Mail gesendet"
        /// }
        /// </returns>
        public JObject ForgotPassword(string email)
        {
            /*data = {
                'email': email
            }*/

            dynamic data = new JObject();
            data.email = email;

            return Request("/users/forgot_pwd", data);
        }

        /// <summary>
        /// Lists invited, active and finished games. 
        /// </summary>
        /// <returns>
        /// Returns the following JSON structure on success:
        /// {
        /// "logged_in": true,
        /// "settings": {...},
        /// "user": {...}
        /// }
        /// </returns>
        public JObject CurrentUserGames()
        {
            dynamic data = new JObject();
            return Request("/users/current_user_games", data);
        }
        #endregion

        #region Friends
        /// <summary>
        /// Adds a Quizduell user as a friend.
        /// </summary>
        /// <param name="user_id">the user_id to add to your friendlist</param>
        /// <returns>
        /// Returns the following JSON structure on success:
        /// {
        /// "popup_mess": "Du bist jetzt mit ... befreundet",
        /// "popup_title": "Neuer Freund"
        /// }
        /// </returns>
        public JObject AddFriend(string user_id)
        {
            /* data = {
                'friend_id': user_id
            }*/

            dynamic data = new JObject();
            data.friend_id = user_id;

            return Request("/users/add_friend", data);
        }


        /// <summary>
        /// Removes a Quizduell user from your friendlist.
        /// </summary>
        /// <param name="user_id">The user_id to remove from your friendlist</param>
        /// <returns>
        /// Returns the following JSON structure on success:
        /// {
        /// "removed_id": "..."
        /// }
        /// </returns>
        public JObject RemoveFriend(string user_id)
        {
            /*data = {
                'friend_id': user_id
            }*/

            dynamic data = new JObject();
            data.friend_id = user_id;

            return Request("/users/remove_friend", data);
        }
        #endregion

        #region Avatar
        /// <summary>
        /// Change the displayed avatar. An avatar consists of individual mouth,
        /// hair, eyes, hats, etc. encoded in a numerical string, e.g. "0010999912"
        /// (A skin-colored avatar with a crown).
        /// </summary>
        /// <param name="avatar_code"></param>
        /// <returns>
        /// Returns the following JSON structure on success:
        /// {
        /// "t": true
        /// }
        /// </returns>
        public JObject UpdateAvatar(string avatar_code)
        {
            /* data = {
                'avatar_code': avatar_code
            }*/

            dynamic data = new JObject();
            data.avatar_code = avatar_code;

            return Request("/users/update_avatar", data);
        }

        #endregion

        #region Message
        /// <summary>
        /// Send a message within a game. The message will be visible in all games against the same opponent. 
        /// </summary>
        /// <param name="game_id"></param>
        /// <param name="message"></param>
        /// <returns>
        /// Returns the following JSON structure on success:
        /// {
        ///     "m": [{
        ///         "created_at": "...,
        ///         "from": ...,
        ///         "id": "...",
        ///         "text": "...",
        ///         "to": ...
        ///     }]
        /// }
        /// </returns>
        public JObject SendMessage(string game_id, string message)
        {
            /* data = {
                'game_id': str(game_id),
                'text': message
            }*/

            dynamic data = new JObject();
            data.game_id = game_id;
            data.text = message;

            return Request("/users/send_message", data);
        }
        #endregion

        #region Ranking
        /// <summary>
        /// Lists the top rated Quizduell players.
        /// </summary>
        /// <returns>
        /// Returns the following JSON structure on success:
        /// {
        ///     "users": [{
        ///         "avatar_code": "...",
        ///         "key": ...,
        ///         "name": "...",
        ///         "rating": ...
        ///     }, ...]
        /// }
        /// 
        /// </returns>
        public JObject TopListRating() { return Request("/users/top_list_rating"); }

        /// <summary>
        /// Lists the top rated Quizduell players.
        /// </summary>
        /// <returns>
        /// Returns the following JSON structure on success:
        /// {
        ///     "users": [{
        ///         "avatar_code": "0010035604",
        ///         "n_approved_questions": 192,
        ///         "name": "Zwaanswijk"
        ///     }, ...]
        /// }
        /// 
        /// </returns>
        public JObject TopListWriters() { return Request("/users/top_list_writers"); }
        #endregion

        #region Blocking
        /// <summary>
        /// Puts a Quizduell user on the blocked list.
        /// </summary>
        /// <param name="user_id">The user_id to block</param>
        /// <returns>
        /// Returns the following JSON structure on success:
        /// {
        ///     "blocked": [{
        ///         "avatar_code": "...",
        ///         "name": "...",
        ///         "user_id": "..."
        ///     }, ...]
        /// }
        /// </returns>
        public JObject AddBlocked(string user_id)
        {
            /* data = {
                'blocked_id': user_id
            }*/
            dynamic data = new JObject();
            data.blocked_id = user_id;

            return Request("/users/add_blocked", data);
        }

        /// <summary>
        /// Removes a Quizduell user from the blocked list.
        /// </summary>
        /// <param name="user_id">The user_id to unblock</param>
        /// <returns>
        /// Returns the following JSON structure on success:
        /// {
        ///     "blocked": [...]
        /// } 
        /// </returns>
        public JObject RemoveBlocked(string user_id)
        {
            /* data = {
                'blocked_id': user_id
            }*/
            dynamic data = new JObject();
            data.blocked_id = user_id;

            return Request("/users/remove_blocked", data);
        }
        #endregion

        #region Device
        public JObject AddDeviceTokenIOS(string device_token) { return AddDeviceToken(device_token, "ios"); }
        private JObject AddDeviceToken(string device_token, string plattform)
        {
            /* POST https://qkgermany.feomedia.se/users/add_device_token_ios HTTP/1.1 
             * Host: qkgermany.feomedia.se
             * User-Agent: Germany/4.2.3 (iPhone; iOS 6.0; Scale/2.00)
             * clientdate: 2014-02-02 16:10:51
             * Content-Type: application/x-www-form-urlencoded; charset=utf-8
             * Authorization: UegIt9KGG248m3Qou7L1RsuN5yaL/CRFSvaA/0L3n6g=
             * 
             * device_token=3abb175a%20b1620a03%200351a5bd%20ab179d88%20524db065%20b3f6bb7f%20d31ef834%20475c80b9
             */

            /* data = {
                'device_token': device_token
            }*/

            // TODO: Add Return Information and XML DOC Comment
            dynamic data = new JObject();
            data.device_token = device_token;

            return Request("/users/add_device_token" + "_" + plattform, data);
        }
        #endregion

        #endregion

        #region /games/

        #region Game
        /// <summary>
        /// Lists details of a game, including upcoming questions and their correct answers.
        /// </summary>
        /// <param name="game_id">the game_id for which you want to request the information</param>
        /// <returns>
        /// Returns the following JSON structure on success:
        /// {
        ///     "game": {
        ///         "cat_choices": [...],
        ///         "elapsed_min": ...,
        ///         "game_id": ...,
        ///         "messages": [],
        ///         "opponent": {...},
        ///         "opponent_answers": [...],
        ///         "questions": [...],
        ///         "state": ...,
        ///         "your_answers": [...],
        ///         "your_turn": false
        ///     }
        /// }
        /// </returns>
        public JObject GetGame(string game_id) { return Request("/games/" + game_id); }

        /// <summary>
        /// Lists details of specified games, but without questions and answers.
        /// </summary>
        /// <param name="game_ids">The game_ids for which you want to request additional information</param>
        /// <returns>
        /// Returns the following JSON structure on success:
        /// {
        /// "games": [{
        /// "cat_choices": [...],
        /// "elapsed_min": ...,
        /// "game_id": ...,
        /// "messages": [...],
        /// "opponent": {...},
        /// "opponent_answers": [...],
        /// "state": ...,
        /// "your_answers": [...],
        /// "your_turn": ...
        /// }, ...]
        /// }
        /// </returns>
        public JObject GetGames(string[] game_ids)
        {
            /* data = {
            'gids': json.dumps([int(i) for i in game_ids])
            }*/

            dynamic data = new JObject();
            data.gids = String.Format("[{0}]", String.Join(",", game_ids));

            return Request("/games/short_games", data);
        }

        /// <summary>
        /// Starts a game against a given opponent.
        /// </summary>
        /// <param name="user_id"></param>
        /// <returns>
        /// Returns the following JSON structure on success:
        /// {
        /// "game": {
        ///     "cat_choices": [...],
        ///     "elapsed_min": ...,
        ///     "game_id": ...,
        ///     "messages": [],
        ///     "opponent": {...},
        ///     "opponent_answers": [...],
        ///     "questions": [...],
        ///     "state": 1,
        ///     "your_answers": [...],
        ///     "your_turn": false
        /// }
        /// }
        /// </returns>
        public JObject StartGame(string user_id)
        {
            /* data = {
                'opponent_id': user_id
            }*/

            dynamic data = new JObject();
            data.opponent_id = user_id;

            return Request("/games/create_game", data);
        }

        /// <summary>
        /// Starts a game against a random opponent.
        /// </summary>
        /// <returns>
        /// Returns the following JSON structure on success:
        /// {
        ///     "game": {
        ///         "cat_choices": [...],
        ///         "elapsed_min": ...,
        ///         "game_id": ...,
        ///         "messages": [],
        ///         "opponent": {...},
        ///         "opponent_answers": [...],
        ///         "questions": [...],
        ///         "state": 1,
        ///         "your_answers": [...],
        ///         "your_turn": false
        ///     }
        /// }
        /// </returns>
        public JObject StartRandomGame() { return Request("/games/start_random_game"); }

        /// <summary>
        /// Declines a Game Invitation
        /// </summary>
        /// <param name="game_id"></param>
        /// <returns>
        /// Returns the following JSON structure on success:
        /// {
        /// "t": true
        /// }
        /// </returns>
        public JObject DeclineGame(string game_id) { return AcceptGame(game_id, false); }
        public JObject AcceptGame(string game_id, bool accept = true)
        {
            /* data = {
                'accept': '1',
                'game_id': str(game_id)
            }*/

            dynamic data = new JObject();
            data.game_id = game_id;
            data.accept = accept ? 1 : 0;

            return Request("/games/accept", data);
        }

        /// <summary>
        /// Gives up a game.
        /// </summary>
        /// <param name="game_id"></param>
        /// <returns>
        /// Returns the following JSON structure on success:
        /// {
        ///     "game": {...},
        ///     "popup": {
        ///         "popup_mess": "Du hast gegen ... aufgegeben\n\nRating: -24",
        ///         "popup_title": "Spiel beendet"
        ///     }
        /// }
        /// </returns>
        public JObject GiveUp(string game_id)
        {
            // HACK: There is also a GET Interface, not sure which one is the newer one?
            /*GET https://qkunited.feomedia.se/games/6685949559832576/give_up HTTP/1.1
            Host: qkunited.feomedia.se
            User-Agent: Quizduell/4.3 CFNetwork/548.1.4 Darwin/11.0.0
            Authorization: IQM3lHeaOKwic/lubCELCX3Su++hAbqugiowG0Flq+w=
            clientdate: 2014-04-23 00:32:23
            Cookie: auth=eyJfdXNlciI6WzYxMzQyMTE2MTg1Mzc0NzIsMSwiaE5iMDJ3dWNnMllBbmZWbHA5MFFNeCIsMTM5ODA4NzY2MywxMzk4MjEzMTEwLCJhYm90Il19|1398213131|939ebb1525cd094486be342762bfbc2f6de02c40
            */

            /* data = {
                'game_id': str(game_id)
            }*/
            dynamic data = new JObject();
            data.game_id = game_id;

            return Request("/games/give_up", data);
        }


        /// <summary>
        /// Upload answers and the chosen category to an active game. The number of
        /// answers depends on the game state and is 3 for the first player in the
        /// first round and the last player in the last round, otherwise 6.
        /// Note: In the answers you must include all answers you gave in the previous rounds of the same game.
        /// </summary>
        /// <param name="game_id"></param>
        /// <param name="answers">3 or 6 values in {0,1,2,3} with 0 being correct, 8 is for round disrupted.</param>
        /// <param name="category_id">value in {0,1,2}</param>
        /// <returns>
        /// Returns the following JSON structure on success:
        /// {
        ///     "game": {
        ///         "your_answers": [...],
        ///         "state": ...,
        ///         "elapsed_min": ...,
        ///         "your_turn": false,
        ///         "game_id": ...,
        ///         "cat_choices": [...],
        ///         "opponent_answers": [...],
        ///         "messages": [...],
        ///         "opponent": {...}
        ///     }
        /// }
        /// </returns>
        public JObject UploadRoundAnswers(string game_id, int[] answers, int category_id)
        {
            // HACK: The game is not submitting the category_id, instead it is submitting an array index, it's linearly using the categories, always 3 in a row per round. next round i +=3 so index can only be 0,1,2
            /* NOTE: Example Request - Why is this using a different endpoint
            POST https://qkunited.feomedia.se/games/5732717270401024/upload_round_answers HTTP/1.1

            data = {
                'game_id': str(game_id),
                'answers': str(answers),
                'cat_choice': str(category_id)
            }*/

            // Answers need to be in multiples of 3
            if (answers.Length % 3 != 0) { throw new Exception("Error: answers need to be in multiples of 3 while the submitted count was: " + answers.Length); }

            dynamic data = new JObject();

            // NOTE: Using alternative Endpoint --> which requires different sort order.
            if (Plattform == QuizDuellPlattform.ANDROID) { data.game_id = game_id.ToString(); }
            data.cat_choice = category_id.ToString();
            data.answers = String.Format("[{0}]", String.Join(", ", answers)); // Android requires a space beetwen the values, IOS accepts this format too so making it general.
            //data.answers = String.Format("[{0}]", String.Join(",", answers));

            /* Alternatives
            var postData = new Dictionary<string, object>()
            {
                    {"game_id", game_id},
                    {"cat_choice", category_id}
                    {"answers", answers},
            };

            // Alternative
            var x = data as IDictionary<string, string>;

            // ALternative
            var n = new NameValueCollection()
            {
                    {"game_id", game_id},
                    {"answers", String.Format("[{0}]", String.Join(",", answers))},
                    {"cat_choice", category_id}
            };*/


            if (Plattform == QuizDuellPlattform.ANDROID) { return Request("/games/upload_round_answers", data); }
            return Request(String.Format("/games/{0}/upload_round_answers", game_id), data);
        }

        #endregion

        #endregion

        #region /stats/

        #region Stats
        /// <summary>
        /// Retrieves category statistics and ranking.
        /// </summary>
        /// <returns>
        /// Returns the following JSON structure on success:
        /// {
        ///     "cat_stats": [...],
        ///     "n_games_lost": ...,
        ///     "n_games_played": ...,
        ///     "n_games_tied": ...,
        ///     "n_games_won": ...,
        ///     "n_perfect_games": ...,
        ///     "n_questions_answered": ...,
        ///     "n_questions_correct": ...,
        ///     "n_users": ...,
        ///     "rank": ...,
        ///     "rating": ...
        /// } 
        /// </returns>
        public JObject CategoryStats() { return Request("/stats/my_stats"); }

        /// <summary>
        /// Retrieves game statistics per opponent.
        /// </summary>
        /// <returns>
        ///  Returns the following JSON structure on success:
        /// {
        ///     "game_stats": [{
        ///         "avatar_code": "...",
        ///         "n_games_lost": 2,
        ///         "n_games_tied": 2,
        ///         "n_games_won": 0,
        ///         "name": "...",
        ///         "user_id": "..."
        ///     }, ...],
        /// } 
        /// </returns>
        public JObject GameStats() { return Request("/stats/my_game_stats"); }
        #endregion

        #endregion

        #region /web/

        #region Category
        /// <summary>
        /// Lists all available categories.
        /// </summary>
        /// <returns>
        /// Returns the following JSON structure on success:
        /// {
        ///     "cats": {
        ///         "1": "Wunder der Technik",
        ///         ...
        ///     }
        /// }
        /// </returns>
        public JObject CategoryList() { return Request("/web/cats"); }

        /// <summary>
        /// Lists the number of Quizduell players.
        /// </summary>
        /// <returns></returns>
        public JObject CurrentPlayerNumber() { return Request("/web/num_players"); }
        #endregion

        #endregion

        #region Client Utility
        /// <summary>
        /// Returns the correct answer depended on a user supplied chance.
        /// </summary>
        /// <param name="correctAnswerChance">The chance to have a correct answer needs to be beetwen 0.0 [inklusive] and 1.0 [inklusive]</param>
        /// <returns></returns>
        public int GetAnswer(double correctAnswerChance = 0.8) { return (random.NextDouble() <= correctAnswerChance) ? 0 : random.Next(1, 4); }

        /// <summary>
        /// Normally we don't care what category we choose except in the last round where we need to chose the enemies category
        /// </summary>
        /// <param name="game"></param>
        /// <param name="requireAnswers">The required amount of answers for the current round</param>
        /// <returns></returns>
        public int GetCorrectCategoryID(JToken game, int requireAnswers)
        {
            // We don't care what category we choose except in the last round
            int category_id = random.Next(0, 3);
            if (requireAnswers == 3 && game["opponent_answers"].Count() != 0)
            {
                // it's the last Round and the opponent already picked a category
                List<int> category_choices = game["cat_choices"].Select(i => (int)i).ToList();
                category_id = category_choices.Last();
            }

            return category_id;
        }

        public int GetRequiredAnswerCount(JToken game)
        {
            // We start or its the last round so we only need 3 answers instead of 6
            int answers_count = 6;
            var opponent_answers = game["opponent_answers"];
            if (!opponent_answers.Any() || opponent_answers.Count() == 18) { answers_count = 3; }
            return answers_count;
        }
        #endregion
    }
}
