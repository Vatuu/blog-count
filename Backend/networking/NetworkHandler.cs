using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using WebSocketSharp.Server;
namespace Backend.networking; 

public class NetworkHandler {

    private readonly WebSocketServer _server;

    public NetworkHandler(string ip, int port) {
        _server = new WebSocketServer(IPAddress.Parse(ip), port);
    }

    public bool Update() {
        IDictionary<int, BlogCounterService.ChangeReason> updates = MainClass.Instance.Counter.UpdatePosts();
        if (updates.Count != 0) {
            var doc = new JsonObject { ["kind"] = "update" };
            var data = new JsonArray();
            foreach (var (key, reason) in updates) {
                switch (reason) {
                    case BlogCounterService.ChangeReason.Added:
                    case BlogCounterService.ChangeReason.Modified: {
                        data.Add(new JsonObject {
                            ["reason"] = reason.ToString(),
                            ["data"] = new JsonObject {
                                ["id"] = key,
                                ["date"] = MainClass.Instance.Counter.AvailablePosts[key]
                            }});
                        break;
                    }
                    case BlogCounterService.ChangeReason.Removed: {
                        data.Add(new JsonObject {
                            ["reason"] = reason.ToString(),
                            ["data"] = key});
                        break;
                    }
                }
            }
            doc["data"] = data;
            Broadcast(JsonSerializer.Serialize(doc));
            return true;
        }
        return false;
    }
    
    public void Start() {
        Console.Write($"Starting WebSocket server on {_server.Address}:{_server.Port}...");
        _server.AddWebSocketService("/", () => new WordCounterBehaviour());
        _server.Start();
        Console.WriteLine(" Ready.");
    }

    public void Stop() {
        _server.Stop();
    }

    public void Broadcast(string data) {
        _server.WebSocketServices.Broadcast(data);
    }
    
}