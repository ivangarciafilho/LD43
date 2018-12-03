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
	
	private float defaultStoppingDistance;

	private Vector2 speedRange;
	private Vector2 angularSpeedRange;
	private Vector2 accelerationRange;

    int waypointIndex;
	bool follow;

	private void Awake()
	{
		itsAnimator = GetComponentInChildren<Animator>();
		walkAnimationHash = Animator.StringToHash("walk");
		speedAnimationHash = Animator.StringToHash("Speed");

		defaultSpeed = agent.speed;
		speedRange.x = defaultSpeed * 0.75f;
		speedRange.y = defaultSpeed * 1.5f;
		defaultStoppingDistance = agent.stoppingDistance;
		
		defaultAngularSpeed = agent.angularSpeed;
		angularSpeedRange.x = defaultAngularSpeed * 0.75f;
		angularSpeedRange.y = defaultAngularSpeed * 1.5f;

		defaultAcceleration = agent.acceleration;
		accelerationRange.x = defaultAcceleration * 0.75f;
		accelerationRange.y = defaultAcceleration * 1.5f;
	}

	void OnEnable()
	{
        waypointIndex = Random.Range(0, GameManager.Instance.sheepsWaypoints.Count);

        agent.speed = Random.Range(speedRange.x, speedRange.y);
		agent.angularSpeed = Random.Range(angularSpeedRange.x, angularSpeedRange.y);
		agent.acceleration = Random.Range(accelerationRange.x, accelerationRange.y);
		agent.stoppingDistance = defaultStoppingDistance;

		follow = false;

		playerTransform = GameManager.Instance.Player.transform;
		caldronTransform = GameManager.Instance.cauldronTransform;
	}

	private void Update()
	{
		if (distanceFromFlautist < GameManager.Instance.playerRange && !follow)
		{
			if (Input.GetMouseButtonDown(1))
			{
				follow = true;
				target = playerTransform;
			}
		}
	}

	private void FixedUpdate()
	{
		if (Time.time < nextDistanceCheck) return;
		
		distanceFromFlautist = Vector3.Distance(playerTransform.position, transform.position);
		if(follow)
		{
			distanceFromCaldron = Vector3.Distance(caldronTransform.position, transform.position);

			if (distanceFromCaldron < 1.2f)
			{
				follow = false;
				gameObject.SetActive(false);
				RemainsPool.PlayVfxOnPosition(transform.position);
				BoltsPool.PlayVfxOnPosition(transform.position);
				GameManager.Instance.ReplaceChild(this);
			}
			else if(distanceFromCaldron <  GameManager.Instance.cauldronRange)
			{
				agent.stoppingDistance = 0;
				target = caldronTransform;
			}
		}

		nextDistanceCheck = Time.time + 0.2f;
	}

    Vector3 GetWaypoint()
    {
        Transform result = GameManager.Instance.sheepsWaypoints[waypointIndex];

        if (agent.remainingDistance <= agent.stoppingDistance + 1)
        {
            waypointIndex = Random.Range(0, GameManager.Instance.sheepsWaypoints.Count);
            result = GameManager.Instance.sheepsWaypoints[waypointIndex];
        }

        return result.position;
    }

	void LateUpdate()
	{
		agent.SetDestination(follow ? target.position : GetWaypoint());
		itsAnimator.SetBool(walkAnimationHash, (agent.remainingDistance - agent.stoppingDistance) > 0.1f);
		itsAnimator.SetFloat(speedAnimationHash, 1 + agent.velocity.magnitude);

		enchantedVfx.loop = follow;
		if (enchantedVfx.isPlaying == false && enchantedVfx.loop) enchantedVfx.Play();
	}
}
