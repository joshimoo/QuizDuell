namespace QuizDuell.Json
{
    public class Question
    {
        // start_random_game
        public int cat_id { get; set; }
        public string cat_name { get; set; }

        public string question { get; set; }
        public object q_id { get; set; }

        public string correct { get; set; }
        public string wrong1 { get; set; }
        public string wrong2 { get; set; }
        public string wrong3 { get; set; }

        public string timestamp { get; set; }
    }
}