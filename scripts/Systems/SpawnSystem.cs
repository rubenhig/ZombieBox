using Godot;
using System;

/// <summary>
/// Centralized spawning system for all game entities.
/// Server-authoritative: only the server can spawn entities.
/// </summary>
public partial class SpawnSystem : Node
{
    [Export]
    public PackedScene PlayerScene { get; set; }

    [Export]
    public PackedScene EnemyScene { get; set; }

    [Export]
    public PackedScene BulletScene { get; set; }

    private Node _playersContainer;
    private Node _enemiesContainer;
    private Node _bulletsContainer;

    public override void _Ready()
    {
        // Load default scenes if not assigned via Editor
        if (PlayerScene == null)
            PlayerScene = GD.Load<PackedScene>("res://scenes/entities/player/player.tscn");

        if (EnemyScene == null)
            EnemyScene = GD.Load<PackedScene>("res://scenes/entities/enemy/enemy.tscn");

        if (BulletScene == null)
            BulletScene = GD.Load<PackedScene>("res://scenes/entities/bullet/bullet.tscn");
    }

    /// <summary>
    /// Configure the spawn containers. Called by SessionSystem after scene setup.
    /// </summary>
    public void Configure(Node playersContainer, Node enemiesContainer, Node bulletsContainer)
    {
        _playersContainer = playersContainer;
        _enemiesContainer = enemiesContainer;
        _bulletsContainer = bulletsContainer;
    }

    /// <summary>
    /// Spawns a player for the given peer ID.
    /// Server-only.
    /// </summary>
    /// <returns>The spawned Player instance, or null if spawn failed.</returns>
    public Player SpawnPlayer(long peerId)
    {
        if (!NetworkUtils.IsServer()) return null;

        if (_playersContainer == null)
        {
            GD.PrintErr("SpawnSystem: Players container not configured!");
            return null;
        }

        if (_playersContainer.HasNode(peerId.ToString()))
        {
            GD.PrintErr($"SpawnSystem: Player {peerId} already exists. Skipping spawn.");
            return null;
        }

        GD.Print($"SpawnSystem: Spawning Player for Peer {peerId}");

        Player player = PlayerScene.Instantiate<Player>();
        player.Name = peerId.ToString();
        _playersContainer.AddChild(player, true);

        // Server owns the player body
        player.SetMultiplayerAuthority(1);

        // Input is owned by the peer
        var input = player.GetNodeOrNull("PlayerInput");
        if (input != null)
        {
            input.SetMultiplayerAuthority((int)peerId);
        }

        return player;
    }

    /// <summary>
    /// Spawns an enemy at the given position.
    /// Server-only.
    /// </summary>
    /// <returns>The spawned Enemy instance, or null if spawn failed.</returns>
    public Enemy SpawnEnemy(Vector2 position)
    {
        if (!NetworkUtils.IsServer()) return null;

        if (_enemiesContainer == null)
        {
            GD.PrintErr("SpawnSystem: Enemies container not configured!");
            return null;
        }

        Enemy enemy = EnemyScene.Instantiate<Enemy>();
        enemy.Name = "Enemy_" + Guid.NewGuid().ToString();
        enemy.GlobalPosition = position;

        _enemiesContainer.AddChild(enemy, true);

        GD.Print($"SpawnSystem: Spawned Enemy at {position}");
        return enemy;
    }

    /// <summary>
    /// Spawns a bullet at the given position with the specified direction.
    /// Server-only.
    /// </summary>
    /// <param name="position">World position to spawn the bullet.</param>
    /// <param name="direction">Direction the bullet will travel.</param>
    /// <param name="ownerName">Name of the player who fired (for naming/tracking).</param>
    /// <returns>The spawned Bullet instance, or null if spawn failed.</returns>
    public Bullet SpawnBullet(Vector2 position, Vector2 direction, string ownerName)
    {
        if (!NetworkUtils.IsServer()) return null;

        if (_bulletsContainer == null)
        {
            GD.PrintErr("SpawnSystem: Bullets container not configured!");
            return null;
        }

        Bullet bullet = BulletScene.Instantiate<Bullet>();
        bullet.Name = "Bullet_" + ownerName + "_" + Time.GetTicksMsec();
        bullet.GlobalPosition = position;
        bullet.SetDirection(direction);

        _bulletsContainer.AddChild(bullet, true);

        return bullet;
    }

    /// <summary>
    /// Handles Player.WeaponFired signal.
    /// Spawns a bullet and connects kill tracking to the player.
    /// Called by SessionSystem when a player fires their weapon.
    /// </summary>
    /// <param name="player">The player who fired the weapon.</param>
    /// <param name="position">Position where the bullet spawns.</param>
    /// <param name="direction">Direction the bullet travels.</param>
    /// <param name="shooterName">Name of the shooter (for bullet naming).</param>
    public void OnPlayerWeaponFired(Player player, Vector2 position, Vector2 direction, string shooterName)
    {
        if (!NetworkUtils.IsServer()) return;

        // Spawn the bullet
        Bullet bullet = SpawnBullet(position, direction, shooterName);

        // Connect kill tracking
        if (bullet != null && player != null)
        {
            bullet.EnemyKilled += player.OnEnemyKilledByBullet;
        }
    }
}
