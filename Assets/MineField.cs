using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class MineField : MonoBehaviour {
	public int Size = 3;
	public float Margin = 2f;
	public GameObject mineFab;
	private Mine[,,] field;

	public float Ratio = 0.15f;

	private Vector3 lastTarget;
	private Vector3 currentTarget;

	private Vector3 lastFocus;
	private Vector3 currentFocus;

	private OcclusionArea occlusion;

	// Use this for initialization
	void Start () {
		occlusion = gameObject.AddComponent<OcclusionArea> ();
		Reset ();
	}

	public void Reset() {
		// Remove children, if any
		foreach (Transform child in transform) {
			Destroy(child.gameObject);
		}

		field = new Mine[Size, Size, Size];

		int halfwayPoint = Mathf.RoundToInt (Size / 2);

		for (int i = 0; i < Size; i++) {
			for (int j = 0; j < Size; j++) {
				for (int k = 0; k < Size; k++) {
					GameObject cube = Instantiate (mineFab, gameObject.transform) as GameObject;

					Mine newMine = cube.GetComponent<Mine>();
					newMine.field = this;
					newMine.isMine = UnityEngine.Random.value < Ratio;
					newMine.mineCoords = new Vector3 (i, j, k);

					field [i, j, k] = newMine;

					cube.transform.position = new Vector3 (i * Margin, j * Margin, k * Margin);
					cube.layer = LayerMask.NameToLayer("Scene");

					if (i == halfwayPoint && j == halfwayPoint && k == halfwayPoint) {
						lastFocus = new Vector3 (i, j, k);
						currentFocus = new Vector3 (i, j, k);
					}
				}
			}
		}

		UpdateCounts ();
		Center ();

		FocusAround (new Vector3 (halfwayPoint, halfwayPoint, halfwayPoint), true);
		FocusCamera ( GetMineAtPos(new Vector3 (halfwayPoint, halfwayPoint, halfwayPoint)).transform.position );
	}

	void UpdateCounts() {
		for (int x = 0; x < Size; x++) {
			for (int y = 0; y < Size; y++) {
				for (int z = 0; z < Size; z++) {
					Mine block = GetMineAtPos (new Vector3 (x, y, z));
					block.SetMineCount (GetCountAtPoint (new Vector3 (x, y, z)));
				}
			}
		}
	}

	void Center() {
		Bounds bounds = new Bounds(transform.position, Vector3.zero);

		foreach(Renderer r in GetComponentsInChildren<Renderer>()) {
			bounds.Encapsulate(r.bounds);
		}

		occlusion.center = bounds.center;
		occlusion.size = bounds.size;

		transform.localPosition = bounds.center * -1f;
	}

	public Mine GetMineAtPos(Vector3 pos){
		bool outtaBounds = pos.x >= Size || pos.y >= Size || pos.z >= Size;
		bool outtaNegBounds = pos.x < 0 || pos.y < 0 || pos.z < 0;

		if (outtaBounds || outtaNegBounds) {
			return null;
		}

		try {
			return field [(int)pos.x, (int)pos.y, (int)pos.z];
		}       
		catch (NullReferenceException) {
			return null;
		}
	}

	public void ToggleFocused(bool display) {
		for (int x = 0; x < Size; x++) {
			for (int y = 0; y < Size; y++) {
				for (int z = 0; z < Size; z++) {
					Mine neighbor = GetMineAtPos (new Vector3 (x, y, z));
					neighbor.isSolid = display;
				}
			}
		}
	}

	public void FocusAround(Vector3 pos, bool clearFocusFirst) {
		if (clearFocusFirst) {
			ToggleFocused (false);
		}

		FocusAround (pos);
	}

	public void FocusAround(Vector3 pos) {
		for (int x = -1; x <= 1; x++) {
			for (int y = -1; y <= 1; y++) {
				for (int z = -1; z <= 1; z++) {
					Mine neighbor = GetMineAtPos (pos + new Vector3 (x, y, z));
					if (neighbor != null) {
						neighbor.isSolid = true;
					}
				}
			}
		}

		lastFocus = currentFocus;
		currentFocus = pos;
	}

	public void FocusCamera(Vector3 target) {
		lastTarget = currentTarget;
		Camera.main.GetComponent<MouseOrbitZoom> ().SetTarget (target);
		currentTarget = target;
	}

	void Update() {
		if (Input.GetKeyDown (KeyCode.Space)) {
			FocusAround (lastFocus, true);
			FocusCamera (lastTarget);
		} else if (Input.GetKeyDown (KeyCode.Return)) {
			Reset ();
		}
	}

	public List<Mine> GetNeighborsOfPoint(Vector3 pos) {
		List<Mine> list = new List<Mine> ();

		for (int x = -1; x <= 1; x++) {
			for (int y = -1; y <= 1; y++) {
				for (int z = -1; z <= 1; z++) {
					Mine neighbor = GetMineAtPos (pos + new Vector3 (x, y, z));
					if (neighbor != null) {
						list.Add (neighbor);
					}
				}
			}
		}

		return list;
	}

	public int GetCountAtPoint(Vector3 pos) {
		int count = 0;
		for (int x = -1; x <= 1; x++) {
			for (int y = -1; y <= 1; y++) {
				for (int z = -1; z <= 1; z++) {
					Mine neighbor = GetMineAtPos (pos + new Vector3 (x, y, z));
					if (neighbor != null) {
						if (neighbor.isMine) {
							count += 1;
						}
					}
				}
			}
		}

		return count;
	}

	public void BombExploded() {
		ToggleFocused (false);

		for (int x = 0; x < Size; x++) {
			for (int y = 0; y < Size; y++) {
				for (int z = 0; z < Size; z++) {
					Mine mine = GetMineAtPos (new Vector3 (x, y, z));
					if (mine.isMine) {
						mine.Explode ();
						mine.isSolid = true;
					}
				}
			}
		}
	}
}
