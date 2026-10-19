using System.Globalization;

namespace _3DDemoA2
{
    class Program
    {
        static void Main(string[] args)
        {
            // Set the default culture to invariant to ensure consistent number formatting (e.g., decimal points) across different locales
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            // Create a new instance of the Game class with the desired settings
            using var game = new Game(1280, 720, "3D Demo A2", false, false);
            // Start the game loop (the frame rate is capped by VSync, which is enabled in Game.OnLoad)
            game.Run();
        }
    }
}