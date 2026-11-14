using System;
using Terraria.WorldBuilding;

namespace AdvancedWorldGen
{
    /// <summary>
    /// 把配置里勾选的官方种子选项写进 WorldGenerationOptions。
    /// 原版 WorldGen.Reset() 会读取这些 Enabled 值。
    /// </summary>
    internal static class SeedOptions
    {
        public static void Apply(AwgConfig cfg)
        {
            try
            {
                if (cfg == null || cfg.Enabled == null || cfg.Enabled.Count == 0)
                {
                    Log.Write("AWG: 没有勾选任何选项，保持原版行为");
                    return;
                }

                int on = 0;

                foreach (SeedEntry entry in SeedCatalog.All())
                {
                    bool enable = !entry.IsNormal && cfg.Has(entry.Key);
                    try
                    {
                        entry.Option.Enabled = enable;
                        if (enable)
                            on++;
                    }
                    catch (Exception e)
                    {
                        Log.Write("AWG: 设置 " + entry.Key + " 失败: " + e.Message);
                    }
                }

                Log.Write("AWG: 已应用世界选项 " + on + " 项: " + string.Join(", ", cfg.Enabled));
            }
            catch (Exception e)
            {
                Log.Write("AWG: 应用世界选项失败: " + e);
            }
        }
    }
}
