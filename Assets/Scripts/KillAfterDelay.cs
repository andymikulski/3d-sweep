using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillAfterDelay : MonoBehaviour {
	public float TimeToLive = 2f;

	void Start()
	{
		StartCoroutine(Example());
	}

	IEnumerator Example()
	{
		yield return new WaitForSeconds(TimeToLive);
		Destroy (gameObject);
	}
}
