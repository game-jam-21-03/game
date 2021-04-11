using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
	[SerializeField] public ItemSpec item;
	[SerializeField] public Animator doorAnim;
	[SerializeField] public BoxCollider doorCollider;
}
