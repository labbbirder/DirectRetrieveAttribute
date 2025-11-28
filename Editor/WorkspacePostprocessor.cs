using System;
using Unity.CodeEditor;
using UnityEditor;

namespace BBBirder.DirectAttribute.Editor
{
    public class WorkspacePostprocessor : AssetPostprocessor
    {
        // Force fix once on Editor startup
        [InitializeOnLoadMethod]
        private static void AutoRegenerateOnStartup()
        {
            const string Key = "SolutionEverGenerated";

            if (!SessionState.GetBool(Key, false))
            {
                SessionState.SetBool(Key, true);
                EditorApplication.delayCall += GenerateCSharpSolutionFiles;
                EditorApplication.QueuePlayerLoopUpdate();
            }
        }

        private static void GenerateCSharpSolutionFiles()
        {
            // Debug.Log(CodeEditor.Editor.CurrentCodeEditor);
            AssetDatabase.Refresh();
            CodeEditor.Editor.CurrentCodeEditor.SyncAll();
        }

        public static string OnGeneratedCSProject(string path, string content)
        {
            content = content.Replace("</DefineConstants>", ";ROSLYN_EDIT_TIME</DefineConstants>");
            return content;
        }
    }
}
