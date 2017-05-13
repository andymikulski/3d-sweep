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

	private int BombCount = 0;
//	private int FlagCount = 0;

	private Vector3 lastTarget;
	private Vector3 currentTarget;

	private Vector3 lastFocus;
	private Vector3 currentFocus;

	private OcclusionArea occlusion;

	private Mine lastHoverMine = null;
	private Mine lastHoverInactiveMine = null;

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

		BombCount = 0;
//		FlagCount = 0;

		for (int i = 0; i < Size; i++) {
			for (int j = 0; j < Size; j++) {
				for (int k = 0; k < Size; k++) {
					GameObject cube = Instantiate (mineFab, gameObject.transform) as GameObject;

					Mine newMine = cube.GetComponent<Mine>();
					newMine.field = this;
					newMine.isMine = UnityEngine.Random.value < Ratio;

					if (newMine.isMine) {
						BombCount += 1;
						newMine.SetFlagged (true);
					}

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

	public void OnMineSelect(Mine mine) {
		Vector3 mineCoords = mine.mineCoords;

		if (Input.GetKey (KeyCode.LeftShift)) {
			FocusAround (mineCoords, true);
			FocusCamera (mine.gameObject.transform.position);
		} else if (mine.isFlagged) {
			return;
		} else if (mine.isMine) {
			BombExploded ();
		} else if (!mine.isExposed) {
			mine.Reveal();
	
			// if this node doesn't have any mines, we click around its neighbors until we hit a 'border'
			if (mine.GetMineCount() == 0) {
				List<Mine> neighbors = GetNeighborsOfPoint (mineCoords);
				foreach (Mine bor in neighbors) {
					OnMineSelect (bor);
				}
				Destroy (mine.gameObject);
			}
		}
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
					if (neighbor != null) {
						neighbor.SetFocused (display);
					}
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
						neighbor.SetFocused (true);
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

	Mine GetMousedMine() {
		return GetMousedMine (false);
	}

	Mine GetMousedMine(bool includeInactive) {
		Ray Alvarado = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit[] hits;
		hits = Physics.RaycastAll(Alvarado);
		int i = 0;
		bool foundTarget = false;
		Mine found = null;

		while (i < hits.Length && !foundTarget) {
			RaycastHit hit = hits[i];
			GameObject obj = hit.collider.gameObject;
			Mine target = obj.GetComponent<Mine> ();

			if (includeInactive || target.isFocused) {
				foundTarget = true;
				found = target;
			}
			i++;
		}

		return found;
	}

	void Update() {
		Mine target = GetMousedMine ();
		if (target != lastHoverMine) {
			if (target != null) { 
				target.SetTargeted (true);
			}

			if (lastHoverMine != null) {
				lastHoverMine.SetTargeted (false);
			}
			lastHoverMine = target;
		}
		Mine inactiveTarget = GetMousedMine (true);
		if (inactiveTarget != lastHoverInactiveMine) {
			if (inactiveTarget != null) { 
				inactiveTarget.SetTargeted (true);
			}

			if (lastHoverInactiveMine != null) {
				lastHoverInactiveMine.SetTargeted (false);
			}
			lastHoverInactiveMine = inactiveTarget;
		}

		if (Input.GetKeyDown (KeyCode.Space)) {
			FocusAround (lastFocus, true);
			FocusCamera (lastTarget);
		} else if (Input.GetKeyDown (KeyCode.Return)) {
			Reset ();

			// User left clicked
		} else if (Input.GetMouseButtonUp (0) && !Input.GetKey(KeyCode.LeftShift)) {
			// And they had the left shift
			if (Input.GetKey (KeyCode.LeftControl) && inactiveTarget != null) {
				FocusAround (inactiveTarget.mineCoords, true);
				FocusCamera (inactiveTarget.GetWorldPosition ());
			} else if (target != null) {
			// And they did NOT have the left shift 
				OnMineSelect (target);
			}
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
					if (mine.isMine && !mine.isFlagged) {
						mine.Explode ();
						mine.SetFocused (true);
					}
				}
			}
		}
	}
}
