using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.WorldBuilding;

namespace AdvancedWorldGen
{
    internal sealed class SeedEntry
    {
        public string Key;
        public string Title;
        public string Hint;
        public string Description;
        public int FallbackIcon;
        public bool IsNormal;
        public AWorldGenerationOption Option;
    }

    /// <summary>
    /// 直接枚举原版 WorldGenerationOptions.Options —— 1.4.5 官方菜单有哪些种子选项，
    /// 这里就有哪些，不需要硬编码。
    /// </summary>
    internal static class SeedCatalog
    {
        private static List<SeedEntry> _cache;

        /// <summary>
        /// 官方中文版里这些选项名仍然是英文，所以这里用中文社区/维基的通行叫法。
        /// </summary>
        private static readonly Dictionary<string, string> ChineseNames =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Normal", "常规世界" },
                { "Drunk", "醉酒世界" },
                { "NotTheBees", "不是蜜蜂" },
                { "Anniversary", "十周年庆典" },
                { "DontStarve", "永恒领域（饥荒联动）" },
                { "ForTheWorthy", "献给配得上的人" },
                { "NoTraps", "没有陷阱" },
                { "Remix", "颠倒世界" },
                { "Everything", "天顶世界" },
                { "Skyblock", "空岛" }
            };

        /// <summary>
        /// 官方中文版里特殊种子的 Description 在部分版本为空，这里按官方中文 Wiki 补齐。
        /// </summary>
        private static readonly Dictionary<string, string> ChineseDescriptions =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Normal", "欢迎来到泰拉瑞亚！以原版泰拉瑞亚体验和游戏玩法开始你的冒险之旅。" },
                { "NotTheBees", "进入一片弥漫着甜腻蜂香的大陆，在这里嗡嗡振翅声和有毒蛰刺的威胁潜伏在每一个角落！（防蜂头盔需自备）" },
                { "Drunk", "猩红加上腐化？二者一起出现？简直疯狂。在这片大陆上，世界生成过程已经彻底失控，带来了一场绝无仅有的泰拉瑞亚冒险！回家吧，世界生成过程，你喝醉了！" },
                { "Anniversary", "泰拉瑞亚十周年！在这个多姿多彩的奇幻世界中一起庆祝——谁知道呢，也许团队留下了一些礼物等你拆开。" },
                { "DontStarve", "将《饥荒》冒险融入到泰拉瑞亚中，你能否在战胜持续饥饿的同时找到泰拉瑞亚的拯救之道？哦，还有别忘了待在光明处！" },
                { "ForTheWorthy", "你够格吗？这是一个毫不留情的世界。忘掉过去的一切，磨炼你的本领，否则等待你的只有审视、衡量与淘汰……" },
                { "NoTraps", "哇哦，陷阱。你讨厌它们，对吧？我也讨厌！这个世界肯定已经清除了所有陷阱……大概吧。也许。" },
                { "Remix", "一片上下颠倒的大陆。在这场乾坤倒转的冒险中让泰拉瑞亚发生翻天覆地的变化。你是否有足够的勇气去探索上界？" },
                { "Everything", "For the Worthy 还不够？所有其他世界种子的主要特色，再加上一些全新的惊喜，这个世界只为击垮你而存在。准备受死吧，伙计！" },
                { "Skyblock", "踏入这片悬浮于天际的地域。初始资源微薄，你需白手起家，逐步构建属于你的世界。" }
            };

        public static List<SeedEntry> All()
        {
            if (_cache != null)
                return _cache;

            List<SeedEntry> list = new List<SeedEntry>();

            try
            {
                foreach (AWorldGenerationOption option in WorldGenerationOptions.Options)
                {
                    if (option == null)
                        continue;

                    string key = SafeKey(option);
                    string title = SafeTitle(option, key);
                    string hint = SafeHint(option, key);

                    list.Add(new SeedEntry
                    {
                        Key = key,
                        Title = title,
                        Hint = hint,
                        Description = SafeDescription(option, key),
                        FallbackIcon = FallbackIconOf(key),
                        IsNormal = key.Equals("Normal", StringComparison.OrdinalIgnoreCase),
                        Option = option
                    });
                }
            }
            catch (Exception e)
            {
                Log.Write("AWG: 枚举世界选项失败: " + e.Message);
            }

            // “普通世界”放到最后
            list = list.OrderBy(e => e.IsNormal ? 1 : 0).ToList();

            // 如果此时原版选项还没注册好（空列表），不要缓存，下次再取
            if (list.Count > 0)
                _cache = list;

            Log.Write("AWG: 官方世界选项 " + list.Count + " 项: "
                + string.Join(", ", list.Select(e => e.Key)));

            return list;
        }

        public static SeedEntry Normal()
        {
            return All().FirstOrDefault(e => e.IsNormal);
        }

        private static string SafeKey(AWorldGenerationOption option)
        {
            // KeyName 是 protected，改用类型名（WorldSeedOption_Drunk -> Drunk），
            // 和旧配置里的键完全一致。
            return option.GetType().Name.Replace("WorldSeedOption_", "");
        }

        private static int FallbackIconOf(string key)
        {
            switch ((key ?? "").ToLowerInvariant())
            {
                case "normal": return 9;          // 木材
                case "notthebees": return 1125;   // 蜂蜜块
                case "drunk": return 353;         // 麦芽酒
                case "anniversary": return 1000;  // 彩纸枪
                case "dontstarve": return 8;      // 火把
                case "fortheworthy": return 367;  // 神锤
                case "notraps": return 539;       // 飞镖陷阱
                case "remix": return 4819;        // 恶魔海螺
                case "everything": return 4956;   // 天顶剑
                case "skyblock": return 751;      // 云
                default: return -1;
            }
        }

        private static string SafeTitle(AWorldGenerationOption option, string key)
        {
            if (!string.IsNullOrEmpty(key) && ChineseNames.TryGetValue(key, out string zh))
                return zh;

            try
            {
                string title = option.Title != null ? option.Title.Value : null;
                if (!string.IsNullOrEmpty(title))
                    return title;
            }
            catch
            {
            }

            return key;
        }

        private static string SafeHint(AWorldGenerationOption option, string key)        {
            try
            {
                string[] names = option.SpecialSeedNames;
                if (names != null && names.Length > 0 && !string.IsNullOrEmpty(names[0]))
                    return names[0];
            }
            catch
            {
            }

            return key;
        }

        private static string SafeDescription(AWorldGenerationOption option, string key)        {
            if (!string.IsNullOrEmpty(key) && ChineseDescriptions.TryGetValue(key, out string zh))
                return zh;

            try
            {
                if (option.Description != null && !string.IsNullOrEmpty(option.Description.Value))
                    return option.Description.Value;
            }
            catch
            {
            }

            return string.Empty;
        }
    }
}
