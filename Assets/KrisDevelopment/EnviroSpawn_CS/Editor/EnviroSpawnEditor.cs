using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using EnvSpawn;

[CustomEditor(typeof(EnviroSpawn_CS))]
[CanEditMultipleObjects]
public class EnviroSpawnEditor : Editor {

	string[] scatterModeOption = {"Random","Fixed Grid","Even Spread"};
	
	public override void  OnInspectorGUI (){
        DrawDefaultInspector();
		
		EnviroSpawn_CS script = (EnviroSpawn_CS) target;
	
		script.scatterMode = EditorGUILayout.Popup("Scatter Mode", script.scatterMode, scatterModeOption);
		
		if(script.scatterMode == 1){
			//script.offsetInEachCell = EditorGUILayout.Toggle("Offset In Each Cell", script.offsetInEachCell);
			script.fixedGridScale = EditorGUILayout.FloatField("Grid Scale ", script.fixedGridScale);
		}
		
		if(GUILayout.Button("Generate")){
			script.InstantiateNew();
		}
		if(GUILayout.Button("Re-Generate All In Scene")){
			script.MassInstantiateNew();
		}
		
		if(script.cCheck)
			GUILayout.Box("WARNING: The prefabs may overlap! Grid cycles > 0!");
	}
	
	public  void  OnEditorGUI (){
		EnviroSpawn_CS scriptg = (EnviroSpawn_CS) target;
		
	}
	
	public  void  OnInspectorUpdate (){
		Repaint();
	}
}
