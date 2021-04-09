using UnityEngine;

[CreateAssetMenu(fileName = "New Item Spec", menuName = "Design/Item Spec")]
public class ItemSpec : ScriptableObject
{
	[Header("Hover over fields with your mouse to see their description.")]

	[Tooltip("What type of item is this?")]
	public ItemType itemType;

	[Tooltip("The prefab used for the object in the world")]
	public GameObject itemPrefab;

	[Tooltip("The material used for the object in the scene")]
	public Material material;

	[Tooltip("The material used for the object when scanned")]
	public Material highlightMaterial;

	[Tooltip("The sprite used for the object in the UI")]
	public Sprite icon;
}
