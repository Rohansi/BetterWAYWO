using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using NDesk.Options;
using Newtonsoft.Json;

namespace BetterWaywo
{
    class Program
    {
        public static readonly Encoding FacepunchEncoding = Encoding.GetEncoding("Windows-1252");

        public static int ThreadId;
        public static string OutputFile;

        public static Config Config;

        private static List<Post> _posts; 

        static void LoadConfig(String path)
        {
            if (!File.Exists(path)) {
                throw new Exception("Could not find config file.");
            }

            Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(path));
        }

        public static void Main(string[] args)
        {
            bool help = false;
            bool cache = false;
            int postCount = 20;

            Config = new Config {
                Authentication = new AuthenticationConfig {
                    UserAgent = "BetterWAYWO highlights generator",
                    Cookies = new Dictionary<string, string>()
                },

                Weights = new WeightsConfig {
                    RatingsDefault = 0f,
                    Ratings = new RatingsConfig[0],

                    ContentDefault = 0f,
                    Content = new ContentConfig[0]
                }
            };

            #region Option Parsing
            var options = new OptionSet()
            {
                { "thread=",
                    "thread ID to generate highlights for", v =>
                    {
                        if (!int.TryParse(v, out ThreadId))
                            throw new OptionException("thread must be given an integer", "thread");
                    } },

                { "out=",
                    "file to output to", 
                    v => OutputFile = v },

                { "posts=",
                    "number of posts to output (default 20)", v =>
                    {
                        if (!int.TryParse(v, out postCount))
                            throw new OptionException("posts must be given an integer value", "posts");
                    }},

                { "config=",
                    "specify a json file to load configuration options from",
                    LoadConfig },

                { "cache", 
                    "enable caching of thread data",
                    v => cache = v != null },

                { "h|help",
                    "show this help message",
                    v => help = v != null }
            };

            try
            {
                options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.WriteLine("error: " + e.Message);
                Console.WriteLine("Try `betterwaywo --help' for more information.");
                return;
            }

            if (ThreadId == default(int) || OutputFile == default(string))
            {
                help = true;
            }

            if (help)
            {
                Console.WriteLine("Usage: betterwaywo -thread=<ThreadID> -out=<OutputFile> [options]");
                Console.WriteLine("Generates highlights for Facepunch threads.");
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }
            #endregion

            postCount = postCount < 1 ? 1 : postCount;

            var cacheFile = string.Format("posts_{0}.json", ThreadId);
            var hasCache = cache && File.Exists(cacheFile);

            if (hasCache)
            {
                try
                {
                    _posts = JsonConvert.DeserializeObject<List<Post>>(File.ReadAllText(cacheFile));
                    Console.WriteLine("Using cached posts");
                }
                catch
                {
                    Console.WriteLine("Failed to read cache, ignoring");
                }
            }

            if (_posts == null)
            {
                int pageCount;
                try
                {
                    pageCount = Scraper.GetPageCount();
                    Console.WriteLine("Thread has {0} pages", pageCount);
                }
                catch
                {
                    Console.WriteLine("Invalid ThreadId (couldn't get page count)");
                    return;
                }

                try
                {
                    _posts = Scraper.GetThreadPosts(pageCount);
                }
                catch
                {
                    Console.WriteLine("Failed to scrape thread");
                    return;
                }
            }

            List<Post> highlights;

            try
            {
                highlights = _posts
                    .OrderByDescending(p => p.RatingsValue)
                    .Take(postCount * 2)                       // lets not read every posts' contents
                    .Where(p => p.ContentValue > 0)
                    .Where(p => !p.IsVotePost)
                    .OrderByDescending(p => p.RatingsValue * p.ContentMultiplier)
                    .GroupBy(p => p.Username)
                    .Select(g => g.First())
                    .Take(postCount)
                    .ToList();
            }
            catch
            {
                Console.WriteLine("Failed to read posts");
                return;
            }

            if (cache)
            {
                try
                {
                    var postsJson = JsonConvert.SerializeObject(_posts);
                    File.WriteAllText(cacheFile, postsJson);
                    Console.WriteLine("Wrote posts to cache");
                }
                catch
                {
                    Console.WriteLine("Failed to write cache, ignoring");
                }
            }

            try
            {
                using (var writer = new StreamWriter(OutputFile, false))
                {
                    foreach (var p in highlights)
                    {
                        if (p.Message.Length == 0)
                        {
                            Console.WriteLine("Failed to read post contents (length is 0)");
                            return;
                        }

                        writer.Write(p.Message);
                        writer.WriteLine();
                        writer.WriteLine();
                    }
                }
            }
            catch
            {
                Console.WriteLine("Failed to write output file");
                return;
            }

            Console.WriteLine("Done!");
        }
    }
}
