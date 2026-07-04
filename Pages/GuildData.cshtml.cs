// Copyright (c) 2026 Cresclent. All rights reserved.
// This Discord bot code is view-only. Hosting or running this bot is strictly prohibited!

using discord_bot.userdataModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using System.Text.Json;

namespace discord_bot.Pages
{
    [IgnoreAntiforgeryToken]
    public class GuildDataModel : PageModel
    {
        private readonly GatewayClient _gatewayClient;
        private readonly RestClient _restClient;

        public GuildDataModel(GatewayClient gatewayClient, RestClient restClient)
        {
            _gatewayClient = gatewayClient;
            _restClient = restClient;
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

        public async Task<IActionResult> OnPost()
        {
            var action = Request.Form["action"].ToString();
            var dataOne = Request.Form["dataOne"].ToString();

            switch (action)
            {
                case "ToServer":
                    return await goToServer(dataOne);
                default:
                    return Page();
            }
        }

        private async Task<IActionResult> goToServer(string data)
        {
            ulong.TryParse(data, out ulong guildId);

            if (guildId == 0)
            {
                return BadRequest($"No guild selected\nguildId: {guildId}\n data: {data}");
            }

            try
            {
                var guild = _gatewayClient.Cache.Guilds.FirstOrDefault(g => g.Key == guildId).Value;

                if (guild == null)
                {
                    return BadRequest("Guild not found");
                }

                var channels = (await guild.GetChannelsAsync()).OfType<TextChannel>();

                if (channels.Count() == 0)
                {
                    return BadRequest("No text channels found in this guild");
                }

                var channel = channels.First();

                var inviteprops = new InviteProperties
                {
                    MaxAge = 300,
                    MaxUses = 1,
                    Temporary = true,
                    Unique = true
                };

                var invite = await _restClient.CreateGuildChannelInviteAsync(channel.Id, inviteprops);

                string html = $@"
                    <html>
                    <head>
                        <script type='text/javascript'>
                            window.open('https://discord.gg/{invite.Code}', '_blank');
                            setTimeout(function() {{
                                window.location.href = '/GuildData/{SelectedGuildId}';
                            }}, 2000);
                        </script>
                    </head>
                    <body>
                        <p>Opening invite in new tab...</p>
                        <p>If the tab doesn't open, <a href='https://discord.gg/{invite.Code}' target='_blank'>click here</a>.</p>
                    </body>
                    </html>";

                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                string errorHtml = $@"
                    <html>
                    <head>
                        <script type='text/javascript'>
                            alert('Failed to create invite: {ex.Message.Replace("'", "\\'")}');
                            window.location.href = '/GuildData/{SelectedGuildId}';
                        </script>
                    </head>
                    <body>
                        <p>Error: {ex.Message}</p>
                        <a href='/GuildData/{SelectedGuildId}'>Go back</a>
                    </body>
                    </html>";

                return Content(errorHtml, "text/html");
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
            {
                return report;
            }

            report.TotalUsers = guildLogs.Count;
            report.TotalCommands = guildLogs.Sum(u => u.Guilds[guildId.ToString()].TotalCommands);

            var userGroups = guildLogs
                .GroupBy(u => u.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    UserName = g.First().UserName ?? "Unknown",
                    TotalCommands = g.Sum(u => u.Guilds[guildId.ToString()].TotalCommands),
                    Commands = g
                        .SelectMany(u => u.Guilds[guildId.ToString()].Channels.Values.SelectMany(c => c.Commands))
                        .ToList()
                })
                .OrderByDescending(u => u.TotalCommands)
                .Take(10)
                .ToList();

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

            var channelStats = new Dictionary<string, int>();

            foreach (var user in guildLogs)
            {
                var guildData = user.Guilds[guildId.ToString()];

                foreach (var channel in guildData.Channels)
                {
                    string channelName = channel.Key;

                    if (channelStats.ContainsKey(channelName))
                    {
                        channelStats[channelName] += channel.Value.CommandCount;
                    }
                    else
                    {
                        channelStats[channelName] = channel.Value.CommandCount;
                    }
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