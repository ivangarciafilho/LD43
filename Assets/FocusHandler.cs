using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class FocusHandler : MonoBehaviour
{
    public Transform player;
    public Rigidbody itsRigidbody;
    public List<Transform> closestSheeps;
    private List<Transform> sheepsOutOfRange;
    public float targetAcquisition = 9f;
    public CinemachineTargetGroup cameraGroupFocus;

    public float playerWeight = 0.75f;
    public float playerRadius = 1f;

    public float secondaryTargetsWeight = 0.25f;
    public float secondarytargetsRadius = 2f;



    private void Awake()
    {
        closestSheeps = new List<Transform>();
        closestSheeps.Add(player);
        sheepsOutOfRange = new List<Transform>();
    }



    private void Update()
    {
        sheepsOutOfRange.Clear();

        foreach (var sheep in closestSheeps)
        {
            if (Vector3.Distance(player.position,sheep.position) > targetAcquisition)
            {
                sheepsOutOfRange.Add(sheep);
            }
        }

        foreach (var sheep in sheepsOutOfRange)
        {
            closestSheeps.Remove(sheep);
        }

        cameraGroupFocus.m_Targets = null;
        var amountOfTargetsToHandle = closestSheeps.Count;
        cameraGroupFocus.m_Targets = new CinemachineTargetGroup.Target[amountOfTargetsToHandle];

        for (int i = 0; i < amountOfTargetsToHandle; i++)
        {
            cameraGroupFocus.m_Targets[i].target = closestSheeps[i];
            if (closestSheeps[i] == player)
            {
                cameraGroupFocus.m_Targets[i].weight = playerWeight;
                cameraGroupFocus.m_Targets[i].radius = playerRadius;
            }
            else
            {
                cameraGroupFocus.m_Targets[i].weight = secondaryTargetsWeight;
                cameraGroupFocus.m_Targets[i].radius = secondarytargetsRadius;
            }
        }
    }

    private void FixedUpdate()
    {
        itsRigidbody.MovePosition (Vector3.MoveTowards(transform.position,player.position, 1f*Time.fixedDeltaTime));
    }

    public void OnTriggerEnter(Collider other)
    {
        if (closestSheeps.Contains(other.transform)) return;
        closestSheeps.Add(other.transform);
    }
}
