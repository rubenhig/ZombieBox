using Godot;
using System;

/// <summary>
/// Utility class for network operations and authority checks.
/// Provides reusable helper functions for multiplayer scenarios.
/// </summary>
public static class NetworkUtils
{
    /// <summary>
    /// Checks if the current instance is the server.
    /// </summary>
    /// <returns>True if this is the server (including offline mode), false if client.</returns>
    public static bool IsServer()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        if (tree == null) return false;
        return tree.GetMultiplayer().IsServer();
    }

    /// <summary>
    /// Checks if the current instance is a client (not the server).
    /// </summary>
    /// <returns>True if this is a client connected to a server, false otherwise.</returns>
    public static bool IsClient()
    {
        return !IsServer();
    }

    /// <summary>
    /// Gets the local peer ID.
    /// </summary>
    /// <returns>The unique ID of the local peer. Returns 1 for server/offline mode.</returns>
    public static int GetLocalPeerId()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        if (tree == null) return 1;
        return tree.GetMultiplayer().GetUniqueId();
    }

    /// <summary>
    /// Checks if the local peer has authority over the given node.
    /// </summary>
    /// <param name="node">The node to check authority for.</param>
    /// <returns>True if the local peer is the multiplayer authority for this node.</returns>
    public static bool HasAuthority(Node node)
    {
        if (node == null) return false;
        return node.IsMultiplayerAuthority();
    }

    /// <summary>
    /// Validates that the current code is running on the server.
    /// Throws an exception if called from a client.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when called from a client.</exception>
    public static void ValidateServerOnly()
    {
        if (!IsServer())
        {
            throw new InvalidOperationException("This operation can only be performed on the server.");
        }
    }

    /// <summary>
    /// Checks if a node is owned by the local peer.
    /// Useful for checking if a Player node belongs to the local client.
    /// </summary>
    /// <param name="node">The node to check ownership for.</param>
    /// <returns>True if the node's multiplayer authority matches the local peer ID.</returns>
    public static bool IsOwnedByLocalPeer(Node node)
    {
        if (node == null) return false;
        return node.GetMultiplayerAuthority() == GetLocalPeerId();
    }

    /// <summary>
    /// Gets a reference to the NetworkSystem autoload.
    /// </summary>
    /// <returns>The NetworkSystem singleton, or null if not found.</returns>
    public static NetworkSystem GetNetworkSystem()
    {
        var tree = Engine.GetMainLoop() as SceneTree;
        if (tree == null) return null;
        return tree.Root.GetNodeOrNull<NetworkSystem>("/root/NetworkSystem");
    }
}
