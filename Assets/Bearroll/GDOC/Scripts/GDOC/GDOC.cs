using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine.SceneManagement;
using System.Diagnostics;
using Bearroll.GDOC_Internal;

namespace Bearroll {

    public partial class GDOC: GDOC_Base {

        public Light directionalLight;
        public bool autoDirectionalLight = true;

        [Range(0, 100)]
        public int accuracy = 70;

		[Range(0, 100)]
		public int prediction = 30;

        public float minimumDistance = 5;
        public Transform referenceObject;

        public bool enableBackgroundScan = true;
        public bool enableNewSceneScan = true;

        public bool includeLODGroups = true;
        public bool includeMeshRenderers = true;
        public bool includePointAndPointLights = true;
        public bool includeReflectionProbes = true;
        public bool includeParticleSystems = true;

        public bool includeDisabledObjects = false;
        public bool includeDisabledComponents = false;

		public bool dynamicAnimators = true;

        public GDOC_UpdateMode[] layerMovementMode = new GDOC_UpdateMode[32];
        public GDOC_ManagementMode[] layerManagementMode = new GDOC_ManagementMode[32];

        public float preCullThreshold = 0.001f;
        public float fullDisableDistance = 100;
		public float hideTime = 0.5f;
        
        public bool debugLayer = false;
        public bool showStats = true;

        public GDOC_LoggingMode editorLoggingMode = GDOC_LoggingMode.ErrorsAndWarnings;
        public GDOC_LoggingMode buildLoggingMode = GDOC_LoggingMode.Errors;

		public Color gizmoColor = new Color(1, 1, 0, 1);
		public Color massGizmoColor = new Color(1, 1, 0, 0.01f);

        public int occludeeInitState = -1;
        public int backgroundScanTimeLimit = 50;
        public int instantScanTimeLimit = 100000;
        public int maxQueuedFrames = 1;
        public bool enableOcclusionCulling = true;
        public bool psOmnidirectionalBounds = false;
		public bool clampShadowVolumes = true;
        public bool autoClampHeight = true;
        public float clampHeight = 0;
        public bool suspendOC = false;
        public bool drawDebugGizmos = false;
        public bool enableTeleportDetection = true;
        public float teleportDistance = 2f;
        public float teleportAngle = 160f;
        public float teleportRV = 0.1f;
		public bool fastCameraSwitch = true;
		public bool keepShadows = true;
		public bool forceSkipShadomap = true;
		public GDOC_VRMode vrMode = GDOC_VRMode.SinglePass;

		bool initDone = false;
        float dynamicStep = 0.1f;

		[SerializeField]
		bool kickstart = false;
        [SerializeField]
        bool ignoreContainers = true;
        [SerializeField]
        bool downscaleDepth = true;
        [SerializeField]
        bool downscaleShadowmap = true;
        [SerializeField]
        bool optimizedVR = true;
		[SerializeField]
		bool debugTextures = true;

        new public Camera camera { get; private set; }
        public GDOC_Error initError { get; private set; }
        public GDOC_Error lightError { get; private set; }

        static GDOC _instance;

        public static GDOC instance {
            get {
                if(_instance == null) {
                    _instance = FindObjectOfType<GDOC>();
                }
                return _instance;
            }
        }

        GDOC_LoggingMode currentLoggingMode {
            get { return Application.isEditor ? editorLoggingMode : buildLoggingMode; }
        }

        bool shouldLogErrors {
            get { return currentLoggingMode >= GDOC_LoggingMode.Errors; }
        }

        bool shouldLogWarnings {
            get { return currentLoggingMode >= GDOC_LoggingMode.ErrorsAndWarnings; }
        }

        bool shouldLogInfo {
            get { return currentLoggingMode >= GDOC_LoggingMode.Verbose; }
        }

        bool debugLogging {
            get { return currentLoggingMode >= GDOC_LoggingMode.Debug; }
        }

        GDOC_ReportData reportData;
        Material sampleMaterial;
        GDOC_InputFrameData frameData;
        RenderingPath lastPath = RenderingPath.UsePlayerSettings;
        Coroutine frameEndWaiter;

        void Reset() {

            directionalLight = GDOC_Utils.FindMainDirectionalLight();

			#if UNITY_EDITOR
			if (UnityEditor.PlayerSettings.stereoRenderingPath == UnityEditor.StereoRenderingPath.SinglePass) {
				vrMode = GDOC_VRMode.SinglePass;
			} else if (UnityEditor.PlayerSettings.stereoRenderingPath == UnityEditor.StereoRenderingPath.MultiPass) {
				vrMode = GDOC_VRMode.MultiPass;
			}
			#endif

        }

