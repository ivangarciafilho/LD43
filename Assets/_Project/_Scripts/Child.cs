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
	public ParticleSystem enchantedVfx;

	private float defaultSpeed;
	private float defaultAngularSpeed;
	private float defaultAcceleration;

	private Vector2 speedRange;
	private Vector2 angularSpeedRange;
	private Vector2 accelerationRange;

	bool follow;

	private void Awake()
	{
		itsAnimator = GetComponentInChildren<Animator>();
		walkAnimationHash = Animator.StringToHash("walk");
		speedAnimationHash = Animator.StringToHash("Speed");

		defaultSpeed = agent.speed;
		speedRange.x = defaultSpeed * 0.75f;
		speedRange.y = defaultSpeed * 1.5f;

		defaultAngularSpeed = agent.angularSpeed;
		angularSpeedRange.x = defaultAngularSpeed * 0.75f;
		angularSpeedRange.y = defaultAngularSpeed * 1.5f;

		defaultAcceleration = agent.acceleration;
		accelerationRange.x = defaultAcceleration * 0.75f;
		accelerationRange.y = defaultAcceleration * 1.5f;
	}

	void OnEnable()
	{
		agent.speed = Random.Range(speedRange.x, speedRange.y);
		agent.angularSpeed = Random.Range(angularSpeedRange.x, angularSpeedRange.y);
		agent.acceleration = Random.Range(accelerationRange.x, accelerationRange.y);

		follow = false;

		playerTransform = GameManager.Instance.Player.transform;
		caldronTransform = GameManager.Instance.cauldronTransform;
	}

	private void Update()
	{
		if (distanceFromFlautist < GameManager.Instance.playerRange)
			if (Input.GetMouseButtonDown(1))
				follow = true;
	}

	private void FixedUpdate()
	{
		if (Time.time < nextDistanceCheck) return;

		distanceFromCaldron = Vector3.Distance(caldronTransform.position, transform.position);

		if (distanceFromCaldron < GameManager.Instance.playerRange + agent.stoppingDistance)
		{
			follow = false;
			gameObject.SetActive(false);
			GameManager.Instance.ReplaceChild(this);
		}
		else
		{
			distanceFromFlautist = Vector3.Distance(playerTransform.position, transform.position);

			if (follow)
			{
				follow =
				distanceFromCaldron < minimumFollowDistance + agent.stoppingDistance
				|| distanceFromFlautist < minimumFollowDistance + agent.stoppingDistance;
			}

			target = distanceFromFlautist > distanceFromCaldron ? caldronTransform : playerTransform;
		}

		nextDistanceCheck = Time.time + 0.2f;
	}

	void LateUpdate()
	{
		agent.SetDestination(follow ? target.position:transform.position);
		itsAnimator.SetBool(walkAnimationHash, (agent.remainingDistance - agent.stoppingDistance) > 0.1f);
		itsAnimator.SetFloat(speedAnimationHash, 1 + agent.velocity.magnitude);

		enchantedVfx.loop = follow;
		if (enchantedVfx.isPlaying == false && enchantedVfx.loop) enchantedVfx.Play();
	}
}
