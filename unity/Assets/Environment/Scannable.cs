using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scannable : MonoBehaviour
{
	Material mat;
	float emissiveMax = 1.0f;
	float currentVal = 0.0f;
	float timeElapsed;
	float disableTimeElapsed;
	float highlightActiveTimer;
	bool enableHighlight = false;

	OutlineRef outlineRef;

	void Start() 
	{
		mat = GetComponent<Renderer>().material;
		outlineRef = FindObjectOfType<OutlineRef>().GetComponent<OutlineRef>();
	}

	public void ObjectScanned()
	{
		mat.SetFloat("_EmissiveStrength", 0);
		mat.SetInt("_HighlightOn", 1);
		if(enableHighlight)
			ResetLerp();
			
		enableHighlight = true;
	}

	void Update() 
	{
		if (enableHighlight)
		{
			float dt = Time.deltaTime;

			if (timeElapsed <= outlineRef.outlineSpec.highlightDuration)
			{
				float floatVal = Mathf.Lerp(currentVal, emissiveMax, timeElapsed / outlineRef.outlineSpec.highlightDuration);
				timeElapsed += dt;
				mat.SetFloat("_EmissiveStrength", floatVal);
			}
			else
			{
				if (highlightActiveTimer <= outlineRef.outlineSpec.highlightLingerDuration)
				{
					highlightActiveTimer += dt;
				}
				else if(disableTimeElapsed <= outlineRef.outlineSpec.dehighlightDuration)
				{
					// turn off highlight
					float floatVal = Mathf.Lerp(emissiveMax, 0.0f, disableTimeElapsed / outlineRef.outlineSpec.dehighlightDuration);
					disableTimeElapsed += dt;
					mat.SetFloat("_EmissiveStrength", floatVal);
				}
				else
				{
					highlightActiveTimer = 0.0f;
					enableHighlight = false;
					disableTimeElapsed = 0.0f;
					timeElapsed = 0.0f;
					currentVal = 0.0f;
				}
			}
		}
	}

	void ResetLerp()
	{
		timeElapsed = outlineRef.outlineSpec.highlightDuration;
		highlightActiveTimer = 0.0f;
		disableTimeElapsed = 0.0f;
	}
}
