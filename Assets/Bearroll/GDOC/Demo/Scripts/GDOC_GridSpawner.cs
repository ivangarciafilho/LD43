using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bearroll.GDOC_Demo {

    public class GDOC_GridSpawner: MonoBehaviour {

	    public bool respawnOnStart = true;
        public GameObject prefab;
        public int n = 15;
        public float step = 10;
        public bool randomAARotation = true;
        public bool randomOffset = true;
        public bool randomMovement = false;
        public float minScale = 1;
        public float maxScale = 1;
        public float minHeight = 0;
        public float maxHeight = 0;
        public int seed = 12345;
        public bool disableOnSpawn = false;
        public bool skipCenter = false;
		public bool controlGDOC = true;

		int currentN {
			get {

				var c = Camera.main;

				if (c.actualRenderingPath == RenderingPath.DeferredShading) return n;

				return n / 2;
			}
		}

	    void Start() {
		    if (respawnOnStart) {
		        Respawn();
		    }
	    }

        public void Respawn() {
            StartCoroutine(RespawnC());
        }

        IEnumerator RespawnC() {

            if (prefab == null) yield break;

			if (controlGDOC) {
				GDOC.Disable();
			}

			yield return null;

            Clear();

            yield return null;

            Random.InitState(seed);

            GameObject processedPrefab = null;

            for (var i = -n; i < currentN; i++) {

                for (var j = -n; j < currentN; j++) {

                    if (skipCenter && i == 0 && j == 0) continue;

                    var scale = Random.Range(minScale, maxScale);
                    var position = transform.position + new Vector3(i * step, Random.Range(minHeight, maxHeight) * scale, j * step);

                    var sample = processedPrefab == null ? prefab : processedPrefab;
                    var e = Instantiate(sample, position, Quaternion.identity).transform;

                    if (processedPrefab == null) {
                        GDOC.ProcessNewObject(e.gameObject);
                        processedPrefab = e.gameObject;
                    }

                    e.transform.SetParent(transform, true);
                    e.localScale = Vector3.one * scale;

                    if (randomAARotation) {
                        e.localEulerAngles = Vector3.up * 90 * Random.Range(0, 4);
                    }

                    if (randomOffset) {
                        e.localPosition += e.forward * Random.Range(0, step * 0.5f);
                    }

                    if (disableOnSpawn) {
                        e.gameObject.SetActive(false);
                    }

                }

                var state = Random.state;

                Random.state = state;

            }

			if (controlGDOC) {
				GDOC.Enable();
			}

		}

        public void Clear() {

            while (transform.childCount > 0) {
                DestroyImmediate((transform.GetChild(0).gameObject));
            }

        }

        void Update() {

            if (!randomMovement) return;

            var time = Time.deltaTime;

            foreach (Transform t in transform) {

                var speed = 2f + 2f * (t.GetSiblingIndex() % 13) / 13f;

                t.position += Vector3.Lerp(t.forward, t.right, (t.GetSiblingIndex() % 17) / 17f) * speed * time;

            }

        }

    }

}