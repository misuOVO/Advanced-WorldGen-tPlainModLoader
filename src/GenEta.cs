using System;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.States;
using Terraria.UI;
using Terraria.WorldBuilding;

namespace AdvancedWorldGen
{
    /// <summary>
    /// 世界生成界面（UIWorldLoad）里增加一行：
    /// 已用时间 / 预计剩余时间（时:分:秒）/ 百分比。
    ///
    /// 直接作为 UI 子元素挂进原版界面，坐标和缩放都跟着原版进度条走。
    /// </summary>
    internal static class GenEta
    {
        public static void Apply(Harmony harmony)
        {
            try
            {
                ConstructorInfo ctor = typeof(UIWorldLoad).GetConstructor(Type.EmptyTypes);

                if (ctor == null)
                {
                    Log.Write("AWG: 找不到 UIWorldLoad 构造函数，无法显示预计时间");
                    return;
                }

                harmony.Patch(ctor, postfix: new HarmonyMethod(
                    typeof(GenEta).GetMethod(nameof(Postfix),
                        BindingFlags.NonPublic | BindingFlags.Static)));

                Log.Write("AWG: 已挂 UIWorldLoad（生成时间显示）");
            }
            catch (Exception e)
            {
                Log.Write("AWG: 挂生成时间显示失败: " + e.Message);
            }
        }

        private static void Postfix(UIWorldLoad __instance)
        {
            try
            {
                FieldInfo barField = typeof(UIWorldLoad).GetField(
                    "_progressBar",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                UIElement bar = barField != null ? barField.GetValue(__instance) as UIElement : null;

                if (bar == null)
                {
                    Log.Write("AWG: 没找到 UIWorldLoad 进度条，跳过时间显示");
                    return;
                }

                __instance.Append(new GenEtaPanel(bar));
            }
            catch (Exception e)
            {
                Log.Write("AWG: 添加生成时间显示失败: " + e.Message);
            }
        }
    }

    internal class GenEtaPanel : UIElement
    {
        private readonly UIElement _bar;
        private readonly Stopwatch _watch = Stopwatch.StartNew();

        private GenerationProgress _progress;
        private double _remainingSeconds = -1;
        private string _text = "";

        private bool _abortRequested;
        private bool _escHeld;

        public GenEtaPanel(UIElement bar)
        {
            _bar = bar;

            Height.Set(52f, 0f);
            HAlign = 0f;
            VAlign = 0f;
            IgnoresMouseInteraction = true;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            FollowBar();
            HandleCancelInput();
            UpdateText();
        }

        /// <summary>生成期间按 Esc 取消（原版会在当前 pass 结束后停下）。</summary>
        private void HandleCancelInput()
        {
            try
            {
                bool esc = Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape);

                if (esc && !_escHeld && !_abortRequested && WorldGen.generatingWorld)
                {
                    WorldGenerator.Controller controller = WorldGenerator.CurrentController;

                    if (controller != null && !controller.QueuedAbort)
                    {
                        controller.QueuedAbort = true;
                        _abortRequested = true;
                        Log.Write("AWG: 用户按 Esc 取消世界生成（"
                            + Main.maxTilesX + " x " + Main.maxTilesY + "）");
                    }
                }

                _escHeld = esc;
            }
            catch
            {
            }
        }

        /// <summary>贴在原版进度条正下方，跟随它随窗口高度移动。</summary>
        private void FollowBar()
        {
            try
            {
                if (_bar == null || Parent == null)
                    return;

                CalculatedStyle bar = _bar.GetDimensions();
                CalculatedStyle parent = Parent.GetInnerDimensions();

                Left.Set(bar.X - parent.X, 0f);
                Top.Set(bar.Y - parent.Y + bar.Height + 10f, 0f);
                Width.Set(bar.Width, 0f);
                Height.Set(52f, 0f);
                HAlign = 0f;
                VAlign = 0f;
            }
            catch
            {
            }
        }

        private void UpdateText()
        {
            try
            {
                GenerationProgress progress = WorldGenerator.CurrentGenerationProgress;

                if (progress == null)
                {
                    _text = "";
                    return;
                }

                if (!ReferenceEquals(progress, _progress))
                {
                    _progress = progress;
                    _remainingSeconds = -1;
                    _abortRequested = false;
                }

                double percent = progress.TotalProgress;

                if (double.IsNaN(percent) || percent < 0)
                    percent = 0;

                if (percent > 1)
                    percent = 1;

                if (_abortRequested)
                {
                    _text = "正在取消生成…（等待当前步骤结束，Esc 可重复按）";
                    return;
                }

                TimeSpan elapsed = _watch.Elapsed;

                if (percent > 0.003)
                {
                    double estimate = elapsed.TotalSeconds * (1.0 - percent) / percent;

                    if (estimate < 0 || double.IsNaN(estimate) || double.IsInfinity(estimate))
                        estimate = 0;

                    if (_remainingSeconds < 0)
                        _remainingSeconds = estimate;
                    else
                        _remainingSeconds = _remainingSeconds * 0.85 + estimate * 0.15;
                }

                string text = "已用 " + Format(elapsed);

                if (_remainingSeconds >= 0)
                    text += "   ·   预计剩余 " + Format(TimeSpan.FromSeconds(_remainingSeconds));
                else
                    text += "   ·   预计剩余 计算中…";

                text += "   (" + (percent * 100.0).ToString("0.0") + "%)";

                _text = text;
            }
            catch
            {
                _text = "";
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (string.IsNullOrEmpty(_text))
                return;

            try
            {
                CalculatedStyle d = GetDimensions();
                Vector2 size = FontAssets.MouseText.Value.MeasureString(_text);

                Vector2 position = new Vector2(
                    d.X + (d.Width - size.X) * 0.5f,
                    d.Y + 2f);

                UiPaint.Text(spriteBatch, _text, position, new Color(235, 242, 255), 1f);

                const string hint = "按 Esc 取消生成";
                Vector2 hintSize = FontAssets.MouseText.Value.MeasureString(hint) * 0.82f;

                UiPaint.Text(spriteBatch, hint,
                    new Vector2(d.X + (d.Width - hintSize.X) * 0.5f, d.Y + 26f),
                    new Color(185, 198, 225), 0.82f);
            }
            catch
            {
            }
        }

        private static string Format(TimeSpan time)
        {
            if (time.TotalHours >= 1)
            {
                return ((int)time.TotalHours) + "时"
                    + time.Minutes.ToString("00") + "分"
                    + time.Seconds.ToString("00") + "秒";
            }

            return time.Minutes + "分" + time.Seconds.ToString("00") + "秒";
        }
    }
}
