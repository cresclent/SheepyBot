// Copyright (c) 2026 Cresclent. All rights reserved.
// This Discord bot code is view-only. Hosting or running this bot is strictly prohibited!
using NetCord.Services.ApplicationCommands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace discord_bot.userdataModels
{
    internal class UserDataLogger
    {
        private static readonly object _fileLock = new object();
        private static string _baseDirectory = string.Empty;
        private static string _logDirectory = string.Empty;
        private static bool _isInitialized = false;
        private static HashSet<ulong> _ignoredUserIds = new HashSet<ulong>();
        private static readonly object _ignoreLock = new object();
        private static string _ignoreFilePath = string.Empty;

        [JsonPropertyName("userId")]
        public ulong UserId { get; set; }

        [JsonPropertyName("userName")]
        public string? UserName { get; set; }

        [JsonPropertyName("guilds")]
        public Dictionary<string, GuildData> Guilds { get; set; } = new Dictionary<string, GuildData>();

        [JsonPropertyName("totalCommands")]
        public int TotalCommands { get; set; }

        [JsonPropertyName("lastCommandTime")]
        public DateTime LastCommandTime { get; set; }

        public UserDataLogger() { }

        public UserDataLogger(ulong userId, string userName)
        {
            UserId = userId;
            UserName = userName;
            Guilds = new Dictionary<string, GuildData>();
            TotalCommands = 0;
            LastCommandTime = DateTime.Now;
        }

        private static void LoadIgnoredUsers()
        {
            lock (_ignoreLock)
            {
                try
                {
                    // Use the same directory as appsettings.json
                    _ignoreFilePath = Path.Combine(AppContext.BaseDirectory, "IgnoredUserIds.json");

                    if (File.Exists(_ignoreFilePath))
                    {
                        string json = File.ReadAllText(_ignoreFilePath);
                        var ignoredList = JsonSerializer.Deserialize<List<ulong>>(json);
                        if (ignoredList != null)
                        {
                            _ignoredUserIds = new HashSet<ulong>(ignoredList);
                        }
                    }
                    else
                    {
                        // Create empty file if it doesn't exist
                        SaveIgnoredUsers();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading ignored users: {ex.Message}");
                }
            }
        }

        public static void AddIgnoredUser(ulong userId)
        {
            lock (_ignoreLock)
            {
                _ignoredUserIds.Add(userId);
                SaveIgnoredUsers();
            }
        }

        public static void RemoveIgnoredUser(ulong userId)
        {
            lock (_ignoreLock)
            {
                _ignoredUserIds.Remove(userId);
                SaveIgnoredUsers();
            }
        }

        public static bool IsUserIgnored(ulong userId)
        {
            lock (_ignoreLock)
            {
                return _ignoredUserIds.Contains(userId);
            }
        }

        public static List<ulong> GetIgnoredUsers()
        {
            lock (_ignoreLock)
            {
                return _ignoredUserIds.ToList();
            }
        }

        private static void SaveIgnoredUsers()
        {
            try
            {
                if (string.IsNullOrEmpty(_ignoreFilePath))
                {
                    _ignoreFilePath = Path.Combine(AppContext.BaseDirectory, "IgnoredUserIds.json");
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_ignoredUserIds.ToList(), options);
                File.WriteAllText(_ignoreFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving ignored users: {ex.Message}");
            }
        }

        public static void Init()
        {
            try
            {
                _baseDirectory = AppContext.BaseDirectory;
                _logDirectory = Path.Combine(_baseDirectory, "totaldata");

                Directory.CreateDirectory(_logDirectory);

                _isInitialized = true;

                LoadIgnoredUsers();

                if (!File.Exists(GetLogFilePath()))
                {
                    var initialData = new List<UserDataLogger>
                    {
                        new UserDataLogger(0, "Bot_System")
                        {
                            Guilds = new Dictionary<string, GuildData>
                            {
                                ["system"] = new GuildData
                                {
                                    GuildId = 0,
                                    GuildName = "System",
                                    Channels = new Dictionary<string, ChannelData>
                                    {
                                        ["system"] = new ChannelData
                                        {
                                            ChannelId = 0,
                                            Commands = { $"Bot Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}" },
                                            CommandCount = 1
                                        }
                                    },
                                    TotalCommands = 1
                                }
                            },
                            TotalCommands = 1,
                            LastCommandTime = DateTime.Now
                        }
                    };

                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        IncludeFields = false
                    };

                    string json = JsonSerializer.Serialize(initialData, options);
                    File.WriteAllText(GetLogFilePath(), json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logger initialization failed: {ex.Message}");
                _isInitialized = false;
            }
        }

        public static void Init(string customDirectory)
        {
            try
            {
                _baseDirectory = customDirectory;
                _logDirectory = Path.Combine(_baseDirectory, "totaldata");

                Directory.CreateDirectory(_logDirectory);

                _isInitialized = true;

                LoadIgnoredUsers();

                if (!File.Exists(GetLogFilePath()))
                {
                    var initialData = new List<UserDataLogger>
                    {
                        new UserDataLogger(0, "Bot_System")
                        {
                            Guilds = new Dictionary<string, GuildData>
                            {
                                ["system"] = new GuildData
                                {
                                    GuildId = 0,
                                    GuildName = "System",
                                    Channels = new Dictionary<string, ChannelData>
                                    {
                                        ["system"] = new ChannelData
                                        {
                                            ChannelId = 0,
                                            Commands = { $"Bot Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}" },
                                            CommandCount = 1
                                        }
                                    },
                                    TotalCommands = 1
                                }
                            },
                            TotalCommands = 1,
                            LastCommandTime = DateTime.Now
                        }
                    };

                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        IncludeFields = false
                    };

                    string json = JsonSerializer.Serialize(initialData, options);
                    File.WriteAllText(GetLogFilePath(), json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logger initialization failed: {ex.Message}");
                _isInitialized = false;
            }
        }

        private static string GetLogFilePath()
        {
            return Path.Combine(_logDirectory, $"Log-{DateOnly.FromDateTime(DateTime.Now).ToString("yyyy-MM-dd")}.json");
        }

        private static string GetLogFilePath(string date)
        {
            return Path.Combine(_logDirectory, $"Log-{date}.json");
        }

        public static bool IsInitialized()
        {
            return _isInitialized;
        }

        public void Logger(ApplicationCommandContext context, string commandName)
        {
            if (!_isInitialized)
            {
                return;
            }

            var userId = context.User.Id;

            if (IsUserIgnored(userId))
            {
                return;
            }

            lock (_fileLock)
            {
                try
                {
                    var userName = context.User.Username;
                    var guildId = context.Guild?.Id ?? 0;
                    var guildName = context.Guild?.Name ?? "DM";
                    var channelId = context.Channel?.Id ?? 0;

                    string fp = GetLogFilePath();

                    List<UserDataLogger> allUserData = new List<UserDataLogger>();

                    if (File.Exists(fp))
                    {
                        try
                        {
                            string json = File.ReadAllText(fp);
                            if (!string.IsNullOrWhiteSpace(json))
                            {
                                allUserData = JsonSerializer.Deserialize<List<UserDataLogger>>(json) ?? new List<UserDataLogger>();
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error loading logger data: {ex.Message}");
                            allUserData = new List<UserDataLogger>();
                        }
                    }

                    var userEntry = allUserData.Find(u => u.UserId == userId);

                    if (userEntry == null)
                    {
                        userEntry = new UserDataLogger(userId, userName);
                        allUserData.Add(userEntry);
                    }
                    else
                    {
                        userEntry.UserName = userName;
                    }

                    string guildKey = guildId.ToString();
                    if (!userEntry.Guilds.ContainsKey(guildKey))
                    {
                        userEntry.Guilds[guildKey] = new GuildData
                        {
                            GuildId = guildId,
                            GuildName = guildName,
                            Channels = new Dictionary<string, ChannelData>()
                        };
                    }

                    var guildData = userEntry.Guilds[guildKey];
                    guildData.GuildName = guildName;
                    guildData.TotalCommands++;

                    string channelKey = channelId.ToString();
                    if (!guildData.Channels.ContainsKey(channelKey))
                    {
                        guildData.Channels[channelKey] = new ChannelData
                        {
                            ChannelId = channelId,
                            Commands = new List<string>(),
                            CommandCount = 0
                        };
                    }

                    var channelData = guildData.Channels[channelKey];
                    channelData.Commands.Add($"{commandName} at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    channelData.CommandCount++;

                    userEntry.TotalCommands++;
                    userEntry.LastCommandTime = DateTime.Now;

                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        IncludeFields = false
                    };

                    string updatedJson = JsonSerializer.Serialize(allUserData, options);
                    File.WriteAllText(fp, updatedJson);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving logger data: {ex.Message}");
                }
            }
        }

        public List<UserDataLogger> ReadLogs()
        {
            if (!_isInitialized)
            {
                return new List<UserDataLogger>();
            }

            return ReadLogs(DateOnly.FromDateTime(DateTime.Now).ToString("yyyy-MM-dd"));
        }

        public List<UserDataLogger> ReadLogs(string date)
        {
            if (!_isInitialized)
            {
                return new List<UserDataLogger>();
            }

            string fp = GetLogFilePath(date);

            if (!File.Exists(fp))
                return new List<UserDataLogger>();

            try
            {
                string json = File.ReadAllText(fp);
                return JsonSerializer.Deserialize<List<UserDataLogger>>(json) ?? new List<UserDataLogger>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading logs: {ex.Message}");
                return new List<UserDataLogger>();
            }
        }

        public int GetCommandCount(ulong userId)
        {
            if (IsUserIgnored(userId))
                return 0;

            var logs = ReadLogs();
            var user = logs.Find(u => u.UserId == userId);
            return user?.TotalCommands ?? 0;
        }

        public int GetCommandCount(ulong userId, ulong guildId)
        {
            if (IsUserIgnored(userId))
                return 0;

            var logs = ReadLogs();
            var user = logs.Find(u => u.UserId == userId);
            if (user != null && user.Guilds.TryGetValue(guildId.ToString(), out var guildData))
            {
                return guildData.TotalCommands;
            }
            return 0;
        }

        public int GetCommandCount(ulong userId, ulong guildId, ulong channelId)
        {
            if (IsUserIgnored(userId))
                return 0;

            var logs = ReadLogs();
            var user = logs.Find(u => u.UserId == userId);
            if (user != null &&
                user.Guilds.TryGetValue(guildId.ToString(), out var guildData) &&
                guildData.Channels.TryGetValue(channelId.ToString(), out var channelData))
            {
                return channelData.CommandCount;
            }
            return 0;
        }

        public int GetUniqueUserCount()
        {
            var logs = ReadLogs();
            return logs.Count;
        }

        public int GetTotalCommandCount()
        {
            var logs = ReadLogs();
            int total = 0;
            foreach (var user in logs)
            {
                total += user.TotalCommands;
            }
            return total;
        }

        public Dictionary<string, int> GetCommandStats()
        {
            var stats = new Dictionary<string, int>();
            var logs = ReadLogs();

            foreach (var user in logs)
            {
                foreach (var guild in user.Guilds.Values)
                {
                    foreach (var channel in guild.Channels.Values)
                    {
                        foreach (var command in channel.Commands)
                        {
                            string commandName = command.Split(' ')[0];
                            if (stats.ContainsKey(commandName))
                                stats[commandName]++;
                            else
                                stats[commandName] = 1;
                        }
                    }
                }
            }

            return stats;
        }

        public List<UserDataLogger> ReadLogsByGuild(ulong guildId)
        {
            var logs = ReadLogs();
            var result = new List<UserDataLogger>();

            foreach (var user in logs)
            {
                if (user.Guilds.ContainsKey(guildId.ToString()))
                {
                    result.Add(user);
                }
            }

            return result;
        }

        public List<UserDataLogger> ReadLogsByChannel(ulong channelId)
        {
            var logs = ReadLogs();
            var result = new List<UserDataLogger>();

            foreach (var user in logs)
            {
                foreach (var guild in user.Guilds.Values)
                {
                    if (guild.Channels.ContainsKey(channelId.ToString()))
                    {
                        result.Add(user);
                        break;
                    }
                }
            }

            return result;
        }

        public UserDataLogger? GetUserLogs(ulong userId)
        {
            if (IsUserIgnored(userId))
                return null;

            var logs = ReadLogs();
            return logs.Find(u => u.UserId == userId);
        }

        public List<GuildData> GetUserGuilds(ulong userId)
        {
            if (IsUserIgnored(userId))
                return new List<GuildData>();

            var user = GetUserLogs(userId);
            if (user != null)
            {
                return user.Guilds.Values.ToList();
            }
            return new List<GuildData>();
        }
    }

    public class GuildData
    {
        [JsonPropertyName("guildId")]
        public ulong GuildId { get; set; }

        [JsonPropertyName("guildName")]
        public string? GuildName { get; set; }

        [JsonPropertyName("channels")]
        public Dictionary<string, ChannelData> Channels { get; set; } = new Dictionary<string, ChannelData>();

        [JsonPropertyName("totalCommands")]
        public int TotalCommands { get; set; }
    }

    public class ChannelData
    {
        [JsonPropertyName("channelId")]
        public ulong ChannelId { get; set; }

        [JsonPropertyName("commands")]
        public List<string> Commands { get; set; } = new List<string>();

        [JsonPropertyName("commandCount")]
        public int CommandCount { get; set; }
    }
}