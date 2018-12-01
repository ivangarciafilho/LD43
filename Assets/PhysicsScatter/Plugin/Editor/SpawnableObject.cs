/***********************************************
                    Easy Scatter
	Copyright © 2014-2015 The Hedgehog Team
      http://www.thehedgehogteam.com/Forum/
		
	     The.Hedgehog.Team@gmail.com
		
**********************************************/
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


[System.Serializable]
public class SpawnableObject : System.ICloneable
{
    
    public bool isScale = false;
    public float minScale = 0.8f;
    public float maxScale = 1.2f;

    public bool isRotationX = false;
    public float xMinRot = -25;
    public float xMaxRot = 25;

    public bool isRotationY = false;
    public float yMinRot = -180;
    public float yMaxRot = 180;

    public bool isRotationZ = false;
    public float zMinRot = -25;
    public float zMaxRot = 25;

    public float minOffset = -10; 
    public float offset = 0;
    public float maxOffset = 10;

    public bool optionDelete = false;
    public bool showOption = false;

    public bool spawn;
    public GameObject prefab;
    public bool isPrefab = false;



    public static void ApplyRandomFactors(SpawnableObject obj, Transform objTransform)
    {
        // Random Scale
        if (obj.isScale)
        {
            float scale = UnityEngine.Random.Range(obj.minScale, obj.maxScale);
            objTransform.localScale = new Vector3(scale, scale, scale);
        }

        // Random Rotation
        if (obj.isRotationX || obj.isRotationY || obj.isRotationZ)
        {
            Vector3 rotation = objTransform.eulerAngles;
            if (obj.isRotationX)
            {
                rotation.x = UnityEngine.Random.Range(obj.xMinRot, obj.xMaxRot);
            }
            if (obj.isRotationY)
            {
                rotation.y = UnityEngine.Random.Range(obj.yMinRot, obj.yMaxRot);
            }
            if (obj.isRotationZ)
            {
                rotation.z = UnityEngine.Random.Range(obj.zMinRot, obj.zMaxRot);
            }
            objTransform.eulerAngles = rotation;
        }
    }


    public object Clone()
    {
        return MemberwiseClone();
    }

}
