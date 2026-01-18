using Godot;
using System;

public partial class Level : Node2D
{
    // Facade pattern: Expose internal nodes securely
    
    public Node SpawnPoints => GetNode("SpawnPoints");
    
    // We can expose Navigation or TileMap here too if needed
    public NavigationRegion2D NavigationRegion => GetNode<NavigationRegion2D>("NavigationRegion2D");
}
