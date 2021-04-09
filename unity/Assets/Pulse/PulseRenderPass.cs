using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PulseRenderPass : ScriptableRenderPass
{
	public PulseRenderFeature.Settings settings;

	RenderTargetIdentifier source;
	RenderTargetIdentifier temporaryRT;
	int temporaryRTId = Shader.PropertyToID("_TempRT");
	string profilerTag;
	int pulsePositionId = Shader.PropertyToID("_StartPosition");
	int pulseDistanceId = Shader.PropertyToID("_DistanceTravelled");

	public PulseRenderPass(string tag)
	{
		profilerTag = tag;
	}

	public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
	{
		RenderTextureDescriptor blitTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
		blitTargetDescriptor.depthBufferBits = 0;

		source = renderingData.cameraData.renderer.cameraColorTarget;
		cmd.GetTemporaryRT(temporaryRTId, blitTargetDescriptor);
		temporaryRT = new RenderTargetIdentifier(temporaryRTId);
	}

	public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
	{
		CommandBuffer cmd = CommandBufferPool.Get(profilerTag);

		// TODO: Add a way to preview pulses without running the game

		// TODO: I doubt using a global variable is the best approach here, but I don't know how to
		// inject CommandBuffer from simulation code that can utilize the current game state.
		if (Main.pulses != null)
		{
			RenderTargetIdentifier src = source;
			RenderTargetIdentifier dst = temporaryRT;

			int pulseCount = Main.pulses.Count;
			for (int iPulse = 0; iPulse < pulseCount; iPulse++)
			{
				Pulse p = Main.pulses[iPulse];

				// TODO: Is it possible to set material properties locally, rather then globally?
				float pulseDuration = Time.time - p.startTime;
				float distanceTravelled = pulseDuration * p.spec.travelSpeed;
				cmd.SetGlobalVector(pulsePositionId, p.startPosition);
				cmd.SetGlobalFloat(pulseDistanceId, distanceTravelled);

				Blit(cmd, src, dst, settings.blitMaterial);

				var temp = src;
				src = dst;
				dst = temp;
			}

			if (pulseCount % 2 == 1)
				Blit(cmd, src, dst);
		}

		context.ExecuteCommandBuffer(cmd);
		CommandBufferPool.Release(cmd);
	}

	public override void FrameCleanup(CommandBuffer cmd)
	{
		cmd.ReleaseTemporaryRT(temporaryRTId);
	}
}
