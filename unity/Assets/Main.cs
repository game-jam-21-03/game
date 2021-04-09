using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public struct GameState
{
	public PlayerState player;
	public List<Pulse> pulses;
	public HashSet<Chest> chestsOpened;
	public List<ItemSpec> items;
}

public struct PlayerState
{
	public Vector3 velocity;
	public Vector3 position;

	public Vector2 yawPitch;
	public Quaternion forward;

	public float timeOfLastFootstep;
	public bool leftFoot;

	public Vector3 cameraPosition;
	public Quaternion cameraForward;
}

public class Pulse
{
	public PulseSpec spec;
	public Material material;
	public Vector3 origin;
	public float distanceTraveled;
}

public static class ColorExtensions
{
	public static Color FromHex(UInt32 hex)
	{
		Color32 color = new Color32();
		color.a = (byte) ((hex >> 24) & 0xFF);
		color.r = (byte) ((hex >> 16) & 0xFF);
		color.g = (byte) ((hex >>  8) & 0xFF);
		color.b = (byte) ((hex >>  0) & 0xFF);
		return color;
	}
}

public enum ItemType {
	LevelKey, Boots, Boltcutters
}

public class Main : MonoBehaviour
{
	// Inspector configuration
	[SerializeField] Vector3 cameraOffset = new Vector3(0, 2, 0); // TODO: Maybe move to a spec?
	[SerializeField] MoveSpec playerMoveSpec;
	[SerializeField] PulseSpec abilityPulseSpec;
	[SerializeField] PulseSpec footstepPulseSpec;
	[SerializeField] bool showFootsteps = false;
	[SerializeField] float timeBetweenFootsteps = 0.67f;
	[SerializeField] float interactionDistance = 10.0f;

	// Inspector references
	[SerializeField] PulseEffect pulseEffect;
	[SerializeField] Camera playerCamera;
	[SerializeField] CharacterController playerController;
	[SerializeField] Transform playerTransform;
	[SerializeField] Scannable[] scannableObjects;
	[SerializeField] Chest[] chestRefs;

	// UI
	[Header("UI")]
	[SerializeField] TextMeshProUGUI chestInfo;
	[SerializeField] TextMeshProUGUI doorInfo;
	[SerializeField] TextMeshProUGUI leverInfo;
	[SerializeField] TextMeshProUGUI keyInfo;
	[SerializeField] Image[] itemImages;
	[SerializeField] GUIItem[] itemImagesItemRefs;

	// Audio
	[Header("Audio")]
	[SerializeField] AudioClip[] musicList;
	[SerializeField] bool RepeatSong = true;
	[SerializeField] AudioSource musicAudioSource;
	[SerializeField, HideInInspector] short clipIndex = 1;

	// Internal state
	[SerializeField, HideInInspector] GameState state;
	[SerializeField, HideInInspector] InputActions inputActions;
	[SerializeField, HideInInspector] Transform cameraTransform;
	[SerializeField, HideInInspector] Dictionary<Material, Material> materials;

	void Awake()
	{
		inputActions = new InputActions();
		inputActions.Gameplay.Enable();

		cameraTransform = playerCamera.transform;
		playerCamera.depthTextureMode = DepthTextureMode.Depth;

		state.pulses = new List<Pulse>();
		pulseEffect.pulses = state.pulses;

		materials = new Dictionary<Material, Material>(12);
		materials[abilityPulseSpec.material] = new Material(abilityPulseSpec.material);
		materials[footstepPulseSpec.material] = new Material(footstepPulseSpec.material);

		// Music
		{
			if (musicList.Length > 0)
			{
				musicAudioSource.clip = musicList[0];
			}
			musicAudioSource.playOnAwake = true;
			musicAudioSource.loop = RepeatSong;
			musicAudioSource.Play();
		}

		state.chestsOpened = new HashSet<Chest>();
		state.items = new List<ItemSpec>();
	}

