namespace MovieSearchApp.Models
{
    public class SearchViewModel
    {
        public List<Movie>? Movies { get; set; }
        public List<Person>? People { get; set; }
        public string? Q { get; set; }
        public string? Pq { get; set; }
        public Movie? MovieDetail { get; set; }
        public List<Principal>? Principals { get; set; }
    }
}