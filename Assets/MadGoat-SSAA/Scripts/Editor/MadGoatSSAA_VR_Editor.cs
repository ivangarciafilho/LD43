using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace MadGoat_SSAA
{
    [CustomEditor(typeof(MadGoatSSAA_VR))]
    public class MadGoatSSAA_VR_Editor : MadGoatSSAA_Editor
    {
        public override string Title
        {
            get
            {
                return base.Title + " VR";
            }
        }
        public override void DrawTab1()
        {
            base.DrawTab1();
        }
        public override void DrawTab2()
        {
            EditorGUILayout.HelpBox("Screenshot functionality is not available in VR mode.", MessageType.Error);

        }
        public override void DrawTab3()
        {

            EditorGUILayout.HelpBox("Set the MadGoat Debugger gameobject from your scene in order to use the debugging features", MessageType.Info);

            EditorGUILayout.PropertyField(madGoatDebugger, new GUIContent("MadGoatDebugger object"));
            if (madGoatDebugger.objectReferenceValue == null)
                if (GUILayout.Button("Get MadGoat Debugger & Benchmark"))
                    Application.OpenURL("https://www.assetstore.unity3d.com/en/#!/content/45279");
            if (GUILayout.Button("Open online documentation"))
                Application.OpenURL("https://drive.google.com/open?id=1QZ0XVhIteEjvne1BoiEBYD5s3Kzsn73_AdWNtCqCj5I");

        }
        public override void DrawPerAxis()
        {
            EditorGUILayout.HelpBox("NOT SUPPORTED IN VR MODE.\nX axis will be used as global multiplier instead.", MessageType.Error);

            base.DrawPerAxis();
        }
    }
}
