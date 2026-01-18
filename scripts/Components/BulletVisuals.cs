using Godot;
using System;
using System.Collections.Generic;

public partial class BulletVisuals : Node
{
    [Export]
    public int TrailLength = 10;

    [Export]
    public float Width = 3.0f;

    [Export]
    public Color TrailColor = new Color(1, 1, 0, 0.8f); // Yellow

    private Line2D _trail;
    private Node2D _bullet;

    public override void _Ready()
    {
        _bullet = GetParent<Node2D>();
        
        // Setup Line2D programmatically to ensure correct settings
        _trail = new Line2D();
        _trail.Width = Width;
        _trail.DefaultColor = TrailColor;
        _trail.BeginCapMode = Line2D.LineCapMode.Round;
        _trail.EndCapMode = Line2D.LineCapMode.Round;
        
        // Important: Make trail independent of bullet rotation/scale, but follow position manually
        // actually, simpler: Make it top_level so it moves in world coordinates
        _trail.TopLevel = true;
        
        AddChild(_trail);
    }

    public override void _Process(double delta)
    {
        // Add current position
        _trail.AddPoint(_bullet.GlobalPosition);

        // Remove old points
        if (_trail.GetPointCount() > TrailLength)
        {
            _trail.RemovePoint(0);
        }
    }

    public override void _ExitTree()
    {
        // When bullet dies, the trail should fade out, not vanish instantly.
        // For now, we accept it vanishes. In Phase 3 we can make it detached and fade.
        if (_trail != null && IsInstanceValid(_trail))
        {
            _trail.QueueFree();
        }
    }
}
