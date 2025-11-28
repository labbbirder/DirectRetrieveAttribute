using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.UnityLinker;
using UnityEngine;

namespace BBBirder.DirectAttribute.Editor
{
    class RootPreserver :
#if UNITY_2019_4_OR_NEWER
        IUnityLinkerProcessor
#else
        IPreprocessBuildWithReport
#endif
    {
        private static BindingFlags bindingFlags = 0
            | BindingFlags.Instance
            | BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Static
            | BindingFlags.DeclaredOnly
            ;

        public int callbackOrder => 0;

        public string GenerateAdditionalLinkXmlFile(BuildReport report, UnityLinkerBuildPipelineData data)
        {
            var targetPath = $"Library/Generated/{PackageUtils.GetPackageName()}/link.xml";
            var roots = GetPreservedRoots();
            WriteAdditionalLinkXmlToFile(targetPath, roots);
            return Path.GetFullPath(targetPath);
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            var targetPath = $"Assets/Generated/{PackageUtils.GetPackageName()}/link.xml";
            var roots = GetPreservedRoots();
            WriteAdditionalLinkXmlToFile(targetPath, roots);
            AssetDatabase.ImportAsset(targetPath);
        }

        public MemberInfo[] GetPreservedRoots()
        {
            var rootsByAttribute = Retriever.GetAllAttributesInCurrentAppDomain()
                .Where(a => a.PreserveTarget)
                .Select(a => a.TargetMember);
            var rootsByType = Retriever.GetAllSubtypesInCurrentAppDomain()
                .Select(IsTypePreservedByBaseType);

            return rootsByAttribute
                .Concat(rootsByType.OfType<MemberInfo>())
                .Distinct()
                .ToArray()
                ;

            static bool IsTypePreservedByBaseType(Type type)
            {
                if (type is null) return false;

                foreach (var interf in type.GetInterfaces())
                {
                    if (interf.GetCustomAttribute<RetrieveSubtypeAttribute>()?.PreserveSubtypes == true)
                    {
                        return true;
                    }
                }

                for (; type != null; type = type.BaseType)
                {
                    if (type.GetCustomAttribute<RetrieveSubtypeAttribute>()?.PreserveSubtypes == true)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void WriteAdditionalLinkXmlToFile(string filePath, params MemberInfo[] roots)
        {
            // refer to https://docs.unity3d.com/Manual/managed-code-stripping-xml-formatting.html
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(Retriever.IsValidAssembly)
                ;

            XmlDocument doc = new XmlDocument();
            var eleLinker = doc.CreateElement("linker");
            doc.AppendChild(eleLinker);


            var assemblyToRootsGroups = roots.GroupBy(m => m.Module.Assembly);
            foreach (var assemblyGroup in assemblyToRootsGroups)
            {
                var assembly = assemblyGroup.Key;
                var typeToMembersGroups = assemblyGroup
                    .Select(r => r is Type type ? (type, null) : (type: r.DeclaringType, member: r))
                    .GroupBy(r => r.type);

                var eleAsm = doc.CreateElement("assembly");
                eleAsm.SetAttribute("fullname", assembly.GetName().Name);
                eleLinker.AppendChild(eleAsm);
                foreach (var typeGroup in typeToMembersGroups)
                {
                    var type = typeGroup.Key;
                    var memberNames = typeGroup.Select(r => r.member?.Name);

                    var eleType = doc.CreateElement("type");
                    //Fix: change "name" to "fullname", it doesn't work as Unity Manual says.
                    eleType.SetAttribute("fullname", type.FullName.Replace("+", "/"));
                    eleType.SetAttribute("preserve", "nothing");

                    foreach (var memberName in memberNames)
                    {
                        if (string.IsNullOrEmpty(memberName)) continue;

                        var members = type.GetMember(memberName, bindingFlags).Where(
                            member => member.GetCustomAttribute<DirectRetrieveAttribute>() != null
                        );

                        foreach (var member in members)
                        {
                            var memberTypeKeyword = member.MemberType switch
                            {
                                MemberTypes.Event => "event",
                                MemberTypes.Constructor => "method",
                                MemberTypes.Field => "field",
                                MemberTypes.Method => "method",
                                MemberTypes.Property => "property",
                                _ => null,
                            };

                            if (string.IsNullOrEmpty(memberTypeKeyword)) continue;

                            // eleType.RemoveAttribute("preserve");
                            var eleMember = doc.CreateElement(memberTypeKeyword);
                            eleMember.SetAttribute("name", memberName);
                            eleType.AppendChild(eleMember);
                        }
                    }

                    eleAsm.AppendChild(eleType);
                }
            }

            var targetDir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            File.WriteAllText(filePath, doc.InnerXml);
        }

    }
}
