// Copyright (c) 2026 Cresclent. All rights reserved.
// This Discord bot code is view-only. Hosting or running this bot is strictly prohibited!
using discord_bot.Helpers;
using discord_bot.Tools;
using discord_bot.SmallDat;
using discord_bot.userdataModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;
using System.Text;
using System.Text.Json;
using discord_bot.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

var token = builder.Configuration["Discord:Token"]
            ?? Environment.GetEnvironmentVariable("DISCORD_TOKEN");

if (string.IsNullOrEmpty(token))
{
    new Write().WriteLine("ERROR: Discord bot token not found!");
    return;
}

builder.Services.Configure<GatewayClientOptions>(options =>
{
    options.Token = token;
});

builder.Services
    .AddDiscordGateway()
    .AddApplicationCommands();

var host = builder.Build();

string[] fiveStarCharacters = new[]
{
    "Sandrone", "Albedo", "Alhaitham", "Arataki Itto", "Arlecchino",
    "Baizhu", "Chasca", "Citlali", "Clorinde", "Cyno", "Emilie",
    "Escoffier", "Eula", "Flins", "Furina", "Ganyu", "Hu Tao",
    "Ineffa", "Kaedehara Kazuha", "Kamisato Ayaka", "Kamisato Ayato",
    "Kinich", "Klee", "Lauma", "Lyney", "Mavuika", "Mualani",
    "Nahida", "Navia", "Nefer", "Neuvillette", "Sigewinne",
    "Raiden Shogun", "Sangonomiya Kokomi", "Shenhe", "Skirk",
    "Tartaglia", "Varesa", "Venti", "Varka", "Wanderer",
    "Wriothesley", "Xiao", "Xianyun", "Xilonen", "Yae Miko",
    "Yelan", "Yoimiya", "Zhongli", "Chiori", "Durin",
    "Columbina", "Zibai", "Linnea", "Lohen", "Nicole"
};

string[] fourStarCharacters = new[]
{
    "Aino", "Amber", "Barbara", "Beidou", "Bennett", "Candace",
    "Charlotte", "Chevreuse", "Chongyun", "Collei", "Dahlia",
    "Diona", "Dori", "Faruzan", "Fischl", "Freminet", "Gaming",
    "Gorou", "Iansan", "Ifa", "Kachina", "Kaveh", "Kaeya",
    "Kirara", "Kujou Sara", "Kuki Shinobu", "Lan Yan", "Layla",
    "Lisa", "Lynette", "Mika", "Noelle", "Ningguang", "Ororon",
    "Razor", "Rosaria", "Sayu", "Sethos", "Shikanoin Heizou",
    "Sucrose", "Thoma", "Xiangling", "Xingqiu", "Xinyan",
    "Yanfei", "Yaoyao", "Yun Jin", "Jahoda", "Illuga", "Prune"
};

string[] threeStarItems = new[]
{
    "Debate Club", "Harbinger of Dawn", "Skyrider Sword",
    "Ferrous Shadow", "Cool Steel", "Bloodtainted Greatsword",
    "Emerald Orb", "Black Tassel", "Magic Guide"
};

string[] standardFiveStars = new[]
{
    "Tighnari", "Jean", "Mona", "Dehya", "Diluc", "Keqing", "Qiqi", "Yumemizuki Mizuki"
};

Dictionary<int, int> pityRates = new()
{
    {0, 167}, {1, 167}, {2, 167}, {3, 167}, {4, 167}, {5, 167},
    {6, 167}, {7, 167}, {8, 167}, {9, 167}, {10, 167}, {11, 167},
    {12, 167}, {13, 167}, {14, 167}, {15, 167}, {16, 167}, {17, 167},
    {18, 167}, {19, 167}, {20, 167}, {21, 167}, {22, 167}, {23, 167},
    {24, 167}, {25, 167}, {26, 167}, {27, 167}, {28, 167}, {29, 167},
    {30, 167}, {31, 167}, {32, 167}, {33, 167}, {34, 167}, {35, 167},
    {36, 167}, {37, 167}, {38, 167}, {39, 167}, {40, 167}, {41, 167},
    {42, 167}, {43, 167}, {44, 167}, {45, 167}, {46, 167}, {47, 167},
    {48, 167}, {49, 167}, {50, 167}, {51, 167}, {52, 167}, {53, 167},
    {54, 167}, {55, 167}, {56, 167}, {57, 167}, {58, 167}, {59, 167},
    {60, 167}, {61, 167}, {62, 167}, {63, 167}, {64, 167}, {65, 167},
    {66, 167}, {67, 167}, {68, 167}, {69, 167}, {70, 167}, {71, 167},
    {72, 167}, {73, 100}, {74, 50}, {75, 33}, {76, 25}, {77, 20},
    {78, 17}, {79, 14}, {80, 12}, {81, 11}, {82, 10}, {83, 9},
    {84, 8}, {85, 7}, {86, 6}, {87, 5}, {88, 4}, {89, 2}, {90, 1}
};

var dataManager = new UserDataManager();
Random random = new Random();
string banner = fiveStarCharacters[random.Next(fiveStarCharacters.Length)];

UserDataLogger.Init();
UserDataLogger logger = new UserDataLogger();

string gameStatus = "Genshin pulling for Cresclent";

var cooldowns = new Dictionary<ulong, DateTime>();

string GetStars(int count)
{
    return new string('⭐', count);
}

string GetSuggestionsDirectory()
{
    string dir = Path.Combine(AppContext.BaseDirectory, "suggestions");
    if (!Directory.Exists(dir))
        Directory.CreateDirectory(dir);
    return dir;
}

int GetNextSuggestionId()
{
    try
    {
        string dir = GetSuggestionsDirectory();
        var files = Directory.GetFiles(dir, "suggestion-*.json");
        if (files.Length == 0) return 1;

        var ids = new List<int>();
        foreach (var file in files)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            var idPart = fileName.Replace("suggestion-", "");
            if (int.TryParse(idPart, out int id))
                ids.Add(id);
        }
        return ids.Count > 0 ? ids.Max() + 1 : 1;
    }
    catch { return 1; }
}

void SaveSuggestion(object data)
{
    string dir = GetSuggestionsDirectory();
    var json = System.Text.Json.JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
    var id = (int)data.GetType().GetProperty("Id").GetValue(data);
    string filePath = Path.Combine(dir, $"suggestion-{id}.json");
    File.WriteAllText(filePath, json);
}

JsonElement? LoadSuggestion(int id)
{
    string dir = GetSuggestionsDirectory();
    string filePath = Path.Combine(dir, $"suggestion-{id}.json");
    if (!File.Exists(filePath)) return null;

    string json = File.ReadAllText(filePath);
    return JsonSerializer.Deserialize<JsonElement>(json);
}

List<JsonElement> LoadAllSuggestions()
{
    var suggestions = new List<JsonElement>();
    string dir = GetSuggestionsDirectory();
    var files = Directory.GetFiles(dir, "suggestion-*.json");

    foreach (var file in files)
    {
        try
        {
            string json = File.ReadAllText(file);
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            if (data.ValueKind != JsonValueKind.Null)
                suggestions.Add(data);
        }
        catch { }
    }
    return suggestions;
}

void UpdateSuggestion(int id, object data)
{
    string dir = GetSuggestionsDirectory();
    string filePath = Path.Combine(dir, $"suggestion-{id}.json");
    string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(filePath, json);
}

host.AddSlashCommand("pity", "Check your current pity count and stats", (ApplicationCommandContext context) =>
{
    var userId = context.User.Id;
    var data = dataManager.GetOrCreateUserData(userId);

    logger.Logger(context, "pity");

    return $"**Your Stats:**\n" +
           $"Pity: **{data.Pity}/90**\n" +
           $"4-Star Pity: **{data.FourStarPity}/10**\n" +
           $"5-Star Guaranteed: {(data.IsGuaranteed ? "Yes ✅" : "No ❌")}\n" +
           $"Total Pulls: **{data.TotalPulls}**\n" +
           $"{GetStars(5)} 5-Stars: **{data.FiveStarCount}**\n" +
           $"{GetStars(4)} 4-Stars: **{data.FourStarCount}**\n" +
           $"{GetStars(3)} 3-Stars: **{data.ThreeStarCount}**";
});

host.AddSlashCommand("inventory", "Check your inventory", (ApplicationCommandContext context) =>
{
    var userId = context.User.Id;
    var data = dataManager.GetOrCreateUserData(userId);

    logger.Logger(context, "inventory");

    if (data.Inventory.Count == 0)
    {
        return "📦 Your inventory is empty! Use `/pull` to get items!";
    }

    var fiveStarItems = data.Inventory
        .Where(x => IsFiveStar(x.Key))
        .OrderBy(x => x.Key)
        .Select(x => $"{GetStars(5)} {x.Key} x{x.Value}")
        .ToList();

    var fourStarItems = data.Inventory
        .Where(x => IsFourStar(x.Key))
        .OrderBy(x => x.Key)
        .Select(x => $"{GetStars(4)} {x.Key} x{x.Value}")
        .ToList();

    var threeStarItemsList = data.Inventory
        .Where(x => !IsFiveStar(x.Key) && !IsFourStar(x.Key))
        .OrderBy(x => x.Key)
        .Select(x => $"{GetStars(3)} {x.Key} x{x.Value}")
        .ToList();

    string response = "📦 **Inventory**\n";
    response += $"Total Items: **{data.Inventory.Values.Sum()}**\n\n";

    if (fiveStarItems.Count > 0)
    {
        response += $"\n{GetStars(5)} **5-Star Items** ({fiveStarItems.Count})\n";
        response += string.Join("\n", fiveStarItems);
        response += "\n";
    }

    if (fourStarItems.Count > 0)
    {
        response += $"\n{GetStars(4)} **4-Star Items** ({fourStarItems.Count})\n";
        response += string.Join("\n", fourStarItems);
        response += "\n";
    }

    if (threeStarItemsList.Count > 0)
    {
        response += $"\n{GetStars(3)} **3-Star Items** ({threeStarItemsList.Count})\n";
        response += string.Join("\n", threeStarItemsList);
    }

    if (response.Length > 2000)
    {
        response = "📦 **Inventory** (Truncated)\n";
        response += $"Total Items: **{data.Inventory.Values.Sum()}**\n\n";
        response += $"{GetStars(5)} 5-Stars: {fiveStarItems.Count}\n";
        response += $"{GetStars(4)} 4-Stars: {fourStarItems.Count}\n";
        response += $"{GetStars(3)} 3-Stars: {threeStarItemsList.Count}";
    }

    return response;
});

