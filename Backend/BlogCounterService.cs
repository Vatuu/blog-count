using System.Collections.Immutable;
using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Backend;
    
public class BlogCounterService {

    //TODO: z.b. | "" (???) | -word word- (remove if no character next to it)
    
    private const string WpRoute = "/wp-json";
    private const string WpEndpointPost = "/wp/v2/posts";
    private const string WpIndexWantedFields = "_fields=id,modified";
    private const string WpPostWantedFields = "_fields=content.rendered,title";

    private const string RegexStrongHighlight = @"<strong>|</strong>";
    private const string RegexHtmlPurge = @"(\t|\r)|<[^>]*>|&.{5}";
    private const string RegexPunctuationPurge = @"\.|,|;|:(?=\s)|!|\?|\(|\)|\[|/|\]""";
    private const string RegexSpacingCleanup = @"\n+|\s+";
    
    private readonly Uri _targetBlog;

    public IDictionary<int, DateTime> AvailablePosts { get; private set; }
    public IDictionary<int, PostData> WordCounts { get; private set; }

    public BlogCounterService(string blogUrl) {
        _targetBlog = new Uri(blogUrl);

        if (!IsBlogAvailable()) {
            throw new Exception($"The desired remote \"{blogUrl}\" is not reachable!");
        }

        Console.Write($"Fetching Index from \"{blogUrl}\"...");
        AvailablePosts = FetchAvailablePosts();
        WordCounts = new Dictionary<int, PostData>();
        Console.WriteLine($" Done.");
        
        UpdateWordCounts(null);
    }

    public bool IsBlogAvailable() {
        const string url = WpRoute;
        return RestUtil.MakeHttpGetRequest(_targetBlog, WpRoute).Result.StatusCode == HttpStatusCode.OK;
    }

    public IDictionary<int, ChangeReason> UpdatePosts() {
        var modified = new Dictionary<int, ChangeReason>();
        IDictionary<int, DateTime> newResults = FetchAvailablePosts();

        foreach (var (key, value) in AvailablePosts) {
            if (newResults.TryGetValue(key, out var result)) {
                if (result != value) {
                    modified[key] = ChangeReason.Modified;
                }
            } else {
                modified[key] = ChangeReason.Removed;
            }
        }
        foreach (var (key, value) in newResults) {
            if (!AvailablePosts.ContainsKey(key)) {
                modified[key] = ChangeReason.Added; 
            }
        }
        AvailablePosts = newResults;
        if (modified.Count > 0) {
            UpdateWordCounts(modified);
        }
        return modified;
    }

    public void UpdateWordCounts(IDictionary<int, ChangeReason>? changes) {
        if (changes != null) {
            Console.Write($"Updating {changes.Count} post{(changes.Count == 1 ? "" : "s")}...");
            var consolePos = Console.GetCursorPosition();
            consolePos.Left++;
            foreach (var (key, value) in changes) {
                Console.SetCursorPosition(consolePos.Left, consolePos.Top);
                switch (value) {
                    default:
                    case ChangeReason.Removed: {
                        Console.Write($"Removing {key}...");
                        WordCounts.Remove(key);
                        break;
                    }
                    case ChangeReason.Modified:
                    case ChangeReason.Added: {
                        Console.Write($"Updating {key}.");
                        WordCounts[key] = FetchPost(key);
                        break;
                    }
                }
            }
            Console.SetCursorPosition(consolePos.Left, consolePos.Top);
            Console.WriteLine("Done.");
        } else {
            Console.Write($"Fetching {AvailablePosts.Count} post{(AvailablePosts.Count == 1 ? "" : "s")}...");
            var consolePos = Console.GetCursorPosition();
            consolePos.Left++;
            WordCounts.Clear();
            foreach (var (key, value) in AvailablePosts) {
                Console.SetCursorPosition(consolePos.Left, consolePos.Top);
                Console.Write($"Fetching {key}...");
                WordCounts[key] = FetchPost(key);
            }
            var extraLength = Console.GetCursorPosition().Left - consolePos.Left;
            Console.SetCursorPosition(consolePos.Left, consolePos.Top);
            Console.Write("Done.");
            Console.WriteLine(Enumerable.Repeat(' ', extraLength - (Console.GetCursorPosition().Left - consolePos.Left)).ToArray());
        }
    }

    private IDictionary<int, DateTime> FetchAvailablePosts() {
        const string url = WpRoute + WpEndpointPost + "?" + WpIndexWantedFields;
        var array = JsonNode.Parse(RestUtil.MakeHttpGetRequest(_targetBlog, url).Result.Content.ReadAsStringAsync().Result).AsArray();
        var dict = new SortedDictionary<int, DateTime>();
        foreach (var json in array) {
            dict[json["id"].GetValue<int>()] = DateTime.Parse(json["modified"].GetValue<string>());
        }
        return dict;
    }

    private PostData FetchPost(int postId) {
        var url = WpRoute + WpEndpointPost + $"/{postId}?" + WpPostWantedFields;
        var obj = JsonNode.Parse(RestUtil.MakeHttpGetRequest(_targetBlog, url).Result.Content.ReadAsStringAsync().Result).AsObject();
        return new PostData(obj["title"]["rendered"].GetValue<string>(), ProcessText(obj["content"]["rendered"].GetValue<string>(), true));
    }

    private static SortedDictionary<string, int> ProcessText(string text, bool ignoreCapitalization) {
        var content = Regex.Replace(text, RegexStrongHighlight, ""); // Remove Strong highlights breaking up words
        content = Regex.Replace(text, RegexHtmlPurge, " "); // Purge HTML Elements
        content = Regex.Replace(content, RegexPunctuationPurge, " "); // Get rid of punctuation
        content = Regex.Replace(content, RegexSpacingCleanup, " "); // Remove clumped up spacing, cleanly separating all words

        var array = Regex.Split(content, " ");
        var map = new SortedDictionary<string, int>();
        foreach (var s in array) {
            var key = ignoreCapitalization ? s.ToLower() : s;
            if (key is "" or "-") {
                continue;
            }
            if (map.ContainsKey(key)) {
                map[key]++;
            } else {
                map[key] = 1;
            }
        }
        return map;
    }
    
    public record PostData(string Title, SortedDictionary<string, int> WordCount);

    public enum ChangeReason {
        Removed,
        Added,
        Modified
    }
}