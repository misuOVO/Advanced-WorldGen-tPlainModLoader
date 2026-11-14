using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Terraria.WorldBuilding;

namespace AdvancedWorldGen
{
    /// <summary>
    /// 原版世界生成配置（Worldgen Config）读写。
    ///
    /// WorldGenConfiguration 内部是两份 JObject（Biomes / Passes），
    /// 生成时按需 ToObject / GetPassConfiguration，所以直接改 JObject 就能生效。
    /// </summary>
    internal static class WorldgenConfig
    {
        public sealed class Entry
        {
            public string Path;
            public string Group;
            public string Label;
            public JValue Value;
            public bool Overridden;
        }

        private const string Resource =
            "Terraria.GameContent.WorldBuilding.Configuration.json";

        private static readonly BindingFlags FieldFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        /// <summary>配置项 / 分组的中文名。</summary>
        private static readonly Dictionary<string, string> Names =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                // 分组
                { "Biomes", "群系参数" },
                { "Passes", "生成步骤参数" },

                // Biomes
                { "CaveHouseBiome", "洞穴小屋" },
                { "IceChestChance", "冰雪箱概率" },
                { "JungleChestChance", "丛林箱概率" },
                { "GoldChestChance", "金箱概率" },
                { "GraniteChestChance", "花岗岩箱概率" },
                { "MarbleChestChance", "大理石箱概率" },
                { "MushroomChestChance", "蘑菇箱概率" },
                { "DesertChestChance", "沙漠箱概率" },

                { "DesertBiome", "沙漠" },
                { "ChanceOfEntrance", "入口生成概率" },

                { "DunesBiome", "沙丘" },
                { "HeightScale", "高度倍率" },
                { "SingleDunesWidth", "单个沙丘宽度" },

                { "EnchantedSwordBiome", "附魔剑冢" },
                { "ChanceOfRealSword", "真剑概率" },

                // Passes
                { "Terrain", "地形" },
                { "FlatBeachPadding", "平坦海滩留白" },

                { "Dunes", "沙丘" },
                { "ChanceOfPyramid", "金字塔概率" },

                { "Marble", "大理石" },
                { "Granite", "花岗岩" },
                { "Dungeon", "地牢" },
                { "MinimumDistanceToBeach", "距海滩最小距离" },

                { "Buried Chests", "埋藏宝箱" },
                { "CaveHouseCount", "洞穴小屋数量" },
                { "CaveChestCount", "洞穴宝箱数量" },
                { "UnderworldChestCount", "地狱宝箱数量" },
                { "AdditionalDesertHouseCount", "额外沙漠小屋数量" },

                { "Micro Biomes", "微型群系" },
                { "DeadManChests", "死人宝箱数量" },
                { "SwordShrineAttempts", "剑冢尝试次数" },
                { "SwordShrinePlacementChance", "剑冢放置概率" },
                { "ThinIcePatchCount", "薄冰区数量" },
                { "CampsiteCount", "营地数量" },
                { "ExplosiveTrapCount", "爆炸陷阱数量" },
                { "LivingTreeCount", "生命树数量" },
                { "CorruptionPitCount", "腐化坑数量" },
                { "StandardTrackCount", "普通铁轨数量" },
                { "StandardTrackLength", "普通铁轨长度" },
                { "LongTrackCount", "长铁轨数量" },
                { "LongTrackLength", "长铁轨长度" },

                // Range 通用字段
                { "Count", "数量" },
                { "Min", "最小" },
                { "Max", "最大" },
                { "ScaleWith", "缩放方式" },
            };

        private static string NameOf(string segment)
        {
            if (Names.TryGetValue(segment, out string name))
                return name;

            return segment;
        }

        private static string MakeLabel(string path)
        {
            string[] parts = path.Split('.');
            if (parts.Length <= 1)
                return NameOf(path);

            List<string> names = new List<string>();
            for (int i = 1; i < parts.Length; i++)
                names.Add(NameOf(parts[i]));

            return string.Join(" · ", names);
        }

        public static WorldGenConfiguration LoadBase()
        {
            try
            {
                return WorldGenConfiguration.FromEmbeddedPath(Resource);
            }
            catch (Exception e)
            {
                Log.Write("AWG: 读取原版世界生成配置失败: " + e.Message);
                return null;
            }
        }

        private static JObject RootOf(WorldGenConfiguration config, string fieldName)
        {
            try
            {
                FieldInfo field = typeof(WorldGenConfiguration).GetField(fieldName, FieldFlags);
                return field != null ? field.GetValue(config) as JObject : null;
            }
            catch
            {
                return null;
            }
        }

        public static List<Entry> Collect(WorldGenConfiguration config)
        {
            List<Entry> entries = new List<Entry>();
            if (config == null)
                return entries;

            Walk(RootOf(config, "_biomeRoot"), "Biomes", "Biomes", entries);
            Walk(RootOf(config, "_passRoot"), "Passes", "Passes", entries);

            return entries;
        }

