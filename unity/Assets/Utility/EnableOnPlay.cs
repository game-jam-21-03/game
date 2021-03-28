using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnableOnPlay : MonoBehaviour
{
    [SerializeField] private GameObject[] ObjectsToEnableOnPlay; 

    void Start()
    {
        foreach (GameObject obj in ObjectsToEnableOnPlay)
        {
            obj.SetActive(true);
        }
    }
}
