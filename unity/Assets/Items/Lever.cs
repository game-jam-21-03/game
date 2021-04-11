using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lever : MonoBehaviour
{
	[SerializeField] public Grate grateRef;
	[SerializeField] public bool triggered;
	[SerializeField] public Animator leverAnim;

	[SerializeField] public MeshRenderer[] meshes;
	[SerializeField] public Material baseMaterial;

	void Awake() 
	{
		for (int i = 0; i < meshes.Length; i++)
		{
			meshes[i].material = new Material(baseMaterial);
		}
	}
}
