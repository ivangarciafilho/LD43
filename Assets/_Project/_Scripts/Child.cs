using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Child : MonoBehaviour
{
	public Transform target;
	public NavMeshAgent agent;
	
    void LateUpdate()
    {
		if(agent.remainingDistance <= 0.1)
		{        
			float dist = (target.position - transform.position).magnitude;
			if(dist < 7)
				agent.SetDestination(target.position);
		}
    }
}
