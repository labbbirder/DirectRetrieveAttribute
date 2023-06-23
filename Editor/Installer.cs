using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditorInternal;
using UnityEngine;

namespace com.bbbirder.unityeditor{
    internal class Installer:RoslynUpdater
    {
        const string SourceDllPath =
            "DirectAttributes/DirectAttribute.sg.dll";
        // static Installer m_Instance;
        // static Installer Instance => m_Instance ??= new();

        private Installer():base(SourceDllPath)
        {

        }

        [InitializeOnLoadMethod]
        static void Install()
        {
            new Installer().RunWorkFlow();
        }
    }
}
