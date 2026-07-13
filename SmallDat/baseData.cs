using System.Linq;
namespace discord_bot.SmallDat
{
    public static class baseData
    {
        public static string[] fiveStarCharacters = new[]
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

        public static string[] fourStarCharacters = new[]
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

        public static string[] threeStarItems = new[]
        {
    "Debate Club", "Harbinger of Dawn", "Skyrider Sword",
    "Ferrous Shadow", "Cool Steel", "Bloodtainted Greatsword",
    "Emerald Orb", "Black Tassel", "Magic Guide"
};

        public static string[] standardFiveStars = new[]
        {
    "Tighnari", "Jean", "Mona", "Dehya", "Diluc", "Keqing", "Qiqi", "Yumemizuki Mizuki"
};
        public static Dictionary<int, int> pityRates = new()
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

        public static string[] statuses = new string[]
{
    "Genshin pulling for Cresclent",
    "I am a Sheepy Boi! I am a Sheepy Boi! I am a Sheepy Sheepy Sheepy Sheepy Boi!",
    "oooh mysterious!",
    "HI!",
    "MINECRAFT",
    "Genshin Impact",
    "Honkai: Star Rail",
    "I dunno. Ask Cresclent!",
    "Watching youtube!",
    "Streaming!",
    "Screaming!",
    ""
};

    }
}
