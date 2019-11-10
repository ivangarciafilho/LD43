using System.Collections.Generic;
using UnityEngine;

namespace Bearroll.GDOC_Demo {

    public class GDOC_DynamicSpawner: MonoBehaviour {

        public List<GameObject> prefabs;
        public float radius = 500;
        public float height = 100;
        //public float interval = 0.5f;
        public float minSize = 1;
        public float maxSize = 50;
        public int count = 1000;
		public KeyCode key = KeyCode.F3;

        float nextSpawnTime = 0;
        Queue<GameObject> objects = new Queue<GameObject>();
		List<Rigidbody> rigidbodies = new List<Rigidbody>();

        void Start() {

            for (var i = 0; i < count; i++) {
                Spawn();
            }

        }

        void Spawn() {
            
            while (objects.Count >= count) {
                // Destroy(objects.Dequeue());
            }

            if (prefabs.Count == 0) return;

            var e = Instantiate(prefabs[Random.Range(0, prefabs.Count)]);

            Vector3 pos = Random.insideUnitCircle * radius;
            pos.z = pos.y;
            pos.y = Random.Range(height, height * 4f);

			e.transform.parent = transform;
            e.transform.position = pos;
            e.transform.rotation = Random.rotation;
            e.transform.localScale = Vector3.one * Random.Range(minSize, maxSize);

			var rb = e.GetComponent<Rigidbody>();

			if (rb != null) {
				rigidbodies.Add(rb);
			}

            GDOC.ProcessNewObject(e);

        }

        void Update() {

			if (Input.GetKeyDown(key)) {

				for(var i = rigidbodies.Count - 1; i >= 0; i--) {

					if (rigidbodies[i] == null) {
						rigidbodies.RemoveAt(i);
						continue;
					}

					rigidbodies[i].AddForce(Vector3.up * Random.Range(-10000, 10000), ForceMode.Impulse);

				}

			}

            //if (Time.time < nextSpawnTime) return;

            //nextSpawnTime = Time.time + interval;

            //Spawn();

        }

    }

}