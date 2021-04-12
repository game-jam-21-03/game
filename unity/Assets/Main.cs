using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public struct GameState
{
	public float prevT;
	public PlayerState player;
	public List<Pulse> pulses;
	public HashSet<Chest> chestsOpened;
	public List<GameObject> trapsDisabled;
	public List<ItemSpec> items;
	public Vector3 checkpoint;
}

public struct PlayerState
{
	public Vector3 velocity;
	public Vector3 position;

	public Vector2 yawPitch;
	public Quaternion forward;

	public Vector3 cameraPosition;
	public Quaternion cameraForward;

	public float timeOfLastFootstep;
	public bool leftFoot;
}

public class Pulse
{
	public PulseSpec spec;
	public Material material;
	public Vector3 startPosition;
	public float startTime;
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

public enum ItemType
{
	LevelKey,
	Boots,
	Boltcutters,
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
	[SerializeField] public float gravityModifier = 1f;

	// Inspector references
	[SerializeField] Camera playerCamera;
	[SerializeField] CharacterController playerController;
	[SerializeField, HideInInspector] Scannable[] scannableObjects;
	[SerializeField] Chest[] chestRefs;

	// UI
	[Header("UI")]
	[SerializeField] TextMeshProUGUI interactionInfoPanel;
	[SerializeField] Image[] itemImages;
	[SerializeField] GUIItem[] itemImagesItemRefs;

	// Audio
	[Header("Audio")]
	[SerializeField] AudioClip[] musicList;
	[SerializeField] bool repeatSong = true;
	[SerializeField] AudioSource musicAudioSource;
	[SerializeField] AudioSource pulseSoundFX;
	[SerializeField] AudioClip disableTrap;
	[SerializeField] AudioClip pullLever;
	[SerializeField] AudioClip openDoor;
	[SerializeField] AudioClip openHatch;
	[SerializeField] AudioClip openChest;
	[SerializeField] AudioClip pickUpBoots;
	[SerializeField] AudioClip pickUpBoltCutter;
	[SerializeField] AudioClip pickUpKey;
	[SerializeField] AudioClip triggerTrap;
	[SerializeField] float sfxVolume;

	[SerializeField, HideInInspector] short clipIndex = 1;

	// Internal state
	[SerializeField, HideInInspector] GameState state;
	[SerializeField, HideInInspector] InputActions inputActions;
	[SerializeField, HideInInspector] Transform cameraTransform;
	[SerializeField, HideInInspector] Transform playerTransform;

	public static List<Pulse> pulses;

	void Awake()
	{
		Cursor.visible = false;
		inputActions = new InputActions();
		inputActions.Gameplay.Enable();

		playerTransform = playerController.transform;
		cameraTransform = playerCamera.transform;
		playerCamera.depthTextureMode = DepthTextureMode.Depth;

		state.pulses = new List<Pulse>();
		state.trapsDisabled = new List<GameObject>();
		pulses = state.pulses;

		InitializePulseSpec(abilityPulseSpec);
		InitializePulseSpec(footstepPulseSpec);

		// Music
		{
			if (musicList.Length > 0)
			{
				musicAudioSource.clip = musicList[0];
			}
			musicAudioSource.playOnAwake = true;
			musicAudioSource.loop = repeatSong;
			musicAudioSource.Play();
		}

		state.chestsOpened = new HashSet<Chest>();
		state.items = new List<ItemSpec>();

	}

	void Start() 
	{
		scannableObjects = FindObjectsOfType<Scannable>();
	}

