using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Bearroll.GDOC_Demo {

    [CustomEditor(typeof(GDOC_GridSpawner))]
    public class GDOC_GridSpawnerEditor: Editor {

        public override void OnInspectorGUI() {

            DrawDefaultInspector();

            var t = target as GDOC_GridSpawner;

            if (GUILayout.Button("Respawn")) {
                t.Respawn();
            } else if (GUILayout.Button("Clear")) {
                t.Clear();
            }

        }

    }

}