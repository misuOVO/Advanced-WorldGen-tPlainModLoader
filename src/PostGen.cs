using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace AdvancedWorldGen
{
    /// <summary>
    /// 世界生成后的附加处理（移植 AdvancedWorldGen 的 Celebrationmk10 子选项）：
    ///   - 海滩出生（Celebrationmk10.BeachSpawn）
    ///   - 彩漆着色（Celebrationmk10.Painted）
    /// </summary>
    internal static class PostGen
    {
        private static readonly int[] Palette =
        {
            PaintID.RedPaint, PaintID.OrangePaint, PaintID.YellowPaint,
            PaintID.LimePaint, PaintID.GreenPaint, PaintID.TealPaint,
            PaintID.CyanPaint, PaintID.SkyBluePaint, PaintID.BluePaint,
            PaintID.PurplePaint, PaintID.VioletPaint, PaintID.PinkPaint
        };

        public static void Apply()
        {
            try
            {
                AwgConfig cfg = AwgConfig.Instance;

                if (cfg.RandomWorld)
                    RandomReplace();

                bool celebration = cfg.Has("Anniversary");

                if (cfg.RandomPainted || (celebration && cfg.CelebrationPainted))
                    PaintWorld();

                if (celebration && cfg.CelebrationBeachSpawn)
                    BeachSpawn();

                Log.Write("AWG: 后处理完成 beachSpawn=" + cfg.CelebrationBeachSpawn
                    + " painted=" + cfg.CelebrationPainted
                    + " random=" + cfg.RandomWorld);
            }
            catch (Exception e)
            {
                Log.Write("AWG: 后处理失败: " + e);
            }
        }

        /// <summary>随机替换：每种实心方块/墙映射到另一种随机类型（移植 RandomReplacer）。</summary>
        private static void RandomReplace()
        {
            Dictionary<ushort, ushort> tileMap = new Dictionary<ushort, ushort>();
            Dictionary<ushort, ushort> wallMap = new Dictionary<ushort, ushort>();
            HashSet<ushort> usedTiles = new HashSet<ushort>();
            HashSet<ushort> usedWalls = new HashSet<ushort>();

            int tiles = 0;
            int walls = 0;

            for (int x = 0; x < Main.maxTilesX; x++)
            {
                for (int y = 0; y < Main.maxTilesY; y++)
                {
                    Tile tile = Main.tile[x, y];
                    if (tile == null)
                        continue;

                    if (tile.active() && IsReplaceableTile(tile.type))
                    {
                        if (!tileMap.TryGetValue(tile.type, out ushort repl))
                        {
                            repl = PickTile(usedTiles, tile.type);
                            if (repl == 0)
                                continue;

                            tileMap[tile.type] = repl;
                            usedTiles.Add(repl);
                        }

                        tile.type = repl;
                        tiles++;
                    }

                    if (tile.wall > 0)
                    {
                        if (!wallMap.TryGetValue(tile.wall, out ushort repl))
                        {
                            repl = PickWall(usedWalls, tile.wall);
                            if (repl == 0)
                                continue;

                            wallMap[tile.wall] = repl;
                            usedWalls.Add(repl);
                        }

                        tile.wall = repl;
                        walls++;
                    }
                }
            }

            try
            {
                WorldGen.RangeFrame(1, 1, Main.maxTilesX - 1, Main.maxTilesY - 1);
            }
            catch
            {
            }

            Log.Write("AWG: 随机替换完成 方块 " + tiles + " 格（" + tileMap.Count + " 种）, 墙 " + walls + " 格（" + wallMap.Count + " 种）");
        }

        private static bool IsReplaceableTile(ushort type)
        {
            if (type == 0)
                return false;

            if (!Main.tileSolid[type])
                return false;

            if (Main.tileFrameImportant[type] || Main.tileContainer[type])
                return false;

            if (Main.tileSolidTop[type] || TileID.Sets.Platforms[type])
                return false;

            return true;
        }

        private static ushort PickTile(HashSet<ushort> used, ushort oldType)
        {
            for (int attempt = 0; attempt < 400; attempt++)
            {
                ushort candidate = (ushort)WorldGen.genRand.Next(1, TileID.Count);

                if (candidate == oldType || used.Contains(candidate))
                    continue;

                if (!IsReplaceableTile(candidate))
                    continue;

                return candidate;
            }

            return 0;
        }

        private static ushort PickWall(HashSet<ushort> used, ushort oldType)
        {
            for (int attempt = 0; attempt < 400; attempt++)
            {
                ushort candidate = (ushort)WorldGen.genRand.Next(1, WallID.Count);

                if (candidate == oldType || used.Contains(candidate))
                    continue;

                return candidate;
            }

            return 0;
        }

        /// <summary>把世界用节庆颜色涂一遍（每种方块/墙固定一个颜色）。</summary>
        private static void PaintWorld()
        {
            Dictionary<ushort, byte> tilePaint = new Dictionary<ushort, byte>();
            Dictionary<ushort, byte> wallPaint = new Dictionary<ushort, byte>();

            for (int x = 0; x < Main.maxTilesX; x++)
            {
                for (int y = 0; y < Main.maxTilesY; y++)
                {
                    Tile tile = Main.tile[x, y];
                    if (tile == null)
                        continue;

                    if (tile.active())
                    {
                        if (!tilePaint.TryGetValue(tile.type, out byte paint))
                        {
                            paint = (byte)Palette[Main.rand.Next(Palette.Length)];
                            tilePaint[tile.type] = paint;
                        }

                        tile.color(paint);
                    }

                    if (tile.wall > 0)
                    {
                        if (!wallPaint.TryGetValue(tile.wall, out byte paint))
                        {
                            paint = (byte)Palette[Main.rand.Next(Palette.Length)];
                            wallPaint[tile.wall] = paint;
                        }

                        tile.wallColor(paint);
                    }
                }
            }

            Log.Write("AWG: 彩漆着色完成，方块 " + tilePaint.Count + " 种，墙 " + wallPaint.Count + " 种");
        }

        /// <summary>把出生点移到海边沙滩（简化版 SpawnPass）。</summary>
        private static void BeachSpawn()
        {
            const int edge = 120;

            int x = Main.rand.Next(2) == 0 ? Main.maxTilesX - edge : edge;
            int y = -1;

            for (int scan = 0; scan < Main.maxTilesY; scan++)
            {
                if (Main.tile[x, scan].active())
                {
                    y = scan;
                    break;
                }
            }

            if (y <= 1)
                return;

            // 太深就再往外/内找一找
            int attempt = 0;
            while (y > Main.worldSurface + 10 && attempt < 40)
            {
                attempt++;
                x += Main.rand.Next(-10, 11);
                x = Math.Max(20, Math.Min(Main.maxTilesX - 20, x));

                for (int scan = 0; scan < Main.maxTilesY; scan++)
                {
                    if (Main.tile[x, scan].active())
                    {
                        y = scan;
                        break;
                    }
                }
            }

            Main.spawnTileX = x;
            Main.spawnTileY = Math.Max(1, y);

            Log.Write("AWG: 出生点设为海滩 (" + x + ", " + y + ")");
        }
    }
}
