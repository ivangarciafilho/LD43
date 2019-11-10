using System.Collections;
using System.Collections.Generic;
using Bearroll.GDOC_Internal;
using UnityEngine;
using UnityEditor;

namespace Bearroll {

    [CustomEditor(typeof(GDOC_Occludee))]
    [CanEditMultipleObjects]
    public class GDOC_OccludeeEditor : GDOC_BaseEditor {

        bool otherSettings = false;

        public override void OnInspectorGUI() {

            EditorGUIUtility.labelWidth = 150;

            if (!Application.isPlaying) {
                foreach (var e in targets) {
                    (e as GDOC_Occludee).UpdateBounds();
                }
            }

            var t = targets[0] as GDOC_Occludee;
	        var isSingle = targets.Length == 1;

	        EditorGUI.BeginChangeCheck();

            if (isSingle && t.runtimeId > -1) {
                EditorGUI.BeginDisabledGroup(true);
            }

            EditorGUI.BeginChangeCheck();
            FastPropertyField("mode", "Mode");

            if (EditorGUI.EndChangeCheck()) {
                serializedObject.ApplyModifiedProperties();
                t.Init();
            }

	        if (isSingle) {

		        if (t.initError != GDOC_Error.None) {

			        EditorGUILayout.HelpBox("Init Error: " + t.initError, MessageType.Error);

		        } else {

                    if (t.isExcluded) {

                        FastPropertyField("withChildren", "With Children");

                    } else {

                        if (t.isMeshRenderer || t.isMeshRendererGroup) {
                            FastPropertyField("managementMode", "Manage mode");
                        }

                        FastPropertyField("movementMode", "Update mode");

                        if (t.isRendererGroup) {

                            GUILayout.Label("Manages " + t.managedRenderersCount + " renderers");

                            if (t.renderers != null && t.renderers.Count > 0) {
                                for (var i = 0; i < t.renderers.Count; i++) {
                                    GUILayout.Label((i + 1) + ": " + t.renderers[i].name);
                                }
                            }

                            if (t.noShadowRenderers != null && t.noShadowRenderers.Count > 0) {

                                GUILayout.Label("No shadows");

                                for (var i = 0; i < t.noShadowRenderers.Count; i++) {
                                    GUILayout.Label((i + 1) + ": " + t.noShadowRenderers[i].name);
                                }
                            }

                            if (t.shadowOnlyRenderers != null && t.shadowOnlyRenderers.Count > 0) {

                                GUILayout.Label("Shadows only");

                                for (var i = 0; i < t.shadowOnlyRenderers.Count; i++) {
                                    GUILayout.Label((i + 1) + ": " + t.shadowOnlyRenderers[i].name);
                                }
                            }

							if (t.GetComponent<LODGroup>() == null) {
								EditorGUI.BeginDisabledGroup(true);
							}

							if (GUILayout.Button("Grab LODGroup renderers")) {
                                t.GrabLODGroupRenderers();
                            }

							if (t.GetComponent<LODGroup>() == null) {
								EditorGUI.EndDisabledGroup();
							}

                            if (GUILayout.Button("Grab all child renderers")) {
                                t.GrabAllChildRenderers();
                            }

                        }

                        if (t.hasCustomBounds) {

                            FastPropertyField("center", "Center");
                            FastPropertyField("extents", "Extents");

                            if (GUILayout.Button("Encapsulate children")) {
                                t.RecalculateContainerBounds();
                                SceneView.RepaintAll();
                            }

                        } else {

                            EditorGUI.BeginDisabledGroup(true);

                            EditorGUILayout.Vector3Field("Position", t.position);
                            EditorGUILayout.Vector3Field("Extents", t.size);

                            EditorGUI.EndDisabledGroup();

                        }

                        FastPropertyField("sizeMultiplier", "Size Multiplier");

                        otherSettings = EditorGUILayout.Foldout(otherSettings, "Other settings");

                        if (otherSettings) {

                            if (t.isDynamic) {
                                FastPropertyField("dynamicUpdateInterval", "Update Interval (sec)");
                            }

                            FastPropertyField("isImportant", "Is Important");
                            FastPropertyField("isShadowSource", "Is Shadow Source");
                            FastPropertyField("disablePrediction", "Disable Prediction");

                        }

                        if (t.runtimeId > -1) {

                            EditorGUI.EndDisabledGroup();

                            EditorGUILayout.HelpBox(string.Format("Runtime ID: {0}\nState: {1}", t.runtimeId, t.currentState), MessageType.Info);

                            if (GUILayout.Button("Update")) {
                                GDOC.UpdateOccludee(t);
                            }

                            if (GUILayout.Button("Disable")) {
                                GDOC.RemoveOccludee(t);
                            }

                        } else if (Application.isPlaying) {

                            if (GUILayout.Button("Enable")) {
                                GDOC.AddOccludee(t);
                            }

                        }

                    }

                }
	           

	        } else {
		        
		        FastPropertyField("movementMode", "Movement Mode");
		        FastPropertyField("center", "Center");
		        FastPropertyField("extents", "Extents");
		        FastPropertyField("sizeMultiplier", "Size Multiplier");

	            EditorGUILayout.HelpBox("Multi-editing of other settings is not supported.", MessageType.Info);

	        }

            serializedObject.ApplyModifiedProperties();

        }

        public void OnSceneGUI() {

            var t = target as GDOC_Occludee;

            if (!Application.isPlaying) {
                t.Init();
            }

			if (GDOC.instance == null) {
				Handles.color = Color.yellow;
			} else {
				Handles.color = GDOC.instance.gizmoColor;
			}

			Handles.DrawWireCube(t.position, t.size * 2);

        }

    }

}