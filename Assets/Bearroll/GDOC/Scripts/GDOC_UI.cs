using UnityEngine;

namespace Bearroll {

    public partial class GDOC {

        public bool enableHotkeys = true;

	    public KeyCode toggleOCKey = KeyCode.F1;
	    public KeyCode toggleInfoKey = KeyCode.F2;
	    public KeyCode calcMetricsKey = KeyCode.F3;
        public KeyCode increaseAccuracyKey = KeyCode.KeypadPlus;
        public KeyCode decreaseAccuracyKey = KeyCode.KeypadMinus;

        GUIStyle statsStyle;
        GUIStyle statsFPSStyle;
        GUIStyle statsHeaderStyle;

        Rect statsHeaderRect;
        Rect statsFPSRect;
        Rect statsRect;

        string headerEnabledString = "gDOC is <color=lightblue>ON</color>";
        string headerDisabledString = "gDOC is <color=red>OFF</color>";
        float lastTime;
        int lastFrameCount;
        float nextFps = 1;
        float hideStatsUntil = 0;
        string fpsString = "";
        string statsString2 = "";
        int validationCount;
        int backgroundCount;
        float dynamicTime;
        float requestTime;
        float nextAccuracyKeyCheck = 0;

        public string statsString { get; private set; }

        void OnGUI() {

            if (!showStats) return;

            if (!Application.isPlaying) return;

            var k = camera.pixelHeight / 1080f;

			if (camera.stereoTargetEye == StereoTargetEyeMask.Both) {
				// k *= 0.5f;
			}

            statsHeaderStyle.fontSize = (int) (44 * k);
            statsFPSStyle.fontSize = (int) (24 * k);
            statsStyle.fontSize = (int) (18 * k);

            GUI.Label(statsHeaderRect, enableOcclusionCulling ? headerEnabledString : headerDisabledString, statsHeaderStyle);

            if (Time.unscaledTime < hideStatsUntil) {

                GUI.Label(statsFPSRect, "FPS: ?", statsFPSStyle);

            } else {

                GUI.Label(statsFPSRect, fpsString, statsFPSStyle);

                if (enableOcclusionCulling) {

                    GUI.Label(statsRect, statsString, statsStyle);

                }

            }

        }

        void OnDrawGizmos() {

            var c = camera;

            if (c == null) return;

            if (drawDebugGizmos) {

                GDOC_Utils.CalculateFrustumPlanes(c, frustumPlanes);

				Gizmos.color = massGizmoColor;

                foreach (var id in occludeeList) {

                    var e = occludees[id];

                    if (e == null) continue;

                    if (!e.isActive) continue;

                    if (e.currentState == 1 || e.currentState == 3) continue;

                    // if (!GeometryUtility.TestPlanesAABB(frustumPlanes, e.bounds)) continue;

                    Gizmos.DrawWireCube(e.position, e.size * 2);

                }

            }

            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.color = Color.blue;
            Gizmos.DrawFrustum(Vector3.zero, c.fieldOfView, c.farClipPlane, c.nearClipPlane, c.aspect);

        }

        void UpdateInfoString() {
            
            statsString = string.Format(
                "SCOC: {9}\nAccuracy: {0}%\nPrediction: {10}%\nDelay: {1}\n<b>Objects:</b> {2}\nStatic: {3}\nDynamic: {4}\n\n<b>Visible: {5}</b>\n\nCompletely: {6}\nNo shadows: {7}\nShadows only: {8}\n\n",
                accuracy,
                reportData.frameLag,
                reportData.occludeeCount,
                reportData.staticOccludeeCount,
                reportData.dynamicOccludeeCount,
                reportData.visibleCount,
                reportData.fullCount,
                reportData.noShadowsCount,
                reportData.shadowsOnlyCount,
				isShadowOCActive ? (shouldSkipShadowmap ? "Fast" : "Full") : "Off",
				prediction
            );

            statsString += statsString2;

            if (enableHotkeys) {

                statsString += string.Format(
                    "\n\n<b>Hotkeys</b>\n|{0}| Toggle GDOC\n|{1}| Toggle Info\n|{2}| Calc metrics\n|{3}| Increase accuracy\n|{4}| Decrease accuracy",
                    toggleOCKey, toggleInfoKey, calcMetricsKey, increaseAccuracyKey, decreaseAccuracyKey);

            }

            if (!string.IsNullOrEmpty(sceneMetrics)) {
                statsString += "\n\n<b>Scene metrics</b>\n" + sceneMetrics;
            }

        }

