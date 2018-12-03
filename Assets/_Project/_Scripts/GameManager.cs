using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
	public static GameManager Instance = null;

	public GameObject childPrefab;
	public Transform[] childGroupPoints;
	public Vector2Int minMaxChildsPerGroup;
	public Transform flautistTransform;
	public Transform cauldronTransform;
	public float cauldronRange = 18.0f;
	public GameObject audioHyerarchy;

	public PlayerController Player;
	public float playerRange = 5;
	
	public Text sheepCounterText;
	public int sheepCounter;

    public List<Transform> sheepsWaypoints = new List<Transform>();

	void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}

		else if (Instance != this)
		{
			Destroy(gameObject);
		}

		// Dont destroy on reloading the scene
		DontDestroyOnLoad(gameObject);

		for (int i = 0; i < childGroupPoints.Length; i++)
		{
			SpawnChildsOnPoint(childGroupPoints[i], i);
		}

		StopAllCoroutines();
		StartCoroutine(DelayedActivation(audioHyerarchy));
	}

	private IEnumerator DelayedActivation(GameObject delayedObject, float delay = 5f)
	{
		yield return new WaitForSeconds(delay);
		delayedObject.SetActive(true);
	}

	void SpawnChildsOnPoint(Transform t, int index)
	{
		int childCount = Random.Range(minMaxChildsPerGroup.x, minMaxChildsPerGroup.y);
		Vector3 p = t.position;

		for (int i = 0; i < childCount; i++)
		{
			Vector3 pos = new Vector3(p.x + Random.Range(-4, 4), p.y, p.z + Random.Range(-4, 4));

			Child child = Instantiate(childPrefab, pos, Quaternion.identity).GetComponent<Child>();
			child.transform.SetParent(t);
			child.parentTransformIndex = index;

			child.target = flautistTransform;
		}
	}

	public void ReplaceChild(Child child)
	{
		Vector3 p = childGroupPoints[child.parentTransformIndex].position;
		Vector3 pos = new Vector3(p.x + Random.Range(-4, 4), p.y, p.z + Random.Range(-4, 4));

		child.target = flautistTransform;
		child.transform.position = pos;
		child.gameObject.SetActive(true);
		
		sheepCounter++;
		sheepCounterText.text = sheepCounter.ToString();
	}

	void OnDrawGizmos()
	{
		if(cauldronTransform)
		{
			Gizmos.color = Color.cyan;
			Gizmos.DrawWireSphere(cauldronTransform.position, cauldronRange);
		}
	}
}
