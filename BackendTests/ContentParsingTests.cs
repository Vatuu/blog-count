using System.Text.RegularExpressions;
namespace BackendTests; 

public class ContentParsingTests {

    private const string KeyWebsite = "https://www.thekey.academy";
    private const string InvalidKeyRegex = @"\.|,|;|:|!|\?|\(|\)|(&#)|-|""";
    private const string EmptyKeyRegex = @"^\s*$";
    
    private BackendServer.PostData data;
    
    [SetUp]
    public void Setup() {
        var server = new BackendServer(KeyWebsite);
        List<BackendServer.IndexResult> results = server.IndexAvailablePosts();
        data = server.FetchPost(results[0].Id);
    }

    [Test]
    public void VerifyNonEmptyKey() {
        Assert.That(data.WordCount.Keys.Any(k => Regex.IsMatch(k, EmptyKeyRegex)), Is.False, "Empty key was found in keyset!");
    }
    
    [Test]
    public void VerifyKeyValidity() {
        foreach (var s in data.WordCount.Keys) {
            Assert.That(Regex.Match(s, InvalidKeyRegex), Is.False, "Invalid Key found in Keyset: \"{0}\"", s);
        }
    }
}