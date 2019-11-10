using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bearroll.GDOC_Demo {

    public class GDOC_Torch: MonoBehaviour {

        public float range = 0.5f;
        public float heightRange = 0.1f;
        public float scale1 = 2.33f;
        public float scale2 = 7.55f;
        public float scale3 = 13.77f;
        public float scale4 = 17.11f;

        new Light light;
        float intensity;
        Vector3 localPosition;
        float offset;

        void OnEnable() {

            light = GetComponent<Light>();

            if (light == null) {
                enabled = false;
                return;
            }

            intensity = light.intensity;
            localPosition = transform.localPosition;
            offset = Random.Range(-100f, 100f);

        }

        void Update() {

            var t = Time.time + offset;

            var f = Mathf.Sin(t * scale1) * Mathf.Sin(t * scale2) * Mathf.Sin(t * scale2) * Mathf.Sin(t * scale2);

            light.intensity = intensity + range * intensity * f;

            transform.localPosition = localPosition + Vector3.one * heightRange * f;

        }

    }

}