using System.Collections.Generic;

namespace QuizDuell.Json
{
    public class User
    {
        // current_user_games
        public string user_id { get; set; }
        public string name { get; set; }
        public bool qc { get; set; }
        public List<Game> games { get; set; }
        public int q_reviewer { get; set; }
        public List<Friend> friends { get; set; }
        public string email { get; set; }
        public string avatar_code { get; set; }
        public List<object> blocked { get; set; }

        // find_user
        public string facebook_id { get; set; }
        //public string user_id { get; set; }
        //public string name { get; set; }
        //public object avatar_code { get; set; }


        // top_list_rating
        public object key { get; set; }
        public int rating { get; set; }
        //public string name { get; set; }
        //public string avatar_code { get; set; }

        // top_list_writers
        public int n_approved_questions { get; set; }
        //public string name { get; set; }
        //public string avatar_code { get; set; }
    }
}