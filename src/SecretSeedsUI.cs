using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;
using tContentPatch.Content.UI;

namespace AdvancedWorldGen
{
    /// <summary>
    /// 1.4.5 的“秘密种子”列表：
    ///   - 按官方中文 Wiki 的分类分组显示；
    ///   - 中文名 + 标准种子文本；
    ///   - 鼠标悬停时在底部显示该世界的游戏内描述；
    ///   - 可滚动。
    /// </summary>
    internal class SecretSeedsState : UIState
    {
        private sealed class SeedInfo
        {
            public string Name;
            public string Seed;
            public string Group;
        }

        private static readonly string[] Groups =
        {
            "世界刷漆", "世界生成", "生物群系生成", "结构生成", "被动效果", "生成点", "其他效果"
        };

        private static readonly Dictionary<string, SeedInfo> Info =
            new Dictionary<string, SeedInfo>(StringComparer.OrdinalIgnoreCase);

        private static void Add(string key, string name, string seed, string group)
        {
            Info[key] = new SeedInfo { Name = name, Seed = seed, Group = group };
        }

        static SecretSeedsState()
        {
            const string g1 = "世界刷漆";
            Add("coatEverythingEcho", "回声世界", "invisible plane", g1);
            Add("paintEverythingGray", "灰度世界", "monochrome", g1);
            Add("paintEverythingNegative", "反色世界", "negative infinity", g1);
            Add("coatEverythingIlluminant", "夜明世界", "x-ray vision", g1);

            const string g2 = "世界生成";
            Add("digExtraHoles", "多洞世界", "jagged rocks", g2);
            Add("noSurface", "无地表", "mole people", g2);
            Add("roundLandmasses", "圆形地块", "planetoids", g2);
            Add("surfaceIsInSpace", "太空地表", "such great heights", g2);
            Add("extraLiquid", "多液世界", "waterpark", g2);

            const string g3 = "生物群系生成";
            Add("noSpiderCaves", "无蜘蛛窝", "arachnophobia", g3);
            Add("hallowOnTheSurface", "地表神圣", "does that sparkle", g3);
            Add("noInfection", "无感染", "fish mox", g3);
            Add("worldIsInfected", "世界被感染", "purify this", g3);
            Add("surfaceIsDesert", "沙漠地表", "sandy britches", g3);
            Add("surfaceIsMushrooms", "蘑菇地表", "toadstool", g3);
            Add("worldIsFrozen", "冰封世界", "winter is coming", g3);

            const string g4 = "结构生成";
            Add("biggerAbandonedHouses", "巨大废屋", "abandoned manors", g4);
            Add("addTeleporters", "添加传送机", "beam me up", g4);
            Add("actuallyNoTraps", "真没陷阱", "more traps please", g4);
            Add("halloweenGen", "万圣节生成", "pumpkin season", g4);
            Add("rainbowStuff", "彩虹要素", "rainbow road", g4);
            Add("extraLivingTrees", "多生命树", "save the rainforest", g4);
            Add("extraFloatingIslands", "多漂浮岛", "the care bears movie", g4);
            Add("pooEverywhere", "处处臭臭", "truck stop", g4);
            Add("portalGunInChests", "宝箱传送枪", "we don't even test for that", g4);

            const string g5 = "被动效果";
            Add("rainsForAYear", "雨下整年", "bring a towel", g5);
            Add("endlessHalloween", "无尽万圣", "hocus pocus", g5);
            Add("endlessChristmas", "无尽圣诞", "jingle all the way", g5);
            Add("moreLightning", "更多雷击", "electric boogaloo", g5);
            Add("noLightning", "无雷击", "calm before the storm", g5);

            const string g6 = "生成点";
            Add("randomSpawn", "随机生成点", "how did i get here", g6);
            Add("teamBasedSpawns", "团队生成点", "royale with cheese", g6);

            const string g7 = "其他效果";
            Add("dualDungeons", "对偶地牢", "double daring dangers", g7);
            Add("errorWorld", "错误世界", "i am error", g7);
            Add("graveyardBloodmoonStart", "开局血月墓地", "night of the living dead", g7);
            Add("startInHardmode", "开局困难模式", "too easy", g7);
            Add("vampirism", "吸血鬼症", "what a horrible night to have a curse", g7);
        }

