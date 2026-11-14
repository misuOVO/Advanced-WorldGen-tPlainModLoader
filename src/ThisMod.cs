using System;
using tContentPatch;
using tContentPatch.ModLoad;
using tContentPatch.Patch;

namespace AdvancedWorldGen
{
    /// <summary>
    /// tPlainModLoader 入口：高级世界生成（秘密种子组合）移植版。
    /// </summary>
    public class ThisMod : Mod
    {
        public override void Load(ModObject mo)
        {
            Log.Write("AWG.Load key=" + (mo != null && mo.config != null ? mo.config.key : "?"));

            try
            {
                AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
                    Log.Write("!!! AWG 未处理异常: " + e.ExceptionObject);
            }
            catch
            {
            }

            AwgConfig.Load();
        }

        public override void AddPatch(IAddPatch addPatch)
        {
            WorldGenHooks.Apply(addPatch);
        }

        public override void Loaded()
        {
            Log.Write("AWG.Loaded");
        }
    }
}
