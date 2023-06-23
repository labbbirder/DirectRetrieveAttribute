using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Plastic.Newtonsoft.Json.Linq;

namespace com.bbbirder.unityeditor{
    static class PackageUtils {

        public static string GetPackageVersion([CallerFilePath]string csPath = null){
            return (string)GetPackageJson(csPath)["version"];
        }

        public static JObject GetPackageJson([CallerFilePath]string csPath = null){
            var path = GetPackagePath(csPath);
            var jsonPath = Path.Join(path,"package.json");
            if(!File.Exists(jsonPath)){
                throw new ($"not package.json located in {path}");
            }
            var json = File.ReadAllText(jsonPath);
            return JObject.Parse(json);
        }

        public static string GetPackagePath([CallerFilePath]string csPath = null){
            var pathSplit = csPath.Split('/','\\').ToList();
            var packageIdx = pathSplit.LastIndexOf("Packages");
            var targetIdx = -~packageIdx;
            if(targetIdx == 0 || targetIdx >= pathSplit.Count ){
                throw new($"file is not under a valid Packages folder:{csPath}");
            }
            return string.Join("/",pathSplit.Take(-~targetIdx));
        }
        
        public static string GetPackageName([CallerFilePath]string csPath = null){
            var path = GetPackagePath(csPath);
            return Path.GetFileName(path);
        }
        
        public static bool IsOutDate(string targetPath,string srcPath){
            if(!File.Exists(targetPath)) return true;
            if(!File.Exists(srcPath)) return false;
            var tarTime = File.GetLastWriteTimeUtc(targetPath);
            var srcTime = File.GetLastWriteTimeUtc(srcPath);
            return tarTime < srcTime;
        }

        public static void UpdateFileDate(string filePath){
            File.SetLastWriteTimeUtc(filePath, DateTime.UtcNow);
        }
    }
}