	void Update()
	{
		if (!musicAudioSource.isPlaying)
		{
			if (musicList.Length > clipIndex)
			{
				musicAudioSource.clip = musicList[clipIndex];
				musicAudioSource.Play();
				clipIndex++;
			}
			else
			{
				musicAudioSource.clip = musicList[0];
				musicAudioSource.Play();
				clipIndex = 1;
			}
		}
	}

	void FixedUpdate()
	{
		float t = Time.fixedTime;
		float dt = Time.fixedDeltaTime;
		InputSystem.Update();

		// Look Direction
		{
			// Player
			Vector2 look = inputActions.Gameplay.Look.ReadValue<Vector2>();

			ref Vector2 yawPitch = ref state.player.yawPitch;
			ref Quaternion forward = ref state.player.forward;
			ref float yaw = ref state.player.yawPitch.x;
			ref float pitch = ref state.player.yawPitch.y;
			float pitchLimit = 0.45f * Mathf.PI;
			float lookScale = playerMoveSpec.cameraTurnSpeed;

			Quaternion prevForward = forward;
			yawPitch += lookScale * look;
			pitch = Mathf.Clamp(pitch, -pitchLimit, +pitchLimit);

			forward = Quaternion.Euler(0, yaw * Mathf.Rad2Deg, 0);
			ref Vector3 v = ref state.player.velocity;
			Quaternion velocityFix = forward * Quaternion.Inverse(prevForward);
			v = velocityFix * v;
			playerTransform.rotation = forward;

			// Camera
			ref Quaternion cameraForward = ref state.player.cameraForward;
			cameraForward = Quaternion.Euler(pitch * Mathf.Rad2Deg, yaw * Mathf.Rad2Deg, 0);
			cameraTransform.rotation = cameraForward;
		}

		// Movement
		{
			// Player
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
			Vector3 dp = (0.5f * a * dt * dt) + (v * dt);
			p += dp;
			if (aInput == Vector3.zero)
			{
				v *= (1 - vDrag);
				if (v.magnitude < vMin)
					v = Vector3.zero;
			}
			v += a * dt;
			v = Vector3.ClampMagnitude(v, vMax);

			playerController.Move(dp);
			p = playerController.transform.position;

			// Camera
			ref Vector3 cameraPosition = ref state.player.cameraPosition;
			cameraPosition = p + cameraOffset;
			cameraTransform.position = cameraPosition;
		}

		// Interactions (Raycasts)
		{
			ref Vector3 cp = ref state.player.cameraPosition;
			Vector3 cf = state.player.cameraForward * Vector3.forward;

			RaycastHit hit;
			Debug.DrawRay(cp, cf * interactionDistance, Color.red);

			if (Physics.Raycast(cp, cf, out hit,interactionDistance, 1 << 6))
			{
				Chest chest = hit.transform.gameObject.GetComponent<Chest>();
				if (chest)
				{
					if (!state.chestsOpened.Contains(chest))
					{
						chestInfo.gameObject.SetActive(true);
						if (inputActions.Gameplay.Interact.triggered && chest.locked)
						{
							bool haveKey = false;
							foreach (var item in state.items)
							{
								if (item.itemType == ItemType.LevelKey)
								{
									// have key to open chest
									haveKey = true;
								}
							}

							if(haveKey)
								{
									chest.locked = false;
									state.chestsOpened.Add(chest);
									// add boltcutters to inventory
									state.items.Add(chest.item);
									for(int i = 0; i < itemImages.Length; i++)
									{
										if (!itemImages[i].IsActive())
										{
											// item not in use
											itemImages[i].sprite = chest.item.icon;
											itemImages[i].gameObject.SetActive(true);
											chestInfo.gameObject.SetActive(false);
											itemImagesItemRefs[i].itemRef = chest.item;
											break;
										}
									}

									for(int i = 0; i < itemImages.Length; i++)
									{
										if (itemImages[i].IsActive() && itemImagesItemRefs[i].itemRef.itemType == ItemType.LevelKey)
										{
											// remove key
											itemImages[i].gameObject.SetActive(false);
											break;
										}
									}

									foreach (var item in state.items)
									{
										if (item.itemType == ItemType.LevelKey)
										{
											// have key to open chest
											state.items.Remove(item);
											break;
										}
									}
								}
						}
					}
				}

				Door door = hit.transform.gameObject.GetComponent<Door>();
				if (door)
				{
					doorInfo.gameObject.SetActive(true);
					if (inputActions.Gameplay.Interact.triggered)
					{
						foreach (var item in state.items)
						{
							if (item.itemType == door.item.itemType)
							{
								// we have matching item
								state.items.Remove(door.item);
								for(int i = 0; i < itemImages.Length; i++)
								{
									if (itemImages[i].IsActive() && itemImages[i].sprite == door.item.icon)
									{
										itemImages[i].gameObject.SetActive(false);
										break;
									}
								}
								hit.transform.gameObject.SetActive(false);
								doorInfo.gameObject.SetActive(false);
								break;
							}
						}
					}
				}

				Lever lever = hit.transform.gameObject.GetComponent<Lever>();
				if(lever)
				{
					leverInfo.gameObject.SetActive(true);
					if (inputActions.Gameplay.Interact.triggered && !lever.triggered)
					{
						//  do an animation / sound for grate being opened?
						Instantiate(lever.grateRef.Item.itemPrefab,lever.grateRef.transform.position, Quaternion.identity);
						lever.grateRef.gameObject.SetActive(false);
						lever.triggered = true;
					}
				}

				Key key = hit.transform.gameObject.GetComponent<Key>();
				if(key)
				{
					keyInfo.gameObject.SetActive(true);
					if (inputActions.Gameplay.Interact.triggered)
					{
						//  sound for key obtained
						state.items.Add(key.item);
						for(int i = 0; i < itemImages.Length; i++)
						{
							if (!itemImages[i].IsActive())
							{
								// item not in use
								itemImages[i].sprite = key.item.icon;
								itemImages[i].gameObject.SetActive(true);
								keyInfo.gameObject.SetActive(false);
								itemImagesItemRefs[i].itemRef = key.item;
								break;
							}
						}
						Destroy(key.gameObject);
					}
				}
			}
			else
			{
				chestInfo.gameObject.SetActive(false);
				doorInfo.gameObject.SetActive(false);
				leverInfo.gameObject.SetActive(false);
				keyInfo.gameObject.SetActive(false);
			}

		}

		// Footsteps
		{
			if (showFootsteps)
			{
				if (state.player.velocity != Vector3.zero)
				{
					float timeSinceLastFootStep = t - state.player.timeOfLastFootstep;
					if (timeSinceLastFootStep >= timeBetweenFootsteps)
					{
						state.player.timeOfLastFootstep = t + timeBetweenFootsteps;

						float leftFootMul = state.player.leftFoot ? 1 : -1f;
						state.player.leftFoot = !state.player.leftFoot;
						Vector3 footOffset = (leftFootMul * 0.25f * playerTransform.right) + (0.5f * playerTransform.forward);
						Vector3 footPosition = state.player.position + footOffset;
						SendPulse(footstepPulseSpec, footPosition);
					}
				}
			}
		}

		// Pulses
		{
			if (inputActions.Gameplay.EchoPulse.triggered)
				SendPulse(abilityPulseSpec, state.player.position);

			for (int i = state.pulses.Count - 1; i >= 0; i--)
			{
				Pulse p = state.pulses[i];

				// Update
				float prevDist = p.distanceTraveled;
				p.distanceTraveled += dt * p.spec.travelSpeed;
				p.distanceTraveled = Mathf.Min(p.distanceTraveled, p.spec.maximumTravelDistance);

				float currDist = p.distanceTraveled;
				foreach (var s in scannableObjects)
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
		state.pulses.Add(new Pulse {
			spec = spec,
			material = materials[spec.material],
			origin = origin,
			distanceTraveled = 0.0f,
		});
	}
}
