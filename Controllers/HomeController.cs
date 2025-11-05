using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using IMDB_UI.Models;
using System.Data.SqlClient;

namespace IMDB_UI.Controllers
{
    public class HomeController : Controller
    {
        private readonly string _connStr;

        public HomeController(IConfiguration configuration)
        {
            _connStr = configuration.GetConnectionString("DefaultConnection");
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(_connStr);
        }

        public IActionResult Index(string q, string type)
        {
            if (string.IsNullOrWhiteSpace(q))
                return View();

            if (type == "people")
                return RedirectToAction("SearchPeople", new { q });

            return RedirectToAction("SearchMovies", new { q });
        }

        public IActionResult SearchMovies(string q)
        {
            var movies = new List<Movie>();
            using (SqlConnection conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new SqlCommand("EXEC dbo.sp_SearchMovies @SearchTerm", conn))
                {
                    cmd.Parameters.AddWithValue("@SearchTerm", q ?? "");
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            movies.Add(new Movie
                            {
                                tconst = reader["tconst"].ToString(),
                                primaryTitle = reader["primaryTitle"].ToString(),
                                originalTitle = reader["originalTitle"].ToString(),
                                genres = reader["genres"].ToString(),
                                runtimeMinutes = reader["runtimeMinutes"] as int?
                            });
                        }
                    }
                }
            }
            ViewBag.Query = q;
            return View("Movies", movies);
        }

        public IActionResult SearchPeople(string q)
        {
            var people = new List<Person>();
            using (SqlConnection conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new SqlCommand("EXEC dbo.sp_SearchPeople @SearchTerm", conn))
                {
                    cmd.Parameters.AddWithValue("@SearchTerm", q ?? "");
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            people.Add(new Person
                            {
                                nconst = reader["nconst"].ToString(),
                                primaryName = reader["primaryName"].ToString(),
                                primaryProfession = reader["primaryProfession"].ToString()
                            });
                        }
                    }
                }
            }
            ViewBag.Query = q;
            return View("People", people);
        }

        public IActionResult MovieDetail(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Movie ID is required!";
                return RedirectToAction("Index");
            }

            try
            {
                var movie = new MovieDetail();

                using (SqlConnection conn = GetConnection())
                {
                    conn.Open();

                    // Get basic movie info
                    string movieQuery = @"
                SELECT tconst, primaryTitle, originalTitle, genres, startYear, runtimeMinutes 
                FROM title_basics 
                WHERE tconst = @tconst";

                    using (var cmd = new SqlCommand(movieQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@tconst", id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                movie.tconst = reader["tconst"]?.ToString() ?? "";
                                movie.primaryTitle = reader["primaryTitle"]?.ToString() ?? "";
                                movie.originalTitle = reader["originalTitle"]?.ToString() ?? "";
                                movie.genres = reader["genres"]?.ToString() ?? "";
                                movie.startYear = reader["startYear"] as int?;
                                movie.runtimeMinutes = reader["runtimeMinutes"] as int?;
                            }
                            else
                            {
                                TempData["ErrorMessage"] = "Movie not found!";
                                return RedirectToAction("Index");
                            }
                        }
                    }

                    // Get cast/crew information
                    string principalsQuery = @"
                SELECT p.nconst, n.primaryName, n.primaryProfession 
                FROM title_principals p 
                INNER JOIN name_basics n ON p.nconst = n.nconst 
                WHERE p.tconst = @tconst 
                ORDER BY p.ordering";

                    using (var cmd2 = new SqlCommand(principalsQuery, conn))
                    {
                        cmd2.Parameters.AddWithValue("@tconst", id);
                        using (var reader = cmd2.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                movie.Principals.Add(new Person
                                {
                                    nconst = reader["nconst"]?.ToString() ?? "",
                                    primaryName = reader["primaryName"]?.ToString() ?? "",
                                    primaryProfession = reader["primaryProfession"]?.ToString() ?? ""
                                });
                            }
                        }
                    }
                }

                return View("MovieDetail", movie);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading movie details: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
        // CREATE MOVIE - GET (Show the form)
        [HttpGet]
        public IActionResult CreateMovie()
        {
            return View();
        }

        // CREATE MOVIE - POST (Save the movie)
        [HttpPost]
        // CREATE MOVIE - POST (Save the movie)
        [HttpPost]
        // CREATE MOVIE - POST (Save the movie)
        // CREATE MOVIE - POST (Simple GUID version)
        [HttpPost]
        public IActionResult CreateMovie(Movie movie)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection conn = GetConnection())
                    {
                        conn.Open();

                        // Generate unique ID using GUID (guaranteed unique)
                        string newTconst = "tt" + Guid.NewGuid().ToString("N").Substring(0, 12);

                        using (var cmd = new SqlCommand("EXEC dbo.sp_InsertMovie @tconst, @titleType, @primaryTitle, @originalTitle, @isAdult, @startYear, @endYear, @runtimeMinutes, @genres", conn))
                        {
                            cmd.Parameters.AddWithValue("@tconst", newTconst);
                            cmd.Parameters.AddWithValue("@titleType", movie.titleType ?? "movie");
                            cmd.Parameters.AddWithValue("@primaryTitle", movie.primaryTitle ?? "");
                            cmd.Parameters.AddWithValue("@originalTitle", movie.originalTitle ?? movie.primaryTitle ?? "");
                            cmd.Parameters.AddWithValue("@isAdult", movie.isAdult ?? false);

                            // Handle startYear
                            if (!string.IsNullOrEmpty(movie.startYear))
                            {
                                cmd.Parameters.AddWithValue("@startYear", movie.startYear);
                            }
                            else
                            {
                                cmd.Parameters.AddWithValue("@startYear", DBNull.Value);
                            }

                            // Handle endYear
                            if (!string.IsNullOrEmpty(movie.endYear))
                            {
                                cmd.Parameters.AddWithValue("@endYear", movie.endYear);
                            }
                            else
                            {
                                cmd.Parameters.AddWithValue("@endYear", DBNull.Value);
                            }

                            // Handle runtimeMinutes
                            if (movie.runtimeMinutes != null && movie.runtimeMinutes > 0)
                            {
                                cmd.Parameters.AddWithValue("@runtimeMinutes", movie.runtimeMinutes);
                            }
                            else
                            {
                                cmd.Parameters.AddWithValue("@runtimeMinutes", DBNull.Value);
                            }

                            cmd.Parameters.AddWithValue("@genres", movie.genres ?? "");

                            cmd.ExecuteNonQuery();
                        }
                    }

                    TempData["SuccessMessage"] = "Movie created successfully!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error creating movie: {ex.Message}";
                }
            }

            return View(movie);
        }
        // Improved method to generate new tconst
        private string GenerateNewTconst(SqlConnection conn)
        {
            try
            {
                // Check both title_basics and title_basics_STG tables
                string maxTconst = "";
                using (var cmd = new SqlCommand(@"
            SELECT MAX(tconst) 
            FROM (
                SELECT tconst FROM title_basics 
                UNION ALL 
                SELECT tconst FROM title_basics_STG
            ) AS combined", conn))
                {
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        maxTconst = result.ToString();
                    }
                }

                // Generate new tconst
                if (string.IsNullOrEmpty(maxTconst) || !maxTconst.StartsWith("tt"))
                {
                    return "tt1000001"; // Start from a higher number
                }
                else
                {
                    // Extract number part and increment
                    if (int.TryParse(maxTconst.Substring(2), out int number))
                    {
                        return $"tt{(number + 1).ToString().PadLeft(7, '0')}";
                    }
                    else
                    {
                        // Fallback: use timestamp
                        return $"tt{DateTime.Now:yyyyMMddHHmmss}";
                    }
                }
            }
            catch (Exception)
            {
                // Ultimate fallback: use GUID
                return "tt" + Guid.NewGuid().ToString("N").Substring(0, 10);
            }
        }        // EDIT MOVIE - GET (Show the edit form)
        [HttpGet]
        public IActionResult EditMovie(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Movie ID is required!";
                return RedirectToAction("Index");
            }

            try
            {
                using (SqlConnection conn = GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SqlCommand("SELECT * FROM title_basics WHERE tconst = @tconst", conn))
                    {
                        cmd.Parameters.AddWithValue("@tconst", id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var movie = new Movie
                                {
                                    tconst = reader["tconst"]?.ToString(),
                                    titleType = reader["titleType"]?.ToString(),
                                    primaryTitle = reader["primaryTitle"]?.ToString(),
                                    originalTitle = reader["originalTitle"]?.ToString(),
                                    isAdult = reader["isAdult"] as bool?,
                                    // Handle text fields properly
                                    startYear = reader["startYear"]?.ToString(),
                                    endYear = reader["endYear"]?.ToString(),
                                    runtimeMinutes = reader["runtimeMinutes"] as int?,
                                    genres = reader["genres"]?.ToString()
                                };
                                return View(movie);
                            }
                        }
                    }
                }

                TempData["ErrorMessage"] = "Movie not found!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading movie: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
        // EDIT MOVIE - POST (Update the movie)
       [HttpPost]
