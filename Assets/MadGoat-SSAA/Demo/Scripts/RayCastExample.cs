using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MadGoat_SSAA
{
    public class RayCastExample : MonoBehaviour
    {
        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyUp(KeyCode.Mouse0))
            {
                RaycastHit hitInfo;

                // Without Wrapper function
                // ---------------
                // Using ScreenPointToRay when the render resolution of the camera is different
                // than the screen resolution results in offsetted mouse position.
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out hitInfo, 1000))
                    Debug.Log("Offsetted Hit Info: " + hitInfo.point);

                // With Wrapper function
                // ------------
                // MadGoatSSAA contains its own ScreenPointToRay method inside it's public api, using it instead
                // of the camera's built in one will return correct ray position relative to the screen size
                Ray ray2 = Camera.main.GetComponent<MadGoatSSAA>().ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray2, out hitInfo, 1000))
                    Debug.Log("Correct Hit Info: " + hitInfo.point);
            }
        }
    }
}