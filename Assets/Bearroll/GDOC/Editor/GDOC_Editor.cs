using System;
using System.Collections;
using System.Collections.Generic;
using Bearroll.GDOC_Internal;
using UnityEngine;
using UnityEditor;
using UnityEditor.Graphs;

namespace Bearroll {

    [CustomEditor(typeof(GDOC))]
    public partial class GDOCEditor: GDOC_BaseEditor {

        GDOC t;
        GUIStyle labelStyle;
        GUIStyle swStyle;
        GUIStyle moduleStyle;
        GUIStyle moduleHeaderStyle;
        GUIStyle innerModuleStyle;
        GUIStyle foldoutButtonStyle;

	    bool debugSettings = false;
	    bool otherSettings = false;
	    bool layerSettings = false;
	    bool layerSettings2 = false;
	    bool objectSettings = false;

        string sceneMetrics = "";

		static Action onBeginInspector;

		void OnEnable() {

            t = target as GDOC;

            labelStyle = new GUIStyle();
            labelStyle.wordWrap = true;
            labelStyle.padding = new RectOffset(2, 2, 2, 2);

            swStyle = new GUIStyle();
            swStyle.padding = new RectOffset(10,10,10,10);
            swStyle.fixedWidth = 0;

            foldoutButtonStyle = new GUIStyle();
            foldoutButtonStyle.fontStyle = FontStyle.Bold;
            foldoutButtonStyle.padding = new RectOffset(2,2,8,6);

            moduleHeaderStyle = new GUIStyle();

		}

        public override void OnInspectorGUI() {            

            if (serializedObject == null) {
                serializedObject = (this as Editor).serializedObject;
            }

            if(moduleStyle == null) {

                moduleStyle = new GUIStyle(EditorStyles.helpBox);
                moduleStyle.padding = new RectOffset(8,8,4,8);

                innerModuleStyle = new GUIStyle(EditorStyles.helpBox);
                innerModuleStyle.padding = new RectOffset(4,4,4,4);

            }

            DrawInspector(t, false);
        }

