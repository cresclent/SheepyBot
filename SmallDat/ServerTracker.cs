using NetCord;
using NetCord.Gateway;
using discord_bot.userdataModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace discord_bot.SmallDat
{
    public class ServerTracker
    {
        private readonly string _dataDirectory;
        private readonly string _serverListFile;
        private HashSet<ulong> _servers;

        public ServerTracker()
        {
            _dataDirectory = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "smalldat", "data");
            _dataDirectory = Path.GetFullPath(_dataDirectory);
            _serverListFile = Path.Combine(_dataDirectory, "server_list.json");

            Directory.CreateDirectory(_dataDirectory);
            LoadServers();
        }

        private void LoadServers()
        {
            _servers = new HashSet<ulong>();

            if (File.Exists(_serverListFile))
            {
                try
                {
                    string json = File.ReadAllText(_serverListFile);
                    var list = JsonSerializer.Deserialize<List<ulong>>(json);
                    if (list != null)
                    {
                        _servers = new HashSet<ulong>(list);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load server list: {ex.Message}");
                }
            }
        }

        private void SaveServers()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_servers.ToList(), options);
                File.WriteAllText(_serverListFile, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save server list: {ex.Message}");
            }
        }

        public void AddServer(ulong guildId)
        {
            if (_servers.Add(guildId))
            {
                SaveServers();
                Console.WriteLine($"Server {guildId} added to tracking list");
            }
        }

        public void RemoveServer(ulong guildId)
        {
            if (_servers.Remove(guildId))
            {
                SaveServers();
                Console.WriteLine($"Server {guildId} removed from tracking list");
            }
        }

        public bool IsServerTracked(ulong guildId)
        {
            return _servers.Contains(guildId);
        }

        public List<ulong> GetAllServers()
        {
            return _servers.ToList();
        }

        public async Task CleanupServerData(ulong guildId)
        {
            Console.WriteLine($"Cleaning up data for server {guildId}...");

            try
            {
                string totalDataPath = Path.Combine(AppContext.BaseDirectory, "totaldata");
                int deletedLogFiles = 0;
                int filesProcessed = 0;
                int usersRemoved = 0;

                if (Directory.Exists(totalDataPath))
                {
                    var logFiles = Directory.GetFiles(totalDataPath, "Log-*.json");

                    foreach (var file in logFiles)
                    {
                        filesProcessed++;
                        try
                        {
                            string json = File.ReadAllText(file);
                            var dayLogs = JsonSerializer.Deserialize<List<UserDataLogger>>(json);

                            if (dayLogs != null && dayLogs.Count > 0)
                            {
                                bool fileModified = false;
                                var guildIdStr = guildId.ToString();

                                foreach (var log in dayLogs)
                                {
                                    if (log.Guilds.ContainsKey(guildIdStr))
                                    {
                                        log.Guilds.Remove(guildIdStr);
                                        usersRemoved++;
                                        fileModified = true;
                                    }
                                }

                                if (fileModified)
                                {
                                    var options = new JsonSerializerOptions { WriteIndented = true };
                                    string updatedJson = JsonSerializer.Serialize(dayLogs, options);
                                    File.WriteAllText(file, updatedJson);
                                    deletedLogFiles++;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing {file}: {ex.Message}");
                        }
                    }

                    Console.WriteLine($"Processed {filesProcessed} log files, removed guild data from {deletedLogFiles} files ({usersRemoved} user entries)");
                }

                string configPath = Path.Combine(AppContext.BaseDirectory, "startup_config.json");
                if (File.Exists(configPath))
                {
                    try
                    {
                        string json = File.ReadAllText(configPath);
                        var config = JsonSerializer.Deserialize<Dictionary<string, StartupChannelConfig>>(json);

                        if (config != null && config.ContainsKey(guildId.ToString()))
                        {
                            config.Remove(guildId.ToString());
                            var options = new JsonSerializerOptions { WriteIndented = true };
                            string updatedJson = JsonSerializer.Serialize(config, options);
                            File.WriteAllText(configPath, updatedJson);
                            Console.WriteLine($"Removed guild {guildId} from startup_config.json");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing startup_config.json: {ex.Message}");
                    }
                }

                RemoveServer(guildId);
                Console.WriteLine($"Server {guildId} data cleanup completed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to cleanup data for server {guildId}: {ex.Message}");
            }
        }

        public async Task CheckAndCleanupRemovedServers(List<ulong> currentGuilds)
        {
            var trackedServers = GetAllServers();
            var removedServers = trackedServers.Where(id => !currentGuilds.Contains(id)).ToList();

            if (removedServers.Count == 0)
            {
                Console.WriteLine("All tracked servers are still present");
                return;
            }

            Console.WriteLine($"Found {removedServers.Count} servers that the bot is no longer in:");
            foreach (var serverId in removedServers)
            {
                Console.WriteLine($"- {serverId}");
                await CleanupServerData(serverId);
            }
        }
    }
}