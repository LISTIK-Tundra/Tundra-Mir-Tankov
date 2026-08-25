using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Listik
{
    internal sealed class UpdateInfo
    {
        public Version Version { get; set; }
        public string Tag { get; set; }
        public string ReleaseUrl { get; set; }
    }

    internal static class UpdateService
    {
        private const string LatestReleaseApi =
            "https://api.github.com/repos/LISTIK-Tundra/Tundra-Mir-Tankov/releases/latest";

        public static async Task<UpdateInfo> GetLatestReleaseAsync()
        {
            try
            {
                using (var client = new WebClient())
                {
                    client.Headers[HttpRequestHeader.UserAgent] = "Listik-Update-Checker";
                    client.Headers[HttpRequestHeader.Accept] = "application/vnd.github+json";
                    var json = await client.DownloadStringTaskAsync(new Uri(LatestReleaseApi));
                    var tagMatch = Regex.Match(json, "\\\"tag_name\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"");
                    var urlMatch = Regex.Match(json, "\\\"html_url\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"");
                    if (!tagMatch.Success || !urlMatch.Success)
                        return null;

                    var version = ParseVersion(tagMatch.Groups[1].Value);
                    if (version == null)
                        return null;

                    return new UpdateInfo
                    {
                        Version = version,
                        Tag = tagMatch.Groups[1].Value,
                        ReleaseUrl = urlMatch.Groups[1].Value.Replace("\\/", "/")
                    };
                }
            }
            catch
            {
                return null;
            }
        }

        public static Version GetLocalVersion(int rawVersion)
        {
            var digits = Math.Abs(rawVersion).ToString();
            if (digits.Length == 3)
                return new Version(digits[0] + "." + digits[1] + "." + digits[2]);
            return new Version(rawVersion, 0);
        }

        private static Version ParseVersion(string tag)
        {
            var normalized = tag.Trim().TrimStart('v', 'V');
            if (Regex.IsMatch(normalized, "^\\d{3}$"))
                return GetLocalVersion(int.Parse(normalized));
            Version version;
            return Version.TryParse(normalized, out version) ? version : null;
        }
    }
}
