using discord_bot.Services;
using discord_bot.SmallDat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using NetCord.Rest;
using NetCord.Gateway;

namespace discord_bot.Pages
{
    [IgnoreAntiforgeryToken]
    public class SuggestionsModel : PageModel
    {
        private readonly ILogger<SuggestionsModel> _logger;
        private readonly RestClient _restClient;

        public SuggestionsModel(ILogger<SuggestionsModel> logger, RestClient restClient)
        {
            _logger = logger;
            _restClient = restClient;
        }

        public string Filter { get; set; } = string.Empty;
        public List<SuggestionItem> Suggestions { get; set; } = new();
        public int TotalCount { get; set; }
        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int DeniedCount { get; set; }

        public void OnGet(string filter)
        {
            Filter = filter ?? string.Empty;
            LoadSuggestions();
        }

        public async Task<IActionResult> OnPost()
        {
            var action = Request.Form["action"].ToString();

            switch (action)
            {
                case "Approve":
                    var approveId = int.Parse(Request.Form["id"]);
                    return await HandleApprove(approveId);
                case "Deny":
                    var denyId = int.Parse(Request.Form["id"]);
                    var reason = Request.Form["reason"].ToString();
                    return await HandleDeny(denyId, reason);
                default:
                    return RedirectToPage();
            }
        }

        private async Task<IActionResult> HandleApprove(int id)
        {
            try
            {
                var suggestion = LoadSuggestion(id);
                if (suggestion == null)
                {
                    TempData["Message"] = "❌ Suggestion not found!";
                    return RedirectToPage(new { filter = Filter });
                }

                suggestion.Status = "approved";
                UpdateSuggestion(suggestion);
                await UpdateDiscordMessage(suggestion, "approved");

                TempData["Message"] = $"✅ Suggestion #{id} approved!";
                _logger.LogInformation($"Suggestion {id} approved via web panel");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error approving suggestion {id}");
                TempData["Message"] = $"❌ Error: {ex.Message}";
            }

            return RedirectToPage(new { filter = Filter });
        }

        private async Task<IActionResult> HandleDeny(int id, string reason)
        {
            try
            {
                var suggestion = LoadSuggestion(id);
                if (suggestion == null)
                {
                    TempData["Message"] = "❌ Suggestion not found!";
                    return RedirectToPage(new { filter = Filter });
                }

                suggestion.Status = "denied";
                suggestion.Reason = reason ?? string.Empty;
                UpdateSuggestion(suggestion);
                await UpdateDiscordMessage(suggestion, "denied", reason);

                TempData["Message"] = $"❌ Suggestion #{id} denied!";
                _logger.LogInformation($"Suggestion {id} denied via web panel");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error denying suggestion {id}");
                TempData["Message"] = $"❌ Error: {ex.Message}";
            }

            return RedirectToPage(new { filter = Filter });
        }

        private void LoadSuggestions()
        {
            var allSuggestions = LoadAllSuggestions();
            
            if (!string.IsNullOrEmpty(Filter))
            {
                allSuggestions = allSuggestions.Where(s => s.Status == Filter).ToList();
            }

            Suggestions = allSuggestions.OrderByDescending(s => s.Id).ToList();
            
            var all = LoadAllSuggestions();
            TotalCount = all.Count;
            PendingCount = all.Count(s => s.Status == "pending");
            ApprovedCount = all.Count(s => s.Status == "approved");
            DeniedCount = all.Count(s => s.Status == "denied");
        }

