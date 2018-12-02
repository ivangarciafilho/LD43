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
		Time.timeScale = 0;

		Application.backgroundLoadingPriority = ThreadPriority.BelowNormal;

		AsyncOperation scenarioScene = SceneManager.LoadSceneAsync("Scenario", LoadSceneMode.Additive);
		scenarioScene.allowSceneActivation = false;

		while (scenarioScene.isDone == false)
		{
			yield return null;
			if (scenarioScene.progress >= 0.9)
			{
				scenarioScene.allowSceneActivation = true;
			}
		}

		AsyncOperation gameplayScene = SceneManager.LoadSceneAsync("Gameplay", LoadSceneMode.Additive);

		while (gameplayScene.isDone == false)
		{
			yield return null;
			if (gameplayScene.progress >= 0.9)
			{
				gameplayScene.allowSceneActivation = true;
			}
		}

		SceneManager.SetActiveScene(SceneManager.GetSceneByName("Scenario"));

		SceneManager.UnloadSceneAsync("Loader");

		Time.timeScale = 1;
	}
}
