using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json.Linq;
using QuizDuell;

namespace QuizDuellBot
{
    internal class QuizDuellBot
    {
        // References
        private QuizDuellConfig config;
        private QuizDuellApi client;
        private JObject result;

        // Game Variables
        private int gamesToStart = 0;  // Number of Games which will always be started even if current games count is bigger then maxGameCount
        private int maxGameCount = 20; // How many random games to maintain
        private int maxGameTime = 360; // Number of minutes to play a game before giving up
        private double correctAnswerChance = 0.8; // Parameter to control the number of correct answers the play gives
        private bool excludeFriends = true; // TODO: add functionality to exclude the users on your friend list from gameplay

        public QuizDuellBot()
        {
            Console.WriteLine("Loading Config File.");
            config = QuizDuellConfig.LoadFromFile("config.json");
            //config = QuizDuellConfig.LoadFromFile("ios-config.json");
            //config = QuizDuellConfig.LoadFromFile("apk-config.json");
            client = new QuizDuellApi(config);
            result = new JObject();
        }

        public void GameStart()
        {
            // Do we have an auth cookie?
            // Try to login with the authentication Cookie
            result = client.CurrentUserGames();
            Debug.WriteLine("Current User Games Response: ");
            Debug.Write(result.ToString());
            Debug.WriteLine("");

            // Did the cookie login work?
            if (result["access"] != null && (bool)result["access"] == false)
            {
                result = client.CreateUser(config.User, config.Pass, config.Mail);
                Debug.WriteLine("Create User Response: ");
                Debug.Write(result.ToString());
                Debug.WriteLine("");

                result = client.LoginUser(config.User, config.Pass);
                Debug.WriteLine("Login User Response: ");
                Debug.Write(result.ToString());
                Debug.WriteLine("");
            }
            else { result["logged_in"] = true; }

            /// Returns the following JSON structure on success:
            /// {
            /// "logged_in": true,
            /// "settings": {...},
            /// "user": {...}
            /// }
            if ((bool)result["logged_in"] == true)
            {
                // TODO: We are logged in so Start the GameLoop - Create a new Thread
                Console.WriteLine("Successfully logged in!");
                GameLoop();

                // TODO: We are done, write the updated Auth Cookie / Config to disk
                QuizDuellConfig.SaveToFile(config);
            }

            // TODO: Remove this Placeholder
            Console.WriteLine("We are done, press any key to exit");
            Console.ReadLine();
        }

        private void GameLoop()
        {
            // TODO: Refactor think about switching over to static (class based) json desererialization
            result = client.CurrentUserGames();
            var user = result["user"];
            var friends = result["user"]["friends"];
            var games = result["user"]["games"];
            int activeGamesCount = 0;

            foreach (var game in games)
            {
                string game_id = (string)game["game_id"];
                var gameState = (GameState)(int)game["state"];

                var opponent = game["opponent"];
                List<int> opponent_answers = game["opponent_answers"].Select(i => (int)i).ToList();
                List<int> my_answers = game["your_answers"].Select(i => (int)i).ToList();

                bool my_turn = (bool)game["your_turn"];
                int elapsed_min = (int)game["elapsed_min"];

                // Count the Active Games, and GiveUp on any games that are beyond the timeout
                if (gameState == GameState.Active)
                {
                    activeGamesCount++;
                    if (!my_turn && elapsed_min > maxGameTime)
                    {
                        activeGamesCount--;
                        Console.WriteLine("Giving up game {0} against {1}", game_id, opponent["name"]);
                        client.GiveUp(game_id);
                    }
                }

                // First we accept any game requests, answer them the next iteration
                if (gameState == GameState.Waiting && my_turn)
                {
                    activeGamesCount++;
                    Console.WriteLine("Accepting game {0} invite from: {1}", game_id, opponent["name"]);
                    client.AcceptGame(game_id);
                }

                // Answer the questions
                else if (gameState == GameState.Active && my_turn)
                {
                    int answerCount = client.GetRequiredAnswerCount(game);
                    int category_id = client.GetCorrectCategoryID(game, answerCount);
                    int[] roundAnswers = new int[answerCount];

                    // This is the final round, so we can remove it from the activeGames 
                    if (answerCount == 3 && opponent_answers.Count > 0) { activeGamesCount--; }

                    int correctCount = 0;
                    for (int i = 0; i < roundAnswers.Length; i++)
                    {
                        int answer = client.GetAnswer(correctAnswerChance);
                        if (answer == 0) { correctCount++; }
                        roundAnswers[i] = answer;
                    }

                    // TODO: Refactor this list madness.
                    Console.WriteLine("Answering {0} questions against: {1} [correct: {2}]", answerCount, opponent["name"], correctCount);
                    var answers = new List<int>(my_answers);
                    answers.AddRange(roundAnswers);
                    client.UploadRoundAnswers(game_id, answers.ToArray(), category_id);
                }
            }

            // Stats
            var stats = client.CategoryStats();
            int rank = (int)stats["rank"];
            int numberOfUsers = (int)stats["n_users"];
            Console.WriteLine("---\nCurrently playing in {0} active games.", activeGamesCount);
            Console.WriteLine("My current rank is {0}/{1}", rank, numberOfUsers);
            Console.WriteLine("---");

            // Create new Games
            int gamesToStart = this.gamesToStart;
            if ((activeGamesCount + gamesToStart) < maxGameCount) { gamesToStart += maxGameCount - (activeGamesCount + gamesToStart); }
            for (int i = 0; i < gamesToStart; i++)
            {
                var newGame = client.StartRandomGame();
                Console.WriteLine("Starting random game against: {0}", newGame["game"]["opponent"]["name"]);
            }
        }
    }
}
