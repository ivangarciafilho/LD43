using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bearroll.GDOC_Demo {

    public class GDOC_DirectionalLight : MonoBehaviour {

        public float speed = 10;

        Vector3 eulerAngles;

        void OnEnable() {
            eulerAngles = transform.eulerAngles;
        }

        void Update() {

            eulerAngles.y += speed * Time.deltaTime;
            transform.eulerAngles = eulerAngles;

        }

    }


}