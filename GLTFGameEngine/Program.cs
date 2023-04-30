namespace GLTFGameEngine
{
    class Program
    {
        static void Main()
        {
            using (Game game = new(1024, 768, "Test"))
            {
                game.Run();
            }
        }
    }
}