using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance = null;

    public GameObject childPrefab;
    public Transform[] childGroupPoints;
    public Vector2Int minMaxChildsPerGroup;
    public Transform flautistTransform;
	public Transform cauldronTransform;
	public float cauldronRange = 18.0f;

    public PlayerController Player;

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
            SpawnChildsOnPoint(childGroupPoints[i]);
        }
    }

    private void LateUpdate()
    {
        
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
   
   	void OnDrawGizmos()
	{
		if(cauldronTransform)
		{
			Gizmos.color = Color.cyan;
			Gizmos.DrawWireSphere(cauldronTransform.position, cauldronRange);
		}
	}
}