        private const string DefaultDescription = "把鼠标移到种子上查看这个世界的描述。";

        /// <summary>每个秘密种子前面的小图标（原版物品贴图）。</summary>
        private static readonly Dictionary<string, int> Icons =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "coatEverythingEcho", ItemID.EchoCoating },
                { "paintEverythingGray", ItemID.GrayPaint },
                { "paintEverythingNegative", ItemID.NegativePaint },
                { "coatEverythingIlluminant", ItemID.GlowPaint },
                { "digExtraHoles", ItemID.CopperPickaxe },
                { "noSurface", ItemID.DirtBlock },
                { "roundLandmasses", ItemID.Boulder },
                { "surfaceIsInSpace", ItemID.Meteorite },
                { "extraLiquid", ItemID.WaterBucket },
                { "noSpiderCaves", ItemID.Cobweb },
                { "hallowOnTheSurface", ItemID.HallowedSeeds },
                { "noInfection", ItemID.PurificationPowder },
                { "worldIsInfected", ItemID.EbonstoneBlock },
                { "surfaceIsDesert", ItemID.SandBlock },
                { "surfaceIsMushrooms", ItemID.Mushroom },
                { "worldIsFrozen", ItemID.IceBlock },
                { "biggerAbandonedHouses", ItemID.Wood },
                { "addTeleporters", ItemID.Teleporter },
                { "actuallyNoTraps", ItemID.VenomDartTrap },
                { "halloweenGen", ItemID.Pumpkin },
                { "rainbowStuff", ItemID.RainbowBrick },
                { "extraLivingTrees", ItemID.LivingWoodWand },
                { "extraFloatingIslands", ItemID.Cloud },
                { "pooEverywhere", ItemID.PoopBlock },
                { "portalGunInChests", ItemID.PortalGun },
                { "rainsForAYear", ItemID.Umbrella },
                { "endlessHalloween", ItemID.Pumpkin },
                { "endlessChristmas", ItemID.Present },
                { "moreLightning", ItemID.NimbusRod },
                { "noLightning", ItemID.Cloud },
                { "randomSpawn", ItemID.TeleportationPotion },
                { "teamBasedSpawns", ItemID.TeamBlockRed },
                { "dualDungeons", ItemID.BlueBrick },
                { "errorWorld", ItemID.Book },
                { "graveyardBloodmoonStart", ItemID.Tombstone },
                { "startInHardmode", ItemID.Pwnhammer },
                { "vampirism", ItemID.VampireKnives }
            };

        private readonly UIWorldCreation _previous;
        private readonly List<WorldGen.SecretSeed> _seeds = new List<WorldGen.SecretSeed>();

        private UIScrollViewer2 _scroll;
        private UIText _description;

        public SecretSeedsState(UIWorldCreation previous)
        {
            _previous = previous;
            LoadSeeds();
            Build();
        }

        private void LoadSeeds()
        {
            try
            {
                if (WorldGen.SecretSeed.AllSecretSeeds == null || WorldGen.SecretSeed.AllSecretSeeds.Count == 0)
                    WorldGen.SecretSeed.InitializeSecretSeeds();

                if (WorldGen.SecretSeed.AllSecretSeeds != null)
                    _seeds.AddRange(WorldGen.SecretSeed.AllSecretSeeds);
            }
            catch (Exception e)
            {
                Log.Write("AWG: 读取秘密种子失败: " + e.Message);
            }

            Log.Write("AWG: 秘密种子 " + _seeds.Count + " 个");
        }

        private void Build()
        {
            const int panelW = 700;
            const int panelH = 620;

            UIPanel panel = new UIPanel();
            panel.Width.Set(panelW, 0f);
            panel.Height.Set(panelH, 0f);
            panel.HAlign = 0.5f;
            panel.VAlign = 0.5f;
            panel.SetPadding(0f);
            Append(panel);

            UIText title = new UIText("秘密种子", 1.25f, true);
            title.HAlign = 0.5f;
            title.Top.Set(12f, 0f);
            panel.Append(title);

            _scroll = new UIScrollViewer2();
            _scroll.Left.Set(20f, 0f);
            _scroll.Top.Set(54f, 0f);
            _scroll.Width.Set(-40f, 1f);
            _scroll.Height.Set(392f, 0f);
            _scroll.ItemMargin = 3;
            panel.Append(_scroll);

            Box descBox = new Box(new Color(40, 52, 90, 180), new Color(90, 110, 170, 120));
            descBox.Left.Set(20f, 0f);
            descBox.Top.Set(456f, 0f);
            descBox.Width.Set(-40f, 1f);
            descBox.Height.Set(70f, 0f);
            panel.Append(descBox);

            _description = new UIText(DefaultDescription, 0.8f);
            _description.Left.Set(12f, 0f);
            _description.Top.Set(8f, 0f);
            _description.Width.Set(-24f, 1f);
            _description.Height.Set(-16f, 1f);
            _description.IsWrapped = true;
            _description.TextColor = new Color(205, 218, 245);
            descBox.Append(_description);

            TextButton back = MakeButton("返回", 20f, 540f, 150f);
            back.OnLeftClick += (evt, el) =>
            {
                try
                {
                    Main.MenuUI.SetState(new SeedOptionsState(_previous));
                }
                catch (Exception e)
                {
                    Log.Write("AWG: 返回失败: " + e.Message);
                }
            };
            panel.Append(back);

            TextButton off = MakeButton("全部关闭", 190f, 540f, 150f);
            off.OnLeftClick += (evt, el) => SetAll(false);
            panel.Append(off);

            TextButton on = MakeButton("全部启用", 360f, 540f, 150f);
            on.OnLeftClick += (evt, el) => SetAll(true);
            panel.Append(on);

            FillList();
        }

        private static TextButton MakeButton(string text, float left, float top, float width)
        {
            TextButton button = new TextButton(text);
            button.Left.Set(left, 0f);
            button.Top.Set(top, 0f);
            button.Width.Set(width, 0f);
            button.Height.Set(40f, 0f);
            return button;
        }

        private void FillList()
        {
            if (_scroll == null)
                return;

            _scroll.ClearChild();

            Dictionary<string, List<WorldGen.SecretSeed>> byGroup =
                new Dictionary<string, List<WorldGen.SecretSeed>>(StringComparer.OrdinalIgnoreCase);
            List<WorldGen.SecretSeed> unknown = new List<WorldGen.SecretSeed>();

            foreach (WorldGen.SecretSeed seed in _seeds)
            {
                string key = InternalName(seed);
                if (!string.IsNullOrEmpty(key) && Info.TryGetValue(key, out SeedInfo info))
                {
                    if (!byGroup.TryGetValue(info.Group, out List<WorldGen.SecretSeed> list))
                    {
                        list = new List<WorldGen.SecretSeed>();
                        byGroup[info.Group] = list;
                    }
                    list.Add(seed);
                }
                else
                {
                    unknown.Add(seed);
                }
            }

            foreach (string group in Groups)
            {
                if (!byGroup.TryGetValue(group, out List<WorldGen.SecretSeed> list) || list.Count == 0)
                    continue;

                _scroll.AddChild(new GroupHeader(group));

                foreach (WorldGen.SecretSeed seed in list)
                    _scroll.AddChild(CreateRow(seed));
            }

            if (unknown.Count > 0)
            {
                _scroll.AddChild(new GroupHeader("其他"));

                foreach (WorldGen.SecretSeed seed in unknown)
                    _scroll.AddChild(CreateRow(seed));
            }
        }

        private ToggleRow CreateRow(WorldGen.SecretSeed seed)
        {
            ToggleRow row = new ToggleRow(
                NameOf(seed),
                SeedTextOf(seed),
                () => seed.Enabled,
                () => Toggle(seed),
                () => ShowDescription(seed),
                () => _description?.SetText(DefaultDescription),
                () => SeedIcons.FromItem(IconOf(seed)));

            row.Width.Set(-8f, 1f);
            row.Height.Set(36f, 0f);
            row.Left.Set(2f, 0f);
            return row;
        }

        private static int IconOf(WorldGen.SecretSeed seed)
        {
            string key = InternalName(seed);
            if (!string.IsNullOrEmpty(key) && Icons.TryGetValue(key, out int item))
                return item;

            return -1;
        }

        private void ShowDescription(WorldGen.SecretSeed seed)
        {
            if (_description == null)
                return;

            string text = DescriptionOf(seed);
            _description.SetText(string.IsNullOrEmpty(text) ? DefaultDescription : text);
        }

        private void Toggle(WorldGen.SecretSeed seed)
        {
            SetSeed(seed, !seed.Enabled);
        }

        private void SetSeed(WorldGen.SecretSeed seed, bool on)
        {
            string text = SeedTextOf(seed);

            try
            {
                if (on && !seed.Enabled)
                {
                    WorldGen.SecretSeed.Enable(seed, true);
                    _previous?.AddSeedFromSeedmenu(text);
                }
                else if (!on && seed.Enabled)
                {
                    WorldGen.SecretSeed.Disable(seed);
                    _previous?.RemoveSeedFromSeedMenu(text);
                }
            }
            catch (Exception e)
            {
                Log.Write("AWG: 切换秘密种子失败: " + e.Message);
            }
        }

        private void SetAll(bool value)
        {
            foreach (WorldGen.SecretSeed seed in _seeds)
                SetSeed(seed, value);
        }

        // ------------------------------------------------------------------

        private static string InternalName(WorldGen.SecretSeed seed)
        {
            try
            {
                string loc = seed.Localization;
                if (!string.IsNullOrEmpty(loc))
                {
                    int dot = loc.LastIndexOf('.');
                    return dot >= 0 ? loc.Substring(dot + 1) : loc;
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private static string NameOf(WorldGen.SecretSeed seed)
        {
            string key = InternalName(seed);
            if (!string.IsNullOrEmpty(key) && Info.TryGetValue(key, out SeedInfo info))
                return info.Name;

            try
            {
                if (!string.IsNullOrEmpty(seed.TextThatWasUsedToUnlock))
                    return seed.TextThatWasUsedToUnlock;
            }
            catch
            {
            }

            return key;
        }

        private static string SeedTextOf(WorldGen.SecretSeed seed)
        {
            string key = InternalName(seed);
            if (!string.IsNullOrEmpty(key) && Info.TryGetValue(key, out SeedInfo info))
                return info.Seed;

            try
            {
                if (!string.IsNullOrEmpty(seed.TextThatWasUsedToUnlock))
                    return seed.TextThatWasUsedToUnlock;
            }
            catch
            {
            }

            return key;
        }

        private static string DescriptionOf(WorldGen.SecretSeed seed)
        {
            try
            {
                // 1.4.5 里 Localization 就是 SecretSeedDescription.xxx
                if (!string.IsNullOrEmpty(seed.Localization))
                {
                    string text = Language.GetTextValue(seed.Localization);
                    if (!string.IsNullOrEmpty(text) && text != seed.Localization)
                        return text;
                }
            }
            catch
            {
            }

            return string.Empty;
        }
    }

    /// <summary>分类标题行。</summary>
    internal class GroupHeader : UIElement
    {
        private readonly string _title;

        public GroupHeader(string title)
        {
            _title = title;
            Width.Set(-8f, 1f);
            Height.Set(30f, 0f);
            Left.Set(2f, 0f);
        }

        protected override void DrawSelf(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
        {
            CalculatedStyle d = GetDimensions();
            Rectangle rect = new Rectangle((int)d.X, (int)d.Y, (int)d.Width, (int)d.Height);

            UiPaint.Fill(spriteBatch, rect, new Color(52, 66, 110, 170));
            UiPaint.Text(spriteBatch, _title,
                new Vector2(rect.X + 10f, rect.Y + 4f), new Color(255, 226, 150), 0.9f);

            UiPaint.Fill(spriteBatch,
                new Rectangle(rect.X, rect.Bottom - 2, rect.Width, 1),
                new Color(120, 140, 200, 90));
        }
    }
}
