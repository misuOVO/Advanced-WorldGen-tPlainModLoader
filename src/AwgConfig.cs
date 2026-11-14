using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using tContentPatch;

namespace AdvancedWorldGen
{
    /// <summary>
    /// 世界选项配置：直接存“启用了哪些官方种子选项”的 KeyName 列表。
    /// 这样 1.4.5 官方菜单里有多少项，这里就能显示/保存多少项。
    /// </summary>
    public class AwgConfig
    {
        public List<string> Enabled = new List<string>();

        // 十周年庆典的子选项（AdvancedWorldGen 专有）
        public bool CelebrationBeachSpawn;
        public bool CelebrationPainted;

        // 随机替换（AdvancedWorldGen 的 Random / Random.Painted）
        public bool RandomWorld;
        public bool RandomPainted;

        // 自定义世界大小（限制在原版最大 8400 x 2400 以内）
        public bool CustomSize;
        public int CustomWidth = 8400;
        public int CustomHeight = 2400;

        // 原版世界生成配置覆盖：路径 -> 值字符串（如 Biomes.CaveHouseBiome.IceChestChance）
        public Dictionary<string, string> WorldGenOverrides = new Dictionary<string, string>();

        // ---- 旧版本字段（仅用于迁移，不再显示） ----
        public bool Drunk;
        public bool NotTheBees;
        public bool NoTraps;
        public bool ForTheWorthy;
        public bool Anniversary;
        public bool DontStarve;
        public bool Remix;
        public bool Everything;
        public bool Skyblock;

        private static string PathFile
        {
            get
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods", "AdvancedWorldGen");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, "config.json");
            }
        }

        public static AwgConfig Instance { get; set; } = new AwgConfig();

        public static void ResetToDefault()
        {
            Instance = new AwgConfig();
        }

        public bool Has(string key)
        {
            return key != null && Enabled != null && Enabled.Contains(key);
        }

        public void Set(string key, bool on)
        {
            if (key == null)
                return;

            Enabled = Enabled ?? new List<string>();

            if (on)
            {
                if (!Enabled.Contains(key))
                    Enabled.Add(key);
            }
            else
            {
                Enabled.Remove(key);
            }
        }

        public static void Load()
        {
            try
            {
                string path = PathFile;
                if (File.Exists(path))
                {
                    AwgConfig loaded = JsonConvert.DeserializeObject<AwgConfig>(File.ReadAllText(path));
                    if (loaded != null)
                        Instance = loaded;
                }

                Instance.MigrateLegacy();
                Instance.Enabled = Instance.Enabled ?? new List<string>();
                Instance.WorldGenOverrides = Instance.WorldGenOverrides
                    ?? new Dictionary<string, string>();

                if (!File.Exists(path))
                    Save();

                Log.Write("AWG 配置已加载: " + path + " (" + Instance.Enabled.Count + " 项)");
            }
            catch (Exception e)
            {
                Log.Write("AWG 配置加载失败，使用默认值: " + e.Message);
                Instance = new AwgConfig();
            }
        }

        private void MigrateLegacy()
        {
            Enabled = Enabled ?? new List<string>();

            void Add(string key, bool on)
            {
                if (on && !Enabled.Contains(key))
                    Enabled.Add(key);
            }

            Add("Drunk", Drunk);
            Add("NotTheBees", NotTheBees);
            Add("NoTraps", NoTraps);
            Add("ForTheWorthy", ForTheWorthy);
            Add("Anniversary", Anniversary);
            Add("DontStarve", DontStarve);
            Add("Remix", Remix);
            Add("Everything", Everything);
            Add("Skyblock", Skyblock);
        }

        public static void Save()
        {
            try
            {
                File.WriteAllText(PathFile, JsonConvert.SerializeObject(Instance, Formatting.Indented));
            }
            catch (Exception e)
            {
                Log.Write("AWG 配置保存失败: " + e.Message);
            }
        }
    }
}
