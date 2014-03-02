using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace BetterWaywo
{
    class Post
    {
        private string _contents;
        private string _username;

        public readonly int Id;
        public readonly Dictionary<string, int> Ratings;

        public float Value
        {
            get
            {
                return Ratings.Sum(r => GetRatingValue(r.Key) * r.Value);
            }
        }

        public string Contents
        {
            get
            {
                if (_contents == null)
                    _contents = GetPostContents(Id);
                return _contents;
            }
        }

        public string Username
        {
            get
            {
                if (_username == null)
                    _username = Regex.Match(Contents, "\\[QUOTE=(.*?);").Groups[1].Value;
                return _username;
            }
        }

        public Post(int id, Dictionary<string, int> ratings)
        {
            Id = id;
            Ratings = ratings;
            _contents = null;
        }

        private static string GetPostContents(int postId)
        {
            Console.WriteLine("Reading post {0}", postId);

            using (var request = new WebClient())
            {
                var values = new NameValueCollection();
                values["do"] = "getquotes";
                values["p"] = postId.ToString("D");

                var response = Program.FacepunchEncoding.GetString(request.UploadValues(string.Format("http://facepunch.com/ajax.php?do=getquotes&p={0}", postId), values));
                var lines = response.Split('\n');

                var result = new StringBuilder();
                for (var i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];

                    if (i == 0 || i >= lines.Length - 3)
                        continue;

                    if (i == 1)
                        result.Append(line.Substring(17));
                    else
                        result.Append(line);

                    result.Append('\n');
                }

                return result.ToString().Trim();
            }
        }

        private static float GetRatingValue(string rating)
        {
            switch (rating)
            {
                case "Programming King":
                case "Lua King":
                    return 3.0f;
                case "Winner":
                case "Useful":
                case "Artistic":
                case "Lua Helper":
                    return 2.0f;
                case "Funny":
                case "Informative":
                    return 1.0f;
                default:
                    return -1.0f; // "junk" ratings
            }
        }
    }
}
