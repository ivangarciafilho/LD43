using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Profiling;

namespace Bearroll {

    public partial class GDOC {

	    static GDOC_Occludee[] occludees;
	    static List<GDOC_Occludee> dynamicOccludees;
	    static List<GDOC_Occludee> occluders;
	    static List<int> occludeeList;
	    static int maxOcludeeId;

	    Stopwatch sw = new Stopwatch();

        bool isFull {
            get { return occludeeList.Count == occludees.Length; }
        }

        public static int AddOccludee(GDOC_Occludee e, GDOC_Occludee parent = null) {

            if (e == null) return -1;

	        if (e.runtimeId > -1) return e.runtimeId;

            e.Init();

            var parentId = (parent != null && parent.isContainer) ? parent.runtimeId : -1;

            var id = AddOccludee(e.position, e.size, (int) e.mode, (int) e.movementMode, parentId, e.isImportant, e.isShadowSource, e.disablePrediction);

            if(id == -1) return id;

	        if (id > maxOcludeeId) {
		        maxOcludeeId = id;
	        }

	        e.runtimeId = id;

            occludees[id] = e;

            if (e.isDynamic) {
                dynamicOccludees.Add(e);
            }

            if (e.isImportant) {
                occluders.Add(e);
            }

            occludeeList.Add(id);

			if(instance.occludeeInitState > -1) {
                e.SetVisibleState(instance.occludeeInitState, true);
            }

            if (instance.autoClampHeight && e.isPotentialShadowReceiver) {
                instance.clampHeight = Mathf.Min(instance.clampHeight, e.position.y - e.size.y);
            }

	        return id;

        }

        void RemoveOccludee(int id) {

	        if (id < 0 || id >= occludees.Length) return;

            if (occludees[id] != null) {
                occludees[id].runtimeId = -1;
            }

            DeleteOccludee(id);

			occludees[id] = null;

        }

        void ChangeOccludeeState(int id, int state) {

            var e = occludees[id];

            if (e == null) return;

            if (debugLogging) {
				UnityEngine.Debug.Log(string.Format("{0} to {1}", id, state), e.gameObject);
            }

			if (!isShadowOCActive) {

				if (keepShadows) {

					if (state == 0) {
						state = 2;
					} else if (state == 3) {
						state = 1;
					}

				} else {

					if (state == 2) {
						state = 0;
					}

				}

			}

			// if (state == 2) state = 1;

			var full = state == 0 && (e.transform.position - transform.position).sqrMagnitude >= fullDisableDistance * fullDisableDistance;

            e.SetVisibleState(state, full);

        }

	    void SyncDynamic() {

			sw.Reset();
		    sw.Start();

	        var currentTime = Time.unscaledTime;

		    for (var i = dynamicOccludees.Count - 1; i >= 0; i--) {

			    var e = dynamicOccludees[i];

		        if (e == null) {
                    dynamicOccludees.RemoveAt(i);
                    continue;
		        }

			    if (!e.TryUpdateBounds(dynamicStep, currentTime)) continue;

				UpdateOccludee(e.runtimeId, e.position, e.size);

		    }

		    var t = (float) sw.ElapsedTicks / Stopwatch.Frequency;

		    dynamicTime += t;

	    }
		
        void Clean() {

	        for (var i = 0; i < maxOcludeeId; i++) {

		        if (occludees[i] != null) {
			        occludees[i].runtimeId = -1;
		        }

		        occludees[i] = null;

            }

            occludeeList.Clear();
            dynamicOccludees.Clear();
            occluders.Clear();

            ResetOccludees();

			StopAllCoroutines();

        }

        void EnableAllOccludees() {

	        for (var i = 0; i < maxOcludeeId; i++) {

		        if (occludees[i] != null) {
			        occludees[i].SetVisibleState(1);
		        }

	        }

        }

        

    }

}