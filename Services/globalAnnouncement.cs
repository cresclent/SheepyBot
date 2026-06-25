// Copyright (c) 2026 Cresclent. All rights reserved.
// This Discord bot code is view-only. Hosting or running this bot is strictly prohibited!
using NetCord;
using NetCord.Rest;
using System.Text.Json;
using System.Collections.Generic;
using discord_bot.Services;
using discord_bot.Tools;

namespace discord_bot.Services
{
    public class GlobalAnnouncement
    {
        private readonly RestClient _restClient;
        private readonly string _configPath;
        private GlobalConfig _config;

        public GlobalAnnouncement(RestClient restClient)
        {
            _restClient = restClient;
            _configPath = Path.Combine(AppContext.BaseDirectory, "startup_config.json");
            LoadConfig();
        }

        private void LoadConfig()
        {
            if (File.Exists(_configPath))
            {
                try
                {
                    string json = File.ReadAllText(_configPath);
                    _config = JsonSerializer.Deserialize<GlobalConfig>(json) ?? new GlobalConfig();
                }
                catch
                {
                    _config = new GlobalConfig();
                }
            }
            else
            {
                _config = new GlobalConfig();
                SaveConfig();
            }
        }

        private void SaveConfig()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_config, options);
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex)
            {
                new Write().WriteLine($"Failed to save global config: {ex.Message}");
            }
        }

        public void SetGlobalChannel(ulong guildId, ulong channelId, ulong? pingRoleId = null)
        {
            if (_config.GlobalChannels.ContainsKey(guildId))
            {
                _config.GlobalChannels[guildId].ChannelId = channelId;
                _config.GlobalChannels[guildId].PingRoleId = pingRoleId;
            }
            else
            {
                _config.GlobalChannels.Add(guildId, new GlobalChannelConfig
                {
                    ChannelId = channelId,
                    PingRoleId = pingRoleId
                });
            }

            SaveConfig();
        }

        public void DisableGlobalAnnouncements(ulong guildId)
        {
            if (_config.GlobalChannels.ContainsKey(guildId))
            {
                _config.GlobalChannels.Remove(guildId);
                SaveConfig();
            }
        }

        public void DisableAllGlobalAnnouncements()
        {
            _config.GlobalChannels.Clear();
            SaveConfig();
        }

        public async Task SendGlobalAnnouncementAsync(string announcement, bool ping = false)
        {
            if (_config.GlobalChannels.Count == 0)
                return;

            if (string.IsNullOrWhiteSpace(announcement))
                return;

            var message = $"🌐 **Global Announcement**\n\n" +
                          $"{announcement}\n\n" +
                          $"📅 **Sent At:** <t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:F>\n" +
                          $"🕐 **Time Since:** <t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:R>";

            var guildsToRemove = new List<ulong>();

            foreach (var kvp in _config.GlobalChannels)
            {
                var guildId = kvp.Key;
                var channelConfig = kvp.Value;

                try
                {
                    var channel = await _restClient.GetChannelAsync(channelConfig.ChannelId) as TextGuildChannel;
                    if (channel == null)
                    {
                        new Write().WriteLine($"Channel {channelConfig.ChannelId} not found for guild {guildId}, removing config");
                        guildsToRemove.Add(guildId);
                        continue;
                    }

                    var finalMessage = message;
                    if (ping && channelConfig.PingRoleId.HasValue)
                    {
                        finalMessage += $"\n🔔 **Ping:** <@&{channelConfig.PingRoleId}>";
                    }

                    await channel.SendMessageAsync(finalMessage);
                }
                catch (Exception ex)
                {
                    new Write().WriteLine($"Failed to send global announcement to guild {guildId}: {ex.Message}");

                    if (ex.Message.Contains("403") || ex.Message.Contains("Forbidden") || ex.Message.Contains("Missing Access"))
                    {
                        new Write().WriteLine($"Bot lacks access to guild {guildId} or channel {channelConfig.ChannelId}, removing config");
                        guildsToRemove.Add(guildId);
                    }
                }
            }

            foreach (var guildId in guildsToRemove)
            {
                if (_config.GlobalChannels.ContainsKey(guildId))
                {
                    _config.GlobalChannels.Remove(guildId);
                    new Write().WriteLine($"Removed guild {guildId} from global announcement config due to missing access");
                }
            }

            if (guildsToRemove.Count > 0)
            {
                SaveConfig();
            }
        }

        public string GetGlobalConfigInfo(ulong guildId)
        {
            if (_config.GlobalChannels.TryGetValue(guildId, out var config))
            {
                return $"Global announcements are enabled\nChannel ID: {config.ChannelId}\nRole ID: {(config.PingRoleId.HasValue ? config.PingRoleId.ToString() : "None")}";
            }

            return "Global announcements are disabled for this guild.";
        }

        public string GetAllGlobalConfigInfo()
        {
            if (_config.GlobalChannels.Count == 0)
                return "No global announcement channels configured.";

            var result = "Configured Global Channels:\n\n";
            foreach (var kvp in _config.GlobalChannels)
            {
                result += $"Guild ID: {kvp.Key} -> Channel ID: {kvp.Value.ChannelId} -> Role ID: {(kvp.Value.PingRoleId.HasValue ? kvp.Value.PingRoleId.ToString() : "None")}\n";
            }
            return result;
        }

        public async Task<bool> ClearGuildGlobalConfig(ulong guildId)
        {
            if (_config.GlobalChannels.ContainsKey(guildId))
            {
                _config.GlobalChannels.Remove(guildId);
                SaveConfig();
                return true;
            }
            return false;
        }
    }

    public class GlobalConfig
    {
        public Dictionary<ulong, StartupChannelConfig> Channels { get; set; } = new Dictionary<ulong, StartupChannelConfig>();
        public Dictionary<ulong, GlobalChannelConfig> GlobalChannels { get; set; } = new Dictionary<ulong, GlobalChannelConfig>();
    }

    public class GlobalChannelConfig
    {
        public ulong ChannelId { get; set; }
        public ulong? PingRoleId { get; set; }
    }
}