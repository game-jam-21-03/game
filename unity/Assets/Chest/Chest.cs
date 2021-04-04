using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum KeyType
{
	Key1, Key2, Key3, Key4
}

public class Chest : MonoBehaviour
{
    [SerializeField] public KeyType key;
}
