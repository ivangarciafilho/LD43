using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Loader : MonoBehaviour
{
    void Awake()
    {
        StartCoroutine(LoadAll());
    }
	
	IEnumerator LoadAll()
	{
		AsyncOperation sampleScene = SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Additive);
		while (sampleScene.isDone == false)
        {
            yield return null;
        }
		
        AsyncOperation gameplayScene = SceneManager.LoadSceneAsync("Gameplay", LoadSceneMode.Additive);
		
		SceneManager.UnloadSceneAsync("Loader");
	}
}
