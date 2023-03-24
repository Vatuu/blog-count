using System.Text.Json;
using System.Text.Json.Nodes;
using WebSocketSharp;
using WebSocketSharp.Server;
namespace Backend.networking; 

public class WordCounterBehaviour: WebSocketBehavior {

    private const string CommandUpdate = "update";
    
    protected override void OnMessage(MessageEventArgs e) {
        if (e.IsText) {
            if (int.TryParse(e.Data, out var id)) {
                IDictionary<int, BlogCounterService.PostData> posts = MainClass.Instance.Counter.WordCounts;
                if (posts.TryGetValue(id, out var val)) {
                    var map = new JsonObject();
                    foreach (var (key, value) in val.WordCount) {
                        map[key] = value;
                    }
                    Send(JsonSerializer.Serialize(new JsonObject {
                        ["kind"] = "post",
                        ["data"] = new JsonObject {
                            ["id"] = id,
                            ["post"] = new JsonObject {
                                ["title"] = val.Title,
                                ["count"] = map}}}));
                } else {
                    SendError($"Unknown post id \"{id}\"!");
                }
            } else {
                if (e.Data.Equals(CommandUpdate, StringComparison.InvariantCultureIgnoreCase)) {
                    if (!MainClass.Instance.Network.Update()) {
                        SendInfo("No new data.");
                    }
                } else {
                    SendError("Unknown command.");
                }
            }
        } else {
            SendError("Invalid request message! Expected a integer identifying a blog post.");
        }
    }
    
    protected override void OnOpen() {
        var map = new JsonObject();
        foreach (var (key, value) in MainClass.Instance.Counter.AvailablePosts) {
            map[key.ToString()] = value.ToBinary();
        }
        var obj = new JsonObject {
            ["kind"] = "index",
            ["data"] = map};
        Send(JsonSerializer.Serialize(obj));
    }

    private void SendError(string msg) {
        Send(JsonSerializer.Serialize(new JsonObject {
            ["kind"] = "msg",
            ["error"] = msg}));
    }
    
    private void SendInfo(string msg) {
        Send(JsonSerializer.Serialize(new JsonObject {
            ["kind"] = "msg",
            ["info"] = msg}));
    }
    
    public void BroadcastMessage(string message) {
        Sessions.Broadcast(message);
    }
}