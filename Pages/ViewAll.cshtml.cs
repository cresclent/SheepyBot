using discord_bot.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NetCord.Gateway;

namespace discord_bot.Pages
{
    [IgnoreAntiforgeryToken]
    public class ViewAllModel : PageModel
    {
        private readonly VoteService _voteService;
        private readonly StartupAnnouncement _startupAnnouncement;
        private readonly GlobalAnnouncement _globalAnnouncement;
        private readonly ILogger<ViewAllModel> _logger;

        public ViewAllModel(
            VoteService voteService,
            StartupAnnouncement startupAnnouncement,
            GlobalAnnouncement globalAnnouncement,
            ILogger<ViewAllModel> logger)
        {
            _voteService = voteService;
            _startupAnnouncement = startupAnnouncement;
            _globalAnnouncement = globalAnnouncement;
            _logger = logger;
        }

        public string ViewType { get; set; } = string.Empty;
        public List<VoteItem> Votes { get; set; } = new();
        public List<ChannelItem> StartupChannels { get; set; } = new();
        public List<ChannelItem> GlobalChannels { get; set; } = new();

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
                        Votes = await GetVotesAsync();
                        StartupChannels = await GetStartupChannelsAsync();
                        GlobalChannels = await GetGlobalChannelsAsync();
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ViewAll page");
                ViewType = "Error";
            }

            return Page();
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
                else
                {
                    votes.Add(new VoteItem { VoteId = "1", UserId = "1157243448093573120", VoteValue = "Passed", Timestamp = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd HH:mm:ss") });
                    votes.Add(new VoteItem { VoteId = "2", UserId = "1157243448093573120", VoteValue = "Failed", Timestamp = DateTime.Now.AddDays(-2).ToString("yyyy-MM-dd HH:mm:ss") });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting votes");
            }

            return votes;
        }

        private async Task<List<ChannelItem>> GetStartupChannelsAsync()
        {
            var channels = new List<ChannelItem>();

            try
            {
                var gatewayClient = HttpContext.RequestServices.GetRequiredService<GatewayClient>();
                var guilds = gatewayClient.Cache.Guilds.Select(g => g.Value).ToList();
                
                foreach (var guild in guilds.Take(5))
                {
                    var info = _startupAnnouncement.GetConfigInfo(guild.Id);
                    if (!string.IsNullOrEmpty(info) && !info.Contains("No startup channel configured"))
                    {
                        channels.Add(new ChannelItem
                        {
                            ChannelId = guild.Id.ToString(),
                            ChannelName = guild.Name,
                            CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        });
                    }
                }

                if (channels.Count == 0)
                {
                    channels.Add(new ChannelItem { ChannelId = "123456789", ChannelName = "Example Startup Channel", CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting startup channels");
            }

            return channels;
        }

        private async Task<List<ChannelItem>> GetGlobalChannelsAsync()
        {
            var channels = new List<ChannelItem>();

            try
            {
                var gatewayClient = HttpContext.RequestServices.GetRequiredService<GatewayClient>();
                var guilds = gatewayClient.Cache.Guilds.Select(g => g.Value).ToList();
                
                foreach (var guild in guilds.Take(5))
                {
                    var info = _globalAnnouncement.GetGlobalConfigInfo(guild.Id);
                    if (!string.IsNullOrEmpty(info) && !info.Contains("No global announcement channel configured"))
                    {
                        channels.Add(new ChannelItem
                        {
                            ChannelId = guild.Id.ToString(),
                            ChannelName = guild.Name,
                            CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        });
                    }
                }

                if (channels.Count == 0)
                {
                    channels.Add(new ChannelItem { ChannelId = "987654321", ChannelName = "Example Global Channel", CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting global channels");
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
}