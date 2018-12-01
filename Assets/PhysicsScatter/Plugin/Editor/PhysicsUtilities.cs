using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class PhysicsUtilities : MonoBehaviour {

    public static List<Rigidbody> RigidBodies;
    public static List<GameObject> GameObjectsWithRigidBodies;
    public static bool physicActive=false;
    public static bool physicsScatter = true; 

    public static void  AddExplosion(Vector3 position, Vector3 normal)
    {
        float radius = ExplosionParams.radius;
        float power = ExplosionParams.power;
        Vector3 positionUp = position;
        positionUp.y = position.y + ExplosionParams.verticalOffset;

        Collider[] colliders = Physics.OverlapSphere(positionUp, radius);
        //GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //sphere.transform.localScale = new Vector3(radius, radius, radius);
        //sphere.transform.position = position;
        foreach (Collider hit in colliders)
        {
            Rigidbody rb = hit.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.AddExplosionForce(power, position, radius, 4.0F);
            }
        }
    }

    public static void AddSimpleForce(Vector3 position, Vector3 normal)
    {
        float radius = SimpleForceParams.radius;
        Vector3 positionUp = position;
        positionUp.y = position.y + SimpleForceParams.verticalOffset;

        Collider[] colliders = Physics.OverlapSphere(positionUp, radius);
        //GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //sphere.transform.localScale = new Vector3(radius, radius, radius);
        //sphere.transform.position = position;
        foreach (Collider hit in colliders)
        {
            Rigidbody rb = hit.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.AddForce (new Vector3(SimpleForceParams.powerX, SimpleForceParams.powerY, SimpleForceParams.powerZ));
            }
        }
    }

    public static void AddBlackHole(Vector3 position, Vector3 normal)
    {
        float radius = BlackHoleParams.radius;
        float power = BlackHoleParams.power;
        Vector3 positionUp = position;
        positionUp.y = position.y + BlackHoleParams.verticalOffset;

        Collider[] colliders = Physics.OverlapSphere(positionUp, radius);

        foreach (Collider hit in colliders)
        {
            Rigidbody rb = hit.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.AddForce(Mathf.Pow(power,BlackHoleParams.modifier)*(positionUp - rb.transform.position)/(Mathf.Pow(Vector3.Distance(positionUp, rb.transform.position), BlackHoleParams.modifier)));
               
            }
        }

    }
    

    public static void PasteRigidbodies()
    {
        int i = 0;
        foreach (GameObject obj in GameObjectsWithRigidBodies)
        {
            if (obj.GetComponent<Rigidbody>() == null)
            {
                GUIutilities.CopyComponent(RigidBodies[i], obj);

            }
            i++;
        }
 
    }

    public static void DeleteRigidBodiesNotInParent(GameObject rootContainer)
    {
        foreach (GameObject gameObject in GameObjectsWithRigidBodies)
        {
            //search in parents of parents etc 
            if (!objectHasParent(gameObject, rootContainer))
            {
                DestroyImmediate(gameObject.GetComponent<Rigidbody>());
            }

        }

    }

    public static bool objectHasParent(GameObject obj, GameObject  parent)
    {
        if (obj.transform.parent == null)
        {
            return false;
        }
        if (obj.transform.parent.gameObject == parent)
        {
            return true;
        }
        bool value = objectHasParent(obj.transform.parent.gameObject, parent);
        return value;
    }

    public static void CopyAllRigidbodies()
    {
        var rigidbodyContainer = returnOrCreateObjectWithName("Rigidbody Container");
        var tmp = GameObject.FindObjectsOfType<Rigidbody>().ToList();
        RigidBodies = new List<Rigidbody>(tmp.Count);
        GameObjectsWithRigidBodies = tmp.Select(x => x.gameObject).ToList();

        int i = 0;
        foreach (Component rigidbody in tmp)
        {
            GameObject newGOtmp = new GameObject();
            RigidBodies.Add((Rigidbody)GUIutilities.CopyComponent(rigidbody, newGOtmp));
            newGOtmp.transform.parent = rigidbodyContainer.transform;
            newGOtmp.SetActive(false);
            i++;
        }
    }

    public static GameObject returnOrCreateObjectWithName(string name)
    {
        GameObject obj = GameObject.Find(name);
        if (obj == null)
        {
            obj = new GameObject(name);
        }
        return obj;
    }

    public static void ActivatePhysics()
    {
        Physics.autoSimulation = false;
        physicActive = true;
    }

}
