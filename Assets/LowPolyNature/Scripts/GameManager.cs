using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {

    public static GameManager Instance = null;                         

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

 
    }

    public PlayerController Player;
    
}
