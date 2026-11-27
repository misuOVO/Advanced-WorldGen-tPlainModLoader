using System;
using System.IO;
using System.Reflection;

namespace AdvancedWorldGen
{
    /// <summary>
    /// 直接开关游戏 / 加载器 exe 的 LAA（Large Address Aware）标志。
    /// 32 位进程开启后可以使用约 4GB 地址空间，但需要重启游戏才生效。
    ///
    /// 注意：这里改的是磁盘上的 PE 头，等于给 exe 打补丁。
    /// 首次修改会备份为 &lt;文件名&gt;.bak。
    /// </summary>
    internal static class LaaToggle
    {
        private static readonly string[] Targets =
        {
            "Terraria.exe",
            "tPlainModLoader.exe",
            "tPlainModLoaderInjector.exe",
        };

        private const ushort LaaFlag = 0x0020;

        private static bool _cached;
        private static DateTime _cacheTime = DateTime.MinValue;

        public static bool IsOn()
        {
            if (DateTime.UtcNow - _cacheTime < TimeSpan.FromSeconds(3))
                return _cached;

            _cached = Compute();
            _cacheTime = DateTime.UtcNow;
            return _cached;
        }

        public static bool SetEnabled(bool enabled)
        {
            int changed = 0;
            int failed = 0;

            foreach (string name in Targets)
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, name);

                if (!File.Exists(path))
                    continue;

                try
                {
                    byte[] bytes = File.ReadAllBytes(path);
                    int offset = CharOffset(bytes);
                    ushort chars = BitConverter.ToUInt16(bytes, offset);
                    bool has = (chars & LaaFlag) != 0;

                    if (has == enabled)
                        continue;

                    string backup = path + ".bak";
                    if (!File.Exists(backup))
                    {
                        try
                        {
                            File.Copy(path, backup);
                        }
                        catch
                        {
                        }
                    }

                    chars = enabled
                        ? (ushort)(chars | LaaFlag)
                        : (ushort)(chars & ~LaaFlag);

                    BitConverter.GetBytes(chars).CopyTo(bytes, offset);
                    File.WriteAllBytes(path, bytes);
                    changed++;
                }
                catch (Exception e)
                {
                    failed++;
                    Log.Write("AWG: 修改 LAA 失败 " + name + ": " + e.Message);
                }
            }

            _cached = enabled;
            _cacheTime = DateTime.UtcNow;

            Log.Write("AWG: LAA " + (enabled ? "开启" : "关闭")
                + " 完成（修改 " + changed + " 个，失败 " + failed + " 个）");

            return failed == 0;
        }

        private static bool Compute()
        {
            bool found = false;

            foreach (string name in Targets)
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, name);

                if (!File.Exists(path))
                    continue;

                try
                {
                    if (!ReadFlag(path))
                        return false;

                    found = true;
                }
                catch
                {
                    return false;
                }
            }

            return found;
        }

        private static bool ReadFlag(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                fs.Position = 0x3C;
                int peOffset = reader.ReadInt32();

                if (peOffset <= 0 || peOffset + 0x18L >= fs.Length)
                    throw new Exception("PE 头无效");

                fs.Position = peOffset + 0x16;
                ushort chars = reader.ReadUInt16();
                return (chars & LaaFlag) != 0;
            }
        }

        private static int CharOffset(byte[] bytes)
        {
            if (bytes.Length < 0x40)
                throw new Exception("不是有效的 PE 文件");

            int peOffset = BitConverter.ToInt32(bytes, 0x3C);

            if (peOffset <= 0 || peOffset + 0x18 >= bytes.Length)
                throw new Exception("PE 头无效");

            if (BitConverter.ToUInt32(bytes, peOffset) != 0x00004550)
                throw new Exception("缺少 PE 签名");

            return peOffset + 0x16;
        }
    }
}
