using Godot;
using System;

public partial class Master : Node
{
    // The container where active scenes (Menu, Game) will be placed
    private Node _currentScene;

    public override void _Ready()
    {
        ParseCmdArgs();
    }

    private void ParseCmdArgs()
    {
        // Get user-provided arguments after the '--' separator
        var args = OS.GetCmdlineUserArgs();
        var logMsg = "Master: User CMD Args received: " + string.Join(" ", args);
        GD.Print(logMsg);
        
        // Debug file log
        try { System.IO.File.WriteAllText("server_status.txt", logMsg + "\n"); } catch {}

        bool isServer = false;
        int port = 7777;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--server")
            {
                isServer = true;
            }
            else if (args[i] == "--port" && i + 1 < args.Length)
            {
                int.TryParse(args[i + 1], out port);
            }
        }

        if (isServer)
        {
            var serverMsg = $"Master: Starting as Dedicated Server on port {port}";
            GD.Print(serverMsg);
            try { System.IO.File.AppendAllText("server_status.txt", serverMsg + "\n"); } catch {}
            
            var networkManager = GetNode<NetworkManager>("/root/NetworkManager");
            
            // We will add this method to NetworkManager in the next step
            networkManager.StartDedicatedServer(port);
            
            // Load game session directly
            LoadGame();
        }
        else
        {
            // Normal client start
            LoadMenu();

            // Auto-connect hack for testing
            foreach (var arg in args)
            {
                if (arg == "--client-test")
                {
                    GD.Print("Master: Auto-connecting via --client-test...");
                    CallDeferred(nameof(AutoConnect));
                }
            }
        }
    }

    private void AutoConnect()
    {
        var networkManager = GetNode<NetworkManager>("/root/NetworkManager");
        networkManager.StartClient("127.0.0.1", 7777);
    }

    public void LoadMenu()
    {
        DeferredLoad("res://scenes/Menu.tscn");
    }

    public void LoadGame()
    {
        DeferredLoad("res://scenes/GameSession.tscn");
    }

    private void DeferredLoad(string path)
    {
        // Use CallDeferred to ensure safe scene changing during physics/process steps
        CallDeferred(nameof(SwitchScene), path);
    }

    private void SwitchScene(string path)
    {
        // 1. Remove current scene if it exists
        if (_currentScene != null)
        {
            _currentScene.QueueFree();
            _currentScene = null;
        }

        // 2. Load the new scene
        var nextSceneResource = GD.Load<PackedScene>(path);
        _currentScene = nextSceneResource.Instantiate();

        // 3. Add it to the tree as a child of Master
        AddChild(_currentScene);
    }
}
