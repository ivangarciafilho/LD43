using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Bearroll.GDOC_Internal;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bearroll {

	public partial class GDOC {

		Coroutine backgroundScan;
		Coroutine onenableScan;
		Coroutine validationScan;
		HashSet<GameObject> scannedObjects = new HashSet<GameObject>();
		HashSet<Scene> scannedScenes = new HashSet<Scene>();
	    float lastScannedTime = float.NegativeInfinity;

		public bool isScanning {
	        get { return Time.unscaledTime - lastScannedTime < 1.1f; }
	    }

	    void NextTransform(ref Transform currentTransform, bool skipChildren = false) {

			if(!skipChildren && currentTransform.childCount > 0) {

				currentTransform = currentTransform.GetChild(0);

			} else {

				var p = currentTransform;

				while(p != null && p.parent != null) {

					if(p.GetSiblingIndex() < p.parent.childCount - 1) {
						currentTransform = p.parent.GetChild(p.GetSiblingIndex() + 1);
						return;
					}

					p = p.parent;

				}

				currentTransform = null;

			}

		}

		IEnumerator ScanAll() {

			var rootGameObjects = new List<GameObject>();
			var sw = new Stopwatch();
		    var r = false;
			
			while(true) {

			    var tickLimit = Stopwatch.Frequency / 1000 / 1000 * backgroundScanTimeLimit;
			    // var objectCount = 0;
			    // var countLimit = int.MaxValue;

				for(var i = 0; i < SceneManager.sceneCount && !isFull; i++) {

					var scene = SceneManager.GetSceneAt(i);

					if(!scene.isLoaded) continue;

					if(scene.rootCount == 0) continue;

					if (enableNewSceneScan && !scannedScenes.Contains(scene)) continue;

					scene.GetRootGameObjects(rootGameObjects);

				    yield return null;

				    for (var j = 0; j < rootGameObjects.Count && !isFull; j++) {

				        if (rootGameObjects[j] == null) break;

				        var currentTransform = rootGameObjects[j].transform;
				        var count = 0;

				        while (currentTransform != null && !isFull) {

				            sw.Reset();
				            sw.Start();

				            while (currentTransform != null && sw.ElapsedTicks < tickLimit && !isFull) {

                                if (!scannedObjects.Contains(currentTransform.gameObject)) {

				                    scannedObjects.Add(currentTransform.gameObject);

				                    r = ProcessGameObject(currentTransform.gameObject);

				                } else {
				                    r = false;
				                }

				                NextTransform(ref currentTransform, r);

				                count++;
				                backgroundCount++;
				                // objectCount++;

				            }

				            // Debug.Log("processed: " + count);

				            yield return null;

				        }

				    }

				}

			    // countLimit = objectCount / 60;

			    yield return null;

			}

		}

	    void ProcessGameObjectWithChildren(GameObject go) {

	        ProcessGameObject(go);

	        if (go.transform.childCount == 0) return;

	        foreach (Transform t in go.transform) {
                ProcessGameObjectWithChildren(t.gameObject);
	        }


	    }

		void StopBackgroundScanning() {

			if(backgroundScan != null) {
				StopCoroutine(backgroundScan);
				backgroundScan = null;
			}

		}

		void StartBackgroundScanning() {
		    if (!enableBackgroundScan) return;
			backgroundScan = StartCoroutine(ScanAll());
		}

		void RestartBackgroundScanning() {
			StopBackgroundScanning();
            StartBackgroundScanning();
		}

		IEnumerator ScanScene(Scene scene, bool restartBackground = false) {

			if (!scene.isLoaded) yield break;

			if (scene.rootCount == 0) yield break;

		    lastScannedTime = Time.unscaledTime;

		    if (restartBackground) {
		        StopBackgroundScanning();
		    }

		    var rootGameObjects = new List<GameObject>();
			var sw = new Stopwatch();
			var tickLimit = Stopwatch.Frequency / 1000 / 1000 * instantScanTimeLimit;

			scene.GetRootGameObjects(rootGameObjects);

		    yield return null;

		    for (var j = 0; j < rootGameObjects.Count; j++) {

		        if (rootGameObjects[j] == null) break;

		        var currentTransform = rootGameObjects[j].transform;

		        while (currentTransform != null) {

		            sw.Reset();
		            sw.Start();

		            while (currentTransform != null && sw.ElapsedTicks < tickLimit) {

		                scannedObjects.Add(currentTransform.gameObject);

		                var r = ProcessGameObject(currentTransform.gameObject);

		                if (isFull) break;

		                NextTransform(ref currentTransform, r);

		                lastScannedTime = Time.unscaledTime;

		            }

		            if (isFull) break;

		            yield return null;

		        }
                
		    }

		    scannedScenes.Add(scene);

		    if (restartBackground && enableBackgroundScan) {
		        StartBackgroundScanning();
		    }

		}

		IEnumerator ScanLoadedScenes() {

		    StopBackgroundScanning();
		    scannedScenes.Clear();
            
			for (var i = 0; i < SceneManager.sceneCount; i++) {

			    var scene = SceneManager.GetSceneAt(i);

			    if (!scene.isLoaded) continue;

				yield return StartCoroutine(ScanScene(scene));

			}

		    if (enableBackgroundScan) {
		        StartOrRestartCoroutine(ref backgroundScan, ScanAll());
		    }

		}

		IEnumerator ValidateOccludees() {

			var sw = Stopwatch.StartNew();
			var count = 0;

			while (true) {

			    if (occludeeList.Count == 0) yield return null;

                var ticks = Stopwatch.Frequency / 1000 / 1000;
			    var timeLimit = ticks * 10;
                var maxLimit = timeLimit * 10;

			    for (var i = occludeeList.Count - 1; i >= 0 && i < occludeeList.Count; i--) {

			        var id = occludeeList[i];
			        var e = occludees[id];

			        if (e == null) {
                        occludeeList.RemoveAt(i);
                        RemoveOccludee(id);
                        if (timeLimit < maxLimit) {
                            timeLimit++;
                        }
                    }

			        validationCount++;

			        if (sw.ElapsedTicks > timeLimit || count > occludeeList.Count / 60) {
			            // Debug.Log("validation: " + count);
			            count = 0;
			            yield return null;
			            sw.Reset();
			            sw.Start();
			        }

			    }

				yield return null;

			}

		}

		void OnSceneLoaded(Scene scene, LoadSceneMode mode) {

		    if (this == null) return;

			if (!enableNewSceneScan) return;

            if (autoDirectionalLight && directionalLight == null) {
                directionalLight = GDOC_Utils.FindMainDirectionalLight();
                ValidateSettings();
            }

			StartCoroutine(ScanScene(scene, true));

		}

		void OnSceneUnloaded(Scene scene) {

		    if (this == null) return;

			scannedScenes.Remove(scene);

		}

	    void StopScanning() {

	        lastScannedTime = float.NegativeInfinity;

	        SceneManager.sceneLoaded -= OnSceneLoaded;
	        SceneManager.sceneUnloaded -= OnSceneUnloaded;

	    }

	    void InitScanning() {

	        lastScannedTime = float.NegativeInfinity;

			scannedObjects.Clear();
			scannedScenes.Clear();

	        if (enableNewSceneScan) {

	            StartOrRestartCoroutine(ref onenableScan, ScanLoadedScenes());

	        } else {
	            
	            if (enableBackgroundScan) {
	                StartOrRestartCoroutine(ref backgroundScan, ScanAll());
	            }

	        }

			SceneManager.sceneLoaded -= OnSceneLoaded;
			SceneManager.sceneLoaded += OnSceneLoaded;

			SceneManager.sceneUnloaded -= OnSceneUnloaded;
			SceneManager.sceneUnloaded += OnSceneUnloaded;

			StartOrRestartCoroutine(ref validationScan, ValidateOccludees());

		}

		void StartOrRestartCoroutine(ref Coroutine coroutine, IEnumerator e) {

			if (coroutine != null) {
				StopCoroutine(coroutine);
			}

			coroutine = StartCoroutine(e);

		}

		void Kickstart() {

			GDOC_Utils.CalculateFrustumPlanes(camera, frustumPlanes);

			var pos = camera.transform.position;

			foreach (var e in FindObjectsOfType<GDOC_Occludee>()) {

				if (e.mode == GDOC_OccludeeMode.Excluded) continue;

				var d = (pos - e.position).sqrMagnitude;

				var rv = e.volumeSizeSqr / d;

				if (rv < 0.01 || (rv < 0.05 && !GeometryUtility.TestPlanesAABB(frustumPlanes, e.bounds))) {
					e.SetVisibleState(0, false);
				} 

			}

		}

	}

}