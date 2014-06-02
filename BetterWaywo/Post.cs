using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace BetterWaywo
{
    class Post
    {
        private static readonly Regex ContentRegex = new Regex(@"\[(img|vid|media|video)[^\]]*?\]([^\[\]]*?)\[/\1\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex VoteImgRegex = new Regex(@"\[img\]http://www\.facepunch\.com/fp/ratings/\S+?\.png\[/img\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex VoteNameRegex = new Regex(@"agree|disagree|funny|winner|zing|informative|friendly|useful|programming king|optimistic|artistic|late|dumb|lua king|lua helper", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private float? _ratingsValue;
        private float? _contentValue;
        private string _message;
        private bool? _isVotePost;
        private string _username;

        public readonly int Id;
        public readonly Dictionary<string, int> Ratings;

        [JsonIgnore]
        public float RatingsValue
        {
            get
            {
                if (!_ratingsValue.HasValue)
                    _ratingsValue = Ratings.Sum(r => Program.Config.Weights.GetRatingValue(r.Key) * r.Value);
                return _ratingsValue.Value;
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
            set
            {
                _message = value;
            }
        }

        [JsonIgnore]
        public bool HasContent
        {
            get
            {
                return ContentValue > 0;
            }
        }

        [JsonIgnore]
        public float ContentValue
        {
            get
            {
                if (!_contentValue.HasValue)
                    _contentValue = GetMessageValue(Message);
                return _contentValue.Value;
            }
        }

        [JsonIgnore]
        public float ContentMultiplier
        {
            get
            {
                const double height = 0.5;
                const double minimum = 0.5;
                const double preferredValue = 1.5;
                const double standardDeviation = 4.0; // preferredValue +/- standardDeviation = ~60% 

                var res = height * Math.Exp(-(Math.Pow(ContentValue - preferredValue, 2) / (2 * Math.Pow(standardDeviation, 2f)))) + minimum;
                //Console.WriteLine("{0} -> {1:R}", ContentValue, res);

                return (float)res;
            }
        }

        [JsonIgnore]
        public bool IsVotePost
        {
            get
            {
                if (!_isVotePost.HasValue)
                {
                    var hasVoteImages = VoteImgRegex.IsMatch(Message);
                    var hasVoteText = VoteNameRegex
                        .Matches(Message)
                        .Cast<Match>()
                        .GroupBy(m => m.Value)
                        .Select(g => g.First())
                        .Count() >= 2;

                    _isVotePost = hasVoteImages || hasVoteText;
                }

                return _isVotePost.Value;
            }
        }

        [JsonIgnore]
        public string Username
        {
            get
            {
                if (_username == null)
                    _username = Regex.Match(Message, @"\[QUOTE=(.*?);").Groups[1].Value;
                return _username;
            }
        }

        public Post(int id, Dictionary<string, int> ratings)
        {
            Id = id;
            Ratings = ratings;

            _ratingsValue = null;
            _contentValue = null;
            _message = null;
            _username = null;
        }

        public bool ShouldSerializeMessage()
        {
            return _message != null;
        }

        public static float GetMessageValue(string message)
        {
            float value = 0;

            var contentTags = ContentRegex.Matches(message);
            foreach (var tag in contentTags.Cast<Match>())
            {
                var tagString = tag.Groups[1].Value.ToLower();
                Uri uri;
                Uri.TryCreate(tag.Groups[2].Value, UriKind.Absolute, out uri);

                value += Program.Config.Weights.GetContentValue(tagString, uri);
            }

            return value;
        }

        private static string GetPostContents(int postId)
        {
            Console.WriteLine("Reading post {0}", postId);

            var request = Scraper.CreateRequest(String.Format("http://facepunch.com/ajax.php?do=getquotes&p={0}", postId));
            request.Method = "POST";

            var values = new NameValueCollection();
            values["do"] = "getquotes";
            values["p"] = postId.ToString("D");

            request.ContentType = "application/x-www-form-urlencoded";

            using (var stream = new StreamWriter(request.GetRequestStream())) {
                stream.Write("do=getquotes&p={0}", postId);
            }

            var text = String.Empty;

            using (var response = request.GetResponse()) {
                using (var stream = new StreamReader(response.GetResponseStream(), Program.FacepunchEncoding)) {
                    text = stream.ReadToEnd();
                }
            }

            var lines = text.Split('\n');

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
}
