// Copyright (c) 2026 Cresclent. All rights reserved.
// This Discord bot code is view-only. Hosting or running this bot is strictly prohibited!
using discord_bot.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace discord_bot.userdataModels
{
    public class UserDataManager
    {
        private readonly string _dataDirectory;
        private readonly Dictionary<ulong, UserWishData> _cache = new();
        private readonly object _lock = new();

        public UserDataManager()
        {
            _dataDirectory = Path.Combine(AppContext.BaseDirectory, "userdata");
            Directory.CreateDirectory(_dataDirectory);
            new Write().WriteLine($"User data directory: {_dataDirectory}");
        }

        private string GetUserFilePath(ulong userId)
        {
            return Path.Combine(_dataDirectory, $"{userId}.json");
        }

        public UserWishData GetOrCreateUserData(ulong userId)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(userId, out var cachedData))
                {
                    return cachedData;
                }

                var filePath = GetUserFilePath(userId);

                if (File.Exists(filePath))
                {
                    try
                    {
                        var json = File.ReadAllText(filePath);
                        var data = JsonSerializer.Deserialize<UserWishData>(json);
                        if (data != null)
                        {
                            _cache[userId] = data;
                            return data;
                        }
                    }
                    catch (Exception ex)
                    {
                        new Write().WriteLine($"Error loading user data: {ex.Message}");
                    }
                }

                var newData = new UserWishData();
                _cache[userId] = newData;
                SaveUserData(userId, newData);
                return newData;
            }
        }

        public void SaveUserData(ulong userId, UserWishData data)
        {
            lock (_lock)
            {
                try
                {
                    var filePath = GetUserFilePath(userId);
                    data.LastUpdated = DateTime.UtcNow;
                    var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(filePath, json);
                    _cache[userId] = data;
                }
                catch (Exception ex)
                {
                    new Write().WriteLine($"Error saving user data: {ex.Message}");
                }
            }
        }

        public void SaveAllData()
        {
            lock (_lock)
            {
                foreach (var kvp in _cache)
                {
                    SaveUserData(kvp.Key, kvp.Value);
                }
            }
        }
    }
}