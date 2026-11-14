using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.IO;

namespace AdvancedWorldGen
{
    /// <summary>
    /// 挂在 WorldGen.Reset() 之前：原版在这里读 WorldGenerationOptions，
    /// 我们先把 Enabled 写好，等于覆盖了种子文本带来的默认设置。
    /// </summary>
    internal static class WorldGenHooks
    {
        private static Harmony _harmony;
        private static bool _applied;
        private static readonly HashSet<long> MapCleared = new HashSet<long>();

        public static void Apply(tContentPatch.Patch.IAddPatch addPatch)
        {
            if (_applied)
                return;
            _applied = true;

            try
            {
                _harmony = new Harmony("AdvancedWorldGen.Patches");

                MethodInfo reset = typeof(WorldGen).GetMethod(
                    "Reset",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null, Type.EmptyTypes, null);

                if (reset == null)
                {
                    Log.Write("AWG: 找不到 WorldGen.Reset()");
                    return;
                }

                _harmony.Patch(reset, prefix: new HarmonyMethod(
                    typeof(WorldGenHooks).GetMethod(nameof(ResetPrefix),
                        BindingFlags.NonPublic | BindingFlags.Static)));

                Log.Write("AWG: 已挂 WorldGen.Reset");

                // 生成结束后的附加处理（海滩出生 / 彩漆着色 / 随机替换）
                try
                {
                    MethodInfo generate = typeof(WorldGen).GetMethod(
                        "GenerateWorld",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                        null,
                        new[]
                        {
                            typeof(Terraria.WorldBuilding.GenerationProgress),
                            typeof(Terraria.WorldBuilding.WorldGenerator.Controller)
                        },
                        null);

                    if (generate != null)
                    {
                        _harmony.Patch(generate,
                            prefix: new HarmonyMethod(
                                typeof(WorldGenHooks).GetMethod(nameof(GenerateWorldPrefix),
                                    BindingFlags.NonPublic | BindingFlags.Static)),
                            postfix: new HarmonyMethod(
                                typeof(WorldGenHooks).GetMethod(nameof(GenerateWorldPostfix),
                                    BindingFlags.NonPublic | BindingFlags.Static)));

                        Log.Write("AWG: 已挂 WorldGen.GenerateWorld（大小 + 后处理）");
                    }
                    else
                    {
                        Log.Write("AWG: 找不到 WorldGen.GenerateWorld");
                    }
                }
                catch (Exception e)
                {
                    Log.Write("AWG: 挂 GenerateWorld 失败: " + e.Message);
                }

                // 地图网格：读档后也要扩容（否则大地图会崩在 MapRenderer.checkMap）
                try
                {
                    MethodInfo setSize = typeof(WorldGen).GetMethod(
                        "setWorldSize",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                        null, Type.EmptyTypes, null);

                    if (setSize != null)
                    {
                        _harmony.Patch(setSize, postfix: new HarmonyMethod(
                            typeof(WorldGenHooks).GetMethod(nameof(SetWorldSizePostfix),
                                BindingFlags.NonPublic | BindingFlags.Static)));

                        Log.Write("AWG: 已挂 WorldGen.setWorldSize（地图网格）");
                    }
                }
                catch (Exception e)
                {
                    Log.Write("AWG: 挂 setWorldSize 失败: " + e.Message);
                }

                // 启动时就把地图网格扩到本模组允许的最大尺寸，
                // 这样读档/超大世界都不会在 MapRenderer.checkMap 越界
                ResizeMapRenderer(10000, 3000);

                // 读档时 LoadHeader 会先设 maxTiles 再调用 WorldGen.clearWorld()，
                // 此时 Main.tile 还停留在 8400x2400，必须在这之前扩容，否则读档失败
                try
                {
                    MethodInfo clear = typeof(WorldGen).GetMethod(
                        "clearWorld",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                        null, Type.EmptyTypes, null);

                    if (clear != null)
                    {
                        _harmony.Patch(clear, prefix: new HarmonyMethod(
                            typeof(WorldGenHooks).GetMethod(nameof(ClearWorldPrefix),
                                BindingFlags.NonPublic | BindingFlags.Static)));

                        Log.Write("AWG: 已挂 WorldGen.clearWorld（读档前扩容）");
                    }
                }
                catch (Exception e)
                {
                    Log.Write("AWG: 挂 clearWorld 失败: " + e.Message);
                }

                // 地图渲染网格扩容时，新块内容是未初始化的显存，清成透明
                try
                {
                    MethodInfo checkMap = typeof(Terraria.MapRenderer).GetMethod(
                        "checkMap",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                        null, new[] { typeof(int), typeof(int) }, null);

                    if (checkMap != null)
                    {
                        _harmony.Patch(checkMap, postfix: new HarmonyMethod(
                            typeof(WorldGenHooks).GetMethod(nameof(CheckMapPostfix),
                                BindingFlags.NonPublic | BindingFlags.Static)));

                        Log.Write("AWG: 已挂 MapRenderer.checkMap（清空新地图块）");
                    }
                }
                catch (Exception e)
                {
                    Log.Write("AWG: 挂 checkMap 失败: " + e.Message);
                }

                // 原版世界生成配置：生成前把覆盖值写进去
                try
                {
                    MethodInfo fromPath = typeof(Terraria.WorldBuilding.WorldGenConfiguration).GetMethod(
                        "FromEmbeddedPath",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                    if (fromPath != null)
                    {
                        _harmony.Patch(fromPath, postfix: new HarmonyMethod(
                            typeof(WorldGenHooks).GetMethod(nameof(FromEmbeddedPathPostfix),
                                BindingFlags.NonPublic | BindingFlags.Static)));

                        Log.Write("AWG: 已挂 WorldGenConfiguration.FromEmbeddedPath（原版配置覆盖）");
                    }
                }
                catch (Exception e)
                {
                    Log.Write("AWG: 挂 FromEmbeddedPath 失败: " + e.Message);
                }

                // 读档后把各种写死尺寸的 section 网格补齐
                try
                {
                    MethodInfo load = typeof(WorldFile).GetMethod(
                        "LoadWorld",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                    if (load != null)
                    {
                        _harmony.Patch(load, postfix: new HarmonyMethod(
                            typeof(WorldGenHooks).GetMethod(nameof(LoadWorldPostfix),
                                BindingFlags.NonPublic | BindingFlags.Static)));

                        Log.Write("AWG: 已挂 WorldFile.LoadWorld（section 网格）");
                    }
                }
                catch (Exception e)
                {
                    Log.Write("AWG: 挂 LoadWorld 失败: " + e.Message);
                }

                // 世界创建界面的模组图标按钮
                CreateMenuPatch.Apply(_harmony);
            }
            catch (Exception e)
            {
                Log.Write("AWG 挂载失败: " + e);
            }
        }

        private static void ResetPrefix()
        {
            SeedOptions.Apply(AwgConfig.Instance);
        }

        private static void FromEmbeddedPathPostfix(ref Terraria.WorldBuilding.WorldGenConfiguration __result)
        {
            if (__result != null)
                WorldgenConfig.ApplyOverrides(__result);
        }

        private static void CheckMapPostfix(int i, int j, bool __result)
        {
            if (!__result)
                return;

            long key = ((long)i << 32) ^ (uint)j;
            if (!MapCleared.Add(key))
                return;

            try
            {
                FieldInfo field = typeof(Terraria.MapRenderer).GetField(
                    "mapTarget",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                RenderTarget2D[,] targets = field != null ? field.GetValue(null) as RenderTarget2D[,] : null;
                if (targets == null)
                    return;

                if (i < 0 || j < 0 || i >= targets.GetLength(0) || j >= targets.GetLength(1))
                    return;

                RenderTarget2D target = targets[i, j];
                if (target == null)
                    return;

                var device = Main.instance.GraphicsDevice;
                RenderTargetBinding[] previous = device.GetRenderTargets();

                device.SetRenderTarget(target);
                device.Clear(Microsoft.Xna.Framework.Color.Transparent);
                device.SetRenderTargets(previous);
            }
            catch
            {
            }
        }

        private static void ClearWorldPrefix()        {
            try
            {
                int needX = Math.Max(8400, ((Main.maxTilesX + Main.sectionWidth - 1) / Main.sectionWidth) * Main.sectionWidth);
                int needY = Math.Max(2400, ((Main.maxTilesY + Main.sectionHeight - 1) / Main.sectionHeight) * Main.sectionHeight);

                if (Main.tile == null
                    || Main.tile.GetLength(0) < needX
                    || Main.tile.GetLength(1) < needY)
                {
                    Main.tile = new Tile[needX, needY];
                    Log.Write("AWG: 读取前分配 Tile 数组 " + needX + " x " + needY);
                }

                ResizeActiveSections();
                EnsureMapSize(needX, needY);
            }
            catch (Exception e)
            {
                Log.Write("AWG: 读档前扩容失败: " + e.Message);
            }
        }

        private static void LoadWorldPostfix()        {
            ResizeActiveSections();

            try
            {
                int allocX = Math.Max(8400, ((Main.maxTilesX + 199) / 200) * 200);
                int allocY = Math.Max(2400, ((Main.maxTilesY + 149) / 150) * 150);
                ResizeMapRenderer(allocX, allocY);
                EnsureMapSize(allocX, allocY);
            }
            catch (Exception e)
            {
                Log.Write("AWG: 读档后处理失败: " + e.Message);
            }
        }

        private static void SetWorldSizePostfix()        {
            try
            {
                if (Main.maxTilesX <= 8400 && Main.maxTilesY <= 2400)
                    return;

                int allocX = Math.Max(8400, ((Main.maxTilesX + 199) / 200) * 200);
                int allocY = Math.Max(2400, ((Main.maxTilesY + 149) / 150) * 150);

                ResizeMapRenderer(allocX, allocY);
                ResizeActiveSections();
            }
            catch (Exception e)
            {
                Log.Write("AWG: setWorldSize 后处理失败: " + e.Message);
            }
        }

        private static void GenerateWorldPostfix()
        {
            PostGen.Apply();

            // 生成后回收一次，减少 32 位进程的碎片/峰值
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                long mb = Process.GetCurrentProcess().PrivateMemorySize64 / 1024 / 1024;
                Log.Write("AWG: 生成后 GC 完成，进程内存约 " + mb + " MB（LAA=" + IsLargeAddressAware() + "）");
            }
            catch
            {
            }
        }

        private static void GenerateWorldPrefix()
        {
            try
            {
                AwgConfig cfg = AwgConfig.Instance;
                if (!cfg.CustomSize)
                    return;

                int width = Math.Min(10000, Math.Max(4200, cfg.CustomWidth));
                int height = Math.Min(3000, Math.Max(1200, cfg.CustomHeight));

                ApplyMemoryBudget(ref width, ref height);

                Main.maxTilesX = width;
                Main.maxTilesY = height;

                ResizeActiveSections();

                if (width > 8400 || height > 2400)
                    ReallocateTilemap(width, height);

                try
                {
                    Main.ActiveWorldFileData.WorldSizeX = width;
                    Main.ActiveWorldFileData.WorldSizeY = height;
                }
                catch
                {
                }

                Log.Write("AWG: 自定义世界大小 " + width + " x " + height
                    + (width > 8400 || height > 2400 ? "（已扩展数组）" : ""));
            }
            catch (Exception e)
            {
                Log.Write("AWG: 应用自定义大小失败: " + e.Message);
            }
        }

        /// <summary>
        /// 根据当前进程已用内存 + 新地图预估内存，自动缩小世界尺寸，避免 32 位 OOM。
        /// 开启了 Large Address Aware 的进程预算更高。
        /// </summary>
        private static void ApplyMemoryBudget(ref int width, ref int height)
        {
            try
            {
                bool laa = IsLargeAddressAware();
                long budget = laa ? 3_000_000_000L : 1_400_000_000L;

                long current = 0;
                try
                {
                    current = Process.GetCurrentProcess().PrivateMemorySize64;
                }
                catch
                {
                }

                for (int attempt = 0; attempt < 12; attempt++)
                {
                    long allocX = ((width + 199) / 200) * 200L;
                    long allocY = ((height + 149) / 150) * 150L;
                    long estimate = allocX * allocY * 32L;   // 每个 Tile 约 32 字节

                    if (current + estimate <= budget)
                        return;

                    int newWidth = Math.Max(4200, (int)(width * 0.9));
                    int newHeight = Math.Max(1200, (int)(height * 0.9));

                    if (newWidth == width && newHeight == height)
                        break;

                    width = newWidth;
                    height = newHeight;

                    Log.Write("AWG: 内存预算不足，缩小到 " + width + " x " + height
                        + "（已用 " + (current / 1024 / 1024) + " MB, 预估 "
                        + (estimate / 1024 / 1024) + " MB, 预算 "
                        + (budget / 1024 / 1024) + " MB, LAA=" + laa + "）");
                }

                // 实在装不下就退回中世界
                if (width > 6400 || height > 1800)
                {
                    width = 6400;
                    height = 1800;
                    Log.Write("AWG: 内存预算严重不足，回退到 6400 x 1800");
                }
            }
            catch (Exception e)
            {
                Log.Write("AWG: 内存预算检查失败: " + e.Message);
            }
        }

        /// <summary>读取自身 PE 头，判断是否已开启 Large Address Aware。</summary>
        private static bool IsLargeAddressAware()
        {
            try
            {
                string path = Process.GetCurrentProcess().MainModule.FileName;

                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    fs.Position = 0x3C;
                    int peOffset = reader.ReadInt32();
                    fs.Position = peOffset + 0x16;
                    ushort characteristics = reader.ReadUInt16();
                    return (characteristics & 0x0020) != 0;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// ActiveSections.LastActiveTime 是在类型初始化时按 8400x2400 写死分配的，
        /// 世界更大时玩家走到右侧会越界（CheckSection/IsSectionActive）。
        /// 这里按当前 maxTiles 重新分配，并同步 sectionManager。
        /// </summary>
        private static void ResizeActiveSections()
        {
            try
            {
                Type type = typeof(Terraria.DataStructures.ActiveSections);
                FieldInfo field = type.GetField(
                    "LastActiveTime",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (field != null)
                {
                    int needW = Math.Max(43, Main.maxTilesX / 200 + 2);
                    int needH = Math.Max(17, Main.maxTilesY / 150 + 2);

                    uint[,] current = field.GetValue(null) as uint[,];
                    if (current == null
                        || current.GetLength(0) < needW
                        || current.GetLength(1) < needH)
                    {
                        field.SetValue(null, new uint[needW, needH]);
                        Log.Write("AWG: ActiveSections 网格扩展为 " + needW + " x " + needH);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Write("AWG: 扩展 ActiveSections 失败: " + e.Message);
            }

            // sectionManager（WorldSections）的 width/height 也是私有的，按需重建
            try
            {
                int needW = Math.Max(42, Main.maxTilesX / Main.sectionWidth + 1);
                int needH = Math.Max(16, Main.maxTilesY / Main.sectionHeight + 1);

                BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                WorldSections sections = Main.sectionManager;

                int curW = 0;
                int curH = 0;

                if (sections != null)
                {
                    FieldInfo fw = typeof(WorldSections).GetField("width", flags);
                    FieldInfo fh = typeof(WorldSections).GetField("height", flags);
                    if (fw != null) curW = (int)fw.GetValue(sections);
                    if (fh != null) curH = (int)fh.GetValue(sections);
                }

                if (sections == null || curW < needW || curH < needH)
                {
                    Main.sectionManager = new WorldSections(needW, needH);
                    Log.Write("AWG: sectionManager 重建为 " + needW + " x " + needH);
                }
            }
            catch (Exception e)
            {
                Log.Write("AWG: 重建 sectionManager 失败: " + e.Message);
            }

            // LeashedEntity.BySection 也是按 8400x2400 写死分配的
            try
            {
                Type type = typeof(Terraria.GameContent.LeashedEntity);
                FieldInfo field = type.GetField(
                    "BySection",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (field != null)
                {
                    int needW = Math.Max(43, Main.maxTilesX / Main.sectionWidth + 2);
                    int needH = Math.Max(17, Main.maxTilesY / Main.sectionHeight + 2);

                    Array current = field.GetValue(null) as Array;
                    if (current == null
                        || current.GetLength(0) < needW
                        || current.GetLength(1) < needH)
                    {
                        Type element = current != null
                            ? current.GetType().GetElementType()
                            : type.Assembly.GetType("Terraria.GameContent.SectionEntityList");

                        if (element != null)
                        {
                            Array bigger = Array.CreateInstance(element, needW, needH);
                            ConstructorInfo ctor = element.GetConstructor(Type.EmptyTypes);

                            if (ctor != null)
                            {
                                for (int i = 0; i < needW; i++)
                                    for (int j = 0; j < needH; j++)
                                        bigger.SetValue(ctor.Invoke(null), i, j);
                            }

                            field.SetValue(null, bigger);
                            Log.Write("AWG: LeashedEntity 网格扩展为 " + needW + " x " + needH);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Write("AWG: 扩展 LeashedEntity 失败: " + e.Message);
            }
        }

        /// <summary>
        /// 世界变大后，地图渲染网格（MapRenderer）也要跟着扩容，
        /// 否则 DrawToMap → checkMap 会索引越界（就是刚才那个崩溃）。
        /// </summary>
        private static void ResizeMapRenderer(int allocX, int allocY)
        {
            try
            {
                Type type = typeof(Terraria.MapRenderer);
                BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

                FieldInfo texWField = type.GetField("textureMaxWidth", flags);
                FieldInfo texHField = type.GetField("textureMaxHeight", flags);
                if (texWField == null || texHField == null)
                    return;

                int texW = (int)texWField.GetValue(null);
                int texH = (int)texHField.GetValue(null);

                // 原版最后一块是固定 400x600，所以总覆盖 = (n-1)*2000+400 / (m-1)*1800+600。
                // 这里按这个公式算需要多少块，保证地图能完整覆盖自定义尺寸。
                int needX = Math.Max(5, (allocX - 400 + texW - 1) / texW + 1);
                int needY = Math.Max(2, (allocY - 600 + texH - 1) / texH + 1);

                int coveredX = (needX - 1) * texW + 400;
                int coveredY = (needY - 1) * texH + 600;

                FieldInfo nx = type.GetField("numTargetsX", flags);
                FieldInfo ny = type.GetField("numTargetsY", flags);
                if (nx == null || ny == null)
                    return;

                int curX = (int)nx.GetValue(null);
                int curY = (int)ny.GetValue(null);

                FieldInfo targetField = type.GetField("mapTarget", flags);
                object currentTargets = targetField != null ? targetField.GetValue(null) : null;
                RenderTarget2D[,] currentArray = currentTargets as RenderTarget2D[,];

                bool sizeOk = currentArray != null
                    && currentArray.GetLength(0) >= needX
                    && currentArray.GetLength(1) >= needY;

                if (curX >= needX && curY >= needY && sizeOk)
                    return;

                nx.SetValue(null, needX);
                ny.SetValue(null, needY);

                if (targetField != null)
                {
                    if (currentArray != null)
                    {
                        foreach (RenderTarget2D rt in currentArray)
                        {
                            try
                            {
                                rt?.Dispose();
                            }
                            catch
                            {
                            }
                        }
                    }

                    targetField.SetValue(null, new RenderTarget2D[needX, needY]);
                }

                type.GetField("initMap", flags)?.SetValue(null, new bool[needX, needY]);
                type.GetField("mapWasContentLost", flags)?.SetValue(null, new bool[needX, needY]);

                FieldInfo queueField = type.GetField("changeQueues", flags);
                if (queueField != null)
                {
                    List<Terraria.DataStructures.Point16>[,] queues = new List<Terraria.DataStructures.Point16>[needX, needY];
                    for (int i = 0; i < needX; i++)
                        for (int j = 0; j < needY; j++)
                            queues[i, j] = new List<Terraria.DataStructures.Point16>(2048);

                    queueField.SetValue(null, queues);
                }

                Log.Write("AWG: 地图渲染网格扩展为 " + needX + " x " + needY
                    + "（覆盖 " + coveredX + " x " + coveredY + "）");

                MapCleared.Clear();
            }
            catch (Exception e)
            {
                Log.Write("AWG: 扩展地图渲染网格失败: " + e.Message);
            }
        }
        /// <summary>
        /// Main.Map 要覆盖所有 section（sectionManager 可能多算一格），
        /// 否则 DrawToMap_Section 会越界。
        /// </summary>
        private static void EnsureMapSize(int minW, int minH)
        {
            try
            {
                int needW = Math.Max(minW, (Main.maxTilesX / Main.sectionWidth + 2) * Main.sectionWidth);
                int needH = Math.Max(minH, (Main.maxTilesY / Main.sectionHeight + 2) * Main.sectionHeight);

                if (Main.Map != null && Main.Map.MaxWidth >= needW && Main.Map.MaxHeight >= needH)
                    return;

                ConstructorInfo ctor = typeof(Terraria.Map.WorldMap).GetConstructor(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new[] { typeof(int), typeof(int) }, null);

                if (ctor == null)
                    return;

                Main.Map = (Terraria.Map.WorldMap)ctor.Invoke(new object[] { needW, needH });
                Log.Write("AWG: 地图数据扩展为 " + needW + " x " + needH);
            }
            catch (Exception e)
            {
                Log.Write("AWG: 扩展地图数据失败: " + e.Message);
            }
        }

        private static void ReallocateTilemap(int width, int height)
        {
            int sectionW = Main.sectionWidth;
            int sectionH = Main.sectionHeight;

            int allocX = Math.Max(8400, ((width + sectionW - 1) / sectionW) * sectionW);
            int allocY = Math.Max(2400, ((height + sectionH - 1) / sectionH) * sectionH);

            try
            {
                Main.tile = new Tile[allocX, allocY];
                Log.Write("AWG: 重新分配 Tile 数组 " + allocX + " x " + allocY);

                ResizeMapRenderer(allocX, allocY);

                EnsureMapSize(allocX, allocY);
            }
            catch (OutOfMemoryException)
            {
                Main.maxTilesX = 8400;
                Main.maxTilesY = 2400;
                Main.tile = new Tile[8400, 2400];
                Log.Write("AWG: 内存不足，回退到 8400 x 2400");
            }
        }
    }
}
