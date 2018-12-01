using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Child : MonoBehaviour
{
	public Transform target;
	public NavMeshAgent agent;

    bool follow;
	
	void Start()
	{
		agent.speed = Random.Range(agent.speed, agent.speed + 5);
	}

    void LateUpdate()
    {
		float dist = (target.position - transform.position).magnitude;
        if (dist < 7)
            follow = true;

		if(follow)
		{
			agent.SetDestination(target.position);
			
			dist = (GameManager.Instance.cauldronTransform.position - transform.position).magnitude;
			if(dist < GameManager.Instance.cauldronRange)
			{
				target = GameManager.Instance.cauldronTransform;
				agent.stoppingDistance = 0;
				
				if(dist < 1)
				{
					gameObject.SetActive(false);
				}
			}
		}
    }
	
}