host.AddSlashCommand("banner", "Shows the current banner", (ApplicationCommandContext context) =>
{
    logger.Logger(context, "banner");

    return $"📌 **Current Banner:** {banner}\n\n" +
           $"This banner features the 5-star character **{banner}**!\n" +
           $"Use `/pull` to wish on this banner!";
});

host.AddSlashCommand("help", "Shows the Help Menu", async (ApplicationCommandContext context) =>
{
    logger.Logger(context, "help");

    var helpMessage = $"# 🐑 **Sheepy Bot Help Menu**\n\n" +
           $"## 📋 **General Commands**\n" +
           $"`/help` - Shows this help menu\n" +
           $"`/pull` - Perform 10 wishes on the current banner (2.5 min cooldown)\n" +
           $"`/banner` - Shows the current banner\n" +
           $"`/inventory` - Shows what you have pulled using `/pull`\n" +
           $"`/pity` - Shows your current pity count and whether you're guaranteed\n" +
           $"`/coinflip` - Simple coinflip command\n" +
           $"`/bannerreset` - Reset the banner (Cresclent only)\n\n" +
           $"## 📢 **Announcement Commands**\n" +
           $"`/setstartupchannel [channel] [role]` - Sets the startup channel for this server (Admin only)\n" +
           $"  • If no channel is provided, uses the current channel\n" +
           $"`/disablestartup` - Disable startup announcements for this server (Admin only)\n" +
           $"`/startupstatus` - Displays startup announcement status for this server (Admin only)\n" +
           $"`/allstartupchannels` - View all configured startup channels (Bot owner only)\n" +
           $"`/setglobalchannel [channel] [role]` - Sets the global announcement channel for this server (Admin only)\n" +
           $"  • If no channel is provided, uses the current channel\n" +
           $"`/disableglobal` - Disable global announcements for this server (Admin only)\n" +
           $"`/globalstatus` - Displays global announcement status for this server (Admin only)\n" +
           $"`/allglobalchannels` - View all configured global announcement channels (Bot owner only)\n" +
           $"`/globalannounce <message>` - Send a global announcement to all configured channels (Bot owner only)\n\n" +
           $"## 💡 **Bot Suggestion Commands**\n" +
           $"`/suggest <suggestion>` - Suggest a new feature for the bot\n" +
           $"`/viewsuggestion <id>` - View a specific suggestion by ID\n" +
           $"`/listsuggestions` - List all bot suggestions\n" +
           $"`/approvesuggestion <id>` - Approve a suggestion (Bot owner only)\n" +
           $"`/denysuggestion <id> [reason]` - Deny a suggestion (Bot owner only)\n\n" +
           $"## 🏆 **Leaderboards**\n" +
           $"`/guildleaderboard` - Shows top command users in **this guild** (This Month)\n" +
           $"  • See who's most active in your server\n" +
           $"  • Monthly stats for your guild\n\n" +
           $"`/totalleaderboard` - Shows top command users across **ALL guilds** (This Month)\n" +
           $"  • Global rankings across all servers\n" +
           $"  • See top users and top guilds\n\n" +
           $"## 📊 **Admin Data Commands**\n" +
           $"`/guilddata` - Shows command logs for **this guild only** (Admin only)\n" +
           $"  • View total users and commands\n" +
           $"  • See top users by command usage\n" +
           $"  • Channel breakdown\n" +
           $"  • Shows data from June 20, 2026 to today\n\n" +
           $"`/alldata` - Shows **ALL command data** across ALL guilds\n" +
           $"  • **⚠️ Restricted to Cresclent's server in the designated channel**\n" +
           $"  • Complete overview (users, commands, guilds)\n" +
           $"  • Top 15 users overall\n" +
           $"  • Top 10 guilds\n" +
           $"  • Shows data from June 20, 2026 to today\n" +
           $"  • Command type distribution\n" +
           $"  • Missing/empty file report\n" +
           $"  • Much more overseeable than checking individual log files!\n\n" +
           $"## 🛡️ **Ignore List Commands**\n" +
           $"`/ignoreuser <user>` - Add a user to the ignore list (Admin only, specific channel)\n" +
           $"`/unignoreuser <user>` - Remove a user from the ignore list (Admin only, specific channel)\n" +
           $"`/listignored` - List all ignored users (Admin only, specific channel)\n\n" +
           $"## 🛠️ **Server Management Commands**\n" +
           $"`/cleanupremovedservers` - Check and cleanup data for removed servers (Bot owner only)\n\n" +
           $"## ℹ️ **Info**\n" +
           $"`/github` - Link to the open source code\n" +
           $"`/privacy` - The privacy policy\n" +
           $"`/terms` - The terms of service\n\n" +
           $"Ask Cresclent for more info (can be DMs, may take a while to answer)\n" +
           $"If you want your data removed from this bot, send me a DM immediately. I will add you to an ignore list.";

    var parts = MessageSplitterHelper.SplitMessage(helpMessage);

    if (parts.Count == 1)
    {
        return parts[0];
    }

    var firstMessage = parts[0];

    for (int i = 1; i < parts.Count; i++)
    {
        await context.Channel.SendMessageAsync(parts[i]);
    }

    return firstMessage;
});

host.AddSlashCommand("coinflip", "Simple Coinflip Command", (ApplicationCommandContext context) =>
{
    logger.Logger(context, "coinflip");

    string coinout = random.Next(0, 2) == 0 ? "Heads" : "Tails";
    return $"# Your coin is: {coinout}";
});

host.AddSlashCommand("bannerreset", "banner resetting, can ONLY be done by cresclent", (ApplicationCommandContext context) =>
{
    var userId = context.User.Id;

    logger.Logger(context, "bannerreset");

    if (userId == 1157243448093573120)
    {
        banner = fiveStarCharacters[random.Next(fiveStarCharacters.Length)];
        return $"# Banner is being rerolled to:\n{banner}";
    }
    else
    {
        return $"You there, you dont have access to this command! only cresclent does!";
    }
});

host.AddSlashCommand("pull", "Perform 10 wishes (2.5 minute cooldown)", async (ApplicationCommandContext context) =>
{
    var userId = context.User.Id;

    logger.Logger(context, "pull");

    if (cooldowns.TryGetValue(userId, out var cooldownEnd))
    {
        if (DateTime.UtcNow < cooldownEnd)
        {
            var timeLeft = cooldownEnd - DateTime.UtcNow;
            return $"⏳ Please wait **{timeLeft.Minutes}m {timeLeft.Seconds}s** before pulling again!";
        }
    }

    cooldowns[userId] = DateTime.UtcNow.AddMinutes(2.5);

    var data = dataManager.GetOrCreateUserData(userId);

    int amount = 10;
    var results = new List<string>();
    int threeStarCount = 0;
    int fourStarCount = 0;
    int fiveStarCount = 0;
    bool gotBannerCharacter = false;
    string pulledFiveStar = "";

    for (int i = 0; i < amount; i++)
    {
        if (random.Next(pityRates[data.Pity]) == 0 || data.Pity >= 90)
        {
            string result;
            if (!data.IsGuaranteed && random.Next(2) == 0)
            {
                result = standardFiveStars[random.Next(standardFiveStars.Length)];
                data.IsGuaranteed = true;
                pulledFiveStar = result;
                gotBannerCharacter = false;
            }
            else
            {
                result = banner;
                data.IsGuaranteed = false;
                pulledFiveStar = result;
                gotBannerCharacter = true;
            }

            AddToInventory(data, result);
            results.Add($"{GetStars(5)} {result}");
            fiveStarCount++;
            data.Pity = 0;
            data.FourStarPity++;
            data.FiveStarCount++;
            data.TotalPulls++;
        }
        else if (data.FourStarPity >= 9 || random.Next(5000) < 250)
        {
            string result = fourStarCharacters[random.Next(fourStarCharacters.Length)];
            AddToInventory(data, result);
            results.Add($"{GetStars(4)} {result}");
            fourStarCount++;
            data.Pity++;
            data.FourStarPity = 0;
            data.FourStarCount++;
            data.TotalPulls++;
        }
        else
        {
            string result = threeStarItems[random.Next(threeStarItems.Length)];
            AddToInventory(data, result);
            results.Add($"{GetStars(3)} {result}");
            threeStarCount++;
            data.Pity++;
            data.FourStarPity++;
            data.ThreeStarCount++;
            data.TotalPulls++;
        }

        if (data.Pity > 90)
            data.Pity = 90;
    }

    dataManager.SaveUserData(userId, data);

    string response = $"🎲 **10 Pull Results**\n";
    response += $"📌 Banner: **{banner}**\n\n";
    response += string.Join("\n", results);
    response += $"\n\n**Summary:**\n";
    response += $"{GetStars(3)} {threeStarCount} 3-Star\n";
    response += $"{GetStars(4)} {fourStarCount} 4-Star\n";
    response += $"{GetStars(5)} {fiveStarCount} 5-Star\n";

    if (fiveStarCount > 0)
    {
        response += $"\n🌟 **5-Star Pulled:** {pulledFiveStar}";
        if (gotBannerCharacter)
            response += " ✅ (Banner Character!)";
        else
            response += " ❌ (Lost 50/50)";
    }
    DateTimeOffset targetTime = DateTimeOffset.UtcNow.AddMinutes(2.5);
    response += $"\n**Pity:** {data.Pity}/90";
    response += data.IsGuaranteed ? " (Guaranteed next 5-star!)" : "";
    response += $"\n\n⏳ Next pull available <t:{targetTime.ToUnixTimeSeconds()}:R>!";

    if (response.Length > 2000)
    {
        response = $"🎲 **10 Pull Results** (Banner: **{banner}**)\n" +
                  $"{GetStars(3)} 3-Stars: {threeStarCount} | {GetStars(4)} 4-Stars: {fourStarCount} | {GetStars(5)} 5-Stars: {fiveStarCount}\n" +
                  $"Pity: {data.Pity}/90" +
                  (data.IsGuaranteed ? " (Guaranteed)" : "") +
                  $"\n\n⏳ Next pull available <t:{targetTime.ToUnixTimeSeconds()}:R>!";
    }

    return response;
});

