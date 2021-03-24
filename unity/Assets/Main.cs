using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public struct GameState
{
	public PlayerState player;
	public List<Pulse> pulses;
}

public struct PlayerState
{
	public Vector3 velocity;
	public Vector3 position;

	public Vector2 yawPitch;
	public Quaternion forward;
}

public class Pulse
{
	public PulseSpec spec;
	public Vector3 origin;
	public float distanceTraveled;
}

public class Main : MonoBehaviour
{
	// TODO: Move camera offset and turn speed to a camera spec
	[SerializeField] Vector3 cameraOffset = new Vector3(0, 2, 0);

	// Inspector fields
	[SerializeField] new Camera camera;
	[SerializeField] Transform playerTransform;
	[SerializeField] Scannable[] ScannableObjects;
	[SerializeField] MoveSpec playerMoveSpec;
	[SerializeField] PulseSpec pulseSpec;


	// Internal state
	[SerializeField, HideInInspector] GameState state;
	[SerializeField, HideInInspector] InputActions inputActions;
	[SerializeField, HideInInspector] Transform cameraTransform;

	// HACK: Maybe remove this?
	public static List<Pulse> Pulses;

	void Awake()
	{
		inputActions = new InputActions();
		inputActions.Gameplay.Enable();

		cameraTransform = camera.transform;
		camera.depthTextureMode = DepthTextureMode.Depth;

		state.pulses = new List<Pulse>();
		Pulses = state.pulses;
	}

	void FixedUpdate()
	{
		float dt = Time.fixedDeltaTime;
		InputSystem.Update();

		// Look Direction
		{
			ref Vector2 yawPitch = ref state.player.yawPitch;
			ref Quaternion forward = ref state.player.forward;
			ref float yaw = ref state.player.yawPitch.x;
			ref float pitch = ref state.player.yawPitch.y;
			float pitchLimit = 0.45f * Mathf.PI;
			float lookScale = playerMoveSpec.cameraTurnSpeed;

			Vector2 look = inputActions.Gameplay.Look.ReadValue<Vector2>();

			Quaternion prevForward = forward;
			yawPitch += lookScale * look;
			pitch = Mathf.Clamp(pitch, -pitchLimit, +pitchLimit);

			forward = Quaternion.Euler(0, yaw * Mathf.Rad2Deg, 0);
			ref Vector3 v = ref state.player.velocity;
			Quaternion velocityFix = forward * Quaternion.Inverse(prevForward);
			v = velocityFix * v;

			playerTransform.rotation = forward;
			cameraTransform.rotation = Quaternion.Euler(pitch * Mathf.Rad2Deg, yaw * Mathf.Rad2Deg, 0);
		}

		// Movement
		{
			Vector3 move = inputActions.Gameplay.Move.ReadValue<Vector2>();
			Vector3 aInput = new Vector3(move.x, 0, move.y);

			ref Quaternion forward = ref state.player.forward;
			ref Vector3 v = ref state.player.velocity;
			ref Vector3 p = ref state.player.position;
			float aScale = playerMoveSpec.acceleration;
			float vDrag = playerMoveSpec.deceleration;
			float vMin = playerMoveSpec.minimumMoveSpeed;
			float vMax = playerMoveSpec.maximumMoveSped;

			Vector3 a = forward * (aScale * aInput);
			p += (0.5f * a * dt * dt) + (v * dt);
			if (aInput == Vector3.zero)
			{
				v *= (1 - vDrag);
				if (v.magnitude < vMin)
					v = Vector3.zero;
			}
			v += a * dt;
			v = Vector3.ClampMagnitude(v, vMax);

			playerTransform.position = p;
			cameraTransform.position = p + cameraOffset;
		}

		// Pulses
		{
			if (inputActions.Gameplay.EchoPulse.triggered)
				SendPulse(pulseSpec, state.player.position);

			for (int i = state.pulses.Count - 1; i >= 0; i--)
			{
				Pulse p = state.pulses[i];

				// Update
				float prevDist = p.distanceTraveled;
				p.distanceTraveled += dt * p.spec.travelSpeed;
				p.distanceTraveled = Mathf.Min(p.distanceTraveled, p.spec.maximumTravelDistance);

				float currDist = p.distanceTraveled;
				foreach (var s in ScannableObjects)
				{
					// If the distance from the pulse origin is within in pulse distance, it has been scanned
					float scannableDist = Vector3.Distance(p.origin, s.transform.position);
					if (scannableDist >= prevDist && scannableDist < currDist)
						s.ObjectScanned();
				}

				// Remove
				if (p.distanceTraveled >= p.spec.maximumTravelDistance)
					state.pulses.RemoveAt(i);
			}
		}
	}

	void SendPulse(PulseSpec spec, Vector3 origin)
	{
		var e = new Pulse {
			spec = spec,
			origin = origin,
			distanceTraveled = 0.0f,
		};
		state.pulses.Add(e);
	}
}
