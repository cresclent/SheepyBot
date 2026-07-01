using discord_bot.userdataModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NetCord.Rest;

namespace discord_bot.Pages
{
    [IgnoreAntiforgeryToken]
    public class IgnoreModel : PageModel
    {
        private readonly RestClient _restClient;

        public IgnoreModel(RestClient restClient)
        {
            _restClient = restClient;
        }

        public List<IgnoredUser> IgnoredUsers { get; set; } = new();

        public async Task OnGet()
        {
            await LoadIgnoredUsers();
        }

        public async Task<IActionResult> OnPost()
        {
            var action = Request.Form["action"].ToString();

            switch (action)
            {
                case "Add":
                    var addUserId = Request.Form["userId"].ToString();
                    return await HandleAdd(addUserId);
                case "Remove":
                    var removeUserId = Request.Form["userId"].ToString();
                    return await HandleRemove(removeUserId);
                default:
                    return RedirectToPage();
            }
        }

        private async Task<IActionResult> HandleAdd(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId) || !ulong.TryParse(userId, out var userIdLong))
            {
                TempData["Message"] = "❌ Invalid User ID!";
                return RedirectToPage();
            }

            if (UserDataLogger.IsUserIgnored(userIdLong))
            {
                TempData["Message"] = $"⚠️ User {userId} is already ignored!";
                return RedirectToPage();
            }

            UserDataLogger.AddIgnoredUser(userIdLong);
            TempData["Message"] = $"✅ User {userId} added to ignore list!";
            return RedirectToPage();
        }

        private async Task<IActionResult> HandleRemove(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId) || !ulong.TryParse(userId, out var userIdLong))
            {
                TempData["Message"] = "❌ Invalid User ID!";
                return RedirectToPage();
            }

            if (!UserDataLogger.IsUserIgnored(userIdLong))
            {
                TempData["Message"] = $"⚠️ User {userId} is not ignored!";
                return RedirectToPage();
            }

            UserDataLogger.RemoveIgnoredUser(userIdLong);
            TempData["Message"] = $"✅ User {userId} removed from ignore list!";
            return RedirectToPage();
        }

        private async Task LoadIgnoredUsers()
        {
            var ignoredIds = UserDataLogger.GetIgnoredUsers();
            IgnoredUsers = new List<IgnoredUser>();

            foreach (var id in ignoredIds)
            {
                try
                {
                    var user = await _restClient.GetUserAsync(id);
                    IgnoredUsers.Add(new IgnoredUser
                    {
                        UserId = id,
                        Username = user?.Username ?? $"Unknown User"
                    });
                }
                catch
                {
                    IgnoredUsers.Add(new IgnoredUser
                    {
                        UserId = id,
                        Username = $"Unknown User"
                    });
                }
            }
        }
    }

    public class IgnoredUser
    {
        public ulong UserId { get; set; }
        public string Username { get; set; } = string.Empty;
    }
}