host.AddSlashCommand("suggest", "Suggest a new feature for the bot", async (ApplicationCommandContext context,
    [SlashCommandParameter(Name = "suggestion", Description = "Your suggestion for the bot")] string suggestion) =>
{
    var userId = context.User.Id;
    var guildId = context.Guild?.Id ?? 0;

    logger.Logger(context, "suggest");

    if (string.IsNullOrWhiteSpace(suggestion))
    {
        return "❌ Please provide a suggestion! Usage: `/suggest <your suggestion>`";
    }

    var suggestionId = GetNextSuggestionId();
    var timestamp = DateTimeOffset.UtcNow;

    var messageText = $"💡 **New Bot Suggestion #{suggestionId}**\n\n" +
                      $"**Suggestion:** {suggestion}\n\n" +
                      $"**Status:** ⏳ Pending Review\n" +
                      $"**Suggested By:** {context.User.Username} (<@{userId}>)\n" +
                      $"**From:** {(context.Guild != null ? context.Guild.Name : "Direct Message")}\n" +
                      $"**ID:** `#{suggestionId}`";

    try
    {
        var restClient = host.Services.GetRequiredService<RestClient>();
        var channel = await restClient.GetChannelAsync(1517986948503834836) as TextGuildChannel;

        if (channel == null)
        {
            return "❌ Could not find the suggestions channel! Please contact the bot owner.";
        }

        var message = await channel.SendMessageAsync(messageText);

        await message.AddReactionAsync("👍");
        await message.AddReactionAsync("👎");
        await message.AddReactionAsync("🤔");

        var suggestionData = new
        {
            Id = suggestionId,
            MessageId = message.Id,
            UserId = userId,
            Username = context.User.Username,
            GuildId = guildId,
            GuildName = context.Guild?.Name ?? "DM",
            ChannelId = 1517986948503834836,
            Suggestion = suggestion,
            Timestamp = timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
            Status = "pending",
            MessageUrl = $"https://discord.com/channels/{guildId}/{1517986948503834836}/{message.Id}"
        };

        SaveSuggestion(suggestionData);

        return $"✅ Your bot suggestion #{suggestionId} has been submitted successfully!\n" +
               $"It will be reviewed by the bot owner in the suggestions channel.";
    }
    catch (Exception ex)
    {
        new Write().WriteLine($"Failed to send suggestion: {ex.Message}");
        return $"❌ Failed to submit suggestion: {ex.Message}";
    }
});

host.AddSlashCommand("approvesuggestion", "Approve a bot suggestion (Bot owner only)", async (ApplicationCommandContext context,
    [SlashCommandParameter(Name = "id", Description = "Suggestion ID")] int id) =>
{
    var userId = context.User.Id;

    logger.Logger(context, "approvesuggestion");

    if (userId != 1157243448093573120)
    {
        return "❌ Only the bot owner can approve suggestions!";
    }

    try
    {
        var suggestionData = LoadSuggestion(id);
        if (suggestionData == null)
        {
            return $"❌ Could not find suggestion #{id}!";
        }

        ulong messageId = suggestionData.Value.GetProperty("MessageId").GetUInt64();
        ulong channelId = suggestionData.Value.GetProperty("ChannelId").GetUInt64();
        ulong guildId = suggestionData.Value.GetProperty("GuildId").GetUInt64();

        RestMessage? message = null;

        if (guildId != 0 && context.Guild != null)
        {
            var channel = context.Guild.Channels.FirstOrDefault(c => c.Value.Id == channelId).Value as TextGuildChannel;
            if (channel != null)
            {
                try { message = await channel.GetMessageAsync(messageId); } catch { }
            }
        }

        if (message == null)
        {
            var restClient = host.Services.GetRequiredService<RestClient>();
            try
            {
                var dmChannel = await restClient.GetDMChannelAsync(channelId);
                if (dmChannel != null)
                {
                    message = await dmChannel.GetMessageAsync(messageId);
                }
            }
            catch { }
        }

        if (message != null)
        {
            var updatedContent = message.Content.Replace("⏳ Pending Review", "✅ Approved");
            await message.ModifyAsync(options => options.Content = updatedContent);
        }

        var updatedData = new
        {
            Id = suggestionData.Value.GetProperty("Id").GetInt32(),
            MessageId = suggestionData.Value.GetProperty("MessageId").GetUInt64(),
            UserId = suggestionData.Value.GetProperty("UserId").GetUInt64(),
            Username = suggestionData.Value.GetProperty("Username").GetString(),
            GuildId = suggestionData.Value.GetProperty("GuildId").GetUInt64(),
            GuildName = suggestionData.Value.GetProperty("GuildName").GetString(),
            ChannelId = suggestionData.Value.GetProperty("ChannelId").GetUInt64(),
            Suggestion = suggestionData.Value.GetProperty("Suggestion").GetString(),
            Timestamp = suggestionData.Value.GetProperty("Timestamp").GetString(),
            Status = "approved",
            MessageUrl = suggestionData.Value.GetProperty("MessageUrl").GetString()
        };

        UpdateSuggestion(id, updatedData);

        return $"✅ Bot Suggestion #{id} has been approved!";
    }
    catch (Exception ex)
    {
        return $"❌ Error: {ex.Message}";
    }
});

host.AddSlashCommand("denysuggestion", "Deny a bot suggestion (Bot owner only)", async (ApplicationCommandContext context,
    [SlashCommandParameter(Name = "id", Description = "Suggestion ID")] int id,
    [SlashCommandParameter(Name = "reason", Description = "Reason for denial (optional)")] string? reason = null) =>
{
    var userId = context.User.Id;

    logger.Logger(context, "denysuggestion");

    if (userId != 1157243448093573120)
    {
        return "❌ Only the bot owner can deny suggestions!";
    }

    try
    {
        var suggestionData = LoadSuggestion(id);
        if (suggestionData == null)
        {
            return $"❌ Could not find suggestion #{id}!";
        }

        ulong messageId = suggestionData.Value.GetProperty("MessageId").GetUInt64();
        ulong channelId = suggestionData.Value.GetProperty("ChannelId").GetUInt64();
        ulong guildId = suggestionData.Value.GetProperty("GuildId").GetUInt64();

        RestMessage? message = null;

        if (guildId != 0 && context.Guild != null)
        {
            var channel = context.Guild.Channels.FirstOrDefault(c => c.Value.Id == channelId).Value as TextGuildChannel;
            if (channel != null)
            {
                try { message = await channel.GetMessageAsync(messageId); } catch { }
            }
        }

        if (message == null)
        {
            var restClient = host.Services.GetRequiredService<RestClient>();
            try
            {
                var dmChannel = await restClient.GetDMChannelAsync(channelId);
                if (dmChannel != null)
                {
                    message = await dmChannel.GetMessageAsync(messageId);
                }
            }
            catch { }
        }

        if (message != null)
        {
            var statusText = string.IsNullOrEmpty(reason) ? "❌ Denied" : $"❌ Denied: {reason}";
            var updatedContent = message.Content.Replace("⏳ Pending Review", statusText);
            await message.ModifyAsync(options => options.Content = updatedContent);
        }

        var updatedData = new
        {
            Id = suggestionData.Value.GetProperty("Id").GetInt32(),
            MessageId = suggestionData.Value.GetProperty("MessageId").GetUInt64(),
            UserId = suggestionData.Value.GetProperty("UserId").GetUInt64(),
            Username = suggestionData.Value.GetProperty("Username").GetString(),
            GuildId = suggestionData.Value.GetProperty("GuildId").GetUInt64(),
            GuildName = suggestionData.Value.GetProperty("GuildName").GetString(),
            ChannelId = suggestionData.Value.GetProperty("ChannelId").GetUInt64(),
            Suggestion = suggestionData.Value.GetProperty("Suggestion").GetString(),
            Timestamp = suggestionData.Value.GetProperty("Timestamp").GetString(),
            Status = "denied",
            Reason = reason ?? "",
            MessageUrl = suggestionData.Value.GetProperty("MessageUrl").GetString()
        };

        UpdateSuggestion(id, updatedData);

        return $"❌ Bot Suggestion #{id} has been denied.";
    }
    catch (Exception ex)
    {
        return $"❌ Error: {ex.Message}";
    }
});

host.AddSlashCommand("viewsuggestion", "View a specific suggestion by ID", async (ApplicationCommandContext context,
    [SlashCommandParameter(Name = "id", Description = "Suggestion ID")] int id) =>
{
    logger.Logger(context, "viewsuggestion");

    try
    {
        var suggestionData = LoadSuggestion(id);
        if (suggestionData == null)
        {
            return $"❌ Could not find suggestion #{id}!";
        }

        var status = suggestionData.Value.GetProperty("Status").GetString();
        var statusEmoji = status == "pending" ? "⏳" : status == "approved" ? "✅" : "❌";

        var response = $"# 📝 Suggestion #{id} {statusEmoji}\n\n";
        response += $"**Status:** {status}\n";
        response += $"**User:** {suggestionData.Value.GetProperty("Username").GetString()} ({suggestionData.Value.GetProperty("UserId").GetUInt64()})\n";
        response += $"**Guild:** {suggestionData.Value.GetProperty("GuildName").GetString()} ({suggestionData.Value.GetProperty("GuildId").GetUInt64()})\n";
        response += $"**Date:** {suggestionData.Value.GetProperty("Timestamp").GetString()}\n\n";
        response += $"**Suggestion:**\n{suggestionData.Value.GetProperty("Suggestion").GetString()}\n\n";
        response += $"**Message:** {suggestionData.Value.GetProperty("MessageUrl").GetString()}";

        if (status == "denied")
        {
            string? reason = null;
            if (suggestionData.Value.TryGetProperty("Reason", out var reasonProp))
            {
                reason = reasonProp.GetString();
            }
            if (!string.IsNullOrEmpty(reason))
                response += $"\n\n**Reason:** {reason}";
        }

        if (response.Length > 2000)
        {
            response = response.Substring(0, 1997) + "...";
        }

        return response;
    }
    catch (Exception ex)
    {
        return $"❌ Error: {ex.Message}";
    }
});

