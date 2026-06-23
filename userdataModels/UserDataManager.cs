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
            Console.WriteLine($"📁 User data directory: {_dataDirectory}");
        }


        private string GetUserFilePath(ulong userId)
        {
            // Just use userId directly - no guild folder
            return Path.Combine(_dataDirectory, $"{userId}.json");
        }

        // ⭐ FIXED: Only takes userId, not guildId
        public UserWishData GetOrCreateUserData(ulong userId)
        {
            lock (_lock)
            {
                // Check cache first
                if (_cache.TryGetValue(userId, out var cachedData))
                {
                    return cachedData;
                }

                // Try to load from file
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
                        Console.WriteLine($"⚠️ Error loading user data: {ex.Message}");
                    }
                }

                // Create new data
                var newData = new UserWishData();
                _cache[userId] = newData;
                SaveUserData(userId, newData);
                return newData;
            }
        }

        // ⭐ FIXED: Only takes userId and data, not guildId
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
                    Console.WriteLine($"⚠️ Error saving user data: {ex.Message}");
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