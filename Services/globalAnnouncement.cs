// Copyright (c) 2026 Cresclent. All rights reserved.
// This Discord bot code is view-only. Hosting or running this bot is strictly prohibited!
using NetCord;
using NetCord.Rest;
using System.Text.Json;
using System.Collections.Generic;
using discord_bot.Tools;

namespace discord_bot.Services
{
    public class GlobalAnnouncement
    {
        private readonly RestClient _restClient;
        private readonly string _configPath;
        private GlobalConfig _config;
        private readonly object _configLock = new object();

        public GlobalAnnouncement(RestClient restClient)
        {
            _restClient = restClient;
            _configPath = Path.Combine(AppContext.BaseDirectory, "startup_config.json");
            LoadConfig();
            new Write().WriteLine($"GlobalAnnouncement: Initialized with {_config.GlobalChannels.Count} global channels");
        }

        public void ReloadConfig()
        {
            lock (_configLock)
            {
                LoadConfig();
                new Write().WriteLine($"GlobalAnnouncement: Config reloaded. Found {_config.GlobalChannels.Count} global channels.");
            }
        }

        private void LoadConfig()
        {
            if (File.Exists(_configPath))
            {
                try
                {
                    string json = File.ReadAllText(_configPath);
                    var newConfig = JsonSerializer.Deserialize<GlobalConfig>(json);
                    if (newConfig != null)
                    {
                        _config = newConfig;
                    }
                    else
                    {
                        new Write().WriteLine("GlobalAnnouncement: Deserialized config was null, using empty config");
                        _config = new GlobalConfig();
                    }
                }
                catch (Exception ex)
                {
                    new Write().WriteLine($"GlobalAnnouncement: Error loading config: {ex.Message}");
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
                lock (_configLock)
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    string json = JsonSerializer.Serialize(_config, options);
                    File.WriteAllText(_configPath, json);
                }
            }
            catch (Exception ex)
            {
                new Write().WriteLine($"Failed to save global config: {ex.Message}");
            }
        }

        public void SetGlobalChannel(ulong guildId, ulong channelId, ulong? pingRoleId = null)
        {
            lock (_configLock)
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
                new Write().WriteLine($"GlobalAnnouncement: Updated global channel for guild {guildId} to channel {channelId}");
            }
        }

        public void DisableGlobalAnnouncements(ulong guildId)
        {
            lock (_configLock)
            {
                if (_config.GlobalChannels.ContainsKey(guildId))
                {
                    _config.GlobalChannels.Remove(guildId);
                    SaveConfig();
                    new Write().WriteLine($"GlobalAnnouncement: Disabled global announcements for guild {guildId}");
                }
            }
        }

        public void DisableAllGlobalAnnouncements()
        {
            lock (_configLock)
            {
                _config.GlobalChannels.Clear();
                SaveConfig();
                new Write().WriteLine($"GlobalAnnouncement: Disabled all global announcements");
            }
        }

        public async Task SendGlobalAnnouncementAsync(string announcement, bool ping = false)
        {
            Dictionary<ulong, GlobalChannelConfig> channelsSnapshot;
            lock (_configLock)
            {
                channelsSnapshot = new Dictionary<ulong, GlobalChannelConfig>(_config.GlobalChannels);
            }

            if (channelsSnapshot.Count == 0)
            {
                new Write().WriteLine("GlobalAnnouncement: No global channels configured, skipping announcement");
                return;
            }

            if (string.IsNullOrWhiteSpace(announcement))
                return;

            var message = $"🌐 **Global Announcement**\n\n" +
                          $"{announcement}\n\n" +
                          $"📅 **Sent At:** <t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:F>\n" +
                          $"🕐 **Time Since:** <t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:R>";

            var guildsToRemove = new List<ulong>();

            foreach (var kvp in channelsSnapshot)
            {
                var guildId = kvp.Key;
                var channelConfig = kvp.Value;

                try
                {
                    var channel = await _restClient.GetChannelAsync(channelConfig.ChannelId) as TextGuildChannel;
                    if (channel == null)
                    {
                        new Write().WriteLine($"Channel {channelConfig.ChannelId} not found for guild {guildId}, marking for removal");
                        guildsToRemove.Add(guildId);
                        continue;
                    }

                    var finalMessage = message;
                    if (ping && channelConfig.PingRoleId.HasValue)
                    {
                        finalMessage += $"\n🔔 **Ping:** <@&{channelConfig.PingRoleId}>";
                    }

                    await channel.SendMessageAsync(finalMessage);
                    new Write().WriteLine($"GlobalAnnouncement: Sent to guild {guildId} (channel {channelConfig.ChannelId})");
                }
                catch (Exception ex)
                {
                    new Write().WriteLine($"Failed to send global announcement to guild {guildId}: {ex.Message}");

                    if (ex.Message.Contains("403") || ex.Message.Contains("Forbidden") || ex.Message.Contains("Missing Access"))
                    {
                        new Write().WriteLine($"Bot lacks access to guild {guildId} or channel {channelConfig.ChannelId}, marking for removal");
                        guildsToRemove.Add(guildId);
                    }
                }
            }

            if (guildsToRemove.Count > 0)
            {
                lock (_configLock)
                {
                    foreach (var guildId in guildsToRemove)
                    {
                        if (_config.GlobalChannels.ContainsKey(guildId))
                        {
                            _config.GlobalChannels.Remove(guildId);
                            new Write().WriteLine($"Removed guild {guildId} from global announcement config due to missing access");
                        }
                    }
                    SaveConfig();
                }
            }
        }

        public string GetGlobalConfigInfo(ulong guildId)
        {
            lock (_configLock)
            {
                if (_config.GlobalChannels.TryGetValue(guildId, out var config))
                {
                    return $"Global announcements are enabled\nChannel ID: {config.ChannelId}\nRole ID: {(config.PingRoleId.HasValue ? config.PingRoleId.ToString() : "None")}";
                }

                return "Global announcements are disabled for this guild.";
            }
        }

        public string GetAllGlobalConfigInfo()
        {
            lock (_configLock)
            {
                if (_config.GlobalChannels.Count == 0)
                    return "No global announcement channels configured.";

                var result = $"Configured Global Channels ({_config.GlobalChannels.Count}):\n\n";
                foreach (var kvp in _config.GlobalChannels)
                {
                    result += $"Guild ID: {kvp.Key} -> Channel ID: {kvp.Value.ChannelId} -> Role ID: {(kvp.Value.PingRoleId.HasValue ? kvp.Value.PingRoleId.ToString() : "None")}\n";
                }
                return result;
            }
        }

        public async Task<bool> ClearGuildGlobalConfig(ulong guildId)
        {
            lock (_configLock)
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