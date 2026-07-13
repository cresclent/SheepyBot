using System.Text.Json.Serialization;

namespace discord_bot.serverDataModels
{
    public class serverBlockModel
    {
        [JsonPropertyName("GuildId")]
        public ulong GuildID { get; set; }

        [JsonPropertyName("Hash")]
        public ulong commandHash { get; set; }

        [JsonPropertyName("BVERS")]
        public string? botVers { get; set; }

        [JsonPropertyName("BinDict")]
        public Dictionary<string, int>? BinaryDictionary { get; set; }

        [Flags]
        public enum BotCommand : ulong
        {
            None = 0,
            Pity = 1 << 0,
            Inventory = 1 << 1,
            Banner = 1 << 2,
            Pull = 1 << 3,
            Help = 1 << 4,
            Coinflip = 1 << 5,
            StartupGuide = 1 << 6,
            BannerReset = 1 << 7,
            GuildData = 1 << 8,
            AllData = 1 << 9,
            GuildLeaderboard = 1 << 10,
            TotalLeaderboard = 1 << 11,
            VoteBanner = 1 << 12,
            VoteHistory = 1 << 13,
            SetVoteChannel = 1 << 14,
            DisableVoteChannel = 1 << 15,
            VoteChannelStatus = 1 << 16,
            SetStartupChannel = 1 << 17,
            DisableStartup = 1 << 18,
            StartupStatus = 1 << 19,
            SetGlobalChannel = 1 << 20,
            GlobalAnnounce = 1 << 21,
            DisableGlobal = 1 << 22,
            GlobalStatus = 1 << 23,
            Github = 1 << 24,
            Terms = 1 << 25,
            Privacy = 1 << 26,
            EnabledCommands = 1 << 27,
            DisabledCommands = 1 << 28,
        }

        public static List<BotCommand> GetCommandsInOrder()
        {
            return Enum.GetValues(typeof(BotCommand))
                .Cast<BotCommand>()
                .Where(c => c != BotCommand.None)
                .OrderBy(c => (ulong)c)
                .ToList();
        }

        public void BuildBinaryDictionaryFromEnum()
        {
            BinaryDictionary = new Dictionary<string, int>();
            var commands = GetCommandsInOrder();

            foreach (var cmd in commands)
            {
                BinaryDictionary[cmd.ToString()] = 1;
            }

            ApplyBinaryDictionaryToHash();
        }

        public void SyncBinaryDictionary()
        {
            if (BinaryDictionary == null)
            {
                BuildBinaryDictionaryFromEnum();
                return;
            }

            var currentCommands = GetCommandsInOrder();
            var newDict = new Dictionary<string, int>();

            var statePreserver = new Dictionary<string, int>();
            foreach (var kvp in BinaryDictionary)
            {
                statePreserver[kvp.Key] = kvp.Value;
            }

            foreach (var cmd in currentCommands)
            {
                string cmdName = cmd.ToString();
                if (statePreserver.TryGetValue(cmdName, out int existingValue))
                {
                    newDict[cmdName] = existingValue;
                }
                else
                {
                    newDict[cmdName] = 1;
                }
            }

            BinaryDictionary = newDict;
            ApplyBinaryDictionaryToHash();
        }

        public void CascadeBinaryLocations()
        {
            if (BinaryDictionary == null)
            {
                BuildBinaryDictionaryFromEnum();
                return;
            }

            var currentCommands = GetCommandsInOrder();
            var newDict = new Dictionary<string, int>();

            var statePreserver = new Dictionary<string, int>();
            foreach (var kvp in BinaryDictionary)
            {
                statePreserver[kvp.Key] = kvp.Value;
            }

            foreach (var cmd in currentCommands)
            {
                string cmdName = cmd.ToString();
                if (statePreserver.TryGetValue(cmdName, out int existingValue))
                {
                    newDict[cmdName] = existingValue;
                }
                else
                {
                    newDict[cmdName] = 1;
                }
            }

            BinaryDictionary = newDict;
            ApplyBinaryDictionaryToHash();
        }

        public void ApplyBinaryDictionaryToHash()
        {
            if (BinaryDictionary == null) return;

            ulong newHash = 0;

            foreach (var kvp in BinaryDictionary)
            {
                if (Enum.TryParse<BotCommand>(kvp.Key, out var cmd) && kvp.Value == 1)
                {
                    newHash |= (ulong)cmd;
                }
            }

            commandHash = newHash;
        }

        public bool NeedsUpdate()
        {
            if (BinaryDictionary == null) return true;

            var enumNames = GetCommandsInOrder().Select(c => c.ToString()).ToList();
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