using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chest : MonoBehaviour
{
	[SerializeField] public GameObject item;
	[SerializeField] public bool locked = true;
	[SerializeField] public Animator chestAnim;
	[SerializeField] public BoxCollider chestCollider;

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
