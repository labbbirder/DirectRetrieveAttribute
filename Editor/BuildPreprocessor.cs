using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace com.bbbirder.unityeditor
{
    class BuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.IsDefined(typeof(GeneratedDirectRetrieveAttribute)))
                ;


            XmlDocument doc = new XmlDocument();
            var eleLinker = doc.CreateElement("linker");
            doc.AppendChild(eleLinker);
            foreach (var assembly in assemblies)
            {
                var types = assembly.GetCustomAttributes<GeneratedDirectRetrieveAttribute>()
                    .Select(dra => dra.type)
                    ;
                var eleAsm = doc.CreateElement("assembly");
                eleAsm.SetAttribute("fullname", assembly.GetName().Name);
                eleLinker.AppendChild(eleAsm);
                foreach (var t in types)
                {
                    var eleType = doc.CreateElement("type");
                    eleType.SetAttribute("preserve", "all");
                    eleType.SetAttribute("fullname", t.FullName);
                    eleAsm.AppendChild(eleType);
                }
            }

            var targetPath = $"Assets/Generated/{PackageUtils.GetPackageName()}/link.xml";
            var targetDir = Path.GetDirectoryName(targetPath);
            if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
            File.WriteAllText(targetPath, doc.InnerXml);

            AssetDatabase.ImportAsset(targetPath);
        }
    }
}