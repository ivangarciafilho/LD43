using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Radar : MonoBehaviour
{
    public Transform temple;
    public float minimumTriggeringDistance = 6f;
    public ParticleSystem[] radarVfx;
    private ParticleSystem.MainModule[] radarVfxMainModule;


    private void Awake()
    {
        var amountOfParticleSystems = radarVfx.Length;
        radarVfxMainModule = new ParticleSystem.MainModule[amountOfParticleSystems];

        for (int i = 0; i < amountOfParticleSystems; i++)
        {
            radarVfxMainModule[i] = radarVfx[i].main;
        }
    }

    // Update is called once per frame
    private void Update()
    {
        transform.rotation = Quaternion.LookRotation(temple.position - transform.position, Vector3.up);
        var adjustedRotation = transform.eulerAngles;
        adjustedRotation.x = 0f;
        adjustedRotation.z = 0f;
        transform.eulerAngles = adjustedRotation;

    }

    private void FixedUpdate()
    {
        if (Vector3.Distance(transform.position, temple.position) < minimumTriggeringDistance)
        {
            for (int i = 0; i < radarVfxMainModule.Length; i++)
            {
                var vfx = radarVfxMainModule[i];
                vfx.loop = false;
            }
        }
        else
        {
            for (int i = 0; i < radarVfxMainModule.Length; i++)
            {
                var vfx = radarVfxMainModule[i];
                vfx.loop = true;
                if (radarVfx[i].isPlaying == false)
                {
                    radarVfx[i].Play();
                }
            }
        }
    }
}
