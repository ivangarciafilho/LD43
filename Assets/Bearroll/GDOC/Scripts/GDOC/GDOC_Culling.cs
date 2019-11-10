using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Bearroll {

    public partial class GDOC {

        Vector3 lastCameraPosition;
        Quaternion lastCameraRotation;
        Plane[] frustumPlanes = new Plane[6];
        Stopwatch precullSW = new Stopwatch();

        void EnableFrustumObjects() {

            if (shouldLogInfo) {
                sw.Reset();
                sw.Start();
            }

			GDOC_Utils.CalculateFrustumPlanes(camera, frustumPlanes);

            var pos = camera.transform.position;
            var count = 0;

            for(var i = occluders.Count - 1; i >= 0; i--) {

                var e = occluders[i];

                if (e == null) {
                    occluders.RemoveAt(i);
                    continue;
                }

                if (!e.isActive) continue;

                if (e.currentState == 1 || e.currentState == 3) continue;

                var d = (pos - e.position).sqrMagnitude;

                var rv = e.volumeSizeSqr / d;

                if (rv < teleportRV) continue;

                if (!GeometryUtility.TestPlanesAABB(frustumPlanes, e.bounds)) continue;

                ChangeOccludeeState(e.runtimeId, 1);

                UpdateOccludeeState(e.runtimeId, 1);

                count++;

            }

            if (shouldLogInfo) {
                
                var t = (float) sw.ElapsedTicks / Stopwatch.Frequency * 1000f;

				UnityEngine.Debug.Log(string.Format("Enabled {0} of {1} objects in {2} ms", count, occluders.Count, t));

            }

        }

        void CheckTeleport() {

            if (isScanning) return;

            var distance = Vector3.Distance(camera.transform.position, lastCameraPosition);
            var angle = Quaternion.Angle(camera.transform.rotation, lastCameraRotation);

            lastCameraPosition = camera.transform.position;
            lastCameraRotation = camera.transform.rotation;

			if (!enableTeleportDetection) return;

            var distanceK = Mathf.Clamp01(distance / teleportDistance);
            var angleK = Mathf.Clamp01(angle / teleportAngle);

            var k = distanceK + angleK;

            if (k < 1) return;

            if (shouldLogInfo) {
				UnityEngine.Debug.Log(string.Format("Teleport detected (distance: {0}, angle: {1})", distance, angle));
            }

            EnableFrustumObjects();

        }

    }

}