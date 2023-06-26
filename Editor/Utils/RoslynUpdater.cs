using System.Collections;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace com.bbbirder.unityeditor{
    /// <summary>
    /// A manager for update roslyn analyzers reasonably
    /// </summary>
    public abstract class RoslynUpdater{
        string SourceDllPath {get;}

        string packageName {get;set;}
        string packagePath {get;set;}
        string packageVersion {get;set;}
        
        string ExportPath => "Assets/Plugins/"+packageName;
        string AssetDllPath => Path.Join(ExportPath,Path.GetFileName(SourceDllPath));
        string UnityPackagePath => Path.Join(packagePath,"plugin.unitypackage");
        
        bool HasNewSourceDll => PackageUtils.IsOutDate(AssetDllPath,SourceDllPath);
        bool HasNewPackage => PackageUtils.IsOutDate(AssetDllPath,UnityPackagePath);

        public RoslynUpdater(string srcDllPath, [CallerFilePath]string csPath = null){
            if(string.IsNullOrEmpty(srcDllPath)){
                throw new($"srcDllPath can't be empty");
            }
            SourceDllPath = srcDllPath;
            packageName = PackageUtils.GetPackageName(csPath);
            packagePath = PackageUtils.GetPackagePath(csPath);
            packageVersion = PackageUtils.GetPackageVersion(csPath);
        }
        protected void RunWorkFlow()
        {
            if(string.IsNullOrEmpty(packageName)){
                throw new($"should setup package first");
            }
            if(File.Exists(SourceDllPath) && HasNewSourceDll) {
                UpdateUnityPackage();
            }else{
                if(HasNewPackage){
                    InstallUnityPackage();
                }
            }
        }
        void UpdateUnityPackage(){
            var co = UpdatePackageProcessProgress().GetEnumerator();
            EditorApplication.update += EditorUpdate;
            
            void EditorUpdate(){
                try{
                    if(!co.MoveNext()) 
                        ClearState();
                }catch{
                    ClearState();
                }
            }
            void ClearState(){
                EditorApplication.update -= EditorUpdate;
                EditorUtility.ClearProgressBar();
                EditorApplication.UnlockReloadAssemblies();
            }
        }

        IEnumerable UpdatePackageProcessProgress(){
            EditorApplication.LockReloadAssemblies();
            Directory.CreateDirectory(Path.GetDirectoryName(AssetDllPath));
            File.Copy(SourceDllPath,AssetDllPath,true);
            AssetDatabase.ImportAsset(AssetDllPath);
            if(EditorUtility.DisplayCancelableProgressBar("RoslynUpdater","waiting for roslyn dll",0.1f)){
                yield break;
            }
            Object dllAsset = null;
            while(!(dllAsset=AssetDatabase.LoadAssetAtPath<Object>(AssetDllPath))){
                yield return null;
            }
            EditorUtility.DisplayProgressBar("RoslynUpdater","processing dll",0.2f);
            var importer = AssetImporter.GetAtPath(AssetDllPath) as PluginImporter;
            importer.SetCompatibleWithAnyPlatform(false); 
            importer.SetCompatibleWithEditor(false);
            importer.SetCompatibleWithPlatform("Standalone",false);
            AssetDatabase.SetLabels(dllAsset,new string[]{"RoslynAnalyzer"});
            AssetDatabase.SaveAssetIfDirty(dllAsset);
            importer.SaveAndReimport();
            AssetDatabase.ExportPackage(new string[]{
                AssetDllPath,
                AssetDllPath+".meta",
            }, UnityPackagePath, ExportPackageOptions.Recurse);
            AssetDatabase.Refresh();
            PackageLog(packageName,"package updated!",packageVersion);

        }
        void InstallUnityPackage(){
            AssetDatabase.ImportPackage(UnityPackagePath,false);
            if(File.Exists(AssetDllPath)){
                PackageUtils.UpdateFileDate(AssetDllPath);
            }
            PackageLog(packageName,"plugin updated!",packageVersion);
        }
        static void PackageLog(params object[] args){
            Debug.Log("[Roslyn Updater] "+string.Join("  ",args));
        }
    }

}
