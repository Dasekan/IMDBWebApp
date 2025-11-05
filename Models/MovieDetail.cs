namespace IMDB_UI.Models
{
    public class MovieDetail
    {
        public string? tconst { get; set; }
        public string? primaryTitle { get; set; }
        public string? originalTitle { get; set; }
        public string? genres { get; set; }
        public int? startYear { get; set; }
        public int? runtimeMinutes { get; set; }

        // MAKE SURE THIS IS INITIALIZED
        public List<Person> Principals { get; set; } = new List<Person>();
    }
}