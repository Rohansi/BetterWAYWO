using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace BetterWaywo
{
    static class Scraper
    {
        private const string ThreadPageString = "http://facepunch.com/showthread.php?t={0}&page={1}";

        public static List<Post> GetThreadPosts(int pageCount)
        {
            var posts = new List<Post>();

            Parallel.For(1, pageCount + 1, page =>
            {
                Console.WriteLine("Scraping page {0}", page);

                try
                {
                    var html = GetHtmlDocument(string.Format(ThreadPageString, Program.ThreadId, page));
                    var postElements = html.DocumentNode.SelectNodes("//ol[@id='posts']//span[@class='rating_results']");
                    var first = true;

                    foreach (var p in postElements)
                    {
                        if (page <= 1 && first)
                        {
                            first = false;
                            continue;
                        }

                        var id = int.Parse(Regex.Match(p.Attributes["id"].Value, "rating_(\\d+)").Groups[1].Value);
                        var ratings = new Dictionary<string, int>();

                        foreach (var r in p.Elements("span"))
                        {
                            var ratingType = r.Element("img").Attributes["alt"].Value;
                            var ratingValue = int.Parse(r.Element("strong").InnerText);
                            ratings.Add(ratingType, ratingValue);
                        }

                        lock (posts)
                            posts.Add(new Post(id, ratings));
                    }
                }
                catch
                {
                    Console.WriteLine("Failed to scrape page {0}, ignoring", page);
                }

                if (pageCount >= 50)
                    Thread.Sleep(2500); // be nice
            });

            return posts;
        }

        public static int GetPageCount()
        {
            var html = GetHtmlDocument(string.Format(ThreadPageString, Program.ThreadId, 1));
            var lastPageUrl = html.DocumentNode.SelectSingleNode("//div[@id='pagination_top']//span[@class='first_last']/a").Attributes["href"].Value;
            var lastPage = Regex.Match(lastPageUrl, @"page=(\d+)").Groups[1].Value;
            return int.Parse(lastPage);
        }

        public static HttpWebRequest CreateRequest(string address)
        {
            var request = (HttpWebRequest) WebRequest.Create(address);
            request.KeepAlive = true;
            request.Timeout = 15000;

            request.CookieContainer = Program.AuthCookies;
            request.UserAgent = Program.UserAgent;
            request.ContentType = "text/html";

            return request;
        }

        private static HtmlDocument GetHtmlDocument(string address)
        {
            var html = new HtmlDocument();

            using (var response = CreateRequest(address).GetResponse())
            using (var reader = new StreamReader(response.GetResponseStream(), Program.FacepunchEncoding))
                html.Load(reader);

            return html;
        }
    }
}
