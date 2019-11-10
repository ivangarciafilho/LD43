using UnityEngine;
using System.Collections;
using UnityEngine.UI;
public class FPS_SS : MonoBehaviour {

    Text TextC;
	// Use this for initialization
	void Start ()
    {
        TextC = this.gameObject.GetComponent<Text>();
        Application.targetFrameRate = 200;
	}
    int Frames = 0;
    float TimePassed = 0;
	// Update is called once per frame
	void Update () {
        TimePassed += Time.deltaTime;
        if (TimePassed > 1.0f)
        {
            //ParticleSystem[] ps = Object.FindObjectsOfType<ParticleSystem>();

            string batches = "";
            #if UNITY_EDITOR
                batches = " batches = " + UnityEditor.UnityStats.batches;
            #endif

            TextC.text = "FPS " + Frames + " " + Screen.width + " x " + Screen.height + " " + SystemInfo.graphicsDeviceType + batches + "\n"
                + SwitchFeatures_SmallShadows.GetCurrentFeatureString() + "\n(Press space to toggle)";

            TimePassed = 0;
            Frames = 0;            
        }
        Frames++;	
	}
}
