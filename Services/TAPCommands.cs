// Copyright (c) 2026 Cresclent. All rights reserved.
// This Discord bot code is view-only. Hosting or running this bot is strictly prohibited!
using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace discord_bot.Services
{
    internal class TAPCommands
    {
        private string filepath = Path.Combine(AppContext.BaseDirectory, "../../../Terms");
        public string TOS()
        {
            if (File.Exists(Path.Combine(filepath, "TERMS.md"))){
                string[] lines = File.ReadAllLines(Path.Combine(filepath, "TERMS.md"));
                return string.Join('\n', lines);
            }
            else
            {
                return $"Terms doesn't exist (this shouldnt happen... send DM immediately)\nFilepath: {Path.Combine(filepath, "TERMS.md")}";
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
                return $"Privacy Policy doesn't exist (this shouldnt happen... send DM immediately)\nFilepath: {Path.Combine(filepath, "PRIVACY.md")}";
            }
        }
    }
}