        void Update() {

            if(!enableOcclusionCulling) return;

            if (suspendOC) return;

            GL.IssuePluginEvent(eventFuncPtr, 4);

            ValidateSettings();
        }

        public bool ValidateSettings() {

            if(directionalLight == null) {
                lightError = GDOC_Error.LightIsNotSet;
                return false;
            }

            if(directionalLight.type != LightType.Directional) {
                lightError = GDOC_Error.LightIsNotDirectional;
                return false;
            }

            if(Vector3.Dot(directionalLight.transform.forward, Vector3.down) <= 0) {
                lightError = GDOC_Error.LightIsNotFromAbove;
                return false;
            }

            lightError = GDOC_Error.None;

            return true;

        }

        void OnValidate() {
			ValidateSettings();
        }

		void Awake() {
			if (initDone) return;
			initDone = true;
			OnEnable();
		}

        void OnEnable() {

            if(SystemInfo.graphicsDeviceType != GraphicsDeviceType.Direct3D11) {
                initError = GDOC_Error.UnsupportedGraphicsAPI;
                enabled = false;
                return;
            }

            var os = SystemInfo.operatingSystem;

            if (os.Contains("Windows") && !os.Contains("64")) {
                initError = GDOC_Error.x86NotSupported;
                enabled = false;
                return;
            }

            camera = GetComponent<Camera>();
            if(camera == null) {
                initError = GDOC_Error.CameraRequired;
                enabled = false;
                return;
            }

			RequestLimits(ref limits);

			if (limits.version == -1) {
				// demo won't work in builds
				enabled = false;
				return;
			}

            if(instance != null && instance != this) {

				if (instance.isActiveAndEnabled) {
					initError = GDOC_Error.SecondInstance;
					enabled = false;
					return;
				}

				/*
				_instance = this;

				if (fastCameraSwitch) {
					fast = true;
				}
				*/

			}

            initError = GDOC_Error.None;

            if (autoDirectionalLight && directionalLight == null) {
                directionalLight = GDOC_Utils.FindMainDirectionalLight();
            }

			Clean(camera);
            Init(camera);

			InitPlugin();

			foreach(var e in Resources.FindObjectsOfTypeAll<GDOC_Occludee>()) {
				if(e.isTemporary) {
					if (!kickstart) {
						Destroy(e);
					}
				} else {
					e.OnRemove();
				}
			}

            lastCameraPosition = camera.transform.position;
            lastCameraRotation = camera.transform.rotation;

            if(maxQueuedFrames > -1) {
                QualitySettings.maxQueuedFrames = maxQueuedFrames;
            }

            maxOcludeeId = 0;

            if(sampleMaterial == null) {
                sampleMaterial = new Material(Shader.Find("Hidden/GDOC/Sample"));
                sampleMaterial.enableInstancing = true;
            }

			InitLightBuffers();

            InitUI();

			if (kickstart) {
				Kickstart();
			}

            InitScanning();

            StartOrRestartCoroutine(ref frameEndWaiter, FrameEndWaiter());

        }
 
        void OnDisable() {

            EnableAllOccludees();

            StopBackgroundScanning();

			RemoveLightBuffers();

            Clean();

            base.Clean(camera);

            StopScanning();

            foreach(var e in Resources.FindObjectsOfTypeAll<GDOC_Occludee>()) {
                if(e.isTemporary) {
					if (!kickstart) {
						Destroy(e);
					}
                } else {
                    e.OnRemove();
                }
            }

        }

        void LateUpdate() {

			ProcessDebugStrings();

            ProcessInput();

            ProcessStats();

        }

        public override void BeforeEverything() {

			// SRP 
			if (camera == null) return;

            if (!enableOcclusionCulling) return;

            if (suspendOC) return;

			precullSW.Reset();
			precullSW.Start();

			if (Time.frameCount <= lastSentFrameIndex) return;

			lastSentFrameIndex = Time.frameCount;

			CheckLightBuffers();

			SendFrameData();

			RequestResults(ref reportData);

			SyncDynamic();

			CheckTeleport();

			var t = (float) precullSW.ElapsedTicks / Stopwatch.Frequency;

			requestTime += t;

		}

        IEnumerator FrameEndWaiter() {

            var w = new WaitForEndOfFrame();

            while (true) {

                yield return w;

                if (!enableOcclusionCulling) continue;

                if (suspendOC) continue;

                GL.IssuePluginEvent(eventFuncPtr, 4);

            }

        }



    }
}
