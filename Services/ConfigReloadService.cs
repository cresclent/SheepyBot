// Copyright (c) 2026 Cresclent. All rights reserved.
// This Discord bot code is view-only. Hosting or running this bot is strictly prohibited!
using discord_bot.Tools;
using System;
using System.Collections.Generic;
using System.Timers;

namespace discord_bot.Services
{
    public class ConfigReloadService : IDisposable
    {
        private readonly System.Timers.Timer _timer;
        private readonly Dictionary<string, ReloadActionEntry> _reloadActions = new();
        private readonly object _lock = new();
        private bool _isDisposed = false;
        private DateTime _lastReloadTime;

        private class ReloadActionEntry
        {
            public Action Action { get; set; }
            public string Name { get; set; }
            public DateTime LastRun { get; set; }
            public int SuccessCount { get; set; }
            public int ErrorCount { get; set; }
            public string LastError { get; set; }
        }

        public ConfigReloadService() : this(15)
        {
        }

        public ConfigReloadService(int intervalMinutes)
        {
            _timer = new System.Timers.Timer(intervalMinutes * 60 * 1000);
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
            _timer.Enabled = true;
            _lastReloadTime = DateTime.Now;

            new Write().WriteLine($"ConfigReloadService: Started with {intervalMinutes} minute interval");
        }

        private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            ReloadAll();
        }

        public void RegisterReloadAction(string name, Action reloadAction)
        {
            lock (_lock)
            {
                if (_reloadActions.ContainsKey(name))
                {
                    new Write().WriteLine($"ConfigReloadService: Warning - Overwriting existing reload action '{name}'");
                }

                _reloadActions[name] = new ReloadActionEntry
                {
                    Action = reloadAction,
                    Name = name,
                    LastRun = DateTime.MinValue,
                    SuccessCount = 0,
                    ErrorCount = 0,
                    LastError = null
                };

                new Write().WriteLine($"ConfigReloadService: Registered reload action '{name}'");
            }
        }

        public bool UnregisterReloadAction(string name)
        {
            lock (_lock)
            {
                return _reloadActions.Remove(name);
            }
        }

        public void ReloadAll()
        {
            lock (_lock)
            {
                _lastReloadTime = DateTime.Now;
                new Write().WriteLine($"ConfigReloadService: Starting reload cycle at {_lastReloadTime} ({_reloadActions.Count} actions)");

                foreach (var kvp in _reloadActions)
                {
                    var action = kvp.Value;
                    try
                    {
                        action.Action();
                        action.LastRun = DateTime.Now;
                        action.SuccessCount++;
                        new Write().WriteLine($"ConfigReloadService: ✓ '{action.Name}' reloaded successfully");
                    }
                    catch (Exception ex)
                    {
                        action.ErrorCount++;
                        action.LastError = ex.Message;
                        new Write().WriteLine($"ConfigReloadService: ✗ '{action.Name}' failed: {ex.Message}");
                    }
                }

                new Write().WriteLine($"ConfigReloadService: Reload cycle completed");
            }
        }

        public bool ReloadAction(string name)
        {
            lock (_lock)
            {
                if (_reloadActions.TryGetValue(name, out var action))
                {
                    try
                    {
                        action.Action();
                        action.LastRun = DateTime.Now;
                        action.SuccessCount++;
                        new Write().WriteLine($"ConfigReloadService: Manually reloaded '{name}'");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        action.ErrorCount++;
                        action.LastError = ex.Message;
                        new Write().WriteLine($"ConfigReloadService: Manual reload of '{name}' failed: {ex.Message}");
                        return false;
                    }
                }
                else
                {
                    new Write().WriteLine($"ConfigReloadService: No reload action found with name '{name}'");
                    return false;
                }
            }
        }

        public string GetStatus()
        {
            lock (_lock)
            {
                if (_reloadActions.Count == 0)
                    return "No reload actions registered.";

                var result = $"**Config Reload Service Status**\n\n";
                result += $"Last Reload: {_lastReloadTime:yyyy-MM-dd HH:mm:ss}\n";
                result += $"Total Actions: {_reloadActions.Count}\n\n";

                foreach (var kvp in _reloadActions)
                {
                    var action = kvp.Value;
                    result += $"**{action.Name}**\n";
                    result += $"  Successes: {action.SuccessCount}\n";
                    result += $"  Errors: {action.ErrorCount}\n";
                    result += $"  Last Run: {(action.LastRun > DateTime.MinValue ? action.LastRun.ToString("yyyy-MM-dd HH:mm:ss") : "Never")}\n";
                    if (!string.IsNullOrEmpty(action.LastError))
                        result += $"  Last Error: {action.LastError}\n";
                    result += "\n";
                }

                return result;
            }
        }

        public string GetSummaryStatus()
        {
            lock (_lock)
            {
                if (_reloadActions.Count == 0)
                    return "No reload actions registered.";

                int totalSuccess = 0;
                int totalErrors = 0;
                foreach (var action in _reloadActions.Values)
                {
                    totalSuccess += action.SuccessCount;
                    totalErrors += action.ErrorCount;
                }

                return $"Config Reload Service | Actions: {_reloadActions.Count} | Successes: {totalSuccess} | Errors: {totalErrors} | Last: {_lastReloadTime:HH:mm:ss}";
            }
        }

        public DateTime GetLastReloadTime() => _lastReloadTime;

        public int ActionCount
        {
            get
            {
                lock (_lock) { return _reloadActions.Count; }
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            lock (_lock)
            {
                _isDisposed = true;
                _timer.Stop();
                _timer.Dispose();
                _reloadActions.Clear();
                new Write().WriteLine("ConfigReloadService: Disposed");
            }
        }
    }
}