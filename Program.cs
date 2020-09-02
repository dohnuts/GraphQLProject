using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Collections.Specialized;
using System.Web;
using System.IO;
using System.Collections.Generic;
using GraphQL;
using GraphQL.Types;



public class WebServer
{
    private readonly HttpListener _listener = new HttpListener();

    public WebServer(string prefix)
    {
        if (!HttpListener.IsSupported)
        {
            throw new NotSupportedException("Needs Windows XP SP2, Server 2003 or later.");
        }

        if (prefix == null)
        {
            throw new ArgumentException("prefix");
        }

        _listener.Prefixes.Add(prefix);
        _listener.Start();
    }

    public void Run()
    {
        ThreadPool.QueueUserWorkItem((o) =>
        {
            var schema = Schema.For(@"
                type Actor {
                    name: String,
                    character: String
                }
                
                type Movie {
                    title: String,
                    year: String,
                    director: String
                    cast: [Actor]
                }

                type Query {
                    movies: [Movie]
                }
            ", _ =>
            {
                _.Types.Include<Query>();
            });

            Console.WriteLine("Webserver running...");
            try
            {
                while (_listener.IsListening)
                {
                    ThreadPool.QueueUserWorkItem((c) =>
                    {
                        var ctx = c as HttpListenerContext;
                        try
                        {
                            HttpListenerRequest request = ctx.Request;

                            // Display some information about the request.
                            Console.WriteLine("Got request: {0}", request.Url.OriginalString);

                            // Obtain a response object.
                            HttpListenerResponse response = ctx.Response;

                            if (request.Url.LocalPath.Contains("/api/graphql"))
                            {
                                // Handle graphQL queries
                                NameValueCollection parsedQuery = HttpUtility.ParseQueryString(request.Url.Query);
                                foreach (string key in parsedQuery.AllKeys)
                                {
                                    if (key.Contains("query"))
                                    {
                                        var json = schema.Execute(_ =>
                                        {
                                            _.Query = parsedQuery["query"];
                                        });

                                        // Construct a response.
                                        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(json);

                                        // Get a response stream and write the response to it.
                                        response.ContentLength64 = buffer.Length;
                                        response.ContentType = "text/html";
                                        response.OutputStream.Write(buffer, 0, buffer.Length);
                                    }
                                }
                            }
                            else
                            {
                                string page = ctx.Request.Url.LocalPath;
                                if (page != string.Empty && page[0] == '/')
                                {
                                    page = page.Substring(1);
                                }
                                if (page == "/" || page == string.Empty)
                                {
                                    page = "index.html";
                                }
                                string pagePath = Directory.GetCurrentDirectory() + "\\webpage\\" + page;

                                if (File.Exists(pagePath))
                                {
                                    string fileContent = File.ReadAllText(pagePath);
                                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(fileContent);
                                    response.ContentLength64 = buffer.Length;
                                    response.OutputStream.Write(buffer, 0, buffer.Length);
                                }
                                else
                                {
                                    Console.WriteLine("file not found");
                                }
                            }
                        }
                        catch
                        {
                            
                        }
                        finally
                        {
                            ctx.Response.OutputStream.Close();
                        }
                    }, _listener.GetContext());
                }
            }
            catch
            {
                
            }
        });
    }

    public void Stop()
    {
        _listener.Stop();
        _listener.Close();
    }
}


public class Actor
{
    public string Name { get; set; }
    public string Character { get; set; }
}


public class Movie
{
    public string Title { get; set; }
    public string Year { get; set; }
    public string Director { get; set; }
    public List<Actor> Cast { get; set; }
}


public class Query
{
    [GraphQLMetadata("movies")]
    public List<Movie> GetMovies(string title, string year, string director)
    {
        return new List<Movie>() {
        new Movie(){ Title ="The Dark Knight", Year="2008", Director="Christopher Nolan", Cast=new List<Actor>(){
            new Actor() { Name= "Christian Bale", Character= "Bruce Wayne / Batman" },
            new Actor() { Name= "Michael Caine", Character= "Alfred Pennyworth" },
            new Actor() { Name= "Heath Ledger", Character= "The Joker" } } },
        new Movie(){ Title ="Inception", Year="2010", Director="Christopher Nolan", Cast=new List<Actor>(){
            new Actor() { Name= "Leonardo DiCaprio", Character= "Dom Cobb" },
            new Actor() { Name= "Joseph Gordon-Levitt", Character= "Arthur" },
            new Actor() { Name= "Ellen Page", Character= "Ariadne" } } },
        new Movie(){ Title ="Django Unchained", Year="2012", Director="Quentin Tarantino", Cast=new List<Actor>(){
            new Actor() { Name= "Jamie Foxx", Character= "Django Freeman" },
            new Actor() { Name= "Leonardo DiCaprio", Character= "Calvin J. Candie" },
            new Actor() { Name= "Samuel L. Jackson", Character= "Stephen Warren" } } }
        };
    }
}



public class Program
{
    public static void Main(string[] args)
    {
        WebServer ws = new WebServer("http://localhost:8080/");
        ws.Run();
        Console.WriteLine("A basic Webserver. Press a key to quit.");
        Console.ReadKey();
        ws.Stop();
    }

    
}