host.AddSlashCommand("listsuggestions", "List all bot suggestions", async (ApplicationCommandContext context) =>
{
    logger.Logger(context, "listsuggestions");

    try
    {
        var suggestions = LoadAllSuggestions();

        if (suggestions.Count == 0)
        {
            return "📭 No suggestions found!";
        }

        suggestions = suggestions.OrderByDescending(s => s.GetProperty("Id").GetInt32()).ToList();

        var pending = suggestions.Where(s => s.GetProperty("Status").GetString() == "pending").Count();
        var approved = suggestions.Where(s => s.GetProperty("Status").GetString() == "approved").Count();
        var denied = suggestions.Where(s => s.GetProperty("Status").GetString() == "denied").Count();

        var response = $"# 📋 Bot Suggestions List ({suggestions.Count} total)\n\n";
        response += $"⏳ Pending: {pending} | ✅ Approved: {approved} | ❌ Denied: {denied}\n\n";

        foreach (var s in suggestions.Take(15))
        {
            var id = s.GetProperty("Id").GetInt32();
            var status = s.GetProperty("Status").GetString();
            var username = s.GetProperty("Username").GetString();
            var suggestionText = s.GetProperty("Suggestion").GetString();

            if (suggestionText.Length > 50)
                suggestionText = suggestionText.Substring(0, 47) + "...";

            var statusEmoji = status == "pending" ? "⏳" : status == "approved" ? "✅" : "❌";

            response += $"{statusEmoji} **#{id}** - {username}: {suggestionText}\n";
        }

        if (suggestions.Count > 15)
        {
            response += $"\n*... and {suggestions.Count - 15} more suggestions*";
            response += $"\nUse `/viewsuggestion <id>` to view details";
        }

        if (response.Length > 2000)
        {
            response = $"# 📋 Bot Suggestions List ({suggestions.Count} total)\n\n";
            response += $"⏳ {pending} pending | ✅ {approved} approved | ❌ {denied} denied\n\n";

            foreach (var s in suggestions.Take(10))
            {
                var id = s.GetProperty("Id").GetInt32();
                var status = s.GetProperty("Status").GetString();
                var username = s.GetProperty("Username").GetString();
                var statusEmoji = status == "pending" ? "⏳" : status == "approved" ? "✅" : "❌";
                response += $"{statusEmoji} #{id} - {username}\n";
            }

            response += $"\nUse `/viewsuggestion <id>` to view details";
        }

        return response;
    }
    catch (Exception ex)
    {
        return $"❌ Error: {ex.Message}";
    }
});

