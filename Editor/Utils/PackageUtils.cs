using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace BBBirder.DirectAttribute.Editor
{
    static class PackageUtils
    {
        [Serializable]
        public class PackageJson
        {
            public string name;
            public string version;
            public string description;
            public string displayName;
        }

        public static string GetPackageVersion([CallerFilePath] string csPath = null)
        {
            return GetPackageJson(csPath).version;
        }

        public static string GetPackageName([CallerFilePath] string csPath = null)
        {
            return GetPackageJson(csPath).name;
        }

        public static PackageJson GetPackageJson([CallerFilePath] string csPath = null)
        {
            var path = GetPackagePath(csPath);
            var jsonPath = Path.Join(path, "package.json");
            if (!File.Exists(jsonPath))
            {
                throw new($"not package.json located in {path}");
            }

            var json = File.ReadAllText(jsonPath);
            return JsonUtility.FromJson<PackageJson>(json);
        }

        public static string GetPackagePath([CallerFilePath] string csPath = null)
        {
            var cwd = Path.GetFullPath(Environment.CurrentDirectory);
            var cur = Path.GetFullPath(Path.GetDirectoryName(csPath));

            while (cur.StartsWith(cwd))
            {
                if (File.Exists(Path.Join(cur, "package.json")))
                {
                    return cur;
                }

                var tmp = Path.GetDirectoryName(cur);
                if (tmp.Length == cur.Length)
                {
                    throw new($"unexpected parent path:{tmp}");
                }

                cur = tmp;
            }

            throw new($"file is not under a valid Packages folder:{csPath}");
        }

        public static bool IsOutDate(string targetPath, string srcPath)
        {
            if (!File.Exists(targetPath)) return true;
            if (!File.Exists(srcPath)) return false;
            var tarTime = File.GetLastWriteTimeUtc(targetPath);
            var srcTime = File.GetLastWriteTimeUtc(srcPath);
            if (tarTime >= srcTime) return false; // target is newer
            var tarBytes = File.ReadAllBytes(targetPath);
            var srcBytes = File.ReadAllBytes(srcPath);
            return !tarBytes.AsSpan().SequenceEqual(srcBytes); // file content compare
        }

        public static void UpdateFileDate(string filePath)
        {
            File.SetLastWriteTimeUtc(filePath, DateTime.UtcNow);
        }
    }
}
