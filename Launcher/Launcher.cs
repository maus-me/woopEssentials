using System;
using System.IO;
using System.Linq;
using Vintagestory.Server;

namespace Launcher
{
    public class Launcher
    {
        public static void Main(string[] args)
        {
            var libs = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Lib");
            var files = Directory.GetFiles(libs, "*.so*").ToList();
            // files.AddRange(Directory.GetFiles(libs, "Mono*").ToList());
            foreach (var file in files)
            {
                var native = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(file));
                if(!File.Exists(native))
                    File.Copy(file,native);
            }
            ServerProgram.Main(args);
        }
    }
}
