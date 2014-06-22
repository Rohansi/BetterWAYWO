using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json;

namespace BetterWaywo
{
    public class AuthenticationConfig
    {
        private CookieContainer _cookies;

        public string UserAgent { get; set; }

        public Dictionary<string, string> Cookies { get; set; }

        public string SecurityToken { get; set; }

        [JsonIgnore]
        public CookieContainer CookieContainer
        {
            get
            {
                if (_cookies == null)
                {
                    _cookies = new CookieContainer();

                    var uri = new Uri("http://facepunch.com/");

                    foreach (var cookie in Cookies)
                    {
                        _cookies.Add(uri, new Cookie(cookie.Key, cookie.Value));
                    }
                }

                return _cookies;
            }
        }
    }

    public class RatingsConfig
    {
        public float Score { get; set; }

        public string[] Labels { get; set; }

        public bool Matches(string label)
        {
            return Labels.Any(x => x.Equals(label));
        }
    }

    public class ContentConfig
    {
        public float Score { get; set; }

        public string[] Extensions { get; set; }

        public string[] Tags { get; set; }

        public bool Matches(string tag, Uri uri)
        {
            if (!Tags.Any(x => x.Equals(tag)))
                return false;

            if (Extensions == null)
                return true;

            var extension = Path.GetExtension(uri.AbsolutePath);

            return Extensions.Any(x => x.Equals(extension));
        }
    }

    public class WeightsConfig
    {
        public float RatingsDefault { get; set; }

        public RatingsConfig[] Ratings { get; set; }

        public float GetRatingValue(string rating)
        {
            if (Ratings != null)
            {
                var match = Ratings.FirstOrDefault(x => x.Matches(rating));

                if (match != null)
                    return match.Score;
            }

            return RatingsDefault;
        }

        public float ContentDefault { get; set; }

        public ContentConfig[] Content { get; set; }

        public float GetContentValue(string tag, Uri uri)
        {
            if (Content != null)
            {
                var match = Content.FirstOrDefault(x => x.Matches(tag, uri));

                if (match != null)
                    return match.Score;
            }

            return ContentDefault;
        }
    }

    class Config
    {
        public AuthenticationConfig Authentication { get; set; }

        public WeightsConfig Weights { get; set; }
    }
}
