using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Mine : MonoBehaviour
{
	public enum MineStatus {
		Untouched,
		Flagged
	};

	public MineStatus status = MineStatus.Untouched;

	public bool isSolid = false;
	public bool isMine = false;
	public MineField field;
	public Vector3 mineCoords;
	private bool isTargeted = false;
	private MeshRenderer mr;
	private int mineCount = 1;

	private bool isExposed = false;

	public GameObject explosion;

	void Start(){
		mr = GetComponent<MeshRenderer> ();
		mr.material.EnableKeyword ("_EMISSION");

		if (explosion == null)
			explosion = Resources.Load ("PlasmaExplosionEffect") as GameObject;
	}

	void SetText(string txt) {
		TextMesh[] texts = GetComponentsInChildren<TextMesh> ();

		string value = txt == "0" ? "" : txt;

		foreach (TextMesh text in texts) {
			text.text = value;
		}
	}

	public void SetMineCount(int count) {
		mineCount = count;
	}

	public void Explode(){
		Instantiate (explosion, gameObject.transform.position, new Quaternion ());
		SetColor (Color.clear);

		Destroy (gameObject);
	}

	void OnSelect() {
		if (status == MineStatus.Flagged) {
			return;
		}

		if (Input.GetKey (KeyCode.LeftShift)) {
			Camera.main.GetComponent<MouseOrbitZoom> ().target = gameObject.transform;
			isSolid = true;
			field.ToggleFocused (false);
			field.FocusAround (mineCoords);
		} else if (isMine) {
			field.BombExploded ();
		} else if (!isExposed) {
			SetText (isMine ? "" : mineCount.ToString ());
			isExposed = true;
			isTargeted = false;

			// if this node doesn't have any mines, we click around its neighbors until we hit a 'border'
			if (mineCount == 0) {
				List<Mine> neighbors = field.GetNeighborsOfPoint (mineCoords);
				foreach (Mine bor in neighbors) {
					bor.OnSelect ();
				}

				Destroy (gameObject);
			}
		}
	}

	void OnFlagToggle() {
		if (status == MineStatus.Flagged) {
			status = MineStatus.Untouched;
		} else {
			status = MineStatus.Flagged;
		}
	}
		
	void Update(){
		Color newColor = new Color (1f, 0.309803922f, 0f, 1f);

		if (isTargeted){
			if (Input.GetMouseButtonUp (0)) {
				OnSelect ();
			} else if (!isExposed && Input.GetMouseButtonUp (1)) {
				OnFlagToggle ();
			}
		}

		if (status == MineStatus.Flagged) {
			if (isTargeted) {
				newColor = Color.magenta;
			} else {
				newColor = Color.blue;
			}
		} else if (status == MineStatus.Untouched) {
			if (isTargeted && !isExposed) {
				newColor = Color.yellow;
			} else {
				newColor = new Color (1f, 0.309803922f, 0f, 1f);
			}
		}

		if (isSolid) {
			newColor.a = 1f;
		} else {
			newColor.a = 0.1f;
		}

		SetColor (newColor);
	}

	void SetColor(Color newColor){
		mr.material.SetColor ("_Color", Color.Lerp (mr.material.color, newColor, Time.deltaTime * 5f));
//		newColor.a = 0.25f;
//		mr.material.SetColor ("_Emission", newColor);
	}

	void OnMouseEnter () {
		if (Input.GetKey (KeyCode.LeftShift) && Input.GetMouseButton(0)) {
			return;
		}
		isTargeted = true;
	}

	void onMouseDrag () {
		isTargeted = false;
	}

	void OnMouseExit () {
		isTargeted = false;
	}
}

