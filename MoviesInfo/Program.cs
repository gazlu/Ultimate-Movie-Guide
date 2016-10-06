using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp;

namespace MoviesInfo
{
    class Program
    {
        private static string posterAPIKey = "3088e9b6";
        private static string movieAPIKey = "3088e9b6";
        private static string API_URL = "http://www.omdbapi.com";
        private static string CSS_BOOTSTRAP = Properties.Resources.bootstrapcss;
        private static string CSS_SITE = Properties.Resources.templatecss;
        private static string TEMPLATE_MOVIE = Properties.Resources.movie;
        private static string TEMPLATE_GENRE = Properties.Resources.genre;
        private static string TEMPLATE_MOVIE_HTML = Properties.Resources.movietemplate;

        static void Main(string[] args)
        {
            string folder = string.Empty;
            string searchNested = "0";

            Console.Write("Enter folder path (enter [blank] to consider current folder): ");
            folder = Console.ReadLine();
            folder = string.IsNullOrWhiteSpace(folder) ? Directory.GetCurrentDirectory() : folder;

            Console.Write("Enter 1: scan nested directories, 0: scan current directory: ");
            searchNested = Console.ReadLine();
            if (searchNested != "0")
            {
                searchNested = searchNested != "1" ? "0" : "1";
            }

            var directory = Directory.Exists(folder) ? folder : Directory.GetCurrentDirectory();

            if (string.IsNullOrWhiteSpace(directory) == false)
            {
                var subDirectories = Directory.GetDirectories(directory);
                foreach (var subDirectory in subDirectories)
                {
                    MovieInfo(subDirectory);
                }

            }

            Console.ReadKey();
        }

        private static void MovieInfo(string directory)
        {
            var directoryInfo = new DirectoryInfo(directory);
            var movieName = CleanupMovieName(directoryInfo.Name);
            RestSharp.Http httpRequest = new RestSharp.Http();

            //RestSharp.RestClient restClient = new RestSharp.RestClient("http://img.omdbapi.com");
            RestSharp.RestClient restClient = new RestSharp.RestClient(API_URL);

            //var response = restClient.ExecuteAsGet(new RestSharp.RestRequest("?i=tt2294629&apikey=3088e9b6"), "GET");

            restClient
                .ExecuteAsyncGet(
                    new RestSharp.RestRequest("?s=" + movieName + "&y=&tomatoes=true&plot=full&r=json&apikey=" + movieAPIKey),
                    (response, handle) =>
                    {
                        ProcessMovie(directory, response.Content);
                    },
                    "GET"
                );

        }

        private static void ProcessMovie(string directory, string json)
        {
            var searchObject = JsonConvert.DeserializeObject<JsonObject>(json);
            if (searchObject != null)
            {
                if (searchObject.Count >= 0)
                {
                    var resultsJson = searchObject[0].ToString();
                    if (resultsJson == "False")
                        return;

                    var movies = JsonConvert.DeserializeObject<List<dynamic>>(resultsJson);
                    if (movies != null)
                    {
                        var movie = movies.FirstOrDefault();
                        if (movie != null)
                        {
                            RestSharp.RestClient restClient = new RestSharp.RestClient(API_URL);
                            restClient
                                    .ExecuteAsyncGet(
                                        new RestSharp.RestRequest("?i=" + movie.imdbID + "&apikey=" + movieAPIKey),
                                        (r, h) =>
                                        {
                                            WriteMovieDetails(directory, r.Content);
                                        },
                                        "GET"
                                    );
                        }
                    }
                }
            }
        }

        private static void WriteMovieDetails(string directory, string json)
        {
            var html = TEMPLATE_MOVIE_HTML;
            var movieDetails = JsonConvert.DeserializeObject<IDictionary<string, string>>(json);
            var posterUrl = movieDetails["Poster"];

            if (posterUrl != "N/A")
            {
                RestClient restClient = new RestClient(posterUrl);
                var posterBytes = restClient.DownloadData(new RestRequest("#", Method.GET));

                File.WriteAllBytes(Path.Combine(directory, "poster-imdb-gazlu.jpg"), posterBytes);
            }
            html = html.Replace("{{styles}}", CSS_BOOTSTRAP + " " + CSS_SITE);
            html = html.Replace("{{imdbid}}", movieDetails["imdbID"]);
            html = html.Replace("{{movie}}", TEMPLATE_MOVIE);
            html = html.Replace("{{title}}", movieDetails["Title"] + " (" + movieDetails["Year"] + ") ");
            html = html.Replace("{{plot}}", movieDetails["Plot"]);
            html = html.Replace("{{rating}}", movieDetails["imdbRating"]);
            var genres = movieDetails["Genre"].Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
            var genreHtml = string.Empty;
            genres.ForEach((genre) =>
            {
                genreHtml += TEMPLATE_GENRE.Replace("{{gen-cat}}", genre);
            });
            html = html.Replace("{{genre}}", genreHtml);
            File.WriteAllText(Path.Combine(directory, "info.html"), html);
            Console.WriteLine("Information page generated for " + movieDetails["Title"]);
        }

        private static string CleanupMovieName(string dirtyname)
        {
            var movieName = dirtyname;
            movieName = movieName.Replace('.', ' ');
            movieName = movieName.Replace('_', ' ');
            if (movieName.IndexOf("[") > 0)
            {
                movieName = movieName.Substring(0, movieName.IndexOf("["));
            }
            if (movieName.IndexOf("(") > 0)
            {
                movieName = movieName.Substring(0, movieName.IndexOf("("));
            }
            return movieName;
        }
    }
}
