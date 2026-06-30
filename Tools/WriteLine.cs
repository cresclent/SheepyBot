//feel free to use this as it doesnt fall under copyright!
using System;
using System.Collections.Generic;
using System.Text;

namespace discord_bot.Tools
{
    internal class Write
    {
        public void WriteLine(string message, int type = 1)
        {
            string prefix = "";
            if (type < 0)
            {
                type = 1;
            }
            if (type == 0) //debug
            {
                prefix = "debug";
            }
            if (type == 1) //info
            {
                prefix = "info";
                Console.ForegroundColor = ConsoleColor.DarkGreen;
            }
            else if (type == 2) //WARN
            {
                prefix = "WARN";
            }
            Console.Write(prefix);
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine(": " + message);
        }
    }
}
