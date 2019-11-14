using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MeshCombineStudio
{
    [CustomEditor(typeof(LODGroupSetup))]
    public class LODGroupSetupEditor : Editor
    {
        LODGroupSetup lodGroupSetup;
        LOD[] oldLods;

        void OnEnable()
        {
            lodGroupSetup = (LODGroupSetup)target;
            oldLods = lodGroupSetup.lodGroup.GetLODs();

            UnityEditor.EditorApplication.update += MyUpdate;
        }

        void OnDisable()
        {
            UnityEditor.EditorApplication.update -= MyUpdate;
        }

        void MyUpdate()
        {
            lodGroupSetup.lodGroup.size = lodGroupSetup.meshCombiner.cellSize;
            LOD[] lods = lodGroupSetup.lodGroup.GetLODs();

            if (lods.Length != oldLods.Length)
            {
                Debug.LogError("Mesh Combine Studio -> Please don't change the amount of LODs, this is just a dummy LOD Group to apply settings to the LOD Groups in all children.");
                lodGroupSetup.lodGroup.SetLODs(oldLods);
                return;
            }

            bool hasChanged = false;

            for (int i = 0; i < lods.Length; i++)
            {
                if (lods[i].renderers.Length != 0)
                {
                    Debug.LogError("Mesh Combine Studio -> Please don't add any renderes, this is just a dummy LOD Group to apply settings to the LOD Groups in all children.");
                    lods[i].renderers = null;
                    lodGroupSetup.lodGroup.SetLODs(lods);
                    return;
                }
                if (lods[i].screenRelativeTransitionHeight != oldLods[i].screenRelativeTransitionHeight) { hasChanged = true; break; }
            }

            if (hasChanged)
            {
                lodGroupSetup.ApplySetup();
                oldLods = lods;
            }
        }

        public override void OnInspectorGUI()
        {
            GUIDraw.DrawSpacer();
            GUI.color = Color.red;
            EditorGUILayout.BeginVertical("Box");
            GUI.color = Color.white;
            GUIDraw.Label("Modifications to this LOD Group will apply to all children", 12);
            EditorGUILayout.EndVertical();
            GUIDraw.DrawSpacer();
            
        }
    }
}