	void Update()
	{
		float t = Time.time;
		float dt = Time.deltaTime;

		// Audio
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

		// Pulse Rendering
		{
			for (int i = 0; i < state.pulses.Count; i++)
			{
				Pulse p = state.pulses[i];

				float pulseDuration = t - p.startTime;
				// TODO: Rendering!
				//UniversalRenderPipeline.RenderSingleCamera
				//Graphics.
				//RenderPipelineManager.beginCameraRendering

				// TODO: Scannable audio can be hooked up here
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

			state.player.velocity += gravityModifier * Physics.gravity * dt;
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
						interactionInfoPanel.text = "I wonder if I have a key for this..";
						interactionInfoPanel.gameObject.SetActive(true);
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

							if (haveKey)
							{
								chest.chestAnim.SetTrigger("Opening");
								AudioSource.PlayClipAtPoint(openChest, chest.transform.position, sfxVolume);
								chest.locked = false;
								state.chestsOpened.Add(chest);

								chest.item.gameObject.GetComponent<BoxCollider>().enabled = true;
								chest.chestCollider.enabled = false;

								for (int i = 0; i < itemImages.Length; i++)
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

				Boltcutter boltcutters = hit.transform.gameObject.GetComponent<Boltcutter>();
				if (boltcutters)
				{
					interactionInfoPanel.text = "I can probably cut a chain with these..";
					interactionInfoPanel.gameObject.SetActive(true);
					if (inputActions.Gameplay.Interact.triggered)
					{
						// add boltcutters to inventory
						state.items.Add(boltcutters.item);
						for (int i = 0; i < itemImages.Length; i++)
						{
							if (!itemImages[i].IsActive())
							{
								// item not in use
								itemImages[i].sprite = boltcutters.item.icon;
								itemImages[i].gameObject.SetActive(true);
								interactionInfoPanel.gameObject.SetActive(false);
								itemImagesItemRefs[i].itemRef = boltcutters.item;
								break;
							}
						}
						//Destroy(boltcutters.gameObject);
						boltcutters.gameObject.SetActive(false);
					}
				}

				Door door = hit.transform.gameObject.GetComponent<Door>();
				if (door)
				{
					interactionInfoPanel.text = "I might need some bolt cutters to get through this door..";
					interactionInfoPanel.gameObject.SetActive(true);
					if (inputActions.Gameplay.Interact.triggered)
					{
						foreach (var item in state.items)
						{
							if (item.itemType == door.item.itemType)
							{
								door.doorAnim.SetTrigger("Opening");
								AudioSource.PlayClipAtPoint(openDoor, door.transform.position, sfxVolume);
								state.items.Remove(door.item);
								for (int i = 0; i < itemImages.Length; i++)
								{
									if (itemImages[i].IsActive() && itemImages[i].sprite == door.item.icon)
									{
										itemImages[i].gameObject.SetActive(false);
										break;
									}
								}
								//hit.transform.gameObject.SetActive(false);
								door.doorCollider.enabled = false;
								interactionInfoPanel.gameObject.SetActive(false);
								break;
							}
						}
					}
				}

				Lever lever = hit.transform.gameObject.GetComponent<Lever>();
				if (lever)
				{
					interactionInfoPanel.text = "I wonder if this lever might open something...";
					interactionInfoPanel.gameObject.SetActive(true);
					if (inputActions.Gameplay.Interact.triggered && !lever.triggered)
					{
						lever.leverAnim.SetTrigger("Opening");
						AudioSource.PlayClipAtPoint(pullLever, lever.transform.position, sfxVolume);
						var keyRef = lever.grateRef.itemLockedRef.GetComponent<Key>();
						if (keyRef)
							keyRef.item.itemEnabled = true;

						var bootRef = lever.grateRef.itemLockedRef.GetComponent<Boots>();
						if (bootRef)
							bootRef.item.itemEnabled = true;

						var rb = lever.grateRef.GetComponent<Rigidbody>();
						rb.useGravity = true;
						rb.AddForce(new Vector3(.0f,.0f, 500.0f));
						lever.grateRef.showMessage = false;
						lever.triggered = true;
					}
				}

				Key key = hit.transform.gameObject.GetComponent<Key>();
				if (key)
				{
					if (key.item.itemEnabled)
					{
						interactionInfoPanel.text = "I wonder if this will open a chest..";
						interactionInfoPanel.gameObject.SetActive(true);
						if (inputActions.Gameplay.Interact.triggered)
						{
							// sound for key obtained
							AudioSource.PlayClipAtPoint(pickUpKey, key.transform.position, sfxVolume);
							state.items.Add(key.item);
							for (int i = 0; i < itemImages.Length; i++)
							{
								if (!itemImages[i].IsActive())
								{
									// item not in use
									itemImages[i].sprite = key.item.icon;
									itemImages[i].gameObject.SetActive(true);
									interactionInfoPanel.gameObject.SetActive(false);
									itemImagesItemRefs[i].itemRef = key.item;
									break;
								}
							}
							// Destroy(key.gameObject);
							key.gameObject.SetActive(false);
						}
					}
				}

				Grate grate = hit.transform.gameObject.GetComponent<Grate>();
				if (grate)
				{
					if (grate.showMessage)
					{
						interactionInfoPanel.text = "It won't budge. I wonder if I can disable it somehow..";
						interactionInfoPanel.gameObject.SetActive(true);
					}
				}

				Boots boots = hit.transform.gameObject.GetComponent<Boots>();
				if (boots)
				{
					if (boots.item.itemEnabled)
					{
						interactionInfoPanel.text = "I might need these later..";
						interactionInfoPanel.gameObject.SetActive(true);
						if (inputActions.Gameplay.Interact.triggered)
						{
							AudioSource.PlayClipAtPoint(pickUpBoots, boots.transform.position, sfxVolume);
							state.items.Add(boots.item);
							for (int i = 0; i < itemImages.Length; i++)
							{
								if (!itemImages[i].IsActive())
								{
									// item not in use
									itemImages[i].sprite = boots.item.icon;
									itemImages[i].gameObject.SetActive(true);
									interactionInfoPanel.gameObject.SetActive(false);
									itemImagesItemRefs[i].itemRef = boots.item;
									break;
								}
							}
							//Destroy(boots.gameObject);
							boots.gameObject.SetActive(false);
						}
					}
				}
			}
			else
			{
				interactionInfoPanel.gameObject.SetActive(false);
			}
		}

		// Downward Raycast
		{
			//Vector3 downVector = Vector3.up * -1;
			ref Vector3 p = ref state.player.position;
			Debug.DrawRay(p, Vector3.up * -1 * 2.0f, Color.green);
			RaycastHit hit;

			if (Physics.Raycast(p, Vector3.up * -1, out hit, 2.0f, 1 << 6))
			{
				// Traps
				Trap trap = hit.transform.gameObject.GetComponent<Trap>();
				if (trap && state.items.Contains(trap.itemSpecToDisableTrap))
				{
					AudioSource.PlayClipAtPoint(disableTrap, trap.transform.position, sfxVolume);
					state.trapsDisabled.Add(trap.gameObject);
					trap.trapEnabled = false;
					state.items.Remove(trap.itemSpecToDisableTrap);
					for (int i = 0; i < itemImages.Length; i++)
					{
						if (itemImages[i].IsActive() && itemImages[i].sprite == trap.itemSpecToDisableTrap.icon)
						{
							itemImages[i].gameObject.SetActive(false);
							break;
						}
					}
				}
				else if (trap)
				{
					if(trap.trapEnabled)
					{
						// don't have boots
						AudioSource.PlayClipAtPoint(triggerTrap, trap.transform.position, sfxVolume);
						playerTransform.position = state.checkpoint;
						p = state.checkpoint;
						for (int i = 0; i < state.trapsDisabled.Count; i++)
						{
							state.trapsDisabled[i].SetActive(true);
						}
						state.trapsDisabled.Clear();
					}
				}

				// Checkpoint
				if (hit.transform.gameObject.name == "Checkpoint")
				{
					state.checkpoint = hit.transform.position;
				}
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
						SendPulse(footstepPulseSpec, footPosition, t);
					}
				}
			}
		}

