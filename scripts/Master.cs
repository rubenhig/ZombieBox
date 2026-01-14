using Godot;
using System;

public partial class Master : Node
{
    // The container where active scenes (Menu, Game) will be placed
    private Node _currentScene;

    public override void _Ready()
    {
        // Start by loading the Menu
        LoadMenu();
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
