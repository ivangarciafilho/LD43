using System.Collections.Generic;
using UnityEngine;

namespace Bearroll {

    public class GDOC_Utils {

		public static void CalculateFrustumPlanes(Camera camera, Plane[] planes) {

			#if UNITY_2017_1_OR_NEWER

			GeometryUtility.CalculateFrustumPlanes(camera, planes);

			#else

			var mat = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true) * camera.worldToCameraMatrix;

			// left
			planes[0].normal = new Vector3(mat.m30 + mat.m00, mat.m31 + mat.m01, mat.m32 + mat.m02);
			planes[0].distance = mat.m33 + mat.m03;
 
			// right
			planes[1].normal = new Vector3(mat.m30 - mat.m00, mat.m31 - mat.m01, mat.m32 - mat.m02);
			planes[1].distance = mat.m33 - mat.m03;
 
			// bottom
			planes[2].normal = new Vector3(mat.m30 + mat.m10, mat.m31 + mat.m11, mat.m32 + mat.m12);
			planes[2].distance = mat.m33 + mat.m13;
 
			// top
			planes[3].normal = new Vector3(mat.m30 - mat.m10, mat.m31 - mat.m11, mat.m32 - mat.m12);
			planes[3].distance = mat.m33 - mat.m13;
 
			// near
			planes[4].normal = new Vector3(mat.m30 + mat.m20, mat.m31 + mat.m21, mat.m32 + mat.m22);
			planes[4].distance = mat.m33 + mat.m23;
 
			// far
			planes[5].normal = new Vector3(mat.m30 - mat.m20, mat.m31 - mat.m21, mat.m32 - mat.m22);
			planes[5].distance = mat.m33 - mat.m23;
 
			// normalize
			for (uint i = 0; i < 6; i++) {
				var length = planes[i].normal.magnitude;
				planes[i].normal /= length;
				planes[i].distance /= length;
			}

			#endif

		}

        public static Bounds TransformBoundsToWorldSpace(Transform transform, Bounds localBounds, bool ignoreScale = true) {

            var extents = localBounds.extents;
			Vector3 center;
			Vector3 axisX;
			Vector3 axisY;
			Vector3 axisZ;

			if (ignoreScale) {

				center = transform.position + transform.TransformDirection(localBounds.center);
				axisX = transform.TransformDirection(extents.x, 0, 0);
				axisY = transform.TransformDirection(0, extents.y, 0);
				axisZ = transform.TransformDirection(0, 0, extents.z);


			} else {

				center = transform.TransformPoint(localBounds.center);
				axisX = transform.TransformVector(extents.x, 0, 0);
				axisY = transform.TransformVector(0, extents.y, 0);
				axisZ = transform.TransformVector(0, 0, extents.z);

			}

			extents.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
            extents.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
            extents.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);
 
            return new Bounds { center = center, extents = extents };
        }

        static public Vector3 ApplyScale(Vector3 a, Vector3 scale, Vector3 originalScale) {

            scale.x /= originalScale.x;
            scale.y /= originalScale.y;
            scale.z /= originalScale.z;

            a.x *= scale.x;
            a.y *= scale.y;
            a.z *= scale.z;

            return a;
        }

        public static Light FindMainDirectionalLight() {

            var lights = new List<Light>();

            foreach(var e in Object.FindObjectsOfType<Light>()) {

                if(!e.gameObject.activeInHierarchy)
                    continue;

                if(!e.enabled)
                    continue;

                if(e.type != LightType.Directional)
                    continue;

                if(e.shadows == LightShadows.None)
                    continue;

                lights.Add(e);

            }

            if(lights.Count == 1)
                return lights[0];

            return null;

        }

    }

}