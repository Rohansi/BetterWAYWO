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
        private float? _value;
        private string _message;
        private string _username;
        private bool? _hasContent;

        public readonly int Id;
        public readonly Dictionary<string, int> Ratings;

        public float Value
        {
            get
            {
                if (!_value.HasValue)
                    _value = Ratings.Sum(r => GetRatingValue(r.Key) * r.Value);
                return _value.Value;
            }
        }

        public string Message
        {
            get
            {
                if (_message == null)
                    _message = GetPostContents(Id);
                return _message;
            }
        }

        public string Username
        {
            get
            {
                if (_username == null)
                    _username = Regex.Match(Message, @"\[QUOTE=(.*?);").Groups[1].Value;
                return _username;
            }
        }

        public bool HasContent
        {
            get
            {
                if (!_hasContent.HasValue)
                    _hasContent = ContentRegex.IsMatch(Message);
                return _hasContent.Value;
            }
        }

        private static readonly Regex ContentRegex = new Regex(@"\[(img|vid|media|video)[^\]]*?\][^\[\]]*?\[/\1\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public Post(int id, Dictionary<string, int> ratings)
        {
            Id = id;
            Ratings = ratings;

            _value = null;
            _message = null;
            _username = null;
            _hasContent = null;
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
