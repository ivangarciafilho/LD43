using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshCombineStudio
{
    [ExecuteInEditMode]
    public class RandomizeTransform : MonoBehaviour
    {
        public Vector2 scaleRange = new Vector2(0.75f, 1.25f);

        void OnEnable()
        {
            transform.localScale *= Random.Range(scaleRange.x, scaleRange.y);
            transform.rotation = Random.rotation;

            enabled = false;    
        }
    }
}

