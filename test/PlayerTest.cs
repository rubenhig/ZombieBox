using Godot;
using GdUnit4;
using static GdUnit4.Assertions;

namespace ZombieBox.Test
{
    [TestSuite]
    public class PlayerTest
    {
        [TestCase]
        public void TestInitialHealth()
        {
            // Verify that the player starts with the default health of 3
            var playerScene = GD.Load<PackedScene>("res://scenes/player.tscn");
            var player = playerScene.Instantiate<Player>();
            
            AssertThat(player.Health).IsEqual(3);
            
            player.Free();
        }
    }
}