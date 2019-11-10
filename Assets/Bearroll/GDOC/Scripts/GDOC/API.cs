using System.Collections;
using System.Collections.Generic;
using Bearroll.GDOC_Internal;
using UnityEngine;

namespace Bearroll {

	public partial class GDOC {

		static public string sceneMetrics { get; private set; }

		static public void SetDirectionalLight(Light light) {

			if(instance == null) 
				return;

			instance.directionalLight = light;

		}

		static public void RestartBackgroundScan() {

			if(instance == null)
				return;

			instance.RestartBackgroundScanning();
		}

		static public void ProcessNewObject(MonoBehaviour component) {

			ProcessNewObject(component.gameObject);

		}

		static public void ProcessNewObject(GameObject gameObject) {

			if(instance == null)
				return;

			if(!instance.enabled)
				return;

			instance.ProcessGameObjectWithChildren(gameObject);

		}

		static public void RemoveOccludee(GDOC_Occludee occludee) {

			if(occludee == null)
				return;

			if(instance == null) 
				return;

			instance.RemoveOccludee(occludee.runtimeId);
		}

		static public void UpdateOccludee(GDOC_Occludee occludee) {

			if(occludee == null || !occludee.isActive)
				return;

			occludee.UpdateBounds();

			UpdateOccludee(occludee.runtimeId, occludee.position, occludee.size);

		}

		static public void AddOrUpdateOccludee(GameObject gameObject) {

			if(instance == null)
				return;

			if(!instance.enabled)
				return;

			var e = gameObject.GetComponent<GDOC_Occludee>();

			if(e != null && e.isActive) {

				e.UpdateBounds();

				UpdateOccludee(e.runtimeId, e.position, e.size);

			} else {

				instance.ProcessGameObject(gameObject, true);

			}

		}

		static public void ScanActiveScenes() {

			if(instance == null)
				return;

			instance.StartOrRestartCoroutine(ref instance.onenableScan, instance.ScanLoadedScenes());

		}

		static public void Disable() {

			if(instance == null)
				return;

			instance.enabled = false;

		}

		static public void Enable() {

			if(instance == null)
				return;

			instance.enabled = true;

		}

		IEnumerator DoRestart() {

			yield return null;
			yield return null;

			OnEnable();

		}

		static public void Restart() {

			if(instance == null)
				return;

			if(!instance.gameObject.activeInHierarchy)
				return;

			if(!instance.enabled) {
				Enable();
				return;
			}

			instance.OnDisable();

			instance.StartCoroutine(instance.DoRestart());

		}



		public static string CalculateSceneMetrics() {

			sceneMetrics = "";

			var meshRendererCount = 0;
			var psRendererCount = 0;
			var triangleCount = 0;
			var lightCount = 0;

			foreach(var t in Resources.FindObjectsOfTypeAll<Renderer>()) {

				if(!t.gameObject.scene.isLoaded)
					continue;

				if(t is MeshRenderer) {

					var meshFilter = t.GetComponent<MeshFilter>();

					if(meshFilter != null && meshFilter.sharedMesh != null) {

						meshRendererCount++;
						triangleCount += meshFilter.sharedMesh.triangles.Length;

					}

				} else if(t is ParticleSystemRenderer) {

					psRendererCount++;

				}

			}

			foreach(var t in Resources.FindObjectsOfTypeAll<Light>()) {

				if(!t.gameObject.scene.isLoaded)
					continue;

				lightCount++;

			}

			string triangleString;
			if(triangleCount >= 1000000) {
				triangleString = triangleCount / 1000000 + "kk";
			} else if(triangleCount >= 1000) {
				triangleString = triangleCount / 1000 + "k";
			} else {
				triangleString = triangleCount.ToString();
			}

			sceneMetrics += "MeshRenderers: " + meshRendererCount + "\n";
			sceneMetrics += "MeshFilter triangles: " + triangleString + "\n";
			sceneMetrics += "ParticleSystems: " + psRendererCount + "\n";
			sceneMetrics += "Lights: " + lightCount + "\n";

			return sceneMetrics;

		}

	}

}