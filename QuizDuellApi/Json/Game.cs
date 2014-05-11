using System.Collections.Generic;

namespace QuizDuell.Json
{
    public class Game
    {
        // uploaded_round_answers
        public List<int> your_answers { get; set; }
        public int state { get; set; }
        public int elapsed_min { get; set; }
        public bool your_turn { get; set; }
        public long game_id { get; set; }
        public List<int> cat_choices { get; set; }
        public List<int> opponent_answers { get; set; }
        public List<Message> messages { get; set; }
        public Opponent opponent { get; set; }

        public List<Question> questions { get; set; }
        public int? rating_bonus { get; set; }

        public long? give_up_player_id { get; set; }
        public bool? you_gave_up { get; set; }
    }
}