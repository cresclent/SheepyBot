// Copyright (c) 2026 Cresclent. All rights reserved.
// This Discord bot code is view-only. Hosting or running this bot is strictly prohibited!

using discord_bot.Services;
using discord_bot.SmallDat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NetCord.Gateway;

namespace discord_bot.Pages
{
    [IgnoreAntiforgeryToken]
    public class IndexModel : PageModel
    {
        private readonly VoteService _voteService;
        private readonly GlobalAnnouncement _globalAnnouncement;
        private readonly StartupAnnouncement _startupAnnouncement;
        private readonly GatewayClient _gatewayClient;
        private readonly ServerTracker _serverTracker;

        public IndexModel(
            VoteService voteService,
            GlobalAnnouncement globalAnnouncement,
            StartupAnnouncement startupAnnouncement,
            GatewayClient gatewayClient,
            ServerTracker serverTracker)
        {
            _voteService = voteService;
            _globalAnnouncement = globalAnnouncement;
            _startupAnnouncement = startupAnnouncement;
            _gatewayClient = gatewayClient;
            _serverTracker = serverTracker;
        }

        public string Message { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string CurrentBanner => _voteService?.GetCurrentBanner() ?? "Unknown";
        public string BotOwnerId => "1157243448093573120";

        public int serverCount { get; set; }

        public void OnGet()
        {
            getServerCount();
        }

        private void getServerCount()
        {
            serverCount = _serverTracker.getServerCount();
        }

        public async Task<IActionResult> OnPost()
        {
            var action = Request.Form["action"].ToString();

            switch (action)
            {
                case "ResetBanner":
                    return HandleResetBanner();
                case "GlobalAnnounce":
                    return await HandleGlobalAnnounce();
                case "CleanupServers":
                    return await HandleCleanupServers();
                default:
                    return Page();
            }
        }

        private IActionResult HandleResetBanner()
        {
            try
            {
                _voteService.RerollBanner();
                var newBanner = _voteService.GetCurrentBanner();
                Message = $"🔄 Banner reset to {newBanner}!";
                IsSuccess = true;
            }
            catch (Exception ex)
            {
                Message = $"❌ Error: {ex.Message}";
                IsSuccess = false;
            }
            return Page();
        }

        private async Task<IActionResult> HandleGlobalAnnounce()
        {
            var announcement = Request.Form["announcement"].ToString();
            var ping = Request.Form["ping"] == "true";

            if (string.IsNullOrWhiteSpace(announcement))
            {
                Message = "❌ Please provide a message!";
                IsSuccess = false;
                return Page();
            }

            try
            {
                await _globalAnnouncement.SendGlobalAnnouncementAsync(announcement, ping);
                Message = $"📢 Global announcement sent! (Ping: {(ping ? "Enabled" : "Disabled")})";
                IsSuccess = true;
            }
            catch (Exception ex)
            {
                Message = $"❌ Error: {ex.Message}";
                IsSuccess = false;
            }
            return Page();
        }

        private async Task<IActionResult> HandleCleanupServers()
        {
            try
            {
                var serverTracker = new ServerTracker();
                var currentGuilds = _gatewayClient.Cache.Guilds.Select(g => g.Value.Id).ToList();

                foreach (var guildId in currentGuilds)
                {
                    serverTracker.AddServer(guildId);
                }
                await serverTracker.CheckAndCleanupRemovedServers(currentGuilds);

                var tracked = serverTracker.GetAllServers();
                Message = $"🧹 Cleanup completed! Tracking {tracked.Count} servers";
                IsSuccess = true;
            }
            catch (Exception ex)
            {
                Message = $"❌ Error: {ex.Message}";
                IsSuccess = false;
            }
            return Page();
        }
    }
}