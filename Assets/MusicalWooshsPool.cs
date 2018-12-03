using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class MusicalWooshsPool : MonoBehaviour
{
    public static MusicalWooshsPool instance { get; private set; }
    [SerializeField] private GameObject[] poolInstances;

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

    private static int currentAvailableSfx;
    public static void PlaySfxOnPosition(Vector3 point)
    {
        if (currentAvailableSfx >= instance.poolInstances.Length) { currentAvailableSfx = 0; }

        //if (Physics.Raycast(point + Vector3.up * 3f, Vector3.down * -9f, out hit))
        //{
        //    point.y = hit.point.y;
        //}

        point.y -= 0.5f;//hardcoded for while

        instance.poolInstances[currentAvailableSfx].gameObject.SetActive(false);
        instance.poolInstances[currentAvailableSfx].transform.position = point;
        instance.poolInstances[currentAvailableSfx].gameObject.SetActive(true);

        currentAvailableSfx++;
    }
}
