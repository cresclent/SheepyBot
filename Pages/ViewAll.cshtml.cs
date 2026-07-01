// Copyright (c) 2026 Cresclent. All rights reserved.
// This Discord bot code is view-only. Hosting or running this bot is strictly prohibited!

using discord_bot.Services;
using discord_bot.SmallDat;
using discord_bot.userdataModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NetCord.Gateway;
using NetCord.Rest;
using System.Text.Json;

namespace discord_bot.Pages
{
    [IgnoreAntiforgeryToken]
    public class ViewAllModel : PageModel
    {
        private readonly VoteService _voteService;
        private readonly StartupAnnouncement _startupAnnouncement;
        private readonly GlobalAnnouncement _globalAnnouncement;
        private readonly GatewayClient _gatewayClient;
        private readonly RestClient _restClient;

        public ViewAllModel(
            VoteService voteService,
            StartupAnnouncement startupAnnouncement,
            GlobalAnnouncement globalAnnouncement,
            GatewayClient gatewayClient,
            RestClient restClient)
        {
            _voteService = voteService;
            _startupAnnouncement = startupAnnouncement;
            _globalAnnouncement = globalAnnouncement;
            _gatewayClient = gatewayClient;
            _restClient = restClient;
        }

        public string ViewType { get; set; } = string.Empty;
        public List<VoteItem> Votes { get; set; } = new();
        public List<ChannelItem> StartupChannels { get; set; } = new();
        public List<ChannelItem> GlobalChannels { get; set; } = new();
        public AllDataReport AllDataReport { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(string viewtype)
        {
            ViewType = viewtype ?? string.Empty;

            try
            {
                switch (ViewType)
                {
                    case "Votes":
                        Votes = await GetVotesAsync();
                        break;
                    case "Startups":
                        StartupChannels = await GetStartupChannelsAsync();
                        break;
                    case "Globals":
                        GlobalChannels = await GetGlobalChannelsAsync();
                        break;
                    case "AllData":
                        await LoadAllDataAsync();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WEB] ViewAll error: {ex.Message}");
            }

            return Page();
        }

        private async Task LoadAllDataAsync()
        {
            var report = new AllDataReport();

            report.VoteChannels = _voteService.GetAllVoteChannels() ?? "No vote channels configured";
            report.StartupChannels = _startupAnnouncement.GetAllConfigInfo() ?? "No startup channels configured";
            report.GlobalChannels = _globalAnnouncement.GetAllGlobalConfigInfo() ?? "No global channels configured";

            var guilds = _gatewayClient.Cache.Guilds.Select(g => g.Value).ToList();
            report.TotalGuilds = guilds.Count;

            string userDataPath = Path.Combine(AppContext.BaseDirectory, "userdata");
            if (Directory.Exists(userDataPath))
            {
                var files = Directory.GetFiles(userDataPath, "*.json");
                report.TotalUsers = files.Length;
            }

            string suggestionsPath = Path.Combine(AppContext.BaseDirectory, "suggestions");
            if (Directory.Exists(suggestionsPath))
            {
                var files = Directory.GetFiles(suggestionsPath, "suggestion-*.json");
                report.TotalSuggestions = files.Length;
                
                int pending = 0, approved = 0, denied = 0;
                foreach (var file in files)
                {
                    try
                    {
                        string json = System.IO.File.ReadAllText(file);
                        var data = JsonSerializer.Deserialize<JsonElement>(json);
                        var status = data.GetProperty("Status").GetString();
                        if (status == "pending") pending++;
                        else if (status == "approved") approved++;
                        else if (status == "denied") denied++;
                    }
                    catch { }
                }
                report.PendingSuggestions = pending;
                report.ApprovedSuggestions = approved;
                report.DeniedSuggestions = denied;
            }

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

            report.TotalCommands = allLogs.Sum(u => u.TotalCommands);
            report.TotalCommandUsers = allLogs.Select(u => u.UserId).Distinct().Count();

            var topUsers = allLogs
                .GroupBy(u => u.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    UserName = g.First().UserName ?? "Unknown",
                    TotalCommands = g.Sum(u => u.TotalCommands)
                })
                .OrderByDescending(u => u.TotalCommands)
                .Take(10)
                .ToList();

            var topUsersWithCommands = new List<TopUserWithCommands>();
            foreach (var user in topUsers)
            {
                var allUserCommands = allLogs
                    .Where(u => u.UserId == user.UserId)
                    .SelectMany(u => u.Guilds.Values.SelectMany(g => g.Channels.Values.SelectMany(c => c.Commands)))
                    .ToList();

                // Group by command name with timestamps
                var commandGroups = allUserCommands
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

                topUsersWithCommands.Add(new TopUserWithCommands
                {
                    UserId = user.UserId,
                    UserName = user.UserName,
                    TotalCommands = user.TotalCommands,
                    CommandCounts = commandGroups
                });
            }

            report.TopUsersWithCommands = topUsersWithCommands;

            AllDataReport = report;
        }

