using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scannable : MonoBehaviour
{
	Material mat;

	void Start() 
	{
		mat = GetComponent<Renderer>().material;
	}

	public void ObjectScanned()
	{
		mat.SetInt("_HighlightOn", 1);
	}
}
