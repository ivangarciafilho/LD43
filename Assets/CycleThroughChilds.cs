using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CycleThroughChilds : MonoBehaviour
{
    private int currentChild;
    public Transform player;
    public float minimumTriggeringDistance = 0.2f;
    private Vector3 previousPlayerPosition;


    private void OnEnable() { previousPlayerPosition = player.position; }
    private void LateUpdate() { EvaluateDistance(); }

    private void EvaluateDistance()
    {
        if (player.hasChanged == false) return;
        if (Vector3.Distance(player.position, previousPlayerPosition) < minimumTriggeringDistance) return;

        previousPlayerPosition = player.position;
        ActivateNextChild();
    }

    public void ActivateNextChild()
    {
        if (currentChild >= transform.childCount) currentChild = 0;

        var child = transform.GetChild(currentChild);

        child.gameObject.SetActive(false);
        child.position = previousPlayerPosition;
        child.gameObject.SetActive(true);

        currentChild++;
    }
}
