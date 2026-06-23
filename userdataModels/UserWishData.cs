// Copyright (c) 2026 Cresclent. All rights reserved.
// This Discord bot code is view-only. Hosting or running this bot is strictly prohibited!
using System;
using System.Collections.Generic;

namespace discord_bot.userdataModels
{
    public class UserWishData
    {
        public int Pity { get; set; } = 0;
        public int FourStarPity { get; set; } = 0;
        public bool IsGuaranteed { get; set; } = false;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public int TotalPulls { get; set; } = 0;
        public int FiveStarCount { get; set; } = 0;
        public int FourStarCount { get; set; } = 0;
        public int ThreeStarCount { get; set; } = 0;

        // Inventory: item name -> count
        public Dictionary<string, int> Inventory { get; set; } = new Dictionary<string, int>();
    }
}