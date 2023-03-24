using System.Reflection;
using System.Text.Json;
using System.Timers;
using Backend.networking;
using Timer = System.Timers.Timer;
namespace Backend; 

public class MainClass {

    public static MainClass Instance { get; private set; }

    public static void Main() {
        Instance = new MainClass();
        Instance.Start();
    }
    
    private const string KeyWebsite = "https://www.thekey.academy";

    public BlogCounterService Counter { get; private set; }
    public  NetworkHandler Network { get; private set; }

    private MainClass() {
        try {
            Counter = new BlogCounterService(KeyWebsite);
            Network = new NetworkHandler("127.0.0.1", 8888);
        } catch (Exception e) {
            Console.WriteLine("An error occured: " + e.Message);
            System.Environment.Exit(-1);
        }
    }

    private void Start() {
        Network.Start();
        
        var timer = new Timer(TimeSpan.FromSeconds(10).TotalMilliseconds);
        timer.AutoReset = true;
        timer.Elapsed += (sender, args) => {
            Network.Update();
        };
        timer.Start();

        while (true) { }
        
        Network.Stop();
    }
}