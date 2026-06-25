//feel free to use this as it doesnt fall under copyright!
using System;
using System.Collections.Generic;
using System.Text;

namespace discord_bot.Tools
{
    internal class Write
    {
        public void WriteLine(string message)
        {
            string info = "info";
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Write(info);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(": " + message);
        }
    }
}