public IActionResult EditMovie(Movie movie)
{
    Console.WriteLine($"Editing movie: {movie.tconst}");
    Console.WriteLine($"Title: {movie.primaryTitle}");
    Console.WriteLine($"Start Year: {movie.startYear}");
    Console.WriteLine($"End Year: {movie.endYear}");
    
    if (ModelState.IsValid)
    {
        try
        {
            using (SqlConnection conn = GetConnection())
            {
                conn.Open();
                
                // First, let's check if the movie exists
                string checkQuery = "SELECT COUNT(*) FROM title_basics WHERE tconst = @tconst";
                using (var checkCmd = new SqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@tconst", movie.tconst ?? "");
                    int exists = (int)checkCmd.ExecuteScalar();
                    Console.WriteLine($"Movie exists: {exists > 0}");
                    
                    if (exists == 0)
                    {
                        TempData["ErrorMessage"] = "Movie not found!";
                        return View(movie);
                    }
                }
                
                // Now update the movie
                string updateQuery = @"
                    UPDATE title_basics 
                    SET titleType = @titleType,
                        primaryTitle = @primaryTitle,
                        originalTitle = @originalTitle,
                        isAdult = @isAdult,
                        startYear = @startYear,
                        endYear = @endYear,
                        runtimeMinutes = @runtimeMinutes,
                        genres = @genres
                    WHERE tconst = @tconst";
                
                using (var cmd = new SqlCommand(updateQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@tconst", movie.tconst ?? "");
                    cmd.Parameters.AddWithValue("@titleType", movie.titleType ?? "movie");
                    cmd.Parameters.AddWithValue("@primaryTitle", movie.primaryTitle ?? "");
                    cmd.Parameters.AddWithValue("@originalTitle", movie.originalTitle ?? movie.primaryTitle ?? "");
                    cmd.Parameters.AddWithValue("@isAdult", movie.isAdult ?? false);
                    
                    // Handle startYear
                    if (!string.IsNullOrEmpty(movie.startYear))
                    {
                        cmd.Parameters.AddWithValue("@startYear", movie.startYear);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@startYear", DBNull.Value);
                    }
                    
                    // Handle endYear
                    if (!string.IsNullOrEmpty(movie.endYear))
                    {
                        cmd.Parameters.AddWithValue("@endYear", movie.endYear);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@endYear", DBNull.Value);
                    }
                    
                    // Handle runtimeMinutes
                    if (movie.runtimeMinutes != null && movie.runtimeMinutes > 0)
                    {
                        cmd.Parameters.AddWithValue("@runtimeMinutes", movie.runtimeMinutes);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@runtimeMinutes", DBNull.Value);
                    }
                    
                    cmd.Parameters.AddWithValue("@genres", movie.genres ?? "");

                    int rowsAffected = cmd.ExecuteNonQuery();
                    Console.WriteLine($"Rows affected: {rowsAffected}");
                    
                    if (rowsAffected > 0)
                    {
                        TempData["SuccessMessage"] = "Movie updated successfully!";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "No changes were made to the movie.";
                        return View(movie);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            TempData["ErrorMessage"] = $"Error updating movie: {ex.Message}";
        }
    }
    else
    {
        // Show validation errors
        var errors = ModelState.Values.SelectMany(v => v.Errors);
        foreach (var error in errors)
        {
            Console.WriteLine($"Validation error: {error.ErrorMessage}");
        }
        TempData["ErrorMessage"] = "Please fix the validation errors below.";
    }

    return View(movie);
} // DELETE MOVIE
        [HttpPost]
        public IActionResult DeleteMovie(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Movie ID is required!";
                return RedirectToAction("Index");
            }

            try
            {
                using (SqlConnection conn = GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SqlCommand("EXEC dbo.sp_DeleteMovie @tconst", conn))
                    {
                        cmd.Parameters.AddWithValue("@tconst", id);
                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["SuccessMessage"] = "Movie deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting movie: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // CREATE PERSON - GET (Show the form)
        [HttpGet]
        public IActionResult CreatePerson()
        {
            return View();
        }

        // CREATE PERSON - POST (Save the person)
        [HttpPost]
        public IActionResult CreatePerson(Person person)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection conn = GetConnection())
                    {
                        conn.Open();
                        using (var cmd = new SqlCommand("EXEC dbo.sp_InsertPerson @nconst, @primaryName, @birthYear, @deathYear, @primaryProfession, @knownForTitles", conn))
                        {
                            cmd.Parameters.AddWithValue("@nconst", person.nconst ?? "");
                            cmd.Parameters.AddWithValue("@primaryName", person.primaryName ?? "");

                            // Handle birthYear
                            if (person.birthYear != null )
                            {
                                cmd.Parameters.AddWithValue("@birthYear", person.birthYear);
                            }
                            else
                            {
                                cmd.Parameters.AddWithValue("@birthYear", DBNull.Value);
                            }

                            // Handle deathYear
                            if (person.deathYear != null )
                            {
                                cmd.Parameters.AddWithValue("@deathYear", person.deathYear);
                            }
                            else
                            {
                                cmd.Parameters.AddWithValue("@deathYear", DBNull.Value);
                            }

                            cmd.Parameters.AddWithValue("@primaryProfession", person.primaryProfession ?? "");
                            cmd.Parameters.AddWithValue("@knownForTitles", person.knownForTitles ?? "");

                            cmd.ExecuteNonQuery();
                        }
                    }

                    TempData["SuccessMessage"] = "Person created successfully!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error creating person: {ex.Message}";
                }
            }

            return View(person);
        }
    }
}