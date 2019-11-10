using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bearroll.GDOC_Demo {

	namespace Bearroll_Demo {

		public class GDOC_SceneLoader : MonoBehaviour {

			public int sceneIndex;
			public KeyCode loadKey = KeyCode.F10;
			public KeyCode unloadKey = KeyCode.F11;

			Scene scene;

			void Start() {

				scene = SceneManager.GetSceneByBuildIndex(sceneIndex);

				if (scene == null) {
					enabled = false;
					return;
				}

			}

			void Update() {

				if (Input.GetKeyDown(loadKey)) {

					if (!scene.isLoaded) {

						SceneManager.LoadSceneAsync(sceneIndex, LoadSceneMode.Additive);

					}

				}


				if (Input.GetKeyDown(unloadKey)) {

					SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene());
				}

			}

		}

	}

}