        private async Task<List<VoteItem>> GetVotesAsync()
        {
            var votes = new List<VoteItem>();
            try
            {
                var history = _voteService.GetVoteHistory(10);
                if (history != null && history.Count > 0)
                {
                    foreach (var item in history)
                    {
                        votes.Add(new VoteItem
                        {
                            VoteId = item.NewBanner ?? item.OldBanner ?? "Unknown",
                            UserId = "System",
                            VoteValue = item.Passed ? "Passed" : "Failed",
                            Timestamp = item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WEB] Error getting votes: {ex.Message}");
            }
            return votes;
        }

        private async Task<List<ChannelItem>> GetStartupChannelsAsync()
        {
            var channels = new List<ChannelItem>();
            try
            {
                var guilds = _gatewayClient.Cache.Guilds.Select(g => g.Value).ToList();
                foreach (var guild in guilds)
                {
                    var info = _startupAnnouncement.GetConfigInfo(guild.Id);
                    if (!string.IsNullOrEmpty(info) && !info.Contains("disabled"))
                    {
                        channels.Add(new ChannelItem
                        {
                            ChannelId = guild.Id.ToString(),
                            ChannelName = guild.Name,
                            CreatedAt = "Configured"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WEB] Error getting startup channels: {ex.Message}");
            }
            return channels;
        }

        private async Task<List<ChannelItem>> GetGlobalChannelsAsync()
        {
            var channels = new List<ChannelItem>();
            try
            {
                var guilds = _gatewayClient.Cache.Guilds.Select(g => g.Value).ToList();
                foreach (var guild in guilds)
                {
                    var info = _globalAnnouncement.GetGlobalConfigInfo(guild.Id);
                    if (!string.IsNullOrEmpty(info) && !info.Contains("disabled"))
                    {
                        channels.Add(new ChannelItem
                        {
                            ChannelId = guild.Id.ToString(),
                            ChannelName = guild.Name,
                            CreatedAt = "Configured"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WEB] Error getting global channels: {ex.Message}");
            }
            return channels;
        }
    }

    public class VoteItem
    {
        public string VoteId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string VoteValue { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
    }

    public class ChannelItem
    {
        public string ChannelId { get; set; } = string.Empty;
        public string ChannelName { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
    }

    public class CommandCount
    {
        public string CommandName { get; set; } = string.Empty;
        public int Count { get; set; }
        public List<string> Timestamps { get; set; } = new();
    }

    public class TopUserWithCommands
    {
        public ulong UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int TotalCommands { get; set; }
        public List<CommandCount> CommandCounts { get; set; } = new();
    }

    public class AllDataReport
    {
        public string VoteChannels { get; set; } = string.Empty;
        public string StartupChannels { get; set; } = string.Empty;
        public string GlobalChannels { get; set; } = string.Empty;
        public int TotalGuilds { get; set; }
        public int TotalUsers { get; set; }
        public int TotalSuggestions { get; set; }
        public int PendingSuggestions { get; set; }
        public int ApprovedSuggestions { get; set; }
        public int DeniedSuggestions { get; set; }
        public int TotalCommands { get; set; }
        public int TotalCommandUsers { get; set; }
        public List<TopUserWithCommands> TopUsersWithCommands { get; set; } = new();
    }
}