// Copyright (c) 2026 Cresclent. All rights reserved.
// This Discord bot code is view-only. Hosting or running this bot is strictly prohibited!
using System.Text.Json.Serialization;

namespace discord_bot.serverDataModels
{
    public class serverBlockModel
    {
        [JsonPropertyName("GuildId")]
        public ulong GuildID { get; set; }

        [JsonPropertyName("BVERS")]
        public string? botVers { get; set; }

        [JsonPropertyName("BinDict")]
        public Dictionary<string, bool>? BinaryDictionary { get; set; }

        public void BuildBinaryDictionaryFromEnum()
        {
            BinaryDictionary = new Dictionary<string, bool>();
            var commands = GetCommandsInOrder();

            foreach (var cmd in commands)
            {
                BinaryDictionary[cmd.ToString()] = true;
            }
        }

        public void SyncBinaryDictionary()
        {
            if (BinaryDictionary == null)
            {
                BuildBinaryDictionaryFromEnum();
                return;
            }

            var currentCommands = GetCommandsInOrder();
            var newDict = new Dictionary<string, bool>();

            foreach (var kvp in BinaryDictionary)
            {
                if (currentCommands.Select(c => c.ToString()).Contains(kvp.Key))
                {
                    newDict[kvp.Key] = kvp.Value;
                }
            }

            foreach (var cmd in currentCommands)
            {
                string cmdName = cmd.ToString();
                if (!newDict.ContainsKey(cmdName))
                {
                    newDict[cmdName] = true;
                }
            }

            BinaryDictionary = newDict;
        }

        public bool IsCommandEnabled(string commandName)
        {
            if (BinaryDictionary == null) return true;
            return BinaryDictionary.TryGetValue(commandName.ToLower(), out bool enabled) && enabled;
        }

        public void SetCommandEnabled(string commandName, bool enabled)
        {
            if (BinaryDictionary == null)
            {
                BuildBinaryDictionaryFromEnum();
            }

            if (BinaryDictionary != null && BinaryDictionary.ContainsKey(commandName))
            {
                BinaryDictionary[commandName] = enabled;
            }
        }

        private List<string> GetCommandsInOrder()
        {
            return new List<string>
            {
                "pity",
    "inventory",
    "banner",
    "pull",
    "help",
    "coinflip",
    "startupguide",
    "bannerreset",
    "guilddata",
    "alldata",
    "guildleaderboard",
    "totalleaderboard",
    "votebanner",
    "votehistory",
    "setvotechannel",
    "disablevotechannel",
    "votechannelstatus",
    "setstartupchannel",
    "disablestartup",
    "startupstatus",
    "setglobalchannel",
    "globalannounce",
    "disableglobal",
    "globalstatus",
    "github",
    "terms",
    "privacy",
    "enabledcommands",
    "disabledcommands"
            };
        }

        public bool NeedsUpdate()
        {
            if (BinaryDictionary == null) return true;

            var enumNames = GetCommandsInOrder();
            var dictKeys = BinaryDictionary.Keys.ToList();

            if (dictKeys.Count != enumNames.Count) return true;

            foreach (var name in enumNames)
            {
                if (!dictKeys.Contains(name)) return true;
            }

            return false;
        }
    }
}