using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GameManager : MonoBehaviour
{
    public GameObject childPrefab;
    public Transform[] childGroupPoints;
    public Vector2Int minMaxChildsPerGroup;
    public Transform flautistTransform;
    public Transform cameraTransform;
    public Vector3 cameraOffset;

    void Awake()
    {
        for (int i = 0; i < childGroupPoints.Length; i++)
        {
            SpawnChildsOnPoint(childGroupPoints[i]);
        }
    }

    private void LateUpdate()
    {
        Vector3 camPos = cameraTransform.position;
        Vector3 flautistPos = flautistTransform.position;

        Vector3 futureCamPos = Vector3.Lerp(camPos, flautistPos, Time.deltaTime * 1.8f);
        futureCamPos += cameraOffset;

        futureCamPos.y = camPos.y;

        cameraTransform.position = futureCamPos;
    }

    void SpawnChildsOnPoint(Transform t)
    {
        int childCount = Random.Range(minMaxChildsPerGroup.x, minMaxChildsPerGroup.y);
        Vector3 p = t.position;

        for (int i = 0; i < childCount; i++)
        {
            Vector3 pos = new Vector3(p.x + Random.Range(-4, 4), p.y, p.z + Random.Range(-4, 4));

            Child child = Instantiate(childPrefab, pos, Quaternion.identity).GetComponent<Child>();

            child.target = flautistTransform;

            NavMeshAgent agent = child.GetComponent<NavMeshAgent>();

        }
    }
   
}
