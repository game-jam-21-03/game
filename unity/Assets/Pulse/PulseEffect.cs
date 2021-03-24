using System.Collections.Generic;
using UnityEngine;

public class PulseEffect : MonoBehaviour
{
	public new Camera camera;

	[ImageEffectOpaque]
	void OnRenderImage(RenderTexture src, RenderTexture dst)
	{
		RaycastCornerBlit(src, dst, Main.Pulses);
	}

	void RaycastCornerBlit(RenderTexture src, RenderTexture dest, List<Pulse> pulses)
	{
		bool swapped = false;
		foreach(var pulse in pulses)
		{
			pulse.EchoPulseMat.SetVector("_WorldSpacePulsePos", pulse.EchoPulseOrigin);
			pulse.EchoPulseMat.SetFloat("_PulseDistance", pulse.EchoPulseDistance);

			// Compute Frustum Corners
			float camFar = camera.farClipPlane;
			float camFov = camera.fieldOfView;
			float camAspect = camera.aspect;

			float fovWHalf = camFov * 0.5f;

			Vector3 toRight = camera.transform.right * Mathf.Tan(fovWHalf * Mathf.Deg2Rad) * camAspect;
			Vector3 toTop = camera.transform.up * Mathf.Tan(fovWHalf * Mathf.Deg2Rad);

			Vector3 topLeft = (camera.transform.forward - toRight + toTop);
			float camScale = topLeft.magnitude * camFar;

			topLeft.Normalize();
			topLeft *= camScale;

			Vector3 topRight = (camera.transform.forward + toRight + toTop);
			topRight.Normalize();
			topRight *= camScale;

			Vector3 bottomRight = (camera.transform.forward + toRight - toTop);
			bottomRight.Normalize();
			bottomRight *= camScale;

			Vector3 bottomLeft = (camera.transform.forward - toRight - toTop);
			bottomLeft.Normalize();
			bottomLeft *= camScale;

			// Custom Blit, encoding Frustum Corners as additional Texture Coordinates
			RenderTexture.active = dest;

			pulse.EchoPulseMat.SetTexture("_MainTex", src);

			GL.PushMatrix();
			GL.LoadOrtho();

			pulse.EchoPulseMat.SetPass(0);

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

			var temp = src;
			src = dest;
			dest = temp;

			swapped = !swapped;
		}

		if (!swapped)
		{
			Graphics.Blit(src, dest);
		}
	}
}
