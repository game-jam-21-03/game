using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PulseEffect : MonoBehaviour
{
	public Transform PulseOrigin;
	public Material PulseMat;
	public float PulseDistance;
	public float PulseSpeed;
	public Camera _Camera;
	public Scannable[] ScannableObjects;
	public GameObject Player;

	bool _scanning;

	void OnEnable() 
	{
		_Camera.depthTextureMode = DepthTextureMode.Depth;
	}

	void Update()
	{
		if (_scanning)
		{
			// Do cool shit here
			PulseDistance += Time.deltaTime * PulseSpeed;

			// probably not efficient to check every object? Not sure?
			foreach(var s in ScannableObjects)
			{
				// If the distance from the pulse origin is within in pulse distance, it has been scanned
				if(Vector3.Distance(PulseOrigin.position, s.transform.position) <= PulseDistance)
				{
					s.ObjectScanned();
				}
			}
		}

		// temp key to send a pulse?
		if (Input.GetKeyDown(KeyCode.P))
		{
			_scanning = true;
			PulseDistance = 0.0f;
			PulseOrigin.position = Player.transform.position;
		}

		if (Input.GetMouseButtonDown(0))
		{
			Ray ray = _Camera.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;

			if (Physics.Raycast(ray, out hit))
			{
				_scanning = true;
				PulseDistance = 0;
				PulseOrigin.position = hit.point;
			}
		}
	}

	[ImageEffectOpaque]
	void OnRenderImage(RenderTexture src, RenderTexture dst)
	{
		PulseMat.SetVector("_WorldSpacePulsePos", PulseOrigin.position);
		PulseMat.SetFloat("_PulseDistance", PulseDistance);
		RaycastCornerBlit(src, dst, PulseMat);
	}

	private void RaycastCornerBlit(RenderTexture src, RenderTexture dest, Material pulseMat)
	{
		// Compute Frustum Corners
		float camFar = _Camera.farClipPlane;
		float camFov = _Camera.fieldOfView;
		float camAspect = _Camera.aspect;

		float fovWHalf = camFov * 0.5f;

		Vector3 toRight = _Camera.transform.right * Mathf.Tan(fovWHalf * Mathf.Deg2Rad) * camAspect;
		Vector3 toTop = _Camera.transform.up * Mathf.Tan(fovWHalf * Mathf.Deg2Rad);

		Vector3 topLeft = (_Camera.transform.forward - toRight + toTop);
		float camScale = topLeft.magnitude * camFar;

		topLeft.Normalize();
		topLeft *= camScale;

		Vector3 topRight = (_Camera.transform.forward + toRight + toTop);
		topRight.Normalize();
		topRight *= camScale;

		Vector3 bottomRight = (_Camera.transform.forward + toRight - toTop);
		bottomRight.Normalize();
		bottomRight *= camScale;

		Vector3 bottomLeft = (_Camera.transform.forward - toRight - toTop);
		bottomLeft.Normalize();
		bottomLeft *= camScale;

		// Custom Blit, encoding Frustum Corners as additional Texture Coordinates
		RenderTexture.active = dest;

		pulseMat.SetTexture("_MainTex", src);

		GL.PushMatrix();
		GL.LoadOrtho();

		pulseMat.SetPass(0);

		GL.Begin(GL.QUADS);

		GL.MultiTexCoord2(0, 0.0f, 0.0f);
		GL.MultiTexCoord(1, bottomLeft);
		GL.Vertex3(0.0f, 0.0f, 0.0f);

		GL.MultiTexCoord2(0, 1.0f, 0.0f);
		GL.MultiTexCoord(1, bottomRight);
		GL.Vertex3(1.0f, 0.0f, 0.0f);

		GL.MultiTexCoord2(0, 1.0f, 1.0f);
		GL.MultiTexCoord(1, topRight);
		GL.Vertex3(1.0f, 1.0f, 0.0f);

		GL.MultiTexCoord2(0, 0.0f, 1.0f);
		GL.MultiTexCoord(1, topLeft);
		GL.Vertex3(0.0f, 1.0f, 0.0f);

		GL.End();
		GL.PopMatrix();
	}
}
