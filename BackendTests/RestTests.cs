using System.Text.RegularExpressions;
namespace BackendTests;

public class RestTests {

    private const string KeyWebsite = "https://www.thekey.academy";
    
    private BackendServer _backendServer;

    [SetUp]
    public void Setup() {
        _backendServer = new BackendServer(KeyWebsite);
    }

    [Test]
    public void VerifyServiceTargetValid() {
        Assert.That(_backendServer.IsBlogAvailable());
        List<BackendServer.IndexResult> results = _backendServer.IndexAvailablePosts();
        Assert.That(results, Is.Not.Empty);
    }
}