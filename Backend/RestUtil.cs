namespace Backend; 

public static class RestUtil {
    
    public static async Task<HttpResponseMessage> MakeHttpGetRequest(Uri domain, string endpoint) {
        using var client = new HttpClient();
        client.BaseAddress = domain;
        return await client.GetAsync(endpoint);
    }
}