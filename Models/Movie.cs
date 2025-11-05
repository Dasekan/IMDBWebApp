namespace IMDB_UI.Models
{
    public class Movie
    {
        public string? tconst { get; set; }
        public string? titleType { get; set; }
        public string? primaryTitle { get; set; }
        public string? originalTitle { get; set; }
        public bool? isAdult { get; set; }
        public string? startYear { get; set; }  // Keep as string to match database
        public string? endYear { get; set; }    // Keep as string to match database
        public int? runtimeMinutes { get; set; }
        public string? genres { get; set; }
    }
}