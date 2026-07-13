// Copyright (c) 2026 Cresclent. All rights reserved.
// This Discord bot code is view-only. Hosting or running this bot is strictly prohibited!
using discord_bot.Tools;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using System.Collections.Concurrent;
using System.Text.Json;

namespace discord_bot.Services
{
    public class VoteService
    {
        private readonly GatewayClient _client;
        private readonly RestClient _rest;
        private readonly string[] _fiveStarCharacters;
        private readonly Random _random;
        private readonly ConcurrentDictionary<ulong, VoteSession> _activeVotes = new();
        private readonly ConcurrentDictionary<ulong, DateTime> _voteCooldowns = new();
        private readonly int _cooldownMinutes = 60;
        private readonly int _voteDurationMinutes = 5;
        private string _currentBanner;
        private readonly object _bannerLock = new();
        private static readonly Dictionary<ulong, VoteChannelConfig> _voteChannels = new();
        private static readonly object _configLock = new();

        public VoteService(GatewayClient client, RestClient rest, string[] fiveStarCharacters, string initialBanner)
        {
            _client = client;
            _rest = rest;
            _fiveStarCharacters = fiveStarCharacters;
            _random = new Random();
            _currentBanner = initialBanner;
            SetupEventHandlers();
            LoadVoteChannels();
        }

        public void ReloadConfig()
        {
            lock (_configLock)
            {
                _voteChannels.Clear();
                LoadVoteChannels();
                new Write().WriteLine($"VoteService: Config reloaded. Found {_voteChannels.Count} vote channels.");
            }
        }

        private void SetupEventHandlers()
        {
            _client.MessageReactionAdd += async (MessageReactionAddEventArgs args) =>
            {
                var voteSession = _activeVotes.Values.FirstOrDefault(v =>
                    v.MessageId == args.MessageId || v.CrossServerMessageIds.Values.Contains(args.MessageId));

                if (voteSession == null || !voteSession.IsActive) return;

                await HandleVoteReaction(args);
            };

            _client.MessageReactionRemove += async (MessageReactionRemoveEventArgs args) =>
            {
                var voteSession = _activeVotes.Values.FirstOrDefault(v =>
                    v.MessageId == args.MessageId || v.CrossServerMessageIds.Values.Contains(args.MessageId));

                if (voteSession == null || !voteSession.IsActive) return;

                if (voteSession.Upvotes.Remove(args.UserId)) { }
                if (voteSession.Downvotes.Remove(args.UserId)) { }

                await UpdateAllVoteStatuses(voteSession);
            };
        }