        private static void Walk(JToken token, string group, string path, List<Entry> entries)
        {
            if (token == null)
                return;

            switch (token.Type)
            {
                case JTokenType.Object:
                    foreach (JProperty prop in ((JObject)token).Properties())
                        Walk(prop.Value, group, path + "." + prop.Name, entries);
                    break;

                case JTokenType.Integer:
                case JTokenType.Float:
                case JTokenType.String:
                case JTokenType.Boolean:
                    entries.Add(new Entry
                    {
                        Path = path,
                        Group = group,
                        Label = MakeLabel(path),
                        Value = (JValue)token,
                        Overridden = HasOverride(path)
                    });
                    break;
            }
        }

        private static bool HasOverride(string path)
        {
            AwgConfig cfg = AwgConfig.Instance;
            return cfg != null
                && cfg.WorldGenOverrides != null
                && cfg.WorldGenOverrides.ContainsKey(path);
        }

        public static string Format(JValue value)
        {
            if (value == null || value.Value == null)
                return "";

            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static bool TryConvert(JTokenType type, string text, out object value)
        {
            value = null;

            try
            {
                switch (type)
                {
                    case JTokenType.Integer:
                        if (!long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l))
                            return false;
                        // 原版这些数值都非负，负数容易让生成直接崩
                        if (l < 0)
                            return false;
                        value = l;
                        return true;

                    case JTokenType.Float:
                        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                            return false;
                        if (d < 0d || double.IsNaN(d) || double.IsInfinity(d))
                            return false;
                        value = d;
                        return true;

                    case JTokenType.Boolean:
                        if (bool.TryParse(text, out bool b))
                        {
                            value = b;
                            return true;
                        }
                        if (text == "1" || text == "0")
                        {
                            value = text == "1";
                            return true;
                        }
                        return false;

                    default:
                        value = text;
                        return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool ValidateText(Entry entry, string text)
        {
            if (entry.Path.EndsWith(".ScaleWith", StringComparison.Ordinal))
            {
                return text == "None"
                    || text == "WorldWidth"
                    || text == "WorldArea";
            }

            return true;
        }

        public static bool SetValue(Entry entry, string text, out string error)
        {
            error = null;

            if (entry == null || entry.Value == null)
            {
                error = "无效的配置项";
                return false;
            }

            if (!ValidateText(entry, text))
            {
                error = "缩放方式可选：None / WorldWidth / WorldArea";
                return false;
            }

            if (!TryConvert(entry.Value.Type, text, out object value))
            {
                error = entry.Value.Type == JTokenType.Integer
                    ? "需要非负整数"
                    : entry.Value.Type == JTokenType.Float
                        ? "需要非负小数"
                        : "格式不正确";
                return false;
            }

            try
            {
                entry.Value.Value = value;
                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }

        private static JToken SelectToken(WorldGenConfiguration config, string path)
        {
            if (string.IsNullOrEmpty(path) || config == null)
                return null;

            int dot = path.IndexOf('.');
            if (dot <= 0)
                return null;

            string root = path.Substring(0, dot);
            string rest = path.Substring(dot + 1);

            JObject jObject = root.Equals("Biomes", StringComparison.OrdinalIgnoreCase)
                ? RootOf(config, "_biomeRoot")
                : root.Equals("Passes", StringComparison.OrdinalIgnoreCase)
                    ? RootOf(config, "_passRoot")
                    : null;

            return jObject != null ? jObject.SelectToken(rest) : null;
        }

        /// <summary>把配置里的覆盖值写进指定 WorldGenConfiguration。</summary>
        public static void ApplyOverrides(WorldGenConfiguration config)
        {
            try
            {
                AwgConfig cfg = AwgConfig.Instance;
                if (config == null || cfg == null
                    || cfg.WorldGenOverrides == null || cfg.WorldGenOverrides.Count == 0)
                    return;

                int applied = 0;

                foreach (KeyValuePair<string, string> kv in cfg.WorldGenOverrides)
                {
                    JToken token = SelectToken(config, kv.Key);
                    if (token is JValue value)
                    {
                        string text = kv.Value ?? "";
                        if (!ValidateText(new Entry { Path = kv.Key }, text))
                            continue;

                        if (TryConvert(value.Type, text, out object converted))
                        {
                            value.Value = converted;
                            applied++;
                        }
                    }
                }

                Log.Write("AWG: 原版世界生成配置覆盖 " + applied + " 项");
            }
            catch (Exception e)
            {
                Log.Write("AWG: 应用世界生成配置失败: " + e.Message);
            }
        }

        public static WorldGenConfiguration LoadWithOverrides()
        {
            WorldGenConfiguration config = LoadBase();
            ApplyOverrides(config);
            return config;
        }

        /// <summary>这一项是不是点击切换的枚举选项（目前只有缩放方式）。</summary>
        public static bool IsChoiceEntry(Entry entry)
        {
            return entry != null
                && entry.Path.EndsWith(".ScaleWith", StringComparison.Ordinal);
        }

        public static string DescribeChoice(string value)
        {
            if (value == "WorldWidth")
                return "按世界宽度";

            if (value == "WorldArea")
                return "按世界面积";

            if (value == "None")
                return "不缩放";

            return value;
        }

        /// <summary>读取某一项的原版默认值（不含该路径自身的覆盖）。</summary>
        public static JValue DefaultValue(string path)
        {
            WorldGenConfiguration config = LoadBase();
            return SelectToken(config, path) as JValue;
        }
    }
}
