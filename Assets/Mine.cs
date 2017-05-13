using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Mine : MonoBehaviour
{
	public bool isFocused = false;
	public bool isMine = false;
	public bool isExposed = false;
	public bool isTargeted = false;
	public bool isFlagged = false;

	public MineField field;
	public Vector3 mineCoords;
	private bool hasExploded = false;
	private MeshRenderer mr;
	private int mineCount = 1;

	public Color NeutralState = new Color (1f, 0.309803922f, 0f, 1f);
	public Color TargetedState;
	public Color RevealedState;
	public Color FlaggedState;
	public Color TargetedFlaggedState;
	public Color ExplodedMine;


	public GameObject explosion;

	void Start(){
		gameObject.isStatic = true;

		mr = GetComponent<MeshRenderer> ();
		mr.material.EnableKeyword ("_EMISSION");
	}

	void SetText(string txt) {
		TextMesh[] texts = GetComponentsInChildren<TextMesh> ();

		string value = txt == "0" ? "" : txt;

		foreach (TextMesh text in texts) {
			text.text = value;
		}
	}

	public int GetMineCount() {
		return mineCount;
	}

	public void SetMineCount(int count) {
		mineCount = count;
	}

	public void Explode(){
		Instantiate (explosion, gameObject.transform.position, Quaternion.Euler (Random.insideUnitSphere * 360));
		hasExploded = true;
	}

	public void SetFlagged(bool flagged) {
		isFlagged = flagged;
	}

	public void ToggleFlagged() {
		isFlagged = !isFlagged;
	}
		
	void Update(){
		Color newColor = NeutralState;

//
//		if (isTargeted) {
//			if (Input.GetMouseButtonUp (0)) {
//				OnSelect ();
//			} else if (!isExposed && Input.GetMouseButtonUp (1)) {
//				OnFlagToggle ();
//			}
//		}

		if (isFlagged) {
			if (isTargeted) {
				newColor = TargetedFlaggedState;
			} else {
				newColor = FlaggedState;
			}
		} else {
			if (isTargeted && !isExposed) {
				newColor = TargetedState;
			} else if (isExposed) {
				newColor = RevealedState;
//				float r = RevealedState.r;
//				float g = RevealedState.g;
//				float b = RevealedState.b;
//
//				Color flagged = new Color (FlaggedState.r, FlaggedState.g, FlaggedState.b);
//				float h;
//				float s;
//				float v;
//				Color.RGBToHSV (FlaggedState, out h, out s, out v);
//				flagged = Color.HSVToRGB (h, s, v);
//
//				r += flagged.r;
//				g += flagged.g;
//				b += flagged.b;
//
//				float factor = 2 * (mineCount / 26);
//
//				newColor = new Color (r / factor, g / factor, b / factor);
			} else {
				newColor = NeutralState;
			}
		}

		if (hasExploded) {
			newColor = ExplodedMine;
		}

		if (isFocused) {
			newColor.a = 1f;
		} else {
			newColor.a = isTargeted ? 0.5f : 0.25f;
		}

		transform.localScale = Vector3.Lerp (transform.localScale, Vector3.one * (isTargeted && (Input.GetMouseButton (0) || Input.GetMouseButton (1)) ? 0.95f : 1f), Time.deltaTime * 30f);
		SetColor (newColor);
	}

	public void SetFocused(bool focus) {
		isFocused = focus;
	}

	public void SetTargeted(bool targeted) {
		isTargeted = targeted;
	}

	public void Reveal() {
		SetText (isMine ? "" : mineCount.ToString ());
		isExposed = true;
	}

	void SetColor(Color newColor){
		mr.material.SetColor ("_Color", Color.Lerp (mr.material.color, newColor, Time.deltaTime * 5f));
	}

	public Vector3 GetWorldPosition() {
		return gameObject.transform.position;
	}
}