host.AddSlashCommand("guildleaderboard", "Show top command users in this guild (This Month)", async (ApplicationCommandContext context) =>
{
    var guildId = context.Guild?.Id ?? 0;

    if (context.Guild == null)
    {
        return "❌ This command can only be used in a server!";
    }

    logger.Logger(context, "guildleaderboard");

    var startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
    var endDate = DateTime.Now.Date;
    var allLogs = new List<UserDataLogger>();

    for (var date = startDate; date <= endDate; date = date.AddDays(1))
    {
        string dateStr = date.ToString("yyyy-MM-dd");
        string filePath = Path.Combine(AppContext.BaseDirectory, "totaldata", $"Log-{dateStr}.json");

        if (File.Exists(filePath))
        {
            try
            {
                string json = File.ReadAllText(filePath);
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
        return $"📊 **No command data found for this guild in {startDate:MMMM yyyy}**\n" +
               $"Be the first to use commands this month!";
    }

    var userStats = guildLogs
        .GroupBy(u => u.UserId)
        .Select(g => new
        {
            UserId = g.Key,
            UserName = g.First().UserName ?? "Unknown",
            TotalCommands = g.Sum(u => u.Guilds.TryGetValue(guildId.ToString(), out var guildData) ? guildData.TotalCommands : 0)
        })
        .OrderByDescending(u => u.TotalCommands)
        .ToList();

    var response = new StringBuilder();
    response.AppendLine($"# 🏆 **Guild Leaderboard**");
    response.AppendLine($"📌 Guild: {context.Guild.Name}");
    response.AppendLine($"📅 Month: {startDate:MMMM yyyy}");
    response.AppendLine($"👥 Total Users: {userStats.Count}");
    response.AppendLine($"📝 Total Commands: {userStats.Sum(u => u.TotalCommands)}");
    response.AppendLine($"");
    response.AppendLine($"## 🥇 **Top Command Users**");

    int rank = 1;
    foreach (var user in userStats.Take(20))
    {
        string medal = rank switch
        {
            1 => "🥇",
            2 => "🥈",
            3 => "🥉",
            _ => $"#{rank}"
        };
        response.AppendLine($"{medal} **{user.UserName}** - {user.TotalCommands} commands");
        rank++;
    }

    if (userStats.Count > 20)
    {
        response.AppendLine($"\n*... and {userStats.Count - 20} more users*");
    }

    if (response.Length > 2000)
    {
        string conciseResponse = $"# 🏆 **Guild Leaderboard**\n";
        conciseResponse += $"📌 {context.Guild.Name} | 📅 {startDate:MMMM yyyy}\n";
        conciseResponse += $"👥 {userStats.Count} users | 📝 {userStats.Sum(u => u.TotalCommands)} commands\n\n";
        conciseResponse += $"**Top 10 Users:**\n";

        int conciseRank = 1;
        foreach (var user in userStats.Take(10))
        {
            conciseResponse += $"{conciseRank}. {user.UserName} - {user.TotalCommands}\n";
            conciseRank++;
        }

        return conciseResponse;
    }

    return response.ToString();
});

host.AddSlashCommand("totalleaderboard", "Show top command users across ALL guilds (This Month)", async (ApplicationCommandContext context) =>
{
    logger.Logger(context, "totalleaderboard");

    var startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
    var endDate = DateTime.Now.Date;
    var allLogs = new List<UserDataLogger>();
    var missingFiles = 0;

    for (var date = startDate; date <= endDate; date = date.AddDays(1))
    {
        string dateStr = date.ToString("yyyy-MM-dd");
        string filePath = Path.Combine(AppContext.BaseDirectory, "totaldata", $"Log-{dateStr}.json");

        if (File.Exists(filePath))
        {
            try
            {
                string json = File.ReadAllText(filePath);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var dayLogs = JsonSerializer.Deserialize<List<UserDataLogger>>(json) ?? new List<UserDataLogger>();
                    allLogs.AddRange(dayLogs);
                }
            }
            catch
            {
                missingFiles++;
            }
        }
        else
        {
            missingFiles++;
        }
    }

    if (allLogs.Count == 0)
    {
        return $"📊 **No command data found for {startDate:MMMM yyyy}**\n" +
               $"Be the first to use commands this month!";
    }

    var userStats = allLogs
        .GroupBy(u => u.UserId)
        .Select(g => new
        {
            UserId = g.Key,
            UserName = g.First().UserName ?? "Unknown",
            TotalCommands = g.Sum(u => u.TotalCommands),
            GuildCount = g.SelectMany(u => u.Guilds.Keys).Distinct().Count()
        })
        .OrderByDescending(u => u.TotalCommands)
        .ToList();

    var guildStats = allLogs
        .SelectMany(u => u.Guilds)
        .GroupBy(g => g.Key)
        .Select(g => new
        {
            GuildId = g.Key,
            GuildName = g.First().Value.GuildName ?? g.Key,
            TotalCommands = g.Sum(v => v.Value.TotalCommands)
        })
        .OrderByDescending(g => g.TotalCommands)
        .ToList();

    var response = new StringBuilder();
    response.AppendLine($"# 🌍 **Global Leaderboard**");
    response.AppendLine($"📅 Month: {startDate:MMMM yyyy}");
    response.AppendLine($"🌐 Total Guilds: {guildStats.Count}");
    response.AppendLine($"👥 Total Users: {userStats.Count}");
    response.AppendLine($"📝 Total Commands: {userStats.Sum(u => u.TotalCommands)}");
    if (missingFiles > 0)
    {
        response.AppendLine($"📄 Missing Days: {missingFiles}");
    }
    response.AppendLine($"");
    response.AppendLine($"## 🥇 **Top Users Overall**");

    int rank = 1;
    foreach (var user in userStats.Take(15))
    {
        string medal = rank switch
        {
            1 => "🥇",
            2 => "🥈",
            3 => "🥉",
            _ => $"#{rank}"
        };
        response.AppendLine($"{medal} **{user.UserName}** - {user.TotalCommands} commands ({user.GuildCount} guilds)");
        rank++;
    }

    if (userStats.Count > 15)
    {
        response.AppendLine($"\n*... and {userStats.Count - 15} more users*");
    }

    response.AppendLine($"");
    response.AppendLine($"## 🏠 **Top Guilds**");

    rank = 1;
    foreach (var guild in guildStats.Take(10))
    {
        string medal = rank switch
        {
            1 => "🥇",
            2 => "🥈",
            3 => "🥉",
            _ => $"#{rank}"
        };
        response.AppendLine($"{medal} **{guild.GuildName}** - {guild.TotalCommands} commands");
        rank++;
    }

    if (guildStats.Count > 10)
    {
        response.AppendLine($"\n*... and {guildStats.Count - 10} more guilds*");
    }

    if (response.Length > 2000)
    {
        string conciseResponse = $"# 🌍 **Global Leaderboard**\n";
        conciseResponse += $"📅 {startDate:MMMM yyyy} | 👥 {userStats.Count} users | 📝 {userStats.Sum(u => u.TotalCommands)} commands\n\n";
        conciseResponse += $"**Top 10 Users:**\n";

        int conciseRank = 1;
        foreach (var user in userStats.Take(10))
        {
            conciseResponse += $"{conciseRank}. {user.UserName} - {user.TotalCommands}\n";
            conciseRank++;
        }

        conciseResponse += $"\n**Top 5 Guilds:**\n";
        conciseRank = 1;
        foreach (var guild in guildStats.Take(5))
        {
            conciseResponse += $"{conciseRank}. {guild.GuildName} - {guild.TotalCommands}\n";
            conciseRank++;
        }

        return conciseResponse;
    }

    return response.ToString();
});

host.AddSlashCommand("guilddata", "Show all command logs for this guild (Admin only)", async (ApplicationCommandContext context) =>
{
    var userId = context.User.Id;
    var guildId = context.Guild?.Id ?? 0;

    if (context.Guild != null)
    {
        var guildUser = await context.Guild.GetUserAsync(userId);
        if (guildUser == null)
        {
            return "❌ Could not find your user in this guild!";
        }

        var permissions = guildUser.GetPermissions(context.Guild);
        if ((permissions & Permissions.Administrator) != Permissions.Administrator)
        {
            return "❌ You need **Administrator** permissions to use this command!";
        }
    }
    else
    {
        return "❌ This command can only be used in a server!";
    }

    logger.Logger(context, "guilddata");

    var startDate = new DateTime(2026, 6, 20);
    var endDate = DateTime.Now.Date;
    var allLogs = new List<UserDataLogger>();
    var missingFiles = new List<string>();

    for (var date = startDate; date <= endDate; date = date.AddDays(1))
    {
        string dateStr = date.ToString("yyyy-MM-dd");
        string filePath = Path.Combine(AppContext.BaseDirectory, "totaldata", $"Log-{dateStr}.json");

        if (File.Exists(filePath))
        {
            try
            {
                string json = File.ReadAllText(filePath);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var dayLogs = JsonSerializer.Deserialize<List<UserDataLogger>>(json) ?? new List<UserDataLogger>();
                    allLogs.AddRange(dayLogs);
                }
            }
            catch (Exception ex)
            {
                missingFiles.Add($"⚠️ Error reading {dateStr}: {ex.Message}");
            }
        }
        else
        {
            missingFiles.Add($"📄 No data for {dateStr}");
        }
    }

    var guildLogs = allLogs.Where(log => log.Guilds.ContainsKey(guildId.ToString())).ToList();

    if (guildLogs.Count == 0)
    {
        var missingSummary = missingFiles.Count > 0
            ? $"\n\n**Missing/Empty Files:**\n{string.Join("\n", missingFiles.Take(10))}"
            : "";
        return $"📊 **No command data found for this guild since 2026-06-20**{missingSummary}";
    }

    var response = new StringBuilder();
    response.AppendLine($"📊 **Guild Data Report**");
    response.AppendLine($"📌 Guild: {context.Guild?.Name ?? "Unknown"} ({guildId})");
    response.AppendLine($"📅 Period: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
    response.AppendLine($"👥 Total Users: {guildLogs.Count}");
    response.AppendLine($"📝 Total Commands: {guildLogs.Sum(u => u.Guilds[guildId.ToString()].TotalCommands)}");
    response.AppendLine($"");
    response.AppendLine($"**User Statistics:**");

    var topUsers = guildLogs
        .Select(u => new
        {
            User = u,
            CommandCount = u.Guilds[guildId.ToString()].TotalCommands
        })
        .OrderByDescending(u => u.CommandCount)
        .Take(10);

    int rank = 1;
    foreach (var user in topUsers)
    {
        response.AppendLine($"{rank}. **{user.User.UserName}** ({user.User.UserId}): {user.CommandCount} commands");
        rank++;
    }

    response.AppendLine($"");
    response.AppendLine($"**Channel Breakdown:**");
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

    foreach (var channel in channelStats.OrderByDescending(c => c.Value))
    {
        response.AppendLine($"- Channel {channel.Key}: {channel.Value} commands");
    }

    if (missingFiles.Count > 0)
    {
        response.AppendLine($"");
        response.AppendLine($"**Data Availability:**");
        int totalDays = (int)(endDate - startDate).TotalDays + 1;
        int existingFiles = totalDays - missingFiles.Count;
        response.AppendLine($"- Files found: {existingFiles}/{totalDays}");

        if (missingFiles.Count <= 5)
        {
            response.AppendLine($"- Missing: {string.Join(", ", missingFiles.Select(f => f.Replace("📄 No data for ", "")))}");
        }
        else
        {
            response.AppendLine($"- Missing {missingFiles.Count} days (showing first 5):");
            foreach (var missing in missingFiles.Take(5))
            {
                response.AppendLine($"  - {missing}");
            }
        }
    }

    if (response.Length > 2000)
    {
        string truncatedResponse = $"📊 **Guild Data Report**\n";
        truncatedResponse += $"📌 Guild: {context.Guild?.Name ?? "Unknown"} ({guildId})\n";
        truncatedResponse += $"📅 Period: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}\n";
        truncatedResponse += $"👥 Total Users: {guildLogs.Count}\n";
        truncatedResponse += $"📝 Total Commands: {guildLogs.Sum(u => u.Guilds[guildId.ToString()].TotalCommands)}\n";
        truncatedResponse += $"\n**Top Users:**\n";

        int truncatedRank = 1;
        foreach (var user in topUsers)
        {
            truncatedResponse += $"{truncatedRank}. **{user.User.UserName}**: {user.CommandCount} commands\n";
            truncatedRank++;
        }

        truncatedResponse += $"\n**Channel Stats:** {channelStats.Count} channels\n";
        int totalDays = (int)(endDate - startDate).TotalDays + 1;
        int existingFiles = totalDays - missingFiles.Count;
        truncatedResponse += $"Files available: {existingFiles}/{totalDays}";

        return truncatedResponse;
    }

    return response.ToString();
});

host.AddSlashCommand("alldata", "Show ALL command data (Admin only, specific channel)", async (ApplicationCommandContext context) =>
{
    var userId = context.User.Id;
    var guildId = context.Guild?.Id ?? 0;
    var channelId = context.Channel?.Id ?? 0;

    if (guildId != 1495153417306247339 || channelId != 1495161155608645656)
    {
        return "❌ This command can only be used in Cresclent's server in the designated channel!";
    }

    bool isAdmin = false;
    bool isOwner = userId == 1157243448093573120;

    if (context.Guild != null)
    {
        var guildUser = await context.Guild.GetUserAsync(userId);
        if (guildUser != null)
        {
            var permissions = guildUser.GetPermissions(context.Guild);
            isAdmin = (permissions & Permissions.Administrator) == Permissions.Administrator;
        }
    }

    if (!isAdmin && !isOwner)
    {
        return "❌ You need **Administrator** permissions or be the bot owner to use this command!";
    }

    logger.Logger(context, "alldata");

    var startDate = new DateTime(2026, 6, 20);
    var endDate = DateTime.Now.Date;
    var allLogs = new List<UserDataLogger>();
    var missingFiles = new List<string>();
    var fileStats = new Dictionary<string, int>();

    for (var date = startDate; date <= endDate; date = date.AddDays(1))
    {
        string dateStr = date.ToString("yyyy-MM-dd");
        string filePath = Path.Combine(AppContext.BaseDirectory, "totaldata", $"Log-{dateStr}.json");

        if (File.Exists(filePath))
        {
            try
            {
                string json = File.ReadAllText(filePath);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var dayLogs = JsonSerializer.Deserialize<List<UserDataLogger>>(json) ?? new List<UserDataLogger>();
                    allLogs.AddRange(dayLogs);
                    fileStats[dateStr] = dayLogs.Count;
                }
                else
                {
                    missingFiles.Add($"Empty file: {dateStr}");
                }
            }
            catch (Exception ex)
            {
                missingFiles.Add($"Error reading {dateStr}: {ex.Message}");
            }
        }
        else
        {
            missingFiles.Add($"No file: {dateStr}");
        }
    }

    if (allLogs.Count == 0)
    {
        return $"📊 **No command data found since 2026-06-20**\n" +
               $"Total days checked: {(endDate - startDate).TotalDays + 1}\n" +
               $"Files found: {fileStats.Count}";
    }

    var response = new StringBuilder();
    response.AppendLine($"# 📊 **Complete Command Data Report**");
    response.AppendLine($"");
    response.AppendLine($"## 📅 Overview");
    response.AppendLine($"- **Period:** {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
    response.AppendLine($"- **Total Days:** {(endDate - startDate).TotalDays + 1}");
    response.AppendLine($"- **Files with Data:** {fileStats.Count}/{(endDate - startDate).TotalDays + 1}");
    response.AppendLine($"- **Total Users:** {allLogs.Select(u => u.UserId).Distinct().Count()}");
    response.AppendLine($"- **Total Commands:** {allLogs.Sum(u => u.TotalCommands)}");
    response.AppendLine($"- **Total Guilds Used:** {allLogs.SelectMany(u => u.Guilds.Keys).Distinct().Count()}");
    response.AppendLine($"");

    var topUsersOverall = allLogs
        .GroupBy(u => new { u.UserId, u.UserName })
        .Select(g => new
        {
            UserId = g.Key.UserId,
            UserName = g.Key.UserName,
            TotalCommands = g.Sum(u => u.TotalCommands),
            GuildCount = g.SelectMany(u => u.Guilds.Keys).Distinct().Count()
        })
        .OrderByDescending(u => u.TotalCommands)
        .Take(15);

    response.AppendLine($"## 👥 Top Users Overall");
    int rankOverall = 1;
    foreach (var user in topUsersOverall)
    {
        response.AppendLine($"{rankOverall}. **{user.UserName}** ({user.UserId}): {user.TotalCommands} commands across {user.GuildCount} guilds");
        rankOverall++;
    }
    response.AppendLine($"");

    response.AppendLine($"## 🏠 Top Guilds");
    var guildStats = new Dictionary<string, (string Name, int Count)>();
    foreach (var user in allLogs)
    {
        foreach (var guild in user.Guilds)
        {
            string guildIdStr = guild.Key;
            string guildName = guild.Value.GuildName ?? guildIdStr;
            int count = guild.Value.TotalCommands;

            if (guildStats.ContainsKey(guildIdStr))
            {
                var existing = guildStats[guildIdStr];
                guildStats[guildIdStr] = (existing.Name, existing.Count + count);
            }
            else
            {
                guildStats[guildIdStr] = (guildName, count);
            }
        }
    }

    foreach (var guild in guildStats.OrderByDescending(g => g.Value.Count).Take(10))
    {
        response.AppendLine($"- **{guild.Value.Name}** ({guild.Key}): {guild.Value.Count} commands");
    }
    response.AppendLine($"");

    response.AppendLine($"## 📈 Daily Activity (Last 30 Days)");
    var dailyStats = new Dictionary<string, int>();
    foreach (var user in allLogs)
    {
        foreach (var guild in user.Guilds)
        {
            foreach (var channel in guild.Value.Channels)
            {
                foreach (var command in channel.Value.Commands)
                {
                    var parts = command.Split(" at ");
                    if (parts.Length == 2)
                    {
                        string dateStr = parts[1].Split(' ')[0];
                        if (dailyStats.ContainsKey(dateStr))
                            dailyStats[dateStr]++;
                        else
                            dailyStats[dateStr] = 1;
                    }
                }
            }
        }
    }

    var last30Days = dailyStats.OrderByDescending(d => d.Key).Take(30);
    foreach (var day in last30Days.OrderBy(d => d.Key))
    {
        response.AppendLine($"- {day.Key}: {day.Value} commands");
    }
    response.AppendLine($"");

    if (missingFiles.Count > 0)
    {
        response.AppendLine($"## 📄 Missing/Empty Files");
        response.AppendLine($"- **Total Missing:** {missingFiles.Count}");

        var missingDates = missingFiles
            .Select(f => f.Replace("No file: ", "").Replace("Empty file: ", "").Replace("Error reading ", ""))
            .Where(d => DateTime.TryParse(d, out _))
            .OrderBy(d => d)
            .ToList();

        if (missingDates.Count <= 10)
        {
            foreach (var date in missingDates)
            {
                response.AppendLine($"- {date}");
            }
        }
        else
        {
            response.AppendLine($"- Showing first 10 of {missingDates.Count}:");
            foreach (var date in missingDates.Take(10))
            {
                response.AppendLine($"  - {date}");
            }
            response.AppendLine($"  - ... and {missingDates.Count - 10} more");
        }
    }

    response.AppendLine($"");
    response.AppendLine($"## 📊 Command Type Distribution");
    var commandStats = new Dictionary<string, int>();
    foreach (var user in allLogs)
    {
        foreach (var guild in user.Guilds)
        {
            foreach (var channel in guild.Value.Channels)
            {
                foreach (var command in channel.Value.Commands)
                {
                    string cmdName = command.Split(' ')[0];
                    if (commandStats.ContainsKey(cmdName))
                        commandStats[cmdName]++;
                    else
                        commandStats[cmdName] = 1;
                }
            }
        }
    }

    foreach (var cmd in commandStats.OrderByDescending(c => c.Value))
    {
        response.AppendLine($"- `{cmd.Key}`: {cmd.Value} times");
    }

    if (response.Length > 2000)
    {
        string conciseResponse = $"# 📊 **Complete Command Data Report**\n\n";
        conciseResponse += $"📅 {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}\n";
        conciseResponse += $"👥 Users: {allLogs.Select(u => u.UserId).Distinct().Count()}\n";
        conciseResponse += $"📝 Commands: {allLogs.Sum(u => u.TotalCommands)}\n";
        conciseResponse += $"🏠 Guilds: {allLogs.SelectMany(u => u.Guilds.Keys).Distinct().Count()}\n\n";

        conciseResponse += $"**Top 5 Users:**\n";
        foreach (var user in topUsersOverall.Take(5))
        {
            conciseResponse += $"{user.UserName}: {user.TotalCommands}\n";
        }

        conciseResponse += $"\n**Top 5 Guilds:**\n";
        foreach (var guild in guildStats.OrderByDescending(g => g.Value.Count).Take(5))
        {
            conciseResponse += $"{guild.Value.Name}: {guild.Value.Count}\n";
        }

        conciseResponse += $"\n**Missing Files:** {missingFiles.Count}";
        conciseResponse += $"\n\n📊 Full data available in logs. Use `/guilddata` for guild-specific stats.";

        return conciseResponse;
    }

    return response.ToString();
});

host.AddSlashCommand("setstartupchannel", "Set the channel for bot startup announcements (Admin only)", async (ApplicationCommandContext context,
    [SlashCommandParameter(Name = "channel", Description = "The channel to send announcements to (optional)")] Channel? channel = null,
    [SlashCommandParameter(Name = "role", Description = "the role to ping.. not putting anything in will make it not ping anything")] Role? role = null) =>
{
    var userId = context.User.Id;
    var guildId = context.Guild?.Id ?? 0;

    logger.Logger(context, "setstartupchannel");

    if (context.Guild == null)
    {
        return "This command can only be used in a server!";
    }

    var guildUser = await context.Guild.GetUserAsync(userId);
    if (guildUser == null)
    {
        return "Could not find your user in this guild!";
    }

    var permissions = guildUser.GetPermissions(context.Guild);
    if ((permissions & Permissions.Administrator) != Permissions.Administrator)
    {
        return "You need Administrator permissions to use this command!";
    }

    ulong targetChannelId;
    if (channel == null)
    {
        targetChannelId = context.Channel.Id;
    }
    else
    {
        targetChannelId = channel.Id;
    }

    try
    {
        var guildChannel = context.Guild.Channels.FirstOrDefault(c => c.Value.Id == targetChannelId);
        if (guildChannel.Value == null)
        {
            return "That channel does not exist in this server!";
        }

        if (guildChannel.Value is not TextGuildChannel)
        {
            return "Please select a text channel!";
        }
    }
    catch
    {
        return "Could not verify the channel!";
    }

    ulong? targetRoleId = null;
    if (role != null)
    {
        targetRoleId = role.Id;
        var guildRole = context.Guild.Roles.FirstOrDefault(r => r.Value.Id == targetRoleId);
        if (guildRole.Value == null)
        {
            return "That role does not exist in this server!";
        }
    }

    var restClient = host.Services.GetRequiredService<RestClient>();
    var startupAnnouncement = new StartupAnnouncement(restClient);
    startupAnnouncement.SetAnnouncementChannel(guildId, targetChannelId, targetRoleId);

    return $"Startup announcements have been set to <#{targetChannelId}> for this server!";
});

host.AddSlashCommand("disablestartup", "Disable bot startup announcements for this guild (Admin only)", async (ApplicationCommandContext context) =>
{
    var userId = context.User.Id;
    var guildId = context.Guild?.Id ?? 0;

    logger.Logger(context, "disablestartup");

    if (context.Guild == null)
    {
        return "This command can only be used in a server!";
    }

    var guildUser = await context.Guild.GetUserAsync(userId);
    if (guildUser == null)
    {
        return "Could not find your user in this guild!";
    }

    var permissions = guildUser.GetPermissions(context.Guild);
    if ((permissions & Permissions.Administrator) != Permissions.Administrator)
    {
        return "You need Administrator permissions to use this command!";
    }

    var restClient = host.Services.GetRequiredService<RestClient>();
    var startupAnnouncement = new StartupAnnouncement(restClient);
    startupAnnouncement.DisableAnnouncements(guildId);
    return "Startup announcements have been disabled for this server.";
});

host.AddSlashCommand("startupstatus", "Check the status of startup announcements for this guild", async (ApplicationCommandContext context) =>
{
    var userId = context.User.Id;
    var guildId = context.Guild?.Id ?? 0;

    logger.Logger(context, "startupstatus");

    if (context.Guild == null)
    {
        return "This command can only be used in a server!";
    }

    var guildUser = await context.Guild.GetUserAsync(userId);
    if (guildUser == null)
    {
        return "Could not find your user in this guild!";
    }

    var permissions = guildUser.GetPermissions(context.Guild);
    if ((permissions & Permissions.Administrator) != Permissions.Administrator)
    {
        return "You need Administrator permissions to use this command!";
    }

    var restClient = host.Services.GetRequiredService<RestClient>();
    var startupAnnouncement = new StartupAnnouncement(restClient);
    var info = startupAnnouncement.GetConfigInfo(guildId);
    var guildName = context.Guild.Name;

    return $"**Startup Announcement Status**\n\n" +
           $"**Guild:** {guildName}\n" +
           $"{info}";
});

host.AddSlashCommand("allstartupchannels", "View all configured startup announcement channels (Bot owner only)", async (ApplicationCommandContext context) =>
{
    var userId = context.User.Id;

    logger.Logger(context, "allstartupchannels");

    if (userId != 1157243448093573120)
    {
        return "Only the bot owner can use this command!";
    }

    var restClient = host.Services.GetRequiredService<RestClient>();
    var startupAnnouncement = new StartupAnnouncement(restClient);
    return startupAnnouncement.GetAllConfigInfo();
});

host.AddSlashCommand("setglobalchannel", "Set the channel for global announcements (Admin only)", async (ApplicationCommandContext context,
    [SlashCommandParameter(Name = "channel", Description = "The channel to send announcements to (optional)")] Channel? channel = null,
    [SlashCommandParameter(Name = "role", Description = "The role to ping (optional)")] Role? role = null) =>
{
    var userId = context.User.Id;
    var guildId = context.Guild?.Id ?? 0;

    logger.Logger(context, "setglobalchannel");

    if (context.Guild == null)
    {
        return "This command can only be used in a server!";
    }

    var guildUser = await context.Guild.GetUserAsync(userId);
    if (guildUser == null)
    {
        return "Could not find your user in this guild!";
    }

    var permissions = guildUser.GetPermissions(context.Guild);
    if ((permissions & Permissions.Administrator) != Permissions.Administrator)
    {
        return "You need Administrator permissions to use this command!";
    }

    ulong targetChannelId;
    if (channel == null)
    {
        targetChannelId = context.Channel.Id;
    }
    else
    {
        targetChannelId = channel.Id;
    }

    try
    {
        var guildChannel = context.Guild.Channels.FirstOrDefault(c => c.Value.Id == targetChannelId);
        if (guildChannel.Value == null)
        {
            return "That channel does not exist in this server!";
        }

        if (guildChannel.Value is not TextGuildChannel)
        {
            return "Please select a text channel!";
        }
    }
    catch
    {
        return "Could not verify the channel!";
    }

    ulong? targetRoleId = null;
    if (role != null)
    {
        targetRoleId = role.Id;
        var guildRole = context.Guild.Roles.FirstOrDefault(r => r.Value.Id == targetRoleId);
        if (guildRole.Value == null)
        {
            return "That role does not exist in this server!";
        }
    }

    var restClient = host.Services.GetRequiredService<RestClient>();
    var globalAnnouncement = new GlobalAnnouncement(restClient);
    globalAnnouncement.SetGlobalChannel(guildId, targetChannelId, targetRoleId);

    if (channel == null)
    {
        return $"Global announcements have been set to the current channel <#{targetChannelId}> for this server!";
    }
    else
    {
        return $"Global announcements have been set to <#{targetChannelId}> for this server!";
    }
});

host.AddSlashCommand("globalannounce", "Send a global announcement to all configured channels (Bot owner only)", async (ApplicationCommandContext context,
    [SlashCommandParameter(Name = "message", Description = "The announcement message")] string message,
    [SlashCommandParameter(Name = "ping", Description = "Whether to ping the configured role (true/false)")] bool? ping = null) =>
{
    var userId = context.User.Id;

    logger.Logger(context, "globalannounce");

    if (userId != 1157243448093573120)
    {
        return "Only the bot owner can send global announcements!";
    }

    if (string.IsNullOrWhiteSpace(message))
    {
        return "Please provide a message!";
    }

    var restClient = host.Services.GetRequiredService<RestClient>();
    var globalAnnouncement = new GlobalAnnouncement(restClient);

    bool shouldPing = ping ?? false;
    await globalAnnouncement.SendGlobalAnnouncementAsync(message, shouldPing);

    return $"Global announcement sent successfully to all configured channels! (Ping: {(shouldPing ? "Enabled" : "Disabled")})";
});

host.AddSlashCommand("disableglobal", "Disable global announcements for this guild (Admin only)", async (ApplicationCommandContext context) =>
{
    var userId = context.User.Id;
    var guildId = context.Guild?.Id ?? 0;

    logger.Logger(context, "disableglobal");

    if (context.Guild == null)
    {
        return "This command can only be used in a server!";
    }

    var guildUser = await context.Guild.GetUserAsync(userId);
    if (guildUser == null)
    {
        return "Could not find your user in this guild!";
    }

    var permissions = guildUser.GetPermissions(context.Guild);
    if ((permissions & Permissions.Administrator) != Permissions.Administrator)
    {
        return "You need Administrator permissions to use this command!";
    }

    var restClient = host.Services.GetRequiredService<RestClient>();
    var globalAnnouncement = new GlobalAnnouncement(restClient);
    globalAnnouncement.DisableGlobalAnnouncements(guildId);

    return "Global announcements have been disabled for this server.";
});

host.AddSlashCommand("globalstatus", "Check the status of global announcements for this guild (Admin only)", async (ApplicationCommandContext context) =>
{
    var userId = context.User.Id;
    var guildId = context.Guild?.Id ?? 0;

    logger.Logger(context, "globalstatus");

    if (context.Guild == null)
    {
        return "This command can only be used in a server!";
    }

    var guildUser = await context.Guild.GetUserAsync(userId);
    if (guildUser == null)
    {
        return "Could not find your user in this guild!";
    }

    var permissions = guildUser.GetPermissions(context.Guild);
    if ((permissions & Permissions.Administrator) != Permissions.Administrator)
    {
        return "You need Administrator permissions to use this command!";
    }

    var restClient = host.Services.GetRequiredService<RestClient>();
    var globalAnnouncement = new GlobalAnnouncement(restClient);
    var info = globalAnnouncement.GetGlobalConfigInfo(guildId);
    var guildName = context.Guild.Name;

    return $"**Global Announcement Status**\n\n" +
           $"**Guild:** {guildName}\n" +
           $"{info}";
});

host.AddSlashCommand("allglobalchannels", "View all configured global announcement channels (Bot owner only)", async (ApplicationCommandContext context) =>
{
    var userId = context.User.Id;

    logger.Logger(context, "allglobalchannels");

    if (userId != 1157243448093573120)
    {
        return "Only the bot owner can use this command!";
    }

    var restClient = host.Services.GetRequiredService<RestClient>();
    var globalAnnouncement = new GlobalAnnouncement(restClient);
    return globalAnnouncement.GetAllGlobalConfigInfo();
});

host.AddSlashCommand("ignoreuser", "Add a user to the ignore list (Admin only, specific channel)", async (ApplicationCommandContext context,
    [SlashCommandParameter(Name = "user", Description = "The user to ignore")] User user) =>
{
    var userId = context.User.Id;
    var guildId = context.Guild?.Id ?? 0;
    var channelId = context.Channel?.Id ?? 0;

    if (guildId != 1495153417306247339 || channelId != 1495161155608645656)
    {
        return "❌ This command can only be used in Cresclent's server in the designated channel!";
    }

    bool isAdmin = false;
    bool isOwner = userId == 1157243448093573120;

    if (context.Guild != null)
    {
        var guildUser = await context.Guild.GetUserAsync(userId);
        if (guildUser != null)
        {
            var permissions = guildUser.GetPermissions(context.Guild);
            isAdmin = (permissions & Permissions.Administrator) == Permissions.Administrator;
        }
    }

    if (!isAdmin && !isOwner)
    {
        return "❌ You need **Administrator** permissions or be the bot owner to use this command!";
    }

    logger.Logger(context, "ignoreuser");

    if (UserDataLogger.IsUserIgnored(user.Id))
    {
        return $"⚠️ User {user.Username} ({user.Id}) is already on the ignore list!";
    }

    UserDataLogger.AddIgnoredUser(user.Id);

    return $"✅ User {user.Username} ({user.Id}) has been added to the ignore list.\n" +
           $"Their commands will no longer be logged.";
});

host.AddSlashCommand("unignoreuser", "Remove a user from the ignore list (Admin only, specific channel)", async (ApplicationCommandContext context,
    [SlashCommandParameter(Name = "user", Description = "The user to unignore")] User user) =>
{
    var userId = context.User.Id;
    var guildId = context.Guild?.Id ?? 0;
    var channelId = context.Channel?.Id ?? 0;

    if (guildId != 1495153417306247339 || channelId != 1495161155608645656)
    {
        return "❌ This command can only be used in Cresclent's server in the designated channel!";
    }

    bool isAdmin = false;
    bool isOwner = userId == 1157243448093573120;

    if (context.Guild != null)
    {
        var guildUser = await context.Guild.GetUserAsync(userId);
        if (guildUser != null)
        {
            var permissions = guildUser.GetPermissions(context.Guild);
            isAdmin = (permissions & Permissions.Administrator) == Permissions.Administrator;
        }
    }

    if (!isAdmin && !isOwner)
    {
        return "❌ You need **Administrator** permissions or be the bot owner to use this command!";
    }

    logger.Logger(context, "unignoreuser");

    if (!UserDataLogger.IsUserIgnored(user.Id))
    {
        return $"⚠️ User {user.Username} ({user.Id}) is not on the ignore list!";
    }

    UserDataLogger.RemoveIgnoredUser(user.Id);

    return $"✅ User {user.Username} ({user.Id}) has been removed from the ignore list.\n" +
           $"Their commands will now be logged again.";
});

host.AddSlashCommand("listignored", "List all ignored users (Admin only, specific channel)", async (ApplicationCommandContext context) =>
{
    var userId = context.User.Id;
    var guildId = context.Guild?.Id ?? 0;
    var channelId = context.Channel?.Id ?? 0;

    if (guildId != 1495153417306247339 || channelId != 1495161155608645656)
    {
        return "❌ This command can only be used in Cresclent's server in the designated channel!";
    }

    bool isAdmin = false;
    bool isOwner = userId == 1157243448093573120;

    if (context.Guild != null)
    {
        var guildUser = await context.Guild.GetUserAsync(userId);
        if (guildUser != null)
        {
            var permissions = guildUser.GetPermissions(context.Guild);
            isAdmin = (permissions & Permissions.Administrator) == Permissions.Administrator;
        }
    }

    if (!isAdmin && !isOwner)
    {
        return "❌ You need **Administrator** permissions or be the bot owner to use this command!";
    }

    logger.Logger(context, "listignored");

    var ignoredUsers = UserDataLogger.GetIgnoredUsers();

    if (ignoredUsers.Count == 0)
    {
        return "📋 No users are currently on the ignore list.";
    }

    var response = $"📋 **Ignored Users List** ({ignoredUsers.Count} total)\n\n";

    var restClient = host.Services.GetRequiredService<RestClient>();
    int count = 0;

    foreach (var id in ignoredUsers)
    {
        count++;
        try
        {
            var user = await restClient.GetUserAsync(id);
            response += $"{count}. {user.Username} ({id})\n";
        }
        catch
        {
            response += $"{count}. Unknown User ({id})\n";
        }

        if (response.Length > 1800 && count < ignoredUsers.Count)
        {
            response += $"\n*... and {ignoredUsers.Count - count} more users*";
            break;
        }
    }

    if (response.Length > 2000)
    {
        response = $"📋 **Ignored Users List** ({ignoredUsers.Count} total)\n\n";
        count = 0;
        foreach (var id in ignoredUsers.Take(10))
        {
            count++;
            try
            {
                var user = await restClient.GetUserAsync(id);
                response += $"{count}. {user.Username} ({id})\n";
            }
            catch
            {
                response += $"{count}. Unknown User ({id})\n";
            }
        }
        if (ignoredUsers.Count > 10)
        {
            response += $"\n*... and {ignoredUsers.Count - 10} more users*";
        }
    }

    return response;
});

host.AddSlashCommand("cleanupremovedservers", "Check and cleanup data for removed servers (Bot owner only)", async (ApplicationCommandContext context) =>
{
    var userId = context.User.Id;

    logger.Logger(context, "cleanupremovedservers");

    if (userId != 1157243448093573120)
    {
        return "❌ Only the bot owner can use this command!";
    }

    try
    {
        var client = host.Services.GetRequiredService<GatewayClient>();
        var serverTracker = new ServerTracker();
        var currentGuilds = client.Cache.Guilds.Select(g => g.Value.Id).ToList();

        foreach (var guildId in currentGuilds)
        {
            serverTracker.AddServer(guildId);
        }
        await serverTracker.CheckAndCleanupRemovedServers(currentGuilds);

        var tracked = serverTracker.GetAllServers();
        return $"✅ Cleanup completed!\n" +
               $"📋 Tracking {tracked.Count} servers\n" +
               $"🌐 Currently in {currentGuilds.Count} servers";
    }
    catch (Exception ex)
    {
        return $"❌ Error: {ex.Message}";
    }
});

host.AddSlashCommand("github", "The open source code!", (ApplicationCommandContext context) =>
{
    logger.Logger(context, "github");
    return "github: [github](https://github.com/cresclent/SheepyBot)";
});

host.AddSlashCommand("terms", "the terms of service", (ApplicationCommandContext context) =>
{
    logger.Logger(context, "terms");
    return new TAPCommands().TOS();
});

host.AddSlashCommand("privacy", "the privacy policy", (ApplicationCommandContext context) =>
{
    logger.Logger(context, "privacy");
    return new TAPCommands().Privacy();
});

void AddToInventory(UserWishData data, string item)
{
    if (data.Inventory.ContainsKey(item))
        data.Inventory[item]++;
    else
        data.Inventory[item] = 1;
}

bool IsFiveStar(string item)
{
    return Array.Exists(fiveStarCharacters, x => x == item) ||
           Array.Exists(standardFiveStars, x => x == item);
}

bool IsFourStar(string item)
{
    return Array.Exists(fourStarCharacters, x => x == item);
}

ulong applicationId = 1517174047169974404;

var client = host.Services.GetRequiredService<GatewayClient>();
var restClient = host.Services.GetRequiredService<RestClient>();

client.Ready += async (ReadyEventArgs args) =>
{
    new Write().WriteLine("Bot is ready!");
    new Write().WriteLine($"Logged in as: {args.User?.Username ?? "Unknown"}");

    try
    {
        await client.UpdatePresenceAsync(new PresenceProperties(UserStatusType.Online)
        {
            Activities = new[] { new UserActivityProperties(gameStatus, UserActivityType.Playing) }
        });

        var serverTracker = new ServerTracker();
        var currentGuilds = client.Cache.Guilds.Select(g => g.Value.Id).ToList();

        for (int i = 0; i < 5 && currentGuilds.Count == 0; i++)
        {
            new Write().WriteLine($"Waiting for cache to populate... (attempt {i + 1}/5)");
            await Task.Delay(3000);
            currentGuilds = client.Cache.Guilds.Select(g => g.Value.Id).ToList();
        }

        new Write().WriteLine($"Found {currentGuilds.Count} servers in cache");

        foreach (var guildId in currentGuilds)
        {
            serverTracker.AddServer(guildId);
        }

        await serverTracker.CheckAndCleanupRemovedServers(currentGuilds);

        var trackedServers = serverTracker.GetAllServers();
        new Write().WriteLine($"Tracking {trackedServers.Count} servers total");

        var commands = new List<ApplicationCommandProperties>
        {
            new SlashCommandProperties("pity", "Check your stats"),
            new SlashCommandProperties("inventory", "Check your inventory"),
            new SlashCommandProperties("banner", "See what the banner is!"),
            new SlashCommandProperties("pull", "Perform 10 wishes (2.5 minute cooldown)"),
            new SlashCommandProperties("help", "Display Help message"),
            new SlashCommandProperties("coinflip", "Simple Coinflip"),
            new SlashCommandProperties("bannerreset", "banner resetting, can ONLY be done by cresclent"),
            new SlashCommandProperties("guilddata", "Show command logs for this guild (Admin only)"),
            new SlashCommandProperties("alldata", "Show ALL command data (Admin only, specific channel)"),
            new SlashCommandProperties("guildleaderboard", "Show top users in this guild (This Month)"),
            new SlashCommandProperties("totalleaderboard", "Show top users across ALL guilds (This Month)"),

            new SlashCommandProperties("suggest", "Suggest a new feature for the bot")
            {
                Options = new[]
                {
                    new ApplicationCommandOptionProperties(ApplicationCommandOptionType.String, "suggestion", "Your suggestion for the bot")
                    {
                        Required = true
                    }
                }
            },
            new SlashCommandProperties("approvesuggestion", "Approve a bot suggestion (Bot owner only)")
            {
                Options = new[]
                {
                    new ApplicationCommandOptionProperties(ApplicationCommandOptionType.Integer, "id", "Suggestion ID")
                    {
                        Required = true
                    }
                }
            },
            new SlashCommandProperties("denysuggestion", "Deny a bot suggestion (Bot owner only)")
            {
                Options = new[]
                {
                    new ApplicationCommandOptionProperties(ApplicationCommandOptionType.Integer, "id", "Suggestion ID")
                    {
                        Required = true
                    },
                    new ApplicationCommandOptionProperties(ApplicationCommandOptionType.String, "reason", "Reason for denial (optional)")
                    {
                        Required = false
                    }
                }
            },
            new SlashCommandProperties("viewsuggestion", "View a specific suggestion by ID")
            {
                Options = new[]
                {
                    new ApplicationCommandOptionProperties(ApplicationCommandOptionType.Integer, "id", "Suggestion ID")
                    {
                        Required = true
                    }
                }
            },
            new SlashCommandProperties("listsuggestions", "List all bot suggestions"),

            new SlashCommandProperties("setstartupchannel", "Set the channel for bot startup announcements (Admin only)")
            {
                Options = new[]
                {
                    new ApplicationCommandOptionProperties(ApplicationCommandOptionType.Channel, "channel", "The channel to send announcements to (optional)")
                    {
                        Required = false
                    },
                    new ApplicationCommandOptionProperties(ApplicationCommandOptionType.Role, "role", "the role to ping.. not putting anything in will make it not ping anything")
                    {
                        Required = false
                    }
                }
            },
            new SlashCommandProperties("disablestartup", "Disable bot startup announcements (Admin only)"),
            new SlashCommandProperties("startupstatus", "Check the status of startup announcements (Admin only)"),
            new SlashCommandProperties("allstartupchannels", "View all configured startup announcement channels (Bot owner only)"),

            new SlashCommandProperties("setglobalchannel", "Set the channel for global announcements (Admin only)")
            {
                Options = new[]
                {
                    new ApplicationCommandOptionProperties(ApplicationCommandOptionType.Channel, "channel", "The channel to send announcements to (optional)")
                    {
                        Required = false
                    },
                    new ApplicationCommandOptionProperties(ApplicationCommandOptionType.Role, "role", "The role to ping (optional)")
                    {
                        Required = false
                    }
                }
            },
            new SlashCommandProperties("globalannounce", "Send a global announcement to all configured channels (Bot owner only)")
            {
                Options = new[]
                    {
                        new ApplicationCommandOptionProperties(ApplicationCommandOptionType.String, "message", "The announcement message")
                        {
                            Required = true
                        },
                        new ApplicationCommandOptionProperties(ApplicationCommandOptionType.Boolean, "ping", "Whether to ping the configured role (true/false)")
                        {
                        Required = false
                    }
                }
            },
            new SlashCommandProperties("disableglobal", "Disable global announcements for this guild (Admin only)"),
            new SlashCommandProperties("globalstatus", "Check the status of global announcements for this guild (Admin only)"),
            new SlashCommandProperties("allglobalchannels", "View all configured global announcement channels (Bot owner only)"),

            new SlashCommandProperties("ignoreuser", "Add a user to the ignore list (Admin only, specific channel)")
            {
                Options = new[]
                {
                    new ApplicationCommandOptionProperties(ApplicationCommandOptionType.User, "user", "The user to ignore")
                    {
                        Required = true
                    }
                }
            },
            new SlashCommandProperties("unignoreuser", "Remove a user from the ignore list (Admin only, specific channel)")
            {
                Options = new[]
                {
                    new ApplicationCommandOptionProperties(ApplicationCommandOptionType.User, "user", "The user to unignore")
                    {
                        Required = true
                    }
                }
            },
            new SlashCommandProperties("listignored", "List all ignored users (Admin only, specific channel)"),
            new SlashCommandProperties("github", "The open source code!"),
            new SlashCommandProperties("terms", "the terms of service"),
            new SlashCommandProperties("privacy", "the privacy policy"),
            new SlashCommandProperties("cleanupremovedservers", "Check and cleanup data for removed servers (Bot owner only)")
        };

        await restClient.BulkOverwriteGlobalApplicationCommandsAsync(applicationId, commands);
        new Write().WriteLine($"{commands.Count} commands registered!");
        new Write().WriteLine("User data saved to: userdata/[userId].json");
        new Write().WriteLine("Suggestions saved to: suggestions/suggestion-[id].json");

        string commandsstring = "";
        foreach (var command in commands)
        {
            commandsstring += $"/{command.Name}, ";
        }
        new Write().WriteLine($"Commands: {commandsstring.TrimEnd(',', ' ')}");

        var startupAnnouncement = new StartupAnnouncement(restClient);
        await startupAnnouncement.SendStartupAnnouncementAsync();
    }
    catch (Exception ex)
    {
        new Write().WriteLine($"Error: {ex.Message}");
        new Write().WriteLine($"Stack trace: {ex.StackTrace}");
    }
};

Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    new Write().WriteLine("\nBot is shutting down...");
    new Write().WriteLine("Goodbye!");
    Environment.Exit(0);
};

AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
{
    new Write().WriteLine("Bot is shutting down...");
    new Write().WriteLine("Goodbye!");
};

new Write().WriteLine("Starting bot...");
await host.RunAsync();