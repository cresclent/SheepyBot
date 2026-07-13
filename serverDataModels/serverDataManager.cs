// Copyright (c) 2026 Cresclent. All rights reserved.
// This Discord bot code is view-only. Hosting or running this bot is strictly prohibited!
using discord_bot.serverDataModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json;

namespace discord_bot.serverDataModels
{
    public class serverDataManager
    {
        private readonly string _configDir;
        private readonly Dictionary<ulong, serverBlockModel> _cache = new();
        private readonly string? _botVer;

        public serverDataManager()
        {
            _configDir = Path.Combine(AppContext.BaseDirectory, "serverconfigs");
            if (!Directory.Exists(_configDir))
                Directory.CreateDirectory(_configDir);
            string json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "appsettings.json"));
            JObject config = JObject.Parse(json);
            _botVer = config["Bot"]?["BotVersion"]?.ToString();
        }

        private string GetConfigPath(ulong guildId)
        {
            return Path.Combine(_configDir, $"config-{guildId}.json");
        }

        public serverBlockModel GetOrCreateConfig(ulong guildId)
        {
            if (_cache.TryGetValue(guildId, out var cached))
            {
                cached.SyncBinaryDictionary();
                return cached;
            }

            string path = GetConfigPath(guildId);
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    var config = System.Text.Json.JsonSerializer.Deserialize<serverBlockModel>(json);
                    if (config != null)
                    {
                        config.SyncBinaryDictionary();
                        _cache[guildId] = config;
                        return config;
                    }
                }
                catch { }
            }

            var newConfig = new serverBlockModel
            {
                GuildID = guildId,
                commandHash = ulong.MaxValue,
                botVers = "1.0.0"
            };
            newConfig.BuildBinaryDictionaryFromEnum();
            _cache[guildId] = newConfig;
            SaveConfig(newConfig);
            return newConfig;
        }

        public void SaveConfig(serverBlockModel config)
        {
            string path = GetConfigPath(config.GuildID);
            string json = System.Text.Json.JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
            _cache[config.GuildID] = config;
        }

        public bool IsCommandEnabled(ulong guildId, serverBlockModel.BotCommand command)
        {
            var config = GetOrCreateConfig(guildId);
            return ((ulong)command & config.commandHash) != 0;
        }

        public void EnableCommands(ulong guildId, List<serverBlockModel.BotCommand> commands)
        {
            var config = GetOrCreateConfig(guildId);
            foreach (var cmd in commands)
            {
                config.commandHash |= (ulong)cmd;
            }
            SaveConfig(config);
        }

        public void DisableCommands(ulong guildId, List<serverBlockModel.BotCommand> commands)
        {
            var config = GetOrCreateConfig(guildId);
            foreach (var cmd in commands)
            {
                config.commandHash &= ~(ulong)cmd;
            }
            SaveConfig(config);
        }

        public List<serverBlockModel.BotCommand> GetEnabledCommands(ulong guildId)
        {
            var config = GetOrCreateConfig(guildId);
            var enabled = new List<serverBlockModel.BotCommand>();
            foreach (serverBlockModel.BotCommand cmd in Enum.GetValues(typeof(serverBlockModel.BotCommand)))
            {
                if (cmd != serverBlockModel.BotCommand.None && ((ulong)cmd & config.commandHash) != 0)
                {
                    enabled.Add(cmd);
                }
            }
            return enabled;
        }

        public List<serverBlockModel.BotCommand> GetDisabledCommands(ulong guildId)
        {
            var config = GetOrCreateConfig(guildId);
            var disabled = new List<serverBlockModel.BotCommand>();
            foreach (serverBlockModel.BotCommand cmd in Enum.GetValues(typeof(serverBlockModel.BotCommand)))
            {
                if (cmd != serverBlockModel.BotCommand.None && ((ulong)cmd & config.commandHash) == 0)
                {
                    disabled.Add(cmd);
                }
            }
            return disabled;
        }

        public void ResetAllCommands(ulong guildId)
        {
            var config = GetOrCreateConfig(guildId);
            config.commandHash = ulong.MaxValue;
            SaveConfig(config);
        }

        public void DeleteConfig(ulong guildId)
        {
            string path = GetConfigPath(guildId);
            if (File.Exists(path))
            {
                File.Delete(path);
                _cache.Remove(guildId);
            }
        }

        public bool ConfigExists(ulong guildId)
        {
            string path = GetConfigPath(guildId);
            return File.Exists(path);
        }

        public Dictionary<ulong, serverBlockModel> GetAllConfigs()
        {
            var allConfigs = new Dictionary<ulong, serverBlockModel>();
            var files = Directory.GetFiles(_configDir, "config-*.json");

            foreach (var file in files)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var config = System.Text.Json.JsonSerializer.Deserialize<serverBlockModel>(json);
                    if (config != null)
                    {
                        allConfigs[config.commandHash] = config;
                    }
                }
                catch { }
            }

            return allConfigs;
        }

        public void ReloadConfig(ulong guildId)
        {
            _cache.Remove(guildId);
            GetOrCreateConfig(guildId);
        }

        public void ClearCache()
        {
            _cache.Clear();
        }

        public void SyncAllConfigs()
        {
            var allConfigs = GetAllConfigs();
            foreach (var config in allConfigs.Values)
            {
                if (config.NeedsUpdate())
                {
                    config.SyncBinaryDictionary();
                    SaveConfig(config);
                }
            }
        }
    }
}