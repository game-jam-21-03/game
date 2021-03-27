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

	// TODO: Finish implementing, forward to material
	//public float width = 1;
	//public float sharpness = 25;
	//public Color leadingEdgeColor = ColorExtensions.FromHex(0xFF17FFF9);
	//public Color middleColor = ColorExtensions.FromHex(0xFF0C8489);
	//public Color trailingEdgeColor = ColorExtensions.FromHex(0xFFEA3DFF);
	//public Color horizontalBarColor = ColorExtensions.FromHex(0xFF808080);
}