        private void LoadVoteChannels()
        {
            lock (_configLock)
            {
                try
                {
                    string path = Path.Combine(AppContext.BaseDirectory, "voteconfig.json");
                    if (File.Exists(path))
                    {
                        string json = File.ReadAllText(path);
                        var configs = JsonSerializer.Deserialize<Dictionary<ulong, VoteChannelConfig>>(json);
                        if (configs != null)
                        {
                            foreach (var kvp in configs)
                            {
                                _voteChannels[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading vote channels: {ex.Message}");
                }
            }
        }

        private void SaveVoteChannels()
        {
            lock (_configLock)
            {
                try
                {
                    string path = Path.Combine(AppContext.BaseDirectory, "voteconfig.json");
                    string json = JsonSerializer.Serialize(_voteChannels, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(path, json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving vote channels: {ex.Message}");
                }
            }
        }

        public void SetVoteChannel(ulong guildId, ulong channelId, ulong? roleId = null)
        {
            lock (_configLock)
            {
                _voteChannels[guildId] = new VoteChannelConfig
                {
                    ChannelId = channelId,
                    RoleId = roleId
                };
                SaveVoteChannels();
            }
        }

        public void DisableVoteChannel(ulong guildId)
        {
            lock (_configLock)
            {
                _voteChannels.Remove(guildId);
                SaveVoteChannels();
            }
        }

        public VoteChannelConfig? GetVoteChannel(ulong guildId)
        {
            lock (_configLock)
            {
                return _voteChannels.TryGetValue(guildId, out var config) ? config : null;
            }
        }

        public string GetVoteChannelStatus(ulong guildId)
        {
            lock (_configLock)
            {
                if (_voteChannels.TryGetValue(guildId, out var config))
                {
                    string roleText = config.RoleId.HasValue ? $"<@&{config.RoleId}>" : "No role set";
                    return $"📢 **Vote Channel:** <#{config.ChannelId}>\n" +
                           $"👥 **Role:** {roleText}";
                }
                return "❌ No vote channel configured for this server.";
            }
        }

        public string GetAllVoteChannels()
        {
            lock (_configLock)
            {
                if (_voteChannels.Count == 0)
                    return "📭 No vote channels configured.";

                var result = "📋 **Configured Vote Channels:**\n\n";
                foreach (var kvp in _voteChannels)
                {
                    string roleText = kvp.Value.RoleId.HasValue ? $"<@&{kvp.Value.RoleId}>" : "None";
                    result += $"**Guild ID:** {kvp.Key}\n";
                    result += $"**Channel:** <#{kvp.Value.ChannelId}>\n";
                    result += $"**Role:** {roleText}\n\n";
                }
                return result;
            }
        }

        public string GetCurrentBanner()
        {
            lock (_bannerLock)
            {
                return _currentBanner;
            }
        }

        public void RerollBanner()
        {
            lock (_bannerLock)
            {
                string newBanner;
                do
                {
                    newBanner = _fiveStarCharacters[_random.Next(_fiveStarCharacters.Length)];
                } while (newBanner == _currentBanner && _fiveStarCharacters.Length > 1);
                _currentBanner = newBanner;
            }
        }

        public async Task<string> StartRerollVote(SlashCommandInteraction interaction)
        {
            try
            {
                var guildId = interaction.GuildId!.Value;
                var config = GetVoteChannel(guildId);

                if (config == null)
                {
                    return "❌ No vote channel configured for this server! Use `/setvotechannel` first.";
                }

                if (_activeVotes.ContainsKey(config.ChannelId))
                {
                    return $"⚠️ There's already an active vote in <#{config.ChannelId}>!";
                }

                if (IsOnCooldown(interaction.User.Id) && interaction.User.Id != 1157243448093573120)
                {
                    var timeLeft = GetCooldownTimeRemaining(interaction.User.Id);
                    return $"⏳ Please wait **{timeLeft}** before starting another vote!";
                }

                var endTime = DateTimeOffset.UtcNow.AddMinutes(_voteDurationMinutes);

                var voteSession = new VoteSession
                {
                    StartTime = DateTime.UtcNow,
                    EndTime = endTime,
                    Upvotes = new HashSet<ulong>(),
                    Downvotes = new HashSet<ulong>(),
                    ChannelId = config.ChannelId,
                    InitiatorId = interaction.User.Id,
                    GuildId = guildId,
                    IsActive = true,
                    CurrentBanner = _currentBanner,
                    RoleId = config.RoleId,
                    IsCrossServer = true
                };

                _activeVotes[config.ChannelId] = voteSession;
                SetCooldown(interaction.User.Id);

                string roleMention = config.RoleId.HasValue ? $"<@&{config.RoleId}> " : "";
                string voteMessage = $"🌍 **GLOBAL Banner Reroll Vote** {roleMention}\n\n" +
                                    $"Should we reroll the banner?\n" +
                                    $"Current banner: **{_currentBanner}**\n\n" +
                                    $"React with ✅ to **upvote** (reroll)\n" +
                                    $"React with ❌ to **downvote** (keep)\n\n" +
                                    $"Vote ends <t:{endTime.ToUnixTimeSeconds()}:R> (<t:{endTime.ToUnixTimeSeconds()}:T>)\n\n" +
                                    $"**Status:** 🟡 Vote in progress";

                var message = await _rest.SendMessageAsync(config.ChannelId, voteMessage);
                await message.AddReactionAsync(new ReactionEmojiProperties("✅"));
                await message.AddReactionAsync(new ReactionEmojiProperties("❌"));
                voteSession.MessageId = message.Id;

                List<ulong> otherChannels = new();
                lock (_configLock)
                {
                    foreach (var kvp in _voteChannels)
                    {
                        if (kvp.Value.ChannelId != config.ChannelId)
                        {
                            otherChannels.Add(kvp.Value.ChannelId);
                        }
                    }
                }

                foreach (var channelId in otherChannels)
                {
                    try
                    {
                        var otherConfig = GetVoteChannelByChannelId(channelId);
                        string otherRoleMention = otherConfig?.RoleId.HasValue == true ? $"<@&{otherConfig.RoleId}> " : "";
                        string crossMessage = $"🌍 **GLOBAL Banner Reroll Vote** {otherRoleMention}\n\n" +
                                             $"Should we reroll the banner?\n" +
                                             $"Current banner: **{_currentBanner}**\n\n" +
                                             $"React with ✅ to **upvote** (reroll)\n" +
                                             $"React with ❌ to **downvote** (keep)\n\n" +
                                             $"Vote ends <t:{endTime.ToUnixTimeSeconds()}:R> (<t:{endTime.ToUnixTimeSeconds()}:T>)\n\n" +
                                             $"**Status:** 🟡 Vote in progress";

                        var crossMessageObj = await _rest.SendMessageAsync(channelId, crossMessage);
                        await crossMessageObj.AddReactionAsync(new ReactionEmojiProperties("✅"));
                        await crossMessageObj.AddReactionAsync(new ReactionEmojiProperties("❌"));

                        voteSession.CrossServerMessageIds[channelId] = crossMessageObj.Id;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending vote to channel {channelId}: {ex.Message}");
                    }
                }

                _ = Task.Run(async () => await EndVoteAfterTimeout(voteSession));

                return $"✅ Vote started in <#{config.ChannelId}> and all configured channels! React with ✅ to upvote or ❌ to downvote. Vote ends <t:{endTime.ToUnixTimeSeconds()}:R>.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting vote: {ex.Message}");
                return $"❌ Error starting vote: {ex.Message}";
            }
        }

        private VoteChannelConfig? GetVoteChannelByChannelId(ulong channelId)
        {
            lock (_configLock)
            {
                return _voteChannels.Values.FirstOrDefault(c => c.ChannelId == channelId);
            }
        }

        private async Task HandleVoteReaction(MessageReactionAddEventArgs args)
        {
            try
            {
                if (args.UserId == _client.Cache.User.Id) return;

                var voteSession = _activeVotes.Values.FirstOrDefault(v =>
                    v.MessageId == args.MessageId || v.CrossServerMessageIds.Values.Contains(args.MessageId));

                if (voteSession == null || !voteSession.IsActive) return;

                if (args.Emoji.Name == "✅")
                {
                    if (voteSession.Downvotes.Remove(args.UserId)) { }
                    voteSession.Upvotes.Add(args.UserId);
                }
                else if (args.Emoji.Name == "❌")
                {
                    if (voteSession.Upvotes.Remove(args.UserId)) { }
                    voteSession.Downvotes.Add(args.UserId);
                }
                else
                {
                    return;
                }

                await UpdateAllVoteStatuses(voteSession);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling vote reaction: {ex.Message}");
            }
        }

        private async Task UpdateVoteStatus(VoteSession session)
        {
            int netVotes = session.Upvotes.Count - session.Downvotes.Count;
            string status = "🟡 In progress";

            string statusMessage = $"🌍 **GLOBAL Banner Reroll Vote**\n\n" +
                                  $"✅ Upvotes: **{session.Upvotes.Count}**\n" +
                                  $"❌ Downvotes: **{session.Downvotes.Count}**\n" +
                                  $"Net votes: **{netVotes}**\n" +
                                  $"Vote ends <t:{session.EndTime.ToUnixTimeSeconds()}:R>\n" +
                                  $"Current banner: **{session.CurrentBanner}**\n\n" +
                                  $"**Status:** {status}\n\n" +
                                  $"Vote passes if **upvotes > downvotes** at the end";

            var message = await _rest.GetMessageAsync(session.ChannelId, session.MessageId);
            await message.ModifyAsync(options => options.Content = statusMessage);
        }

        private async Task UpdateAllVoteStatuses(VoteSession session)
        {
            try
            {
                int netVotes = session.Upvotes.Count - session.Downvotes.Count;
                string status = "🟡 In progress";

                string statusMessage = $"🌍 **GLOBAL Banner Reroll Vote**\n\n" +
                                      $"✅ Upvotes: **{session.Upvotes.Count}**\n" +
                                      $"❌ Downvotes: **{session.Downvotes.Count}**\n" +
                                      $"Net votes: **{netVotes}**\n" +
                                      $"Vote ends <t:{session.EndTime.ToUnixTimeSeconds()}:R>\n" +
                                      $"Current banner: **{session.CurrentBanner}**\n\n" +
                                      $"**Status:** {status}\n\n" +
                                      $"Vote passes if **upvotes > downvotes** at the end";

                var mainMessage = await _rest.GetMessageAsync(session.ChannelId, session.MessageId);
                await mainMessage.ModifyAsync(options => options.Content = statusMessage);

                foreach (var kvp in session.CrossServerMessageIds)
                {
                    try
                    {
                        var crossMessage = await _rest.GetMessageAsync(kvp.Key, kvp.Value);
                        await crossMessage.ModifyAsync(options => options.Content = statusMessage);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error updating cross-server message in channel {kvp.Key}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating vote statuses: {ex.Message}");
            }
        }

        private async Task EndVoteAfterTimeout(VoteSession session)
        {
            await Task.Delay(TimeSpan.FromMinutes(_voteDurationMinutes));

            if (_activeVotes.ContainsKey(session.ChannelId))
            {
                await FinalizeVote(session);
            }
        }

        private async Task FinalizeVote(VoteSession session)
        {
            try
            {
                if (!session.IsActive) return;
                session.IsActive = false;
                _activeVotes.TryRemove(session.ChannelId, out _);

                bool shouldReroll = session.Upvotes.Count > session.Downvotes.Count && session.Upvotes.Count >= 1;

                var logEntry = new VoteLogEntry
                {
                    ChannelId = session.ChannelId,
                    GuildId = session.GuildId ?? 0,
                    Timestamp = DateTime.UtcNow,
                    Upvotes = session.Upvotes.Count,
                    Downvotes = session.Downvotes.Count,
                    NetVotes = session.Upvotes.Count - session.Downvotes.Count,
                    Voters = session.Upvotes.Union(session.Downvotes).ToList(),
                    Passed = shouldReroll,
                    OldBanner = session.CurrentBanner,
                    NewBanner = shouldReroll ? "Rerolled" : session.CurrentBanner
                };

                string resultMessage;
                string newBanner = session.CurrentBanner;

                if (shouldReroll)
                {
                    RerollBanner();
                    newBanner = _currentBanner;
                    logEntry.NewBanner = newBanner;
                    resultMessage = $"🎉 **Banner Rerolled!**\n\n" +
                                   $"New banner: **{newBanner}**\n" +
                                   $"✅ Upvotes: {session.Upvotes.Count}\n" +
                                   $"❌ Downvotes: {session.Downvotes.Count}\n" +
                                   $"Net votes: {session.Upvotes.Count - session.Downvotes.Count}";
                }
                else
                {
                    resultMessage = $"❌ **Vote Failed**\n\n" +
                                   $"The banner remains: **{session.CurrentBanner}**\n" +
                                   $"✅ Upvotes: {session.Upvotes.Count}\n" +
                                   $"❌ Downvotes: {session.Downvotes.Count}\n" +
                                   $"Net votes: {session.Upvotes.Count - session.Downvotes.Count}\n\n" +
                                   $"Vote passes if **upvotes > downvotes**";
                }

                SaveVoteLog(logEntry);

                var mainMessage = await _rest.GetMessageAsync(session.ChannelId, session.MessageId);
                await mainMessage.ModifyAsync(options => options.Content = resultMessage);

                foreach (var kvp in session.CrossServerMessageIds)
                {
                    try
                    {
                        var crossMessage = await _rest.GetMessageAsync(kvp.Key, kvp.Value);
                        await crossMessage.ModifyAsync(options => options.Content = resultMessage);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error updating cross-server final message in channel {kvp.Key}: {ex.Message}");
                    }
                }

                string roleMention = session.RoleId.HasValue ? $"<@&{session.RoleId}> " : "";
                string announcement = shouldReroll ?
                    $"{roleMention}🌍 **The global banner has been rerolled to: {newBanner}**! Use `/banner` to see it!" :
                    $"{roleMention}🌍 **The global banner vote failed!** The banner remains: **{session.CurrentBanner}**";

                await _rest.SendMessageAsync(session.ChannelId, announcement);

                foreach (var kvp in session.CrossServerMessageIds)
                {
                    try
                    {
                        await _rest.SendMessageAsync(kvp.Key, announcement);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending cross-server announcement to channel {kvp.Key}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error finalizing vote: {ex.Message}");
            }
        }

        private bool IsOnCooldown(ulong userId)
        {
            if (_voteCooldowns.TryGetValue(userId, out var cooldownEnd))
            {
                return DateTime.UtcNow < cooldownEnd;
            }
            return false;
        }

        private void SetCooldown(ulong userId)
        {
            _voteCooldowns[userId] = DateTime.UtcNow.AddMinutes(_cooldownMinutes);
        }

        private string GetCooldownTimeRemaining(ulong userId)
        {
            if (_voteCooldowns.TryGetValue(userId, out var cooldownEnd))
            {
                var remaining = cooldownEnd - DateTime.UtcNow;
                if (remaining > TimeSpan.Zero)
                {
                    return $"{remaining.Minutes}m {remaining.Seconds}s";
                }
            }
            return "0m 0s";
        }

        public void ClearCooldown(ulong userId)
        {
            _voteCooldowns.TryRemove(userId, out _);
        }

        public void ClearAllCooldowns()
        {
            _voteCooldowns.Clear();
        }

        private static string VoteDataPath => Path.Combine(AppContext.BaseDirectory, "votedata");
        private static readonly object _logLock = new();

        private void SaveVoteLog(VoteLogEntry entry)
        {
            lock (_logLock)
            {
                try
                {
                    if (!Directory.Exists(VoteDataPath))
                        Directory.CreateDirectory(VoteDataPath);

                    string fileName = $"vote_{entry.Timestamp:yyyyMMdd_HHmmss}.json";
                    string filePath = Path.Combine(VoteDataPath, fileName);
                    string json = JsonSerializer.Serialize(entry, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(filePath, json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving vote log: {ex.Message}");
                }
            }
        }

        public List<VoteLogEntry> GetVoteHistory(int count = 10)
        {
            var votes = new List<VoteLogEntry>();
            lock (_logLock)
            {
                try
                {
                    if (!Directory.Exists(VoteDataPath))
                        return votes;

                    var files = Directory.GetFiles(VoteDataPath, "vote_*.json")
                        .OrderByDescending(f => f)
                        .Take(count);

                    foreach (var file in files)
                    {
                        try
                        {
                            string json = File.ReadAllText(file);
                            var entry = JsonSerializer.Deserialize<VoteLogEntry>(json);
                            if (entry != null)
                                votes.Add(entry);
                        }
                        catch { }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading vote logs: {ex.Message}");
                }
            }
            return votes;
        }

        public bool IsVoteActive(ulong channelId)
        {
            return _activeVotes.ContainsKey(channelId);
        }

        public VoteSession? GetActiveVote(ulong channelId)
        {
            return _activeVotes.TryGetValue(channelId, out var session) ? session : null;
        }
    }

    public class VoteChannelConfig
    {
        public ulong ChannelId { get; set; }
        public ulong? RoleId { get; set; }
    }

    public class VoteSession
    {
        public DateTime StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public HashSet<ulong> Upvotes { get; set; } = new();
        public HashSet<ulong> Downvotes { get; set; } = new();
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
        public Dictionary<ulong, ulong> CrossServerMessageIds { get; set; } = new();
        public ulong? GuildId { get; set; }
        public ulong InitiatorId { get; set; }
        public bool IsActive { get; set; }
        public string CurrentBanner { get; set; } = string.Empty;
        public ulong? RoleId { get; set; }
        public bool IsCrossServer { get; set; } = true;
    }

    public class VoteLogEntry
    {
        public ulong ChannelId { get; set; }
        public ulong GuildId { get; set; }
        public DateTime Timestamp { get; set; }
        public int Upvotes { get; set; }
        public int Downvotes { get; set; }
        public int NetVotes { get; set; }
        public List<ulong> Voters { get; set; } = new();
        public bool Passed { get; set; }
        public string OldBanner { get; set; } = string.Empty;
        public string NewBanner { get; set; } = string.Empty;
    }
}