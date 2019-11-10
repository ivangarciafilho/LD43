using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bearroll.GDOC_Demo {

    public class GDOC_LockCursor: MonoBehaviour {

        public KeyCode key = KeyCode.F5;
		public bool dontDestroyOnLoad = false;

		void Start() {

			if (dontDestroyOnLoad) {
				DontDestroyOnLoad(gameObject);
			}

		}

        void Update() {

            if (Input.GetKeyUp(key)) {

                Cursor.visible = !Cursor.visible;
                Cursor.lockState = Cursor.visible ? CursorLockMode.None : CursorLockMode.Locked;

            }

        }

    }

}