        void ProcessInput() {

            if(!enableHotkeys) return;

            if (Input.GetKeyUp(toggleInfoKey)) {
                showStats = !showStats;
            }

            if (Input.GetKeyUp(toggleOCKey)) {

                enableOcclusionCulling = !enableOcclusionCulling;

                nextFps = Time.unscaledTime + 2.1f;
                hideStatsUntil = Time.unscaledTime + 2.1f;

                if (enableOcclusionCulling) {
	                OnEnable();
                } else {
	                OnDisable();
                }

            }

            if (Input.GetKeyUp(calcMetricsKey)) {
                CalculateSceneMetrics();
            }

            if (Time.unscaledTime >= nextAccuracyKeyCheck) {

                var delta = 0;

                if (Input.GetKey(increaseAccuracyKey)) {
                    delta = 1;
                }

                if (Input.GetKey(decreaseAccuracyKey)) {
                    delta = -1;
                }

                if (delta != 0) {
                    accuracy = Mathf.Clamp(accuracy + delta, 0, 100);
                    nextAccuracyKeyCheck = Time.unscaledTime + 0.05f;
                    UpdateInfoString();
                }

            }

        }

        void ProcessStats() {            

            if (Time.unscaledTime < nextFps) return;

	        var t = Time.unscaledTime - lastTime;
            var frameCount = (float) (Time.frameCount - lastFrameCount);
            var fps = Mathf.RoundToInt(frameCount / t);

            if (Application.isPlaying) {
                fpsString = "FPS: " + fps;
                if (QualitySettings.vSyncCount > 0) {
                    fpsString += " (vsync)";
                }
            } else {
                fpsString = "Editor mode";
            }

            if (isScanning) {
                fpsString += " (scanning)";
            }

            lastFrameCount = Time.frameCount;
            lastTime = Time.unscaledTime;
            nextFps = Time.unscaledTime + 1f;

            statsString2 = string.Format(
                "<b>Timings</b>\nValidation: {0}/s\nBackground: {1}/s\nSyncDynamic: {2} μs\nPreCull: {3} μs",
                Mathf.RoundToInt(validationCount / t),
                Mathf.RoundToInt(backgroundCount / t),
                Mathf.RoundToInt(dynamicTime / frameCount * 1000f * 1000f),
                Mathf.RoundToInt((requestTime - dynamicTime) / frameCount * 1000f * 1000f)
            );

            UpdateInfoString();

	        validationCount = 0;
	        backgroundCount = 0;
	        dynamicTime = 0;
            requestTime = 0;

        }

        void InitUI() {
            
            statsHeaderStyle = new GUIStyle();
            statsHeaderStyle.richText = true;
            statsHeaderStyle.normal.textColor = Color.white;

            statsFPSStyle = new GUIStyle();
            statsFPSStyle.richText = true;
            statsFPSStyle.normal.textColor = Color.white;

            statsStyle = new GUIStyle();
            statsStyle.richText = true;
            statsStyle.normal.textColor = Color.white;

            statsHeaderRect = new Rect(40, 40, 500, 100);
            statsFPSRect = new Rect(40, 100, 500, 100);
            statsRect = new Rect(40, 150, 500, 100);

            nextFps = Time.unscaledTime + 1.1f;
            hideStatsUntil = Time.unscaledTime + 1.2f;

        }

    }

}