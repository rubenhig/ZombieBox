using Godot;

/// <summary>
/// MovementUtils - Shared movement physics for Player entity.
///
/// CRITICAL: This code is used by BOTH server and client:
/// - Server: PlayerController applies this to calculate authoritative position
/// - Client: ClientPredictor applies this to predict local player movement
///
/// ANY divergence between server and client physics will cause constant reconciliations.
/// This single source of truth ensures both use IDENTICAL physics calculations.
/// </summary>
public static class MovementUtils
{
	/// <summary>
	/// Apply movement physics to a player.
	///
	/// This method MUST be deterministic and identical on both server and client.
	/// Used by:
	/// - PlayerController (server-side authoritative physics)
	/// - ClientPredictor (client-side prediction)
	/// </summary>
	/// <param name="player">The player entity to move</param>
	/// <param name="moveVector">Movement input vector (from PlayerInput)</param>
	/// <param name="aimDirection">Aim direction vector (from PlayerInput)</param>
	/// <param name="speed">Movement speed (from Player.Speed property)</param>
	public static void ApplyMovement(
		Player player,
		Vector2 moveVector,
		Vector2 aimDirection,
		float speed
	)
	{
		// Apply movement velocity
		if (moveVector != Vector2.Zero)
		{
			// Normalize to prevent diagonal speed boost
			player.Velocity = moveVector.Normalized() * speed;

			// Rotate player to face aim direction
			player.Rotation = aimDirection.Angle();
		}
		else
		{
			// Stop when no input
			player.Velocity = Vector2.Zero;
		}

		// Apply physics and handle collisions
		// This is a Godot built-in that handles collision detection/response
		player.MoveAndSlide();
	}
}
