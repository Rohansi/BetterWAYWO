using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BetterWaywo
{
    class Program
    {
        public static readonly Encoding FacepunchEncoding = Encoding.GetEncoding("Windows-1252");

        public static int ThreadId;
        public static string OutputFile;

        public static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: BetterWaywo <ThreadId> <OutputFile> [PostCount]");
                return;
            }

            if (!int.TryParse(args[0], out ThreadId))
            {
                Console.WriteLine("Invalid ThreadId");
                return;
            }

            OutputFile = args[1];

            int postCount;
            if (args.Length < 3 || !int.TryParse(args[2], out postCount))
                postCount = 20;

            postCount = postCount < 1 ? 1 : postCount;

            int pageCount;
            List<Post> posts;

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
                posts = Scraper.GetThreadPosts(pageCount);
            }
            catch
            {
                Console.WriteLine("Failed to get posts");
                return;
            }

            posts = posts.OrderByDescending(p => p.Value)
                         .Take(postCount * 2)                       // lets not read every posts' contents
                         .Where(p => p.HasContent)                  // ignore posts with no content
                         .Distinct(new PostUsernameComparer())      // one highlight per person
                         .Take(postCount)
                         .ToList();

            try
            {
                using (var writer = new StreamWriter(OutputFile, false))
                {
                    foreach (var p in posts)
                    {
                        if (p.Message.Length == 0)
                        {
                            Console.WriteLine("Failed to read post contents");
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
            }

            Console.WriteLine("Done!");
        }
    }

    class PostUsernameComparer : IEqualityComparer<Post>
    {
        public bool Equals(Post x, Post y)
        {
            return x.Username == y.Username;
        }

        public int GetHashCode(Post p)
        {
            return p.Username.GetHashCode();
        }
    }
}
