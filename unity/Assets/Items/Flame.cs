using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flame : MonoBehaviour
{
	[SerializeField] GameObject mainCam;

	void Update() 
	{
		transform.forward = mainCam.transform.forward * -1;
	}
}
