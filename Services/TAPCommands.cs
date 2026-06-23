using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace discord_bot.Services
{
    internal class TAPCommands
    {
        private string filepath = Path.Combine(AppContext.BaseDirectory, "Terms");
        public string TOS()
        {
            if (File.Exists(Path.Combine(filepath, "TERMS.md"))){
                string[] lines = File.ReadAllLines(Path.Combine(filepath, "TERMS.md"));
                return string.Join('\n', lines);
            }
            else
            {
                return "Terms doesn't exist (this shouldnt happen... send DM immediately)";
            }
        }

        public string Privacy()
        {
            if (File.Exists(Path.Combine(filepath, "PRIVACY.md")))
            {
                string[] lines = File.ReadAllLines(Path.Combine(filepath, "PRIVACY.md"));
                return string.Join('\n', lines);
            }
            else
            {
                return "Privacy doesn't exist (this shouldnt happen... send DM immediately)";
            }
        }
    }
}
