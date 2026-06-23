using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NetCord;
using NetCord.Rest;

namespace discord_bot.Helpers
{
    public static class MessageSplitterHelper
    {
        public static List<string> SplitMessage(string message, int maxLength = 2000)
        {
            var parts = new List<string>();

            if (string.IsNullOrEmpty(message))
                return parts;

            if (message.Length <= maxLength)
            {
                parts.Add(message);
                return parts;
            }

            var lines = message.Split('\n');
            var currentPart = "";

            foreach (var line in lines)
            {
                if (line.Length > maxLength)
                {
                    if (!string.IsNullOrEmpty(currentPart))
                    {
                        parts.Add(currentPart.TrimEnd('\n'));
                        currentPart = "";
                    }

                    var words = line.Split(' ');
                    var tempLine = "";

                    foreach (var word in words)
                    {
                        if (tempLine.Length + word.Length + 1 > maxLength)
                        {
                            parts.Add(tempLine.TrimEnd());
                            tempLine = word + " ";
                        }
                        else
                        {
                            tempLine += word + " ";
                        }
                    }

                    if (!string.IsNullOrEmpty(tempLine))
                    {
                        parts.Add(tempLine.TrimEnd());
                    }

                    continue;
                }

                if (currentPart.Length + line.Length + 1 > maxLength)
                {
                    parts.Add(currentPart.TrimEnd('\n'));
                    currentPart = line + "\n";
                }
                else
                {
                    currentPart += line + "\n";
                }
            }

            if (!string.IsNullOrEmpty(currentPart))
            {
                parts.Add(currentPart.TrimEnd('\n'));
            }

            return parts;
        }

        public static async Task SendLongMessageAsync(TextGuildChannel channel, string message, int maxLength = 2000)
        {
            var parts = SplitMessage(message, maxLength);

            if (parts.Count == 0)
                return;

            await channel.SendMessageAsync(parts[0]);

            for (int i = 1; i < parts.Count; i++)
            {
                await channel.SendMessageAsync(parts[i]);
            }
        }

        public static async Task SendLongMessageAsync(DMChannel channel, string message, int maxLength = 2000)
        {
            var parts = SplitMessage(message, maxLength);

            if (parts.Count == 0)
                return;

            await channel.SendMessageAsync(parts[0]);

            for (int i = 1; i < parts.Count; i++)
            {
                await channel.SendMessageAsync(parts[i]);
            }
        }
    }
}