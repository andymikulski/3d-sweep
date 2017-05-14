using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShowWithKeyDown : MonoBehaviour {
	public float fadeSpeed = 10f;
	private CanvasGroup canvas;
	public bool forceShow = false;

	void Start () {
		canvas = GetComponent<CanvasGroup> ();
	}

	// Update is called once per frame
	void Update () {
		float newAlpha = 0f;

		if (forceShow || Input.GetKey (KeyCode.Tab)) {
			newAlpha = 1f;
		}

		canvas.alpha = Mathf.Lerp (canvas.alpha, newAlpha, Time.deltaTime * fadeSpeed);
	}
}
