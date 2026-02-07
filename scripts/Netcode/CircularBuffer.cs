using Godot;

namespace ZombieBox.Netcode
{
    /// <summary>
    /// Generic circular buffer for storing tick-indexed data.
    /// Stores a fixed-size history (e.g., last 120 ticks = 2 seconds at 60Hz).
    /// Automatically overwrites oldest entries when full.
    /// </summary>
    public class CircularBuffer<T>
    {
        private readonly T[] _buffer;
        private readonly uint[] _ticks; // Track which tick each slot represents
        private readonly int _capacity;

        public int Capacity => _capacity;

        public CircularBuffer(int capacity)
        {
            _capacity = capacity;
            _buffer = new T[capacity];
            _ticks = new uint[capacity];
        }

        /// <summary>
        /// Add an item indexed by tick. Overwrites oldest if full.
        /// </summary>
        public void Add(uint tick, T item)
        {
            int index = (int)(tick % (uint)_capacity);
            _buffer[index] = item;
            _ticks[index] = tick;
        }

        /// <summary>
        /// Get an item by tick. Throws if not found.
        /// </summary>
        public T Get(uint tick)
        {
            if (TryGet(tick, out T item))
            {
                return item;
            }

            throw new System.Exception($"CircularBuffer: Tick {tick} not found in buffer");
        }

        /// <summary>
        /// Try to get an item by tick. Returns false if not found or overwritten.
        /// </summary>
        public bool TryGet(uint tick, out T item)
        {
            int index = (int)(tick % (uint)_capacity);

            // Check if this slot contains the requested tick
            if (_ticks[index] == tick)
            {
                item = _buffer[index];
                return true;
            }

            // Tick not found (either never added or overwritten)
            item = default;
            return false;
        }

        /// <summary>
        /// Check if buffer contains a specific tick.
        /// </summary>
        public bool Contains(uint tick)
        {
            int index = (int)(tick % (uint)_capacity);
            return _ticks[index] == tick;
        }

        /// <summary>
        /// Clear all entries in the buffer.
        /// </summary>
        public void Clear()
        {
            System.Array.Clear(_buffer, 0, _capacity);
            System.Array.Clear(_ticks, 0, _capacity);
        }

        public override string ToString()
        {
            return $"CircularBuffer<{typeof(T).Name}>(Capacity={_capacity})";
        }
    }
}
