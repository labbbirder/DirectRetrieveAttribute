using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditorInternal;
using UnityEngine;

internal partial class Installer
{
    const string ExportPath = "Assets/Plugins/com.bbbirder.DirectAttribute";
    const string DllName = "DirectAttributes.SourceGenerator.dll";
    const string AssetGUID = "600e34158b49ddc4eae10d068763849b";
    [DidReloadScripts]
    static void CheckAndInstallUnityPackage()
    {
        if(IsOutDate()){
            Debug.Log("install package com.bbbirder.DirectAttribute");
            var assetPath = AssetDatabase.GUIDToAssetPath(AssetGUID);
            AssetDatabase.ImportPackage(assetPath,false);
            var dllPath = Path.Join(ExportPath,DllName);
            if(File.Exists(dllPath)){
                File.SetLastWriteTimeUtc(dllPath, DateTime.UtcNow);
            }
        }
    }
    static bool IsOutDate(){
        var dllPath = Path.Join(ExportPath,DllName);
        if(!File.Exists(dllPath)) return true;
        var pkgPath = AssetDatabase.GUIDToAssetPath(AssetGUID);
        var dllTime = File.GetLastWriteTimeUtc(dllPath);
        var pkgTime = File.GetLastWriteTimeUtc(pkgPath);
        return dllTime < pkgTime;
    }
}