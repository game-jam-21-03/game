using UnityEngine;

[CreateAssetMenu(fileName = "New Outline Spec", menuName = "Design/Outline Spec")]
public class OutlineSpec : ScriptableObject
{

	[Header("Hover over fields with your mouse to see their description.")]

	[Tooltip("How long should the highligh effect take in seconds?")]
	public float highlightDuration = 1.5f;

	[Tooltip("How long should the highligh remain on the object in seconds?")]
	public float highlightLingerDuration = 2f;

	[Tooltip("How long should the highligh falloff take in seconds?")]
	public float dehighlightDuration = 3f;
}