        void DrawInspector(GDOC t, bool isSeparateWindow) {

            EditorGUIUtility.labelWidth = 150;
            EditorGUIUtility.hierarchyMode = false;

			GUILayout.Space(10);

            if(t.initError != GDOC_Error.None) {
                GUILayout.Label("Init error: " + t.initError);
                GUILayout.Label("Please fix the error and re-enable this component.");
                return;
            }

			if (onBeginInspector != null) {
				onBeginInspector();
			}

			EditorGUILayout.BeginVertical(moduleStyle);

            GUILayout.Label("General", EditorStyles.boldLabel);

            FastPropertyField("enableOcclusionCulling", "Enabled");

			if (PlayerSettings.virtualRealitySupported) {

				FastPropertyField("vrMode", "VR Mode");

				var single = t.vrMode == GDOC_VRMode.SinglePass && PlayerSettings.stereoRenderingPath == StereoRenderingPath.SinglePass;
				var multi = t.vrMode == GDOC_VRMode.MultiPass && PlayerSettings.stereoRenderingPath == StereoRenderingPath.MultiPass;

				if (!single && !multi) {
					EditorGUILayout.HelpBox("PlayerSettings.stereoRenderingPath is " + PlayerSettings.stereoRenderingPath, MessageType.Warning);
				}

			}

			if (FastPropertyFieldCheck("directionalLight", "Directional Light")) {
				t.ValidateSettings();
			}

			t.ValidateSettings();

            if(t.lightError != GDOC_Error.None) {
                EditorGUILayout.HelpBox("Light error: " + t.lightError, MessageType.Warning);
            }
            
            FastPropertyField("accuracy", "Accuracy");
            FastPropertyField("prediction", "Prediction");

            FastPropertyField("minimumDistance", "Minimum distance");

            FastPropertyField("referenceObject", "Player object");

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(moduleStyle);

            GUILayout.Space(5);
            GUILayout.Label("Object management", EditorStyles.boldLabel);

	        EditorGUILayout.BeginVertical(innerModuleStyle);

            FastPropertyField("enableNewSceneScan", "Scan new scenes");

	        if (t.enableNewSceneScan) {
		        FastPropertyField("instantScanTimeLimit", "Time limit per frame (μs)");
	        }

	        EditorGUILayout.EndVertical();

	        EditorGUILayout.BeginVertical(innerModuleStyle);

            FastPropertyField("enableBackgroundScan", "Enable background scan");

	        if (t.enableBackgroundScan) {
		        FastPropertyField("backgroundScanTimeLimit", "Time limit per frame (μs)");
	        }

	        EditorGUILayout.EndVertical();

            if (t.enableBackgroundScan || t.enableNewSceneScan) {

                GUILayout.Space(5);
                objectSettings = EditorGUILayout.Foldout(objectSettings, "Objects");

                if (objectSettings) {

                    FastPropertyField("includeMeshRenderers", "Mesh renderers");
                    FastPropertyField("includeLODGroups", "LOD groups");
                    FastPropertyField("includeParticleSystems", "Particle systems");

                    FastPropertyField("includePointAndPointLights", "Point/Spot lights");
                    FastPropertyField("includeReflectionProbes", "Reflection Probes");

                    GUILayout.Space(5);

                    FastPropertyField("includeDisabledObjects", "Disabled objects");
                    FastPropertyField("includeDisabledComponents", "Disabled components");
                    
                    GUILayout.Space(5);

					GUILayout.Label("Dynamic by default", EditorStyles.boldLabel);

					FastPropertyField("dynamicAnimators", "Animator children");

					GUILayout.Space(5);

                }

                
                layerSettings = EditorGUILayout.Foldout(layerSettings, "Layers (management)");

                if (layerSettings) {

                    var layerManagementModes = serializedObject.FindProperty("layerManagementMode");

                    for (var i = 0; i < 32; i++) {

                        var name = LayerMask.LayerToName(i);

                        if (string.IsNullOrEmpty(name)) continue;

                        name = i + ". " + name;

                        var mode = (GDOC_ManagementMode) layerManagementModes.GetArrayElementAtIndex(i).intValue;

                        var newMode = (GDOC_ManagementMode) EditorGUILayout.EnumPopup(name, mode, EditorStyles.popup);

                        if (newMode != mode) {
                            layerManagementModes.GetArrayElementAtIndex(i).intValue = (int) newMode;
                        }

                    }

                }

                layerSettings2 = EditorGUILayout.Foldout(layerSettings2, "Layers (movement)");

                if (layerSettings2) {

                    var layerMovementModes = serializedObject.FindProperty("layerMovementMode");

                    for (var i = 0; i < 32; i++) {

                        var name = LayerMask.LayerToName(i);

                        if (string.IsNullOrEmpty(name)) continue;

                        name = i + ". " + name;

                        var movementMode = (GDOC_UpdateMode) layerMovementModes.GetArrayElementAtIndex(i).intValue;

                        var newMovementMode = (GDOC_UpdateMode) EditorGUILayout.EnumPopup(name, movementMode, EditorStyles.popup);

                        if (newMovementMode != movementMode) {
                            layerMovementModes.GetArrayElementAtIndex(i).intValue = (int) newMovementMode;
                        }

                    }

                }

            }

            EditorGUILayout.EndVertical();
            
           
            EditorGUILayout.BeginVertical(moduleStyle);

            GUILayout.Space(5);
            GUILayout.Label("Features", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(innerModuleStyle);

            FastPropertyField("enableTeleportDetection", "Teleport Detection");

            if (t.enableTeleportDetection) {

                FastPropertyField("teleportDistance", "teleportDistance");
                FastPropertyField("teleportAngle", "teleportAngle");
                FastPropertyField("teleportRV", "teleportRV");

            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();


            EditorGUILayout.BeginVertical(moduleStyle);

            if (GUILayout.Button("Debug", foldoutButtonStyle)) {
                debugSettings = !debugSettings;
            }

            if (debugSettings) {
                
                FastPropertyField("showStats", "Show Info");
                FastPropertyField("editorLoggingMode", "Logging in Editor");
                FastPropertyField("buildLoggingMode", "Logging in Build");
                
                FastPropertyField("enableHotkeys", "Enable hotkeys");

                if (t.enableHotkeys) {

                    FastPropertyField("toggleOCKey", "Toggle GDOC");
                    FastPropertyField("toggleInfoKey", "Toggle info");
                    FastPropertyField("calcMetricsKey", "Calc metrics");
                    FastPropertyField("increaseAccuracyKey", "Increase accuracy");
                    FastPropertyField("decreaseAccuracyKey", "Decrease accuracy");

                }

                FastPropertyField("suspendOC", "Suspend OC");
                FastPropertyField("gizmoColor", "gizmoColor");
				FastPropertyField("massGizmoColor", "massGizmoColor");
                FastPropertyField("drawDebugGizmos", "drawDebugGizmos");

            }

            EditorGUILayout.EndVertical();


            EditorGUILayout.BeginVertical(moduleStyle);

            if (GUILayout.Button("Advanced settings", foldoutButtonStyle)) {
                otherSettings = !otherSettings;
            }

	        if (otherSettings) {
		        
                FastPropertyField("autoDirectionalLight", "autoDirectionalLight");
                FastPropertyField("keepShadows", "keepShadows");
                // FastPropertyField("fastCameraSwitch", "fastCameraSwitch");

		        FastPropertyField("preCullThreshold", "preCullThreshold");
		        FastPropertyField("hideTime", "hideTime");

		        FastPropertyField("fullDisableDistance", "fullDisableDistance");
		        FastPropertyField("maxQueuedFrames", "maxQueuedFrames");
		        FastPropertyField("occludeeInitState", "occludeeInitState");
		        FastPropertyField("psOmnidirectionalBounds", "psOmnidirectionalBounds");
		        
		        FastPropertyField("clampShadowVolumes", "clampShadowVolumes");
		        FastPropertyField("autoClampHeight", "autoClampHeight");

                EditorGUI.BeginDisabledGroup(t.autoClampHeight);
                FastPropertyField("clampHeight", "clampHeight");
                EditorGUI.EndDisabledGroup();
		        

                #if GDOC_DEV

	            GUILayout.Space(5);
                GUILayout.Label("Dev", EditorStyles.boldLabel);
	            GUILayout.Space(5);

				FastPropertyField("forceSkipShadomap", "forceSkipShadomap");
	            FastPropertyField("ignoreContainers", "ignoreContainers");
	            FastPropertyField("downscaleDepth", "downscaleDepth");
	            FastPropertyField("downscaleShadowmap", "downscaleShadowmap");
                FastPropertyField("optimizedVR", "optimizedVR");
                FastPropertyField("debugLayer", "debugLayer");
                FastPropertyField("kickstart", "kickstart");
                // FastPropertyField("debugTextures", "debugTextures"); // does nothing

                #endif

            }

            EditorGUILayout.EndVertical();

	        GUILayout.Space(10);

            if (!string.IsNullOrEmpty(sceneMetrics)) {
                EditorGUILayout.HelpBox(sceneMetrics, MessageType.Info); 
            }

            if (GUILayout.Button("Calculate scene metrics")) {
                sceneMetrics = GDOC.CalculateSceneMetrics();
            }

            if (Application.isPlaying && t.isActiveAndEnabled) {

                if (GUILayout.Button("Scan active scenes")) {
                    GDOC.ScanActiveScenes();
                }

                if (GUILayout.Button("Restart")) {
                    GDOC.Restart();
                }
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUIUtility.labelWidth = 0;

            if (isSeparateWindow) {
                EditorGUILayout.EndVertical();
            }

        }

    }

}