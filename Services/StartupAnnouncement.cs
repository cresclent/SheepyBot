// Copyright (c) 2026 Cresclent. All rights reserved.
// This Discord bot code is view-only. Hosting or running this bot is strictly prohibited!
using discord_bot.Tools;
using discord_bot.Services;
using NetCord;
using NetCord.Rest;
using System.Collections.Generic;
using System.Text.Json;

public class StartupAnnouncement
{
    private readonly RestClient _restClient;
    private readonly string _configPath;
    private GlobalConfig _config;

    public StartupAnnouncement(RestClient restClient)
    {
        _restClient = restClient;
        _configPath = Path.Combine(AppContext.BaseDirectory, "startup_config.json");
        LoadConfig();
    }

    public void ReloadConfig()
    {
        LoadConfig();
        new Write().WriteLine($"StartupAnnouncement: Config reloaded. Found {_config.Channels.Count} startup channels.");
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
            new Write().WriteLine($"Failed to save startup config: {ex.Message}");
        }
    }

    public void SetAnnouncementChannel(ulong guildId, ulong channelId, ulong? pingRoleId = null)
    {
        if (_config.Channels.ContainsKey(guildId))
        {
            _config.Channels[guildId].ChannelId = channelId;
            _config.Channels[guildId].PingRoleId = pingRoleId;
        }
        else
        {
            _config.Channels.Add(guildId, new StartupChannelConfig
            {
                ChannelId = channelId,
                PingRoleId = pingRoleId
            });
        }

        SaveConfig();
    }

    public void DisableAnnouncements(ulong guildId)
    {
        if (_config.Channels.ContainsKey(guildId))
        {
            _config.Channels.Remove(guildId);
            SaveConfig();
        }
    }

    public void DisableAllAnnouncements()
    {
        _config.Channels.Clear();
        SaveConfig();
    }

    public async Task SendStartupAnnouncementAsync()
    {
        if (_config.Channels.Count == 0)
            return;

        var message = $"🟢 **Bot Started Successfully!**\n\n" +
                      $"The bot is now online and ready to use!\n\n" +
                      $"📅 **Started At:** <t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:F>\n" +
                      $"🕐 **Time Since:** <t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:R>\n" +
                      $"🤖 **Status:** ✅ Online";

        var guildsToRemove = new List<ulong>();

        foreach (var kvp in _config.Channels)
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
                if (channelConfig.PingRoleId.HasValue)
                {
                    finalMessage += $"\n🔔 **Ping:** <@&{channelConfig.PingRoleId}>";
                }

                await channel.SendMessageAsync(finalMessage);
            }
            catch (Exception ex)
            {
                new Write().WriteLine($"Failed to send startup announcement to guild {guildId}: {ex.Message}");

                if (ex.Message.Contains("403") || ex.Message.Contains("Forbidden") || ex.Message.Contains("Missing Access"))
                {
                    new Write().WriteLine($"Bot lacks access to guild {guildId} or channel {channelConfig.ChannelId}, removing config");
                    guildsToRemove.Add(guildId);
                }
            }
        }

        foreach (var guildId in guildsToRemove)
        {
            if (_config.Channels.ContainsKey(guildId))
            {
                _config.Channels.Remove(guildId);
                new Write().WriteLine($"Removed guild {guildId} from startup announcement config due to missing access");
            }
        }

        if (guildsToRemove.Count > 0)
        {
            SaveConfig();
        }
    }

    public string GetConfigInfo(ulong guildId)
    {
        if (_config.Channels.TryGetValue(guildId, out var config))
        {
            return $"Startup announcements are enabled\nChannel ID: {config.ChannelId}\nRole ID: {(config.PingRoleId.HasValue ? config.PingRoleId.ToString() : "None")}";
        }

        return "Startup announcements are disabled for this guild.";
    }

    public string GetAllConfigInfo()
    {
        if (_config.Channels.Count == 0)
            return "No startup announcement channels configured.";

        var result = "Configured Startup Channels:\n\n";
        foreach (var kvp in _config.Channels)
        {
            result += $"Guild ID: {kvp.Key} -> Channel ID: {kvp.Value.ChannelId} -> Role ID: {(kvp.Value.PingRoleId.HasValue ? kvp.Value.PingRoleId.ToString() : "None")}\n";
        }
        return result;
    }

    public async Task<bool> ClearGuildConfig(ulong guildId)
    {
        if (_config.Channels.ContainsKey(guildId))
        {
            _config.Channels.Remove(guildId);
            SaveConfig();
            return true;
        }
        return false;
    }
}

public class StartupConfig
{
    public Dictionary<ulong, StartupChannelConfig> Channels { get; set; } = new Dictionary<ulong, StartupChannelConfig>();
}

public class StartupChannelConfig
{
    public ulong ChannelId { get; set; }
    public ulong? PingRoleId { get; set; }
}