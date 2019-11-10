using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchFeatures_SmallShadows : MonoBehaviour
{
    SmallShadowOptimizer smallShadows;
    public static int selection = 0;

    // Use this for initialization
    void Start()
    {
        smallShadows = this.GetComponent<SmallShadowOptimizer>();
    }

    // Update is called once per frame
    void Update()
    {
        bool buttonPressed = Input.GetButtonDown("Xbox_A");
        if (Input.GetKeyDown(KeyCode.Space) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began ) ||
            buttonPressed )
        {
            selection = (selection + 1) % 2;
        }

        //Debug.Log("Xbox_Y  = " + buttonPressed);
        smallShadows.enabled = (selection == 0);
    }

    public static string GetCurrentFeatureString()
    {
        switch (selection)
        {
            case 0: return "Rendering with SmallShadowsOptimizer"; break;
            case 1: return "Rendering without SmallShadowsOptimizer"; break;
        }

        return "error";
    }
}