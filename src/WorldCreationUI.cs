using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.ID;
using Terraria.UI;
using tContentPatch.Content.UI;
using tContentPatch.Content.UI.ModSet;
using TerrariaUtils = Terraria.Utils;

namespace AdvancedWorldGen
{
    /// <summary>
    /// 接管世界创建界面种子栏右侧的齿轮按钮，打开“种子选项”面板。
    /// </summary>
    internal static class CreateMenuPatch
    {
        public static void Apply(HarmonyLib.Harmony harmony)
        {
            try
            {
                MethodInfo gear = typeof(UIWorldCreation).GetMethod(
                    "ClickAdvancedSeedMenu",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (gear != null)
                {
                    harmony.Patch(gear, prefix: new HarmonyLib.HarmonyMethod(
                        typeof(CreateMenuPatch).GetMethod(nameof(GearPrefix),
                            BindingFlags.NonPublic | BindingFlags.Static)));

                    Log.Write("AWG: 已接管种子栏齿轮按钮");
                }
            }
            catch (Exception e)
            {
                Log.Write("AWG: 接管齿轮失败: " + e.Message);
            }

            try
            {
                ConstructorInfo ctor = typeof(UIWorldCreation).GetConstructor(Type.EmptyTypes);
                if (ctor == null)
                    return;

                harmony.Patch(ctor, postfix: new HarmonyLib.HarmonyMethod(
                    typeof(CreateMenuPatch).GetMethod(nameof(Postfix),
                        BindingFlags.NonPublic | BindingFlags.Static)));

                Log.Write("AWG: 已挂 UIWorldCreation（种子入口按钮）");
            }
            catch (Exception e)
            {
                Log.Write("AWG: 世界创建界面挂钩失败: " + e.Message);
            }
        }

        private static bool GearPrefix(UIWorldCreation __instance)
        {
            try
            {
                Main.MenuUI.SetState(new SeedOptionsState(__instance));
                return false;
            }
            catch (Exception e)
            {
                Log.Write("AWG: 打开种子面板失败: " + e.Message);
                return true;
            }
        }

        private static void Postfix(UIWorldCreation __instance)
        {
            try
            {
                FieldInfo gearField = typeof(UIWorldCreation).GetField(
                    "_advancedSeedButton",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                UIElement gear = gearField != null ? gearField.GetValue(__instance) as UIElement : null;

                if (gear != null && gear.Parent != null)
                {
                    GearOverlayButton overlay = new GearOverlayButton(__instance, gear);

                    overlay.Left = gear.Left;
                    overlay.Top = gear.Top;
                    overlay.Width = gear.Width;
                    overlay.Height = gear.Height;
                    overlay.HAlign = gear.HAlign;
                    overlay.VAlign = gear.VAlign;

                    gear.Parent.Append(overlay);
                    Log.Write("AWG: 图标按钮已覆盖到原版齿轮位置");
                }
                else
                {
                    __instance.Append(new ModIconButton(__instance));
                    Log.Write("AWG: 没找到齿轮，图标按钮放到右上角");
                }
            }
            catch (Exception e)
            {
                Log.Write("AWG: 添加图标按钮失败: " + e.Message);
            }
        }
    }

    // ------------------------------------------------------------------
    // 绘制工具
    // ------------------------------------------------------------------

    internal static class UiPaint
    {
        public static void Fill(SpriteBatch sb, Rectangle r, Color c)
        {
            sb.Draw(TextureAssets.MagicPixel.Value, r, c);
        }

        public static void Outline(SpriteBatch sb, Rectangle r, Color c, int t = 1)
        {
            Texture2D px = TextureAssets.MagicPixel.Value;
            sb.Draw(px, new Rectangle(r.X, r.Y, r.Width, t), c);
            sb.Draw(px, new Rectangle(r.X, r.Bottom - t, r.Width, t), c);
            sb.Draw(px, new Rectangle(r.X, r.Y, t, r.Height), c);
            sb.Draw(px, new Rectangle(r.Right - t, r.Y, t, r.Height), c);
        }

        public static void Line(SpriteBatch sb, Vector2 a, Vector2 b, Color c, float thickness = 2f)
        {
            Vector2 d = b - a;
            float len = d.Length();
            if (len < 0.01f)
                return;

            sb.Draw(TextureAssets.MagicPixel.Value, a, null, c,
                (float)Math.Atan2(d.Y, d.X), Vector2.Zero,
                new Vector2(len, thickness), SpriteEffects.None, 0f);
        }

        public static void Text(SpriteBatch sb, string text, Vector2 pos, Color color, float scale = 0.95f)
        {
            TerrariaUtils.DrawBorderString(sb, text, pos, color, scale);
        }

        public static void CenteredText(SpriteBatch sb, string text, Rectangle area, Color color, float scale)
        {
            Vector2 size = FontAssets.MouseText.Value.MeasureString(text) * scale;
            Text(sb, text,
                new Vector2(area.X + (area.Width - size.X) / 2f, area.Y + (area.Height - size.Y) / 2f),
                color, scale);
        }

        public static void Pill(SpriteBatch sb, Rectangle area, bool on)
        {            Color track = on ? new Color(70, 150, 85) : new Color(55, 62, 85);
            Color border = on ? new Color(150, 230, 160) : new Color(120, 135, 170);

            Fill(sb, new Rectangle(area.X + 3, area.Y, area.Width - 6, area.Height), track);
            Fill(sb, new Rectangle(area.X + 1, area.Y + 2, area.Width - 2, area.Height - 4), track);
            Fill(sb, new Rectangle(area.X, area.Y + 5, area.Width, area.Height - 10), track);
            Outline(sb, area, border, 2);

            int knobSize = area.Height - 8;
            int knobX = on ? area.Right - knobSize - 4 : area.X + 4;
            Fill(sb, new Rectangle(knobX, area.Y + 4, knobSize, knobSize),
                on ? new Color(225, 255, 230) : new Color(190, 200, 220));
        }
    }

    /// <summary>
    /// tContentPatch 的 UITextBox 在不同 beta 版本里 API 不同：
    /// beta16 是 UIPanel（可设 BackgroundColor/BorderColor/SetTextScale）；
    /// beta17 改成 UIElement + 内部 Style1 白底黑字，颜色字段变成 UITextColor。
    /// 这里用反射做兼容，避免任何一边编译/运行失败。
    /// </summary>
    internal static class TextBoxCompat
    {
        private static readonly PropertyInfo BorderProp =
            typeof(tContentPatch.Content.UI.UITextBox).GetProperty(
                "BorderColor", BindingFlags.Instance | BindingFlags.Public);

        private static readonly FieldInfo TextColorField =
            typeof(tContentPatch.Content.UI.UITextBox).GetField(
                "UITextColor", BindingFlags.Instance | BindingFlags.Public);

        public static void Normal(tContentPatch.Content.UI.UITextBox box)
        {
            Apply(box, new Color(110, 130, 190), Color.Black);
        }

        public static void Saved(tContentPatch.Content.UI.UITextBox box)
        {
            Apply(box, new Color(110, 190, 130), new Color(20, 130, 50));
        }

        public static void Error(tContentPatch.Content.UI.UITextBox box)
        {
            Apply(box, new Color(220, 96, 96), new Color(190, 30, 30));
        }

        private static void Apply(tContentPatch.Content.UI.UITextBox box, Color border, Color text)
        {
            if (box == null)
                return;

            try
            {
                if (BorderProp != null && BorderProp.CanWrite)
                {
                    BorderProp.SetValue(box, border, null);
                    return;
                }

                if (TextColorField != null)
                    TextColorField.SetValue(box, text);
            }
            catch
            {
            }
        }
    }

    internal static class SeedIcons
    {
        public static Texture2D FromItem(int id)
        {
            try
            {
                if (id <= 0)
                    return null;

                // 物品贴图是懒加载的，先让游戏把它加载出来
                Main.instance.LoadItem(id);

                if (id < TextureAssets.Item.Length)
                {
                    Texture2D tex = TextureAssets.Item[id].Value;
                    if (tex != null)
                        return tex;
                }
            }
            catch
            {
            }

            try
            {
                var asset = Main.Assets.Request<Texture2D>(
                    "Images/Item_" + id,
                    ReLogic.Content.AssetRequestMode.ImmediateLoad);

                return asset != null ? asset.Value : null;
            }
            catch
            {
            }

            return null;
        }

        public static Texture2D FromOption(Terraria.WorldBuilding.AWorldGenerationOption option)
        {
            try
            {
                if (option == null)
                    return null;

                PropertyInfo prop = option.GetType().GetProperty(
                    "Texture",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (prop == null)
                    prop = typeof(Terraria.WorldBuilding.AWorldGenerationOption).GetProperty(
                        "Texture",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (prop == null)
                    return null;

                object asset = prop.GetValue(option);
                if (asset == null)
                    return null;

                Type assetType = asset.GetType();

                try
                {
                    PropertyInfo valueProp = assetType.GetProperty("Value",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    Texture2D tex = valueProp != null ? valueProp.GetValue(asset) as Texture2D : null;
                    if (tex != null)
                        return tex;
                }
                catch
                {
                }

                // 还没加载：拿资源名走立即加载
                PropertyInfo nameProp = assetType.GetProperty("Name",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                string name = nameProp != null ? nameProp.GetValue(asset) as string : null;
                if (!string.IsNullOrEmpty(name))
                {
                    var request = Main.Assets.Request<Texture2D>(
                        name, ReLogic.Content.AssetRequestMode.ImmediateLoad);

                    if (request != null)
                        return request.Value;
                }
            }
            catch
            {
            }

            return null;
        }
    }

    internal class Box : UIElement
    {
        private readonly Color _fill;
        private readonly Color _border;

        public Box(Color fill, Color border)
        {
            _fill = fill;
            _border = border;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle d = GetDimensions();
            Rectangle r = new Rectangle((int)d.X, (int)d.Y, (int)d.Width, (int)d.Height);
            UiPaint.Fill(spriteBatch, r, _fill);
            UiPaint.Outline(spriteBatch, r, _border);
        }
    }

    internal class ModIconButton : UIElement
    {
        private static Texture2D _icon;
        private readonly UIWorldCreation _menu;

        public ModIconButton(UIWorldCreation menu)
        {
            _menu = menu;

            Width.Set(52f, 0f);
            Height.Set(52f, 0f);
            HAlign = 1f;
            VAlign = 0f;
            Left.Set(-84f, 0f);
            Top.Set(58f, 0f);

            OnLeftClick += (evt, el) =>
            {
                try
                {
                    Main.MenuUI.SetState(new SeedOptionsState(_menu));
                }
                catch (Exception e)
                {
                    Log.Write("AWG: 打开种子面板失败: " + e.Message);
                }
            };
        }

        internal static Texture2D IconTexture
        {
            get
            {
                if (_icon == null)
                {
                    Assembly asm = Assembly.GetExecutingAssembly();
                    using (Stream s = asm.GetManifestResourceStream("AdvancedWorldGen.Assets.ico.png"))
                    {
                        if (s == null)
                            return null;

                        using (MemoryStream ms = new MemoryStream())
                        {
                            s.CopyTo(ms);
                            ms.Position = 0;
                            _icon = Texture2D.FromStream(Main.instance.GraphicsDevice, ms);
                        }
                    }
                }
                return _icon;
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle d = GetDimensions();
            Rectangle box = new Rectangle((int)d.X, (int)d.Y, (int)d.Width, (int)d.Height);

            UiPaint.Fill(spriteBatch, box, new Color(10, 14, 28, 170));
            UiPaint.Outline(spriteBatch, box,
                IsMouseHovering ? new Color(255, 235, 150) : new Color(120, 140, 200), 2);

            Texture2D tex = IconTexture;
            if (tex != null)
            {
                spriteBatch.Draw(tex,
                    new Rectangle(box.X + 6, box.Y + 4, box.Width - 12, box.Height - 12), Color.White);
            }
        }
    }

    internal class GearOverlayButton : UIElement
    {
        private readonly UIWorldCreation _menu;
        private readonly UIElement _target;

        public GearOverlayButton(UIWorldCreation menu, UIElement target)
        {
            _menu = menu;
            _target = target;

            OnLeftClick += (evt, el) =>
            {
                try
                {
                    Main.MenuUI.SetState(new SeedOptionsState(_menu));
                }
                catch (Exception e)
                {
                    Log.Write("AWG: 打开种子面板失败: " + e.Message);
                }
            };

            OnMouseOver += (evt, el) => Main.hoverItemName = "种子选项";
            OnMouseOut += (evt, el) =>
            {
                if (Main.hoverItemName == "种子选项")
                    Main.hoverItemName = "";
            };
        }

        // 1.4.5.8 的原版齿轮是靠 SnapPoint 定位的，直接抄 Left/Top 会落到错误位置。
        // 每帧按齿轮的实际屏幕位置同步，保证图标和点击区域都在齿轮上。
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (_target == null || Parent == null)
                return;

            try
            {
                CalculatedStyle t = _target.GetDimensions();
                CalculatedStyle p = Parent.GetInnerDimensions();

                float x = t.X - p.X;
                float y = t.Y - p.Y;

                if (Math.Abs(Left.Pixels - x) > 0.1f)
                    Left.Set(x, 0f);

                if (Math.Abs(Top.Pixels - y) > 0.1f)
                    Top.Set(y, 0f);

                if (Math.Abs(Width.Pixels - t.Width) > 0.1f)
                    Width.Set(t.Width, 0f);

                if (Math.Abs(Height.Pixels - t.Height) > 0.1f)
                    Height.Set(t.Height, 0f);

                HAlign = 0f;
                VAlign = 0f;
            }
            catch
            {
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle d = GetDimensions();
            Rectangle box = new Rectangle((int)d.X, (int)d.Y, (int)d.Width, (int)d.Height);

            UiPaint.Fill(spriteBatch, box, new Color(8, 10, 20, 235));
            UiPaint.Outline(spriteBatch, box,
                IsMouseHovering ? new Color(255, 232, 140) : new Color(120, 140, 200), 2);

            Texture2D tex = ModIconButton.IconTexture;
            if (tex != null)
            {
                spriteBatch.Draw(tex,
                    new Rectangle(box.X + 3, box.Y + 3, box.Width - 6, box.Height - 6), Color.White);
            }
        }
    }

    // ------------------------------------------------------------------
    // 种子选项面板
    // ------------------------------------------------------------------

    internal class SeedOptionsState : UIState
    {
        private const string DefaultHint =
            "请选择你想要的选项（可多选，只影响新建世界）。把鼠标移到选项上可查看详细描述。";

        private readonly UIWorldCreation _previous;
        private UIText _countText;
        private UIText _hintText;
        private tContentPatch.Content.UI.UITextBox _seedInput;
        private string _lastSeed = "";

        private static readonly FieldInfo SeedField = typeof(UIWorldCreation).GetField(
            "_optionSeed", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo SpecialSeedTextField = typeof(UIWorldCreation).GetField(
            "_isSpecialSeedText", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo UpdateFieldsMethod = typeof(UIWorldCreation).GetMethod(
            "UpdateInputFields", BindingFlags.Instance | BindingFlags.NonPublic);

        public SeedOptionsState(UIWorldCreation previous)
        {
            _previous = previous;
            Build();
        }

        private void Build()
        {
            List<SeedEntry> entries = SeedCatalog.All();

            const int panelW = 680;
            const int rowStep = 40;
            const int listTop = 104;

            int listH = Math.Max(120, (entries.Count + 4) * rowStep + 12);
            int hintTop = listTop + listH + 10;
            int countTop = hintTop + 58 + 8;
            int btnTop = countTop + 30;
            int panelH = btnTop + 40 + 22;

            UIPanel panel = new UIPanel();
            panel.Width.Set(panelW, 0f);
            panel.Height.Set(panelH, 0f);
            panel.HAlign = 0.5f;
            panel.VAlign = 0.5f;
            panel.SetPadding(0f);
            Append(panel);

            UIText title = new UIText("种子选项", 1.3f, true);
            title.HAlign = 0f;
            title.Left.Set(24f, 0f);
            title.Top.Set(12f, 0f);
            panel.Append(title);

            TextButton sizeButton = new TextButton("世界大小 ▸");
            sizeButton.Left.Set(panelW - 406f, 0f);
            sizeButton.Top.Set(18f, 0f);
            sizeButton.Width.Set(124f, 0f);
            sizeButton.Height.Set(34f, 0f);
            sizeButton.OnLeftClick += (evt, el) =>
            {
                try
                {
                    Main.MenuUI.SetState(new SizeState(_previous));
                }
                catch (Exception e)
                {
                    Log.Write("AWG: 打开世界大小失败: " + e.Message);
                }
            };
            panel.Append(sizeButton);

            TextButton secretButton = new TextButton("秘密种子 ▸");
            secretButton.Left.Set(panelW - 274f, 0f);
            secretButton.Top.Set(18f, 0f);
            secretButton.Width.Set(124f, 0f);
            secretButton.Height.Set(34f, 0f);
            secretButton.OnLeftClick += (evt, el) =>
            {
                try
                {
                    Main.MenuUI.SetState(new SecretSeedsState(_previous));
                }
                catch (Exception e)
                {
                    Log.Write("AWG: 打开秘密种子失败: " + e.Message);
                }
            };
            panel.Append(secretButton);

            TextButton configButton = new TextButton("原版配置 ▸");
            configButton.Left.Set(panelW - 142f, 0f);
            configButton.Top.Set(18f, 0f);
            configButton.Width.Set(124f, 0f);
            configButton.Height.Set(34f, 0f);
            configButton.OnLeftClick += (evt, el) =>
            {
                try
                {
                    Main.MenuUI.SetState(new WorldgenConfigState(_previous));
                }
                catch (Exception e)
                {
                    Log.Write("AWG: 打开原版配置失败: " + e.Message);
                }
            };
            panel.Append(configButton);

            // 种子输入：直接写回世界创建界面的种子字段
            Box seedBg = new Box(new Color(10, 12, 22, 220), new Color(120, 140, 200, 140));
            seedBg.Left.Set(20f, 0f);
            seedBg.Top.Set(50f, 0f);
            seedBg.Width.Set(-40f, 1f);
            seedBg.Height.Set(44f, 0f);
            panel.Append(seedBg);

            UIText seedLabel = new UIText("种子", 0.9f);
            seedLabel.Left.Set(12f, 0f);
            seedLabel.Top.Set(12f, 0f);
            seedLabel.TextColor = new Color(210, 222, 245);
            seedBg.Append(seedLabel);

            _lastSeed = GetCurrentSeed();
            _seedInput = new tContentPatch.Content.UI.UITextBox(_lastSeed);
            _seedInput.Text_MaxLength = 40;
            _seedInput.Left.Set(64f, 0f);
            _seedInput.Top.Set(6f, 0f);
            _seedInput.Width.Set(300f, 0f);
            _seedInput.Height.Set(32f, 0f);
            seedBg.Append(_seedInput);

            TextButton random = MakeButton("随机", panelW - 168f, 56f, 70f);
            random.Height.Set(32f, 0f);
            random.OnLeftClick += (evt, el) => SetSeedText(Main.rand.Next().ToString());
            panel.Append(random);

            TextButton clear = MakeButton("清空", panelW - 90f, 56f, 70f);
            clear.Height.Set(32f, 0f);
            clear.OnLeftClick += (evt, el) => SetSeedText("");
            panel.Append(clear);

            Box list = new Box(new Color(14, 18, 32, 175), new Color(90, 110, 170, 130));
            list.Left.Set(20f, 0f);
            list.Top.Set(listTop, 0f);
            list.Width.Set(-40f, 1f);
            list.Height.Set(listH, 0f);
            panel.Append(list);

            float top = 6f;
            foreach (SeedEntry entry in entries)
            {
                SeedEntry captured = entry;

                ToggleRow row = new ToggleRow(
                    captured.Title,
                    captured.Hint,
                    () =>
                    {
                        AwgConfig cfg = AwgConfig.Instance;

                        if (captured.IsNormal)
                            return cfg.Enabled == null || cfg.Enabled.Count == 0 || cfg.Has(captured.Key);

                        return cfg.Has(captured.Key);
                    },
                    () =>
                    {
                        AwgConfig cfg = AwgConfig.Instance;
                        SeedEntry normal = SeedCatalog.Normal();

                        if (captured.IsNormal)
                        {
                            cfg.Enabled.Clear();
                            cfg.Enabled.Add(captured.Key);
                        }
                        else
                        {
                            if (normal != null)
                                cfg.Enabled.Remove(normal.Key);

                            cfg.Set(captured.Key, !cfg.Has(captured.Key));
                        }

                        AwgConfig.Save();
                        RefreshCount();
                    },
                    () => ShowOptionDescription(captured),
                    ResetHint,
                    () => SeedIcons.FromOption(captured.Option) ?? SeedIcons.FromItem(captured.FallbackIcon));

                row.Width.Set(-20f, 1f);
                row.Height.Set(rowStep - 4f, 0f);
                row.Left.Set(10f, 0f);
                row.Top.Set(top, 0f);
                list.Append(row);
                top += rowStep;
            }

            // 十周年庆典的附加选项（AdvancedWorldGen 专有）
            AddExtraRow(list, ref top, "海滩出生", "需开启十周年庆典",
                () => AwgConfig.Instance.CelebrationBeachSpawn,
                v => AwgConfig.Instance.CelebrationBeachSpawn = v,
                ItemID.SandBlock);

            AddExtraRow(list, ref top, "彩漆着色", "需开启十周年庆典",
                () => AwgConfig.Instance.CelebrationPainted,
                v => AwgConfig.Instance.CelebrationPainted = v,
                ItemID.RainbowBrick);

            // 随机替换（AdvancedWorldGen 的 Random）
            AddExtraRow(list, ref top, "随机替换方块/墙", "每种方块随机换成另一种",
                () => AwgConfig.Instance.RandomWorld,
                v => AwgConfig.Instance.RandomWorld = v,
                ItemID.DirtBlock);

            AddExtraRow(list, ref top, "随机上色", "随机替换后每种方块随机刷漆",
                () => AwgConfig.Instance.RandomPainted,
                v => AwgConfig.Instance.RandomPainted = v,
                ItemID.RainbowBrick);

            Box hintBar = new Box(new Color(40, 52, 90, 180), new Color(90, 110, 170, 120));
            hintBar.Left.Set(20f, 0f);
            hintBar.Top.Set(hintTop, 0f);
            hintBar.Width.Set(-40f, 1f);
            hintBar.Height.Set(58f, 0f);
            panel.Append(hintBar);

            _hintText = new UIText(DefaultHint, 0.82f);
            _hintText.Left.Set(10f, 0f);
            _hintText.Top.Set(6f, 0f);
            _hintText.Width.Set(-20f, 1f);
            _hintText.Height.Set(-12f, 1f);
            _hintText.IsWrapped = true;
            _hintText.TextColor = new Color(210, 222, 245);
            hintBar.Append(_hintText);

            _countText = new UIText("", 0.85f);
            _countText.Left.Set(22f, 0f);
            _countText.Top.Set(countTop, 0f);
            _countText.TextColor = new Color(195, 210, 240);
            panel.Append(_countText);

            const int btnW = 150;
            const int gap = 12;

            TextButton back = MakeButton("返回", 20f, btnTop, btnW);
            back.OnLeftClick += (evt, el) =>
            {
                try
                {
                    if (_previous != null)
                        Main.MenuUI.SetState(_previous);
                }
                catch (Exception e)
                {
                    Log.Write("AWG: 返回失败: " + e.Message);
                }
            };
            panel.Append(back);

            TextButton all = MakeButton("全选", 20f + (btnW + gap), btnTop, btnW);
            all.OnLeftClick += (evt, el) =>
            {
                AwgConfig cfg = AwgConfig.Instance;
                cfg.Enabled.Clear();
                foreach (SeedEntry entry in SeedCatalog.All())
                {
                    if (!entry.IsNormal)
                        cfg.Enabled.Add(entry.Key);
                }
                AwgConfig.Save();
                RefreshCount();
            };
            panel.Append(all);

            TextButton none = MakeButton("清空", 20f + (btnW + gap) * 2, btnTop, btnW);
            none.OnLeftClick += (evt, el) =>
            {
                AwgConfig.Instance.Enabled.Clear();
                AwgConfig.Save();
                RefreshCount();
            };
            panel.Append(none);

            TextButton vanilla = MakeButton("原版高级选项", 20f + (btnW + gap) * 3, btnTop, btnW);
            vanilla.OnLeftClick += (evt, el) =>
            {
                try
                {
                    Main.MenuUI.SetState(new UIWorldCreationAdvanced(_previous));
                }
                catch (Exception e)
                {
                    Log.Write("AWG: 打开原版高级选项失败: " + e.Message);
                }
            };
            panel.Append(vanilla);

            RefreshCount();
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

        private string GetCurrentSeed()
        {
            try
            {
                return SeedField != null ? (SeedField.GetValue(_previous) as string) ?? "" : "";
            }
            catch
            {
                return "";
            }
        }

        private void ApplySeed(string text)
        {
            if (_previous == null)
                return;

            try
            {
                SeedField?.SetValue(_previous, text ?? "");
                SpecialSeedTextField?.SetValue(_previous, false);
                UpdateFieldsMethod?.Invoke(_previous, null);
            }
            catch (Exception e)
            {
                Log.Write("AWG: 写入种子失败: " + e.Message);
            }
        }

        private void SetSeedText(string text)
        {
            text = text ?? "";
            _lastSeed = text;
            ApplySeed(text);

            try
            {
                _seedInput?.SetText(text);
            }
            catch
            {
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (_seedInput == null)
                return;

            string current = _seedInput.Text ?? "";
            if (current == _lastSeed)
                return;

            _lastSeed = current;
            ApplySeed(current);
        }

        private void AddExtraRow(Box list, ref float top, string title, string hint,
            Func<bool> get, Action<bool> set, int iconItem)
        {
            ToggleRow row = new ToggleRow(
                title, hint, get,
                () =>
                {
                    set(!get());
                    AwgConfig.Save();
                },
                null, null,
                () => SeedIcons.FromItem(iconItem));

            row.Width.Set(-20f, 1f);
            row.Height.Set(36f, 0f);
            row.Left.Set(10f, 0f);
            row.Top.Set(top, 0f);
            list.Append(row);
            top += 40f;
        }

        private void ShowOptionDescription(SeedEntry entry)
        {
            if (_hintText == null)
                return;

            string text = entry != null ? entry.Description : null;

            if (string.IsNullOrEmpty(text))
            {
                try
                {
                    if (entry != null && entry.Option != null && entry.Option.Description != null)
                        text = entry.Option.Description.Value;
                }
                catch
                {
                }
            }

            if (string.IsNullOrEmpty(text))
                text = entry != null ? entry.Title : DefaultHint;

            _hintText.SetText(text);
        }

        private void ResetHint()
        {
            if (_hintText != null)
                _hintText.SetText(DefaultHint);
        }

        private void RefreshCount()        {
            if (_countText == null)
                return;

            AwgConfig cfg = AwgConfig.Instance;
            int count = 0;

            foreach (SeedEntry entry in SeedCatalog.All())
            {
                if (!entry.IsNormal && cfg.Has(entry.Key))
                    count++;
            }

            _countText.SetText("已选 " + count + " / " + Math.Max(0, SeedCatalog.All().Count - 1));
        }
    }

    internal class ToggleRow : UIElement
    {
        private readonly string _title;
        private readonly string _hint;
        private readonly Func<bool> _isOn;
        private readonly Action _toggle;
        private readonly Func<Texture2D> _icon;

        public ToggleRow(string title, string hint, Func<bool> isOn, Action toggle,
            Action onHover = null, Action onLeave = null, Func<Texture2D> icon = null)
        {
            _title = title;
            _hint = hint;
            _isOn = isOn;
            _toggle = toggle;
            _icon = icon;

            OnLeftClick += (evt, el) =>
            {
                try
                {
                    _toggle?.Invoke();
                }
                catch (Exception e)
                {
                    Log.Write("AWG: 切换种子失败: " + e.Message);
                }
            };

            if (onHover != null)
                OnMouseOver += (evt, el) => onHover();

            if (onLeave != null)
                OnMouseOut += (evt, el) => onLeave();
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle d = GetDimensions();
            Rectangle rect = new Rectangle((int)d.X, (int)d.Y, (int)d.Width, (int)d.Height);
            bool on = _isOn != null && _isOn();

            Color bg = on
                ? new Color(58, 96, 74, 190)
                : new Color(30, 38, 62, 190);

            if (IsMouseHovering)
                bg = new Color(
                    Math.Min(255, bg.R + 25),
                    Math.Min(255, bg.G + 25),
                    Math.Min(255, bg.B + 30),
                    bg.A);

            UiPaint.Fill(spriteBatch, rect, bg);
            UiPaint.Outline(spriteBatch, rect,
                on ? new Color(130, 210, 145, 160) : new Color(90, 110, 160, 110));

            int textX = 16;

            // 种子前面画一个小图标（原版物品或世界种子图标）
            Texture2D icon = _icon != null ? _icon() : null;
            if (icon != null && icon.Width > 0 && icon.Height > 0)
            {
                const float box = 26f;
                float scale = Math.Min(box / icon.Width, box / icon.Height);
                float w = icon.Width * scale;
                float h = icon.Height * scale;

                spriteBatch.Draw(icon,
                    new Vector2(rect.X + 8f + (box - w) / 2f, rect.Y + (rect.Height - h) / 2f),
                    null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

                textX = 8 + (int)box + 8;
            }

            UiPaint.Text(spriteBatch, _title,
                new Vector2(rect.X + textX, rect.Y + rect.Height / 2f - 11f),
                on ? Color.White : new Color(210, 218, 235), 0.98f);

            if (!string.IsNullOrEmpty(_hint))
            {
                Vector2 size = FontAssets.MouseText.Value.MeasureString(_title) * 0.98f;
                UiPaint.Text(spriteBatch, "· " + _hint,
                    new Vector2(rect.X + textX + size.X + 8f, rect.Y + rect.Height / 2f - 8f),
                    new Color(140, 158, 195), 0.72f);
            }

            Rectangle pill = new Rectangle(rect.Right - 66, rect.Y + 8, 52, 22);
            UiPaint.Pill(spriteBatch, pill, on);
        }
    }

    internal class TextButton : UIElement
    {
        private readonly string _text;

        public TextButton(string text)
        {
            _text = text;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle d = GetDimensions();
            Rectangle rect = new Rectangle((int)d.X, (int)d.Y, (int)d.Width, (int)d.Height);

            Color bg = IsMouseHovering
                ? new Color(96, 122, 196)
                : new Color(58, 74, 132);

            UiPaint.Fill(spriteBatch, rect, bg);
            UiPaint.Outline(spriteBatch, rect,
                IsMouseHovering ? new Color(190, 210, 255) : new Color(120, 140, 200), 2);

            if (IsMouseHovering)
            {
                UiPaint.Fill(spriteBatch,
                    new Rectangle(rect.X + 3, rect.Y + 3, rect.Width - 6, 2),
                    new Color(255, 255, 255, 45));
            }

            UiPaint.CenteredText(spriteBatch, _text, rect, Color.White, 1f);
        }
    }
}
