using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Child : MonoBehaviour
{
	public Transform target;
	public NavMeshAgent agent;

    bool follow;

    void LateUpdate()
    {
		float dist = (target.position - transform.position).magnitude;
        if (dist < 7)
            follow = true;

            if(follow)
				agent.SetDestination(target.position);
    }
}
