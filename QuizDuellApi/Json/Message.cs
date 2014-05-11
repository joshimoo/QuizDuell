namespace QuizDuell.Json
{
    public class Message
    {
        public string text { get; set; }
        public string created_at { get; set; }
        public long from { get; set; }
        public string id { get; set; }
        public long to { get; set; }
    }
}
