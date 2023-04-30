namespace GLTFGameEngine
{
    class Program
    {
        static void Main()
        {
            using (Game game = new(1600, 900, "Test"))
            {
                game.CenterWindow();
                game.Run();
            }
        }
    }
}