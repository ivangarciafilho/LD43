using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bearroll.GDOC_Demo {

    public class GDOC_Mover: MonoBehaviour {

		public float speed = 5f;

		float nextChange = 0;
		Vector3 direction;

        void Update() {

			if (Time.time >= nextChange) {

				direction = Random.insideUnitSphere;
				direction.y = 0;
				direction.Normalize();
				nextChange = Time.time + Random.Range(1, 10f);

			}

			transform.rotation = Quaternion.LookRotation(direction);
			transform.position += direction * speed * Time.deltaTime;

		}

    }

}