		// Pulse Behavior
		{
			if (inputActions.Gameplay.EchoPulse.triggered)
			{
				SendPulse(abilityPulseSpec, state.player.position, t);
				pulseSoundFX.Play();
			}

			for (int iPulse = state.pulses.Count - 1; iPulse >= 0; iPulse--)
			{
				Pulse p = state.pulses[iPulse];

				// Update
				float prevPulseDuration = (t - Time.fixedDeltaTime) - p.startTime;
				float pulseDuration = t - p.startTime;
				pulseDuration = Mathf.Min(pulseDuration, p.spec.maximumTravelTime);

				for (int iScannable = 0; iScannable < scannableObjects.Length; iScannable++)
				{
					Scannable s = scannableObjects[iScannable];

					float distanceToScannable = Vector3.Distance(p.startPosition, s.transform.position);
					float durationToScannable = distanceToScannable / p.spec.travelSpeed;
					if (durationToScannable >= prevPulseDuration && durationToScannable < pulseDuration)
					s.ObjectScanned();
				}

				// Remove
				if (pulseDuration == p.spec.maximumTravelTime)
					state.pulses.RemoveAt(iPulse);
			}
		}
	}

	void InitializePulseSpec(PulseSpec spec)
	{
		spec.maximumTravelTime = spec.maximumTravelDistance / spec.travelSpeed;
	}

	void SendPulse(PulseSpec spec, Vector3 startPosition, float startTime)
	{
		Assert.AreNotEqual(spec.maximumTravelTime, 0.0f);
		state.pulses.Add(new Pulse {
			spec = spec,
			startPosition = startPosition,
			startTime = startTime,
		});
	}
}
