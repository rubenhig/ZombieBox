using Godot;

namespace ZombieBox.Netcode
{
    /// <summary>
    /// Snapshot of entity state at a specific tick.
    /// Used for client prediction history and lag compensation.
    /// </summary>
    public struct StateSnapshot
    {
        public uint Tick;
        public Vector2 Position;
        public Vector2 Velocity;
        public float Rotation;

        public StateSnapshot(uint tick, Vector2 position, Vector2 velocity, float rotation)
        {
            Tick = tick;
            Position = position;
            Velocity = velocity;
            Rotation = rotation;
        }

        public override string ToString()
        {
            return $"StateSnapshot(Tick={Tick}, Pos={Position}, Vel={Velocity}, Rot={Rotation:F2})";
        }
    }

    /// <summary>
    /// Snapshot of player input at a specific tick.
    /// Used for client prediction history and reconciliation.
    /// </summary>
    public struct InputSnapshot
    {
        public uint Tick;
        public Vector2 MoveVector;
        public Vector2 AimDirection;

        public InputSnapshot(uint tick, Vector2 moveVector, Vector2 aimDirection)
        {
            Tick = tick;
            MoveVector = moveVector;
            AimDirection = aimDirection;
        }

        public override string ToString()
        {
            return $"InputSnapshot(Tick={Tick}, Move={MoveVector}, Aim={AimDirection})";
        }
    }
}
