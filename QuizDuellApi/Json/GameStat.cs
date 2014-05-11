namespace QuizDuell.Json
{
    public class GameStat
    {
        public string name { get; set; }
        public string user_id { get; set; }
        public string facebook_id { get; set; }
        public object avatar_code { get; set; }

        public int n_games_tied { get; set; }
        public int n_games_lost { get; set; }
        public int n_games_won { get; set; }
    }
}
