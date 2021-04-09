using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PulseRenderFeature : ScriptableRendererFeature
{
	[Serializable]
	public class Settings
	{
		public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
		public Material blitMaterial = null;
	}

	public Settings settings = new Settings();
	PulseRenderPass blitPass;

	public override void Create()
	{
		blitPass = new PulseRenderPass(name);
	}

	public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
	{
		if (settings.blitMaterial == null)
		{
			Debug.LogWarningFormat("Missing Blit Material. {0} blit pass will not execute. Check for missing reference in the assigned renderer.", GetType().Name);
			return;
		}

		blitPass.renderPassEvent = settings.renderPassEvent;
		blitPass.settings = settings;
		renderer.EnqueuePass(blitPass);
	}
}
