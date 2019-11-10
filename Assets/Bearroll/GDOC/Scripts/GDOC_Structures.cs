using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Bearroll.GDOC_Internal {

    public enum GDOC_OccludeeMode {
        Excluded,
        MeshRenderer,
        MeshRendererGroup,
        ParticleSystem,
        Generic = 100
    }

    public enum GDOC_Error {
        None,
        SecondInstance,
        CameraRequired,
        LightIsNotSet,
        LightIsNotDirectional,
        LightIsNotFromAbove,
        RendererNotFound,
        GroupIsEmpty,
        ParticleSystemNotFound,
        UnsupportedGraphicsAPI,
        NoRenderers,
        x86NotSupported,
    }

    public enum GDOC_UpdateMode {
        Static,
        Dynamic,
        // SemiDynamic
    }

    public enum GDOC_ManagementMode {
        Full,
        ShadowsOnly,
        WithoutShadows,
        None,
    }

    public enum GDOC_LoggingMode {
        None,
        Errors,
        ErrorsAndWarnings,
        Verbose,
        Debug,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct GDOC_ReportData {

        public int frameLag;
        public int occludeeCount;
        public int staticOccludeeCount;
        public int dynamicOccludeeCount;
        public int visibleCount;
        public int fullCount;
        public int noShadowsCount;
        public int shadowsOnlyCount;

    }

	[StructLayout(LayoutKind.Sequential)]
	public struct GDOC_Limits {
		public int StaticMax;
		public int DynamicMax;
		public int TotalMax;
		public int version;
	}

	public enum GDOC_VRMode {
		SinglePass = 1,
		MultiPass = 2,
	}

    [StructLayout(LayoutKind.Sequential)]
    public struct GDOC_InputFrameData {

        public Matrix4x4 matrixVP;
        public Matrix4x4 matrixVP_Right;
        public Matrix4x4 worldToCamera;
        public Matrix4x4 cameraToWorld;
        public Matrix4x4 worldToLight;
        public Vector4 cameraPosition;
        public Vector4 cameraDirection;
        public Vector4 lightDirection;
        public Vector4 referencePosition;
		public Vector4 cameraAngularMovement;
        public int frameIndex;
        public int debug;
        public int cascadeCount;
        public int downsampleDepth;
        public int downsampleShadowmap;
        public int enableShadowOcclussion;
        public int enableAverageDepth;
        public int clampShadowVolumes;
        public int isVR;
        public int optimizedVR;
        public int skipShadomap;
        public float shadowDistance;
        public float minimumDistance;
        public float renderDistance;
        public float hideTime;
        public float deltaTime;
        public float time;
        public int screenWidth;
        public int screenHeight;
        public float cameraFOV;
        public float dynamicStep;
        public float preCullThreshold;
        public float accuracy;
        public float prediction;
        public float farClip;
        public float nearClip;
        public float clampHeight;

    }

	public struct GDOC_ScopeItem {

		public Transform transform;

	}

	public class GDOC_Scope {

		List<GDOC_ScopeItem> chain = new List<GDOC_ScopeItem>();

		public GDOC_UpdateMode updateMode { get; private set; }

		void Refresh() {

			updateMode = GDOC_UpdateMode.Static;

			for (var i = 0; i < chain.Count; i++) {
			}

		}

		public void Push(Transform t) {

			chain.Add(new GDOC_ScopeItem {
				transform = t
			});

			Refresh();

		}

		public void Pop() {

			if (chain.Count == 0) return;

			chain.RemoveAt(chain.Count - 1);

			Refresh();

		}

	}

}