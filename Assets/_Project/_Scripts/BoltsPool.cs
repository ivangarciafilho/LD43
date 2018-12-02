using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BoltsPool : MonoBehaviour
{
    public static BoltsPool instance { get; private set; }
    [SerializeField]private ParticleSystem[] poolInstances;

    private void Awake()
    {
        if (instance != null)
        {
            if (instance != this)
            {
                DestroyImmediate(gameObject);
                return;
            }
        }

        instance = this;
    }

    private static int currentAvailableVfx;
    private static RaycastHit hit;
    public static void PlayVfxOnPosition(Vector3 point)
    {
        if (currentAvailableVfx >= instance.poolInstances.Length) { currentAvailableVfx = 0; }

        //if (Physics.Raycast(point + Vector3.up * 3f, Vector3.down * -9f, out hit))
        //{
        //    point.y = hit.point.y;
        //}

        point.y = 0.25f;//hardcoded for while

        instance.poolInstances[currentAvailableVfx].transform.position = point;
        instance.poolInstances[currentAvailableVfx].gameObject.SetActive(true);
        instance.poolInstances[currentAvailableVfx].Play();

        currentAvailableVfx++;
    }
}
