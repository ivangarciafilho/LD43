using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Child : MonoBehaviour
{
	public Transform target;
	public int parentTransformIndex;
	public NavMeshAgent agent;
	public float followTime = 4f;
	private float nextDistanceCheck = 0f;
	private float distanceFromFlautist = 0f;
	private float distanceFromCaldron = 0f;
	private Transform caldronTransform;
	private Transform playerTransform;
	public float minimumFollowDistance = 9f;
	private Animator itsAnimator;
	private int walkAnimationHash;
	private int speedAnimationHash;

	bool follow;

	void OnEnable()
	{
		itsAnimator = GetComponentInChildren<Animator>();
		walkAnimationHash = Animator.StringToHash("walk");
		speedAnimationHash = Animator.StringToHash("Speed");

		agent.speed = Random.Range(agent.speed, agent.speed + 5);
		follow = false;

		playerTransform = GameManager.Instance.Player.transform;
		caldronTransform = GameManager.Instance.cauldronTransform;
	}

	private void Update()
	{
		if (distanceFromFlautist < GameManager.Instance.playerRange)
			if (Input.GetMouseButtonDown(1))
			{
				follow = true;
				nextDistanceCheck = Time.time + followTime;
			}
	}

	private void FixedUpdate()
	{
		if (Time.time < nextDistanceCheck) return;

		distanceFromCaldron = Vector3.Distance(caldronTransform.position, transform.position);

		if (distanceFromCaldron < 1 + agent.stoppingDistance)
		{
			follow = false;
			gameObject.SetActive(false);
			GameManager.Instance.ReplaceChild(this);
		}
		else
		{
			distanceFromFlautist = Vector3.Distance(playerTransform.position, transform.position);

			follow =
				distanceFromCaldron < minimumFollowDistance + agent.stoppingDistance
				|| distanceFromFlautist < minimumFollowDistance + agent.stoppingDistance;

			target = distanceFromFlautist > distanceFromCaldron ? caldronTransform : playerTransform;
		}

		nextDistanceCheck = Time.time + 0.2f;
	}

	void LateUpdate()
	{
		agent.SetDestination(follow ? target.position:transform.position);
		itsAnimator.SetBool(walkAnimationHash, (agent.remainingDistance - agent.stoppingDistance) > 0.1f);
		itsAnimator.SetFloat(speedAnimationHash, 1 + agent.velocity.magnitude);
	}
}
