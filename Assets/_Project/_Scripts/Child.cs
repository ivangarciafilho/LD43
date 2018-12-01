using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Child : MonoBehaviour
{
	public Transform target;
	public int parentTransformIndex;
	public NavMeshAgent agent;

    bool follow;
	
	void OnEnable()
	{
		agent.speed = Random.Range(agent.speed, agent.speed + 5);
	}

    void LateUpdate()
    {
		float dist = (target.position - transform.position).magnitude;
        if (dist < GameManager.Instance.playerRange)
            follow = true;

		if(follow)
		{
			agent.SetDestination(target.position);
			
			dist = (GameManager.Instance.cauldronTransform.position - transform.position).magnitude;
			if(dist < GameManager.Instance.cauldronRange)
			{
				target = GameManager.Instance.cauldronTransform;
				agent.stoppingDistance = 0;
				
				if(dist <= 0.98)
				{
					follow = false;
					gameObject.SetActive(false);
					GameManager.Instance.ReplaceChild(this);
				}
			}
		}
    }
	
}
