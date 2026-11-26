using UnityEngine;

public class FireLightScript : MonoBehaviour
{

    //This script randomly changes the intensity of light object to simulate fire

    [Header ("Light")]
	public float minIntensity = 2f; 
	public float maxIntensity = 3f;

	public Light fireLight;

	float random;

	void Update()
	{
		if (fireLight == null || Time.timeScale <= 0f) return;
		random = Random.Range(0.0f, 150.0f);
		float noise = Mathf.PerlinNoise(random, Time.time);
		fireLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
	}
}
