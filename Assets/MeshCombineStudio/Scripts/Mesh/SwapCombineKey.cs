using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshCombineStudio
{
    public class SwapCombineKey : MonoBehaviour
    {
        static public SwapCombineKey instance;
        public List<MeshCombiner> meshCombinerList = new List<MeshCombiner>();
        MeshCombiner meshCombiner;
        GUIStyle textStyle;

        void Awake()
        {
            instance = this;
            meshCombiner = GetComponent<MeshCombiner>();
            meshCombinerList.Add(meshCombiner);
        }

        void OnDestroy()
        {
            instance = null;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                QualitySettings.vSyncCount = 0;
                meshCombiner.SwapCombine();
            }
        }

        private void OnGUI()
        {
            if (textStyle == null)
            {
                textStyle = new GUIStyle("label");
                textStyle.fontStyle = FontStyle.Bold;
                textStyle.fontSize = 16;
            }

            textStyle.normal.textColor = this.meshCombiner.combinedActive ? Color.green : Color.red;

            GUI.Label(new Rect(10, 45 + (meshCombinerList.Count * 22), 200, 30), "Toggle with 'Tab' key.", textStyle);

            for (int i = 0; i < meshCombinerList.Count; i++)
            {
                MeshCombiner meshCombiner = meshCombinerList[i];
                if (meshCombiner.combinedActive) GUI.Label(new Rect(10, 30 + (i * 22), 300, 30), meshCombiner.gameObject.name + " is Enabled.", textStyle);
                else GUI.Label(new Rect(10, 30 + (i * 22), 300, 30), meshCombiner.gameObject.name + " is Disabled.", textStyle);
            }
        }
    }
}
