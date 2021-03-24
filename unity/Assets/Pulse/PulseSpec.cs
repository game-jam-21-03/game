using UnityEngine;

[CreateAssetMenu(fileName = "New Pulse Spec", menuName = "Design/Pulse Spec")]
public class PulseSpec : ScriptableObject
{
	[Header("Hover over fields with your mouse to see their description.")]

	[Tooltip("The material used to draw the pulse")]
	public Material material;

	[Tooltip("How fast the pulse will travel through the world. Units: unspecified")]
	public float travelSpeed;

	[Tooltip("How far the pulse will travel through the world. Units: unspecified")]
	public float maximumTravelDistance;
}