        private List<SuggestionItem> LoadAllSuggestions()
        {
            var suggestions = new List<SuggestionItem>();
            string dir = Path.Combine(AppContext.BaseDirectory, "suggestions");
            
            if (!Directory.Exists(dir))
                return suggestions;

            var files = Directory.GetFiles(dir, "suggestion-*.json");

            foreach (var file in files)
            {
                try
                {
                    string json = System.IO.File.ReadAllText(file);
                    var data = JsonSerializer.Deserialize<JsonElement>(json);
                    if (data.ValueKind != JsonValueKind.Null)
                    {
                        suggestions.Add(new SuggestionItem
                        {
                            Id = data.GetProperty("Id").GetInt32(),
                            UserId = data.GetProperty("UserId").GetUInt64(),
                            Username = data.TryGetProperty("Username", out var u) ? u.GetString() ?? "Unknown" : "Unknown",
                            GuildId = data.TryGetProperty("GuildId", out var g) ? g.GetUInt64() : 0,
                            GuildName = data.TryGetProperty("GuildName", out var gn) ? gn.GetString() ?? "Unknown" : "Unknown",
                            Suggestion = data.TryGetProperty("Suggestion", out var s) ? s.GetString() ?? "" : "",
                            Timestamp = data.TryGetProperty("Timestamp", out var t) ? t.GetString() ?? "" : "",
                            Status = data.TryGetProperty("Status", out var st) ? st.GetString() ?? "pending" : "pending",
                            MessageUrl = data.TryGetProperty("MessageUrl", out var mu) ? mu.GetString() ?? "" : "",
                            MessageId = data.TryGetProperty("MessageId", out var mi) ? mi.GetUInt64() : 0,
                            ChannelId = data.TryGetProperty("ChannelId", out var ci) ? ci.GetUInt64() : 0,
                            Reason = data.TryGetProperty("Reason", out var r) ? r.GetString() ?? "" : ""
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error loading suggestion file: {file}");
                }
            }

            return suggestions;
        }

        private SuggestionItem? LoadSuggestion(int id)
        {
            string dir = Path.Combine(AppContext.BaseDirectory, "suggestions");
            string filePath = Path.Combine(dir, $"suggestion-{id}.json");
            
            if (!System.IO.File.Exists(filePath))
                return null;

            try
            {
                string json = System.IO.File.ReadAllText(filePath);
                var data = JsonSerializer.Deserialize<JsonElement>(json);
                
                return new SuggestionItem
                {
                    Id = data.GetProperty("Id").GetInt32(),
                    UserId = data.GetProperty("UserId").GetUInt64(),
                    Username = data.TryGetProperty("Username", out var u) ? u.GetString() ?? "Unknown" : "Unknown",
                    GuildId = data.TryGetProperty("GuildId", out var g) ? g.GetUInt64() : 0,
                    GuildName = data.TryGetProperty("GuildName", out var gn) ? gn.GetString() ?? "Unknown" : "Unknown",
                    Suggestion = data.TryGetProperty("Suggestion", out var s) ? s.GetString() ?? "" : "",
                    Timestamp = data.TryGetProperty("Timestamp", out var t) ? t.GetString() ?? "" : "",
                    Status = data.TryGetProperty("Status", out var st) ? st.GetString() ?? "pending" : "pending",
                    MessageUrl = data.TryGetProperty("MessageUrl", out var mu) ? mu.GetString() ?? "" : "",
                    MessageId = data.TryGetProperty("MessageId", out var mi) ? mi.GetUInt64() : 0,
                    ChannelId = data.TryGetProperty("ChannelId", out var ci) ? ci.GetUInt64() : 0,
                    Reason = data.TryGetProperty("Reason", out var r) ? r.GetString() ?? "" : ""
                };
            }
            catch
            {
                return null;
            }
        }

        private void UpdateSuggestion(SuggestionItem suggestion)
        {
            string dir = Path.Combine(AppContext.BaseDirectory, "suggestions");
            string filePath = Path.Combine(dir, $"suggestion-{suggestion.Id}.json");
            
            var data = new
            {
                Id = suggestion.Id,
                MessageId = suggestion.MessageId,
                UserId = suggestion.UserId,
                Username = suggestion.Username,
                GuildId = suggestion.GuildId,
                GuildName = suggestion.GuildName,
                ChannelId = suggestion.ChannelId,
                Suggestion = suggestion.Suggestion,
                Timestamp = suggestion.Timestamp,
                Status = suggestion.Status,
                Reason = suggestion.Reason ?? "",
                MessageUrl = suggestion.MessageUrl
            };

            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(filePath, json);
        }

        private async Task UpdateDiscordMessage(SuggestionItem suggestion, string status, string? reason = null)
        {
            try
            {
                if (suggestion.MessageId == 0 || suggestion.ChannelId == 0)
                {
                    _logger.LogWarning($"No message ID or channel ID for suggestion {suggestion.Id}");
                    return;
                }

                var message = await _restClient.GetMessageAsync(suggestion.ChannelId, suggestion.MessageId);
                if (message == null)
                {
                    _logger.LogWarning($"Message {suggestion.MessageId} in channel {suggestion.ChannelId} not found");
                    return;
                }

                var statusEmoji = status == "approved" ? "✅" : "❌";
                var statusText = status == "approved" ? "Approved" : $"Denied{(string.IsNullOrEmpty(reason) ? "" : $": {reason}")}";
                
                var updatedContent = message.Content.Replace("⏳ Pending Review", $"{statusEmoji} {statusText}");
                await message.ModifyAsync(options => options.Content = updatedContent);

                _logger.LogInformation($"Updated Discord message for suggestion {suggestion.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating Discord message for suggestion {suggestion.Id}: {ex.Message}");
            }
        }
    }

    public class SuggestionItem
    {
        public int Id { get; set; }
        public ulong UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public ulong GuildId { get; set; }
        public string GuildName { get; set; } = string.Empty;
        public string Suggestion { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public string Status { get; set; } = "pending";
        public string Reason { get; set; } = string.Empty;
        public string MessageUrl { get; set; } = string.Empty;
        public ulong MessageId { get; set; }
        public ulong ChannelId { get; set; }
    }
}