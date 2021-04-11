using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chest : MonoBehaviour
{
	[SerializeField] public GameObject item;
	[SerializeField] public bool locked = true;
	[SerializeField] public Animator chestAnim;
	[SerializeField] public BoxCollider chestCollider;
}
