
namespace Perfy.Display
{
    static class DisplayMethods
    {
        public static void PrintInColor(string message, ConsoleColor color)
        {
            ConsoleColor currentColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(message);
            Console.ForegroundColor = currentColor;
        }
        public static void PrintWarning(string message) => PrintInColor(message + "\n", ConsoleColor.Yellow);
        public static void PrintError(string message) => PrintInColor(message + "\n", ConsoleColor.Red);
    }
}