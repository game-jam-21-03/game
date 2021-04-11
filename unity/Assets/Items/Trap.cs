using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trap : MonoBehaviour
{
    [SerializeField] public ItemSpec itemSpecToDisableTrap;
	[SerializeField] public Animator trapAnim;
	[SerializeField] public bool trapEnabled;
}
