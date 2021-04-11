using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trap : MonoBehaviour
{
    [SerializeField] public ItemSpec itemSpecToDisableTrap;
	[SerializeField] public Animator trapAnim;
	[SerializeField] public bool trapEnabled;

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
