using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CopyRotation : MonoBehaviour
{
    public Transform target;
    // Update is called once per frame
    void Update()
    {
        transform.rotation = target.rotation;
        var adjustedRotation = transform.eulerAngles;
        adjustedRotation.x = 0f;
        adjustedRotation.z = 0f;

        transform.eulerAngles = adjustedRotation;
    }
}
