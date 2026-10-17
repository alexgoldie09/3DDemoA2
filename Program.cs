namespace _3DDemoA2
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create a new instance of the Game class with the desired settings
            using var game = new Game(1280, 720, "3D Demo A2", false);
            // Start the game loop (the frame rate is capped by VSync, which is enabled in Game.OnLoad)
            game.Run();
        }
    }
}