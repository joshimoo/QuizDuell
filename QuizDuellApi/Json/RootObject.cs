using System.Collections.Generic;

namespace QuizDuell.Json
{
    public class RootObject
    {
        // current_user_games
        public bool logged_in { get; set; }
        public User user { get; set; }
        public Settings settings { get; set; }

        // start_random_game
        public Game game { get; set; }

        // top_list_rating
        public List<User> users { get; set; }

        // my_stats
        public int rating { get; set; }
        public int n_users { get; set; }
        public int rank { get; set; }
        public int n_questions_answered { get; set; }
        public int n_games_tied { get; set; }
        public int n_questions_correct { get; set; }
        public int n_perfect_games { get; set; }
        public List<object> cat_stats { get; set; }
        public int n_games_played { get; set; }
        public int n_games_lost { get; set; }
        public int n_games_won { get; set; }

        // my_game_stats
        public List<GameStat> game_stats { get; set; }
    }
}