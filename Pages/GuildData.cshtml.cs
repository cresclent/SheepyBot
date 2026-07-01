using discord_bot.SmallDat;
using discord_bot.userdataModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NetCord.Gateway;
using System.Text.Json;

namespace discord_bot.Pages
{
    [IgnoreAntiforgeryToken]
    public class GuildDataModel : PageModel
    {
        private readonly GatewayClient _gatewayClient;

        public GuildDataModel(GatewayClient gatewayClient)
        {
            _gatewayClient = gatewayClient;
        }

        public List<GuildInfo> Guilds { get; set; } = new();
        public ulong SelectedGuildId { get; set; }
        public string SelectedGuildName { get; set; } = string.Empty;
        public GuildDataReport? GuildData { get; set; }

        public async Task OnGetAsync(string guildId)
        {
            if (!string.IsNullOrEmpty(guildId) && ulong.TryParse(guildId, out var parsedId))
            {
                SelectedGuildId = parsedId;
            }

            var guilds = _gatewayClient.Cache.Guilds.Select(g => g.Value).ToList();
            Guilds = guilds.Select(g => new GuildInfo
            {
                Id = g.Id,
                Name = g.Name
            }).OrderBy(g => g.Name).ToList();

            if (SelectedGuildId != 0)
            {
                var selectedGuild = guilds.FirstOrDefault(g => g.Id == SelectedGuildId);
                SelectedGuildName = selectedGuild?.Name ?? "Unknown";
                GuildData = await LoadGuildData(SelectedGuildId);
            }
        }

        private async Task<GuildDataReport> LoadGuildData(ulong guildId)
        {
            var report = new GuildDataReport();
            var startDate = new DateTime(2026, 6, 20);
            var endDate = DateTime.Now.Date;
            var allLogs = new List<UserDataLogger>();

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                string dateStr = date.ToString("yyyy-MM-dd");
                string filePath = Path.Combine(AppContext.BaseDirectory, "totaldata", $"Log-{dateStr}.json");

                if (System.IO.File.Exists(filePath))
                {
                    try
                    {
                        string json = System.IO.File.ReadAllText(filePath);
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            var dayLogs = JsonSerializer.Deserialize<List<UserDataLogger>>(json) ?? new List<UserDataLogger>();
                            allLogs.AddRange(dayLogs);
                        }
                    }
                    catch { }
                }
            }

            var guildLogs = allLogs.Where(log => log.Guilds.ContainsKey(guildId.ToString())).ToList();

            if (guildLogs.Count == 0)
                return report;

            report.TotalUsers = guildLogs.Count;
            report.TotalCommands = guildLogs.Sum(u => u.Guilds[guildId.ToString()].TotalCommands);

            // Merge users by UserId and collect their command data
            var userGroups = guildLogs
                .GroupBy(u => u.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    UserName = g.First().UserName ?? "Unknown",
                    TotalCommands = g.Sum(u => u.Guilds[guildId.ToString()].TotalCommands),
                    // Collect all commands with timestamps
                    Commands = g
                        .SelectMany(u => u.Guilds[guildId.ToString()].Channels.Values.SelectMany(c => c.Commands))
                        .ToList()
                })
                .OrderByDescending(u => u.TotalCommands)
                .Take(10)
                .ToList();

            // Build command counts with timestamps
            var topUsers = new List<TopUserData>();
            foreach (var user in userGroups)
            {
                var commandGroups = user.Commands
                    .Select(cmd =>
                    {
                        var parts = cmd.Split(" at ");
                        return new
                        {
                            CommandName = parts.Length > 0 ? parts[0] : cmd,
                            Timestamp = parts.Length > 1 ? parts[1] : DateTime.Now.ToString("yyyy-MM-dd HH:mm")
                        };
                    })
                    .GroupBy(c => c.CommandName)
                    .Select(g => new CommandCount
                    {
                        CommandName = g.Key,
                        Count = g.Count(),
                        Timestamps = g.Select(c => c.Timestamp).ToList()
                    })
                    .OrderByDescending(c => c.Count)
                    .ToList();

                topUsers.Add(new TopUserData
                {
                    UserId = user.UserId,
                    UserName = user.UserName,
                    TotalCommands = user.TotalCommands,
                    CommandCounts = commandGroups
                });
            }

            report.TopUsers = topUsers;

            // Channel stats (keeping as is)
            var channelStats = new Dictionary<string, int>();
            foreach (var user in guildLogs)
            {
                var guildData = user.Guilds[guildId.ToString()];
                foreach (var channel in guildData.Channels)
                {
                    string channelName = channel.Key;
                    if (channelStats.ContainsKey(channelName))
                        channelStats[channelName] += channel.Value.CommandCount;
                    else
                        channelStats[channelName] = channel.Value.CommandCount;
                }
            }

            report.ChannelStats = channelStats.Select(c => new ChannelStat
            {
                ChannelName = c.Key,
                CommandCount = c.Value
            }).ToList();

            report.ChannelCount = report.ChannelStats.Count;

            return report;
        }
    }

    public class GuildInfo
    {
        public ulong Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class TopUserData
    {
        public ulong UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int TotalCommands { get; set; }
        public List<CommandCount> CommandCounts { get; set; } = new();
    }

    public class GuildDataReport
    {
        public int TotalUsers { get; set; }
        public int TotalCommands { get; set; }
        public int ChannelCount { get; set; }
        public List<TopUserData> TopUsers { get; set; } = new();
        public List<ChannelStat> ChannelStats { get; set; } = new();
    }

    public class ChannelStat
    {
        public string ChannelName { get; set; } = string.Empty;
        public int CommandCount { get; set; }
    }
}