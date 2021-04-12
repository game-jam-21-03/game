using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flame : MonoBehaviour
{
	void Update() 
	{
		transform.forward = Camera.main.transform.forward * -1;
	}
}
