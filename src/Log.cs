using System;
using System.IO;

namespace AdvancedWorldGen
{
    internal static class Log
    {
        private static readonly string FilePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AdvancedWorldGen.log");

        public static void Write(string message)
        {
            try
            {
                File.AppendAllText(FilePath, DateTime.Now.ToString("HH:mm:ss.fff") + " " + message + Environment.NewLine);
            }
            catch
            {
            }
        }
    }
}
