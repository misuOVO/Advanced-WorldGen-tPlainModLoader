using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.UI;
using tContentPatch.Content.UI;

namespace AdvancedWorldGen
{
    /// <summary>
    /// 自定义世界大小面板：数字输入 + 预设按钮。
    /// </summary>
    internal class SizeState : UIState
    {
        private readonly UIWorldCreation _previous;

        private tContentPatch.Content.UI.UITextBox _widthBox;
        private tContentPatch.Content.UI.UITextBox _heightBox;
        private string _lastWidth = "";
        private string _lastHeight = "";

        public SizeState(UIWorldCreation previous)
        {
            _previous = previous;
            Build();
        }

        private void Build()
        {
            const int panelW = 640;
            const int panelH = 430;

            UIPanel panel = new UIPanel();
            panel.Width.Set(panelW, 0f);
            panel.Height.Set(panelH, 0f);
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
            area.Height.Set(150f, 0f);
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

            AddNumberRow(area, 58f, "宽度（格）", 4200, 10000,
                AwgConfig.Instance.CustomWidth, out _widthBox, ref _lastWidth);
            AddNumberRow(area, 100f, "高度（格）", 1200, 3000,
                AwgConfig.Instance.CustomHeight, out _heightBox, ref _lastHeight);

            UIText hint = new UIText(
                "范围：宽度 4200~10000，高度 1200~3000。超出原版尺寸会重新分配地图数组。",
                0.76f);
            hint.Left.Set(22f, 0f);
            hint.Top.Set(214f, 0f);
            hint.Width.Set(-44f, 1f);
            hint.IsWrapped = true;
            hint.TextColor = new Color(190, 205, 235);
            panel.Append(hint);

            float by = 268f;
            int bw = 136;
            int gap = 12;

            TextButton back = MakeButton("返回", 20f, by, bw);
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

            TextButton small = MakeButton("小 4200x1200", 20f + bw + gap, by, bw);
            small.OnLeftClick += (evt, el) => ApplyPreset(4200, 1200);
            panel.Append(small);

            TextButton medium = MakeButton("中 6400x1800", 20f + (bw + gap) * 2, by, bw);
            medium.OnLeftClick += (evt, el) => ApplyPreset(6400, 1800);
            panel.Append(medium);

            TextButton large = MakeButton("大 8400x2400", 20f + (bw + gap) * 3, by, bw);
            large.OnLeftClick += (evt, el) => ApplyPreset(8400, 2400);
            panel.Append(large);

            TextButton huge = MakeButton("超大 9000x2700", 20f, by + 50f, 190f);
            huge.OnLeftClick += (evt, el) => ApplyPreset(9000, 2700);
            panel.Append(huge);

            TextButton max = MakeButton("极限 10000x3000", 20f + 190f + gap, by + 50f, 190f);
            max.OnLeftClick += (evt, el) => ApplyPreset(10000, 3000);
            panel.Append(max);
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
            box.SetTextScale(0.9f);
            box.BackgroundColor = new Color(10, 12, 22) * 0.95f;
            box.BorderColor = new Color(120, 140, 200);
            box.Left.Set(200f, 0f);
            box.Top.Set(top, 0f);
            box.Width.Set(150f, 0f);
            box.Height.Set(34f, 0f);
            area.Append(box);

            string capturedLast = box.Text;
            last = capturedLast;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            ApplyNumber(_widthBox, ref _lastWidth, 4200, 10000, v =>
            {
                AwgConfig.Instance.CustomWidth = v;
                AwgConfig.Instance.CustomSize = true;
            });

            ApplyNumber(_heightBox, ref _lastHeight, 1200, 3000, v =>
            {
                AwgConfig.Instance.CustomHeight = v;
                AwgConfig.Instance.CustomSize = true;
            });
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
        }
    }
}
