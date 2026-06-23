// Copyright (c) 2026 Cresclent. All rights reserved.
// This Discord bot code is view-only. Hosting or running this bot is strictly prohibited!
using NetCord;
using NetCord.Rest;
using System.Text.Json;
using System.Collections.Generic;

public class StartupAnnouncement
{
    private readonly RestClient _restClient;
    private readonly string _configPath;
    private StartupConfig _config;

    public StartupAnnouncement(RestClient restClient)
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
                _config = JsonSerializer.Deserialize<StartupConfig>(json) ?? new StartupConfig();
            }
            catch
            {
                _config = new StartupConfig();
            }
        }
        else
        {
            _config = new StartupConfig();
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
            Console.WriteLine($"Failed to save startup config: {ex.Message}");
        }
    }

    public void SetAnnouncementChannel(ulong guildId, ulong channelId)
    {
        if (_config.Channels.ContainsKey(guildId))
        {
            _config.Channels[guildId] = channelId;
        }
        else
        {
            _config.Channels.Add(guildId, channelId);
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

        foreach (var kvp in _config.Channels)
        {
            var guildId = kvp.Key;
            var channelId = kvp.Value;

            try
            {
                var channel = await _restClient.GetChannelAsync(channelId) as TextGuildChannel;
                if (channel == null)
                    continue;

                await channel.SendMessageAsync(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send startup announcement to guild {guildId}: {ex.Message}");
            }
        }
    }

    public string GetConfigInfo(ulong guildId)
    {
        if (_config.Channels.TryGetValue(guildId, out var channelId))
        {
            return $"Startup announcements are enabled\nChannel ID: {channelId}";
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
            result += $"Guild ID: {kvp.Key} -> Channel ID: {kvp.Value}\n";
        }
        return result;
    }
}

public class StartupConfig
{
    public Dictionary<ulong, ulong> Channels { get; set; } = new Dictionary<ulong, ulong>();
}