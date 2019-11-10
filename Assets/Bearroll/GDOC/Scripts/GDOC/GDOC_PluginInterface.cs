using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using AOT;
using Bearroll.GDOC_Internal;
using UnityEngine;
using UnityEngine.Rendering;

namespace Bearroll {

	public partial class GDOC {

		CommandBuffer cb1;
		CommandBuffer cb2;
		CommandBuffer cb3;
		CommandBuffer cb4;
		CommandBuffer cb5;

		int lastSentFrameIndex = -1;
		float nextLightCheck = 0;
		static List<string> debugString = new List<string>(8);

		public bool isShadowOCActive {
			get { return directionalLight != null && lightError == GDOC_Error.None; }
		}

		bool shouldSkipShadowmap {
			get { return forceSkipShadomap || isSRP; }
		}

		void InitLightBuffers() {

			if (cb1 == null) {

				cb1 = new CommandBuffer();
				cb1.name = "GDOC_Cascade0";
				cb2 = new CommandBuffer();
				cb2.name = "GDOC_Cascade1";
				cb3 = new CommandBuffer();
				cb3.name = "GDOC_Cascade2";
				cb4 = new CommandBuffer();
				cb4.name = "GDOC_Cascade3";
				cb5 = new CommandBuffer();
				cb5.name = "GDOC_Process";

				cb1.IssuePluginEvent(eventFuncPtr, 1);

				cb1.SetGlobalFloat("GDOC_ShadowPassIndex", 0);
				cb2.SetGlobalFloat("GDOC_ShadowPassIndex", 1);
				cb3.SetGlobalFloat("GDOC_ShadowPassIndex", 2);
				cb4.SetGlobalFloat("GDOC_ShadowPassIndex", 3);

				cb1.DrawProcedural(Matrix4x4.identity, sampleMaterial, 0, MeshTopology.Points, 1);
				cb2.DrawProcedural(Matrix4x4.identity, sampleMaterial, 0, MeshTopology.Points, 1);
				cb3.DrawProcedural(Matrix4x4.identity, sampleMaterial, 0, MeshTopology.Points, 1);
				cb4.DrawProcedural(Matrix4x4.identity, sampleMaterial, 0, MeshTopology.Points, 1);

				cb5.IssuePluginEvent(eventFuncPtr, 2);

			}

			nextLightCheck = 0;

			CheckLightBuffers(true);

		}

		void RemoveLightBuffers() {

			if (directionalLight == null || cb1 == null) return;

			directionalLight.RemoveCommandBuffer(LightEvent.BeforeShadowMapPass, cb1);
			directionalLight.RemoveCommandBuffer(LightEvent.BeforeShadowMapPass, cb2);
			directionalLight.RemoveCommandBuffer(LightEvent.BeforeShadowMapPass, cb3);
			directionalLight.RemoveCommandBuffer(LightEvent.BeforeShadowMapPass, cb4);
			directionalLight.RemoveCommandBuffer(LightEvent.AfterShadowMap, cb5);

		}

		void CheckLightBuffers(bool force = false) {

			if (directionalLight == null) return;

			if (Time.unscaledTime < nextLightCheck) return;

			nextLightCheck = Time.unscaledTime + 2f;
			
			RemoveLightBuffers();

			if (shouldSkipShadowmap) return;

			directionalLight.AddCommandBuffer(LightEvent.BeforeShadowMapPass, cb1, ShadowMapPass.DirectionalCascade0);
			directionalLight.AddCommandBuffer(LightEvent.BeforeShadowMapPass, cb2, ShadowMapPass.DirectionalCascade1);
			directionalLight.AddCommandBuffer(LightEvent.BeforeShadowMapPass, cb3, ShadowMapPass.DirectionalCascade2);
			directionalLight.AddCommandBuffer(LightEvent.BeforeShadowMapPass, cb4, ShadowMapPass.DirectionalCascade3);
			directionalLight.AddCommandBuffer(LightEvent.AfterShadowMap, cb5);

		}

		IntPtr eventFuncPtr;
		DebugDelegate callback_delegate = DebugFunction;
		IntPtr intptr_delegate;
		CallbackDelegate callback_delegate2 = CallbackFunction;
		IntPtr intptr_delegate2;

		GDOC_Limits limits = new GDOC_Limits();

		[DllImport("GDOC")]
		static extern IntPtr GetRenderEventFunc();

		[DllImport("GDOC")]
		static extern void SetCallbacks(IntPtr debug, IntPtr state);

		[DllImport("GDOC")]
		static extern void SetFrameData(ref GDOC_InputFrameData frameData);

		[DllImport("GDOC")]
		static extern void RequestResults([MarshalAs(UnmanagedType.Struct)] ref GDOC_ReportData reportData);

		[DllImport("GDOC")]
		static extern void RequestLimits([MarshalAs(UnmanagedType.Struct)] ref GDOC_Limits limits);

		[DllImport("GDOC")]
		static extern void ResetOccludees();

		[DllImport("GDOC")]
		static extern int AddOccludee(Vector3 position, Vector3 size, int mode, int movementMode, int parent, bool isOccluder, bool isShadowCaster, bool disablePrediction);

		[DllImport("GDOC")]
		static extern void UpdateOccludee(int id, Vector3 position, Vector3 size);

		[DllImport("GDOC")]
		static extern void DeleteOccludee(int id);

		[DllImport("GDOC")]
		static extern void UpdateOccludeeState(int id, int state);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void DebugDelegate(string str);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void CallbackDelegate(int id, int state);

		void ProcessDebugStrings() {

			lock (debugString) {

				foreach (var e in debugString) {
					UnityEngine.Debug.Log(e);
				}

				debugString.Clear();
			}
		}

		[MonoPInvokeCallback(typeof(DebugDelegate))]
		static void DebugFunction(string str) {

			if(instance != null && instance.debugLogging) {
				lock (debugString) {
					debugString.Add(str);
				}
			}

		}

		[MonoPInvokeCallback(typeof(CallbackDelegate))]
		static void CallbackFunction(int id, int state) {

			if(instance == null)
				return;

			instance.ChangeOccludeeState(id, state);

		}

		bool InitPlugin() {

			eventFuncPtr = GetRenderEventFunc();

			intptr_delegate = Marshal.GetFunctionPointerForDelegate(callback_delegate);
			intptr_delegate2 = Marshal.GetFunctionPointerForDelegate(callback_delegate2);

			SetCallbacks(intptr_delegate, intptr_delegate2);

			RequestLimits(ref limits);

			if(occludees == null) {
				occludees = new GDOC_Occludee[limits.TotalMax];
				dynamicOccludees = new List<GDOC_Occludee>(limits.DynamicMax / 10);
				occludeeList = new List<int>(limits.TotalMax / 10);
				occluders = new List<GDOC_Occludee>(limits.TotalMax / 10);
			}

			Clean();

			return true;

		}

		void SendFrameData() {

			frameData.worldToCamera = camera.worldToCameraMatrix;
			frameData.cameraToWorld = camera.cameraToWorldMatrix;

			var vr = camera.stereoActiveEye != Camera.MonoOrStereoscopicEye.Mono ? (int) vrMode : 0;
			
			if(vr > 0) {

				var left = GL.GetGPUProjectionMatrix(camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left), true) * camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left);
				var right = GL.GetGPUProjectionMatrix(camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right), true) * camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right);

				frameData.matrixVP = left;
				frameData.matrixVP_Right = right;

			} else {
				frameData.matrixVP = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true) * frameData.worldToCamera;
			}

			frameData.cameraPosition = camera.transform.position;
			frameData.cameraDirection = camera.transform.forward;

			if(isShadowOCActive) {
				frameData.lightDirection = (Vector4) directionalLight.transform.forward;
				frameData.enableShadowOcclussion = 1;
				// should be renamed probably
                frameData.worldToLight = Matrix4x4.TRS(Vector3.zero, directionalLight.transform.rotation, Vector3.one);
            } else {
				frameData.lightDirection = Vector4.zero;
				frameData.enableShadowOcclussion = 0;
				frameData.worldToLight = Matrix4x4.identity;
			}

			frameData.accuracy = Mathf.Clamp01(accuracy / 100f);
			frameData.prediction = Mathf.Clamp01(prediction / 100f);

			frameData.enableAverageDepth = 0;
			frameData.clampShadowVolumes = clampShadowVolumes ? 1 : 0;
			frameData.frameIndex = Time.frameCount;
			frameData.debug = debugLayer ? 1 : 0;
			frameData.cascadeCount = QualitySettings.shadowCascades;
			frameData.downsampleDepth = downscaleDepth ? 1 : 0;
			frameData.downsampleShadowmap = downscaleShadowmap ? 1 : 0;
			frameData.shadowDistance = QualitySettings.shadowDistance;

			frameData.minimumDistance = minimumDistance;
			frameData.renderDistance = camera.farClipPlane;
			frameData.hideTime = hideTime;
			frameData.preCullThreshold = preCullThreshold;

			frameData.deltaTime = Time.unscaledDeltaTime;
			frameData.time = Time.unscaledTime;
			frameData.cameraFOV = camera.fieldOfView;
			frameData.screenWidth = camera.pixelWidth;
			frameData.screenHeight = camera.pixelHeight;
			frameData.dynamicStep = dynamicStep;
			frameData.farClip = camera.farClipPlane;
			frameData.nearClip = camera.nearClipPlane;
			frameData.clampHeight = clampHeight;
			frameData.isVR = vr;
			frameData.optimizedVR = optimizedVR ? 1 : 0;
			frameData.skipShadomap = shouldSkipShadowmap ? 1 : 0;

			if(referenceObject != null) {
				frameData.referencePosition = referenceObject.position;
			} else {
				frameData.referencePosition = transform.position;
			}

			SetFrameData(ref frameData);

		}

	}

}