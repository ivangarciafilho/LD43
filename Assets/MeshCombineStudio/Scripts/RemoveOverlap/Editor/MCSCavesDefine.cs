using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MeshCombineStudio
{
    [InitializeOnLoad]
    public class MCSCavesDefine
    {
        const string define = "MCSCaves";

        static MCSCavesDefine()
        {
            // Get defines.
            BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);

            // Append only if not defined already.
            if (defines.Contains(define))
            {
                // Debug.LogWarning("Selected build target (" + EditorUserBuildSettings.activeBuildTarget.ToString() + ") already contains <b>" + define + "</b> <i>Scripting Define Symbol</i>.");
                return;
            }

            // Append.
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, (defines + ";" + define));
            Debug.Log("<b>" + define + "</b> added to <i>Scripting Define Symbols</i> for selected build target (" + EditorUserBuildSettings.activeBuildTarget.ToString() + ").");
        }
    }
}