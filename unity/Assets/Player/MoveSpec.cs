using UnityEngine;

[CreateAssetMenu(fileName = "New Move Spec", menuName = "Design/Move Spec")]
public class MoveSpec : ScriptableObject
{
	[Header("Hover over fields with your mouse to see their description.")]

	[Tooltip("How fast the player accelerates when holding WASD. Units: m/s^2")]
	public float acceleration = 50.0f;

	[Tooltip("How fast the player decelerates when not holding WASD. Units: m/s^2")]
	public float deceleration = 0.15f;

	[Tooltip("Once the player speed drops below this value they will come to a complete stop. Units: m/s")]
	public float minimumMoveSpeed = 0.5f;

	[Tooltip("The run speed of the character. Units: m/s")]
	public float maximumMoveSped = 10f;

	[Tooltip("How fast the camera turns when you move the mouse. Units: unspecified.")]
	public float cameraTurnSpeed = 0.005f;
}
