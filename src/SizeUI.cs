using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.UI;
using tContentPatch.Content.UI;

namespace AdvancedWorldGen
{
    /// <summary>
    /// 自定义世界大小面板：数字输入 + 预设 + 省内存模式。
    /// </summary>
    internal class SizeState : UIState
    {
        private const int PanelW = 660;
        private const int PanelH = 560;

        private readonly UIWorldCreation _previous;

        private tContentPatch.Content.UI.UITextBox _widthBox;
        private tContentPatch.Content.UI.UITextBox _heightBox;
        private UIText _memoryText;
        private UIText _status;
        private string _lastWidth = "";
        private string _lastHeight = "";
        private int _memoryTimer;

        public SizeState(UIWorldCreation previous)
        {
            _previous = previous;
            Build();
        }

        private void Build()
        {
            UIPanel panel = new UIPanel();
            panel.Width.Set(PanelW, 0f);
            panel.Height.Set(PanelH, 0f);
            panel.HAlign = 0.5f;
            panel.VAlign = 0.5f;
            panel.SetPadding(0f);
            Append(panel);

            UIText title = new UIText("世界大小", 1.3f, true);
            title.HAlign = 0.5f;
            title.Top.Set(12f, 0f);
            panel.Append(title);

            Box area = new Box(new Color(14, 18, 32, 175), new Color(90, 110, 170, 130));
            area.Left.Set(20f, 0f);
            area.Top.Set(56f, 0f);
            area.Width.Set(-40f, 1f);
            area.Height.Set(206f, 0f);
            panel.Append(area);

            ToggleRow enable = new ToggleRow(
                "启用自定义大小", "原版上限 8400 x 2400",
                () => AwgConfig.Instance.CustomSize,
                () =>
                {
                    AwgConfig.Instance.CustomSize = !AwgConfig.Instance.CustomSize;
                    AwgConfig.Save();
                });

            enable.Width.Set(-20f, 1f);
            enable.Height.Set(38f, 0f);
            enable.Left.Set(10f, 0f);
            enable.Top.Set(8f, 0f);
            area.Append(enable);

            AddNumberRow(area, 58f, "宽度（格）", 4200, 12000,
                AwgConfig.Instance.CustomWidth, out _widthBox, ref _lastWidth);
            AddNumberRow(area, 100f, "高度（格）", 1200, 3600,
                AwgConfig.Instance.CustomHeight, out _heightBox, ref _lastHeight);

            ToggleRow saver = new ToggleRow(
                "省内存生成", "生成前释放旧世界/地图数据，生成后再恢复",
                () => AwgConfig.Instance.MemorySaver,
                () =>
                {
                    AwgConfig.Instance.MemorySaver = !AwgConfig.Instance.MemorySaver;
                    AwgConfig.Save();
                });

            saver.Width.Set(-20f, 1f);
            saver.Height.Set(38f, 0f);
            saver.Left.Set(10f, 0f);
            saver.Top.Set(142f, 0f);
            area.Append(saver);

            UIText hint = new UIText(
                "范围：宽度 4200~12000，高度 1200~3600。开启「省内存生成」时可以少占几百 MB，更容易生成超大世界。",
                0.76f);
            hint.Left.Set(22f, 0f);
            hint.Top.Set(270f, 0f);
            hint.Width.Set(-44f, 1f);
            hint.IsWrapped = true;
            hint.TextColor = new Color(190, 205, 235);
            panel.Append(hint);

            _memoryText = new UIText("", 0.78f);
            _memoryText.Left.Set(22f, 0f);
            _memoryText.Top.Set(316f, 0f);
            _memoryText.TextColor = new Color(255, 226, 150);
            panel.Append(_memoryText);

            TextButton recommend = MakeButton("推荐尺寸", 20f, 342f, 150f);
            recommend.OnLeftClick += (evt, el) => RecommendSize();
            panel.Append(recommend);

            UIText recommendHint = new UIText("按当前内存选最大的可行尺寸", 0.75f);
            recommendHint.Left.Set(182f, 0f);
            recommendHint.Top.Set(352f, 0f);
            recommendHint.TextColor = new Color(190, 205, 235);
            panel.Append(recommendHint);

            const int bw = 198;
            const int gap = 12;
            float row1 = 388f;
            float row2 = 438f;

            TextButton small = MakeButton("小 4200x1200", 20f, row1, bw);
            small.OnLeftClick += (evt, el) => ApplyPreset(4200, 1200);
            panel.Append(small);

            TextButton medium = MakeButton("中 6400x1800", 20f + bw + gap, row1, bw);
            medium.OnLeftClick += (evt, el) => ApplyPreset(6400, 1800);
            panel.Append(medium);

            TextButton large = MakeButton("大 8400x2400", 20f + (bw + gap) * 2, row1, bw);
            large.OnLeftClick += (evt, el) => ApplyPreset(8400, 2400);
            panel.Append(large);

            TextButton huge = MakeButton("超大 9000x2700", 20f, row2, bw);
            huge.OnLeftClick += (evt, el) => ApplyPreset(9000, 2700);
            panel.Append(huge);

            TextButton max = MakeButton("极限 10000x3000", 20f + bw + gap, row2, bw);
            max.OnLeftClick += (evt, el) => ApplyPreset(10000, 3000);
            panel.Append(max);

            TextButton mega = MakeButton("巨构 12000x3600", 20f + (bw + gap) * 2, row2, bw);
            mega.OnLeftClick += (evt, el) => ApplyPreset(12000, 3600);
            panel.Append(mega);

            _status = new UIText("", 0.75f);
            _status.Left.Set(22f, 0f);
            _status.Top.Set(488f, 0f);
            _status.Width.Set(-44f, 1f);
            _status.TextColor = new Color(200, 214, 240);
            panel.Append(_status);

            TextButton back = MakeButton("返回", 20f, 516f, 150f);
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

            RefreshMemoryText();
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

        private void AddNumberRow(Box area, float top, string label, int min, int max, int value,
            out tContentPatch.Content.UI.UITextBox box, ref string last)
        {
            UIText text = new UIText(label, 0.86f);
            text.Left.Set(14f, 0f);
            text.Top.Set(top + 6f, 0f);
            text.TextColor = new Color(215, 225, 245);
            area.Append(text);

            box = new tContentPatch.Content.UI.UITextBox(value.ToString());
            box.Text_MaxLength = 5;
            box.Left.Set(200f, 0f);
            box.Top.Set(top, 0f);
            box.Width.Set(150f, 0f);
            box.Height.Set(34f, 0f);
            box.SetText(value.ToString());
            area.Append(box);

            last = value.ToString();
        }

        private long CurrentMb()
        {
            try
            {
                return Process.GetCurrentProcess().PrivateMemorySize64 / 1024 / 1024;
            }
            catch
            {
                return 0;
            }
        }

        private static long EstimateMb(int width, int height)
        {
            return (long)width * height * 60L / 1024 / 1024;
        }

        private void RefreshMemoryText()
        {
            if (_memoryText == null)
                return;

            try
            {
                AwgConfig cfg = AwgConfig.Instance;
                bool laa = WorldGenHooks.IsLargeAddressAware();

                int width = Math.Max(4200, Math.Min(12000, cfg.CustomWidth));
                int height = Math.Max(1200, Math.Min(3600, cfg.CustomHeight));

                long budget = laa ? 3400 : 1500;

                _memoryText.SetText("当前内存 " + CurrentMb() + " MB · 预计 "
                    + EstimateMb(width, height) + " MB · 预算 " + budget + " MB（"
                    + (laa ? "LAA 已开启" : "LAA 未开启") + "）");
            }
            catch
            {
            }
        }

        private void RecommendSize()
        {
            bool laa = WorldGenHooks.IsLargeAddressAware();
            long budget = laa ? 3_400_000_000L : 1_500_000_000L;
            long current = 0;

            try
            {
                current = Process.GetCurrentProcess().PrivateMemorySize64;
            }
            catch
            {
            }

            int[,] sizes =
            {
                { 12000, 3600 },
                { 10000, 3000 },
                { 9000, 2700 },
                { 8400, 2400 },
                { 6400, 1800 },
                { 4200, 1200 },
            };

            for (int i = 0; i < sizes.GetLength(0); i++)
            {
                long estimate = (long)sizes[i, 0] * sizes[i, 1] * 60L;

                if (current + estimate <= budget)
                {
                    ApplyPreset(sizes[i, 0], sizes[i, 1]);

                    if (_status != null)
                    {
                        _status.SetText("推荐：" + sizes[i, 0] + " x " + sizes[i, 1]
                            + "（预估 " + (estimate / 1024 / 1024) + " MB）");
                    }

                    return;
                }
            }

            ApplyPreset(4200, 1200);
            _status?.SetText("内存太紧，推荐先用小世界。");
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            ApplyNumber(_widthBox, ref _lastWidth, 4200, 12000, v =>
            {
                AwgConfig.Instance.CustomWidth = v;
                AwgConfig.Instance.CustomSize = true;
            });

            ApplyNumber(_heightBox, ref _lastHeight, 1200, 3600, v =>
            {
                AwgConfig.Instance.CustomHeight = v;
                AwgConfig.Instance.CustomSize = true;
            });

            if (--_memoryTimer <= 0)
            {
                _memoryTimer = 30;
                RefreshMemoryText();
            }
        }

        private static void ApplyNumber(tContentPatch.Content.UI.UITextBox box, ref string last,
            int min, int max, Action<int> set)
        {
            if (box == null)
                return;

            string text = box.Text ?? "";
            if (text == last)
                return;

            last = text;

            if (!int.TryParse(text, out int value))
                return;

            value = Math.Max(min, Math.Min(max, value));
            set(value);
            AwgConfig.Save();
        }

        private void ApplyPreset(int width, int height)
        {
            AwgConfig cfg = AwgConfig.Instance;
            cfg.CustomSize = true;
            cfg.CustomWidth = width;
            cfg.CustomHeight = height;
            AwgConfig.Save();

            try
            {
                _widthBox?.SetText(width.ToString());
                _heightBox?.SetText(height.ToString());
                _lastWidth = width.ToString();
                _lastHeight = height.ToString();
            }
            catch
            {
            }

            RefreshMemoryText();
        }
    }
}
