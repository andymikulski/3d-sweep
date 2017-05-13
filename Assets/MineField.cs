using UnityEngine;
using UnityEngine.UI;
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
	private int FlagCount = 0;

	private Vector3 lastTarget;
	private Vector3 currentTarget;

	private Vector3 lastFocus;
	private Vector3 currentFocus;

	private OcclusionArea occlusion;

	private bool hadFirstClick = false;

	private Mine lastHoverMine = null;
	private Mine lastHoverInactiveMine = null;

	private Text flagText;
	private Text gameOverText;
	private Text timerText;

	private float timeStart = -1f;
	private bool isPlaying = false;
	private bool isGameOver = false;

	// Use this for initialization
	void Start () {
		occlusion = gameObject.AddComponent<OcclusionArea> ();

		flagText = GameObject.Find ("FlagText").GetComponent<Text> ();
		timerText = GameObject.Find ("TimerText").GetComponent<Text> ();
		gameOverText = GameObject.Find ("GameOverText").GetComponent<Text> ();

		Reset ();
	}

	void UpdateGUI() {
		flagText.text = "Flags: " + (BombCount - FlagCount);

		if (timeStart < 0) {
			timerText.text = "0:00";
			return;
		}

		if (isPlaying) {
			float now = Time.time;
			float duration = now - timeStart;

			float minutes = Mathf.Floor(duration / 60);
			float seconds = Mathf.RoundToInt(duration % 60);

			string displayedMins = minutes.ToString ();
			string displayedSeconds = Mathf.RoundToInt (seconds).ToString ();

			if(seconds < 10) { displayedSeconds = "0" + displayedSeconds; }

			timerText.text = displayedMins + ":" + displayedSeconds;
		}
	}

	public void Reset() {
		// Remove children, if any
		foreach (Transform child in transform) {
			Destroy(child.gameObject);
		}

		gameOverText.text = "";

		field = new Mine[Size, Size, Size];
		timeStart = -1;
		isPlaying = false;
		isGameOver = false;
		hadFirstClick = false;

		int halfwayPoint = Mathf.RoundToInt (Size / 2);

		BombCount = 0;
		FlagCount = 0;

		for (int i = 0; i < Size; i++) {
			for (int j = 0; j < Size; j++) {
				for (int k = 0; k < Size; k++) {
					GameObject cube = Instantiate (mineFab, gameObject.transform) as GameObject;

					Mine newMine = cube.GetComponent<Mine>();
					newMine.field = this;
					newMine.isMine = UnityEngine.Random.value < Ratio;

					if (newMine.isMine) {
						BombCount += 1;
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

	Mine DeMineIfy(Mine mine){
		mine.isMine = false;

		bool hasBeenReplaced = false;

		// this is a little dangerous, technically if the whole map is mines,
		// then this will never end
		while (!hasBeenReplaced) {
			Mine replacement = GetRandomMine ();
			if (!replacement.isMine) {
				replacement.isMine = true;
				UpdateMine (replacement);
				hasBeenReplaced = true;
			}
		}

		UpdateMine (mine);

		// update counts across the board since mines have moved
		UpdateCounts ();

		return mine;
	}

	void UpdateMine(Mine mine){
		field [(int)mine.mineCoords.x, (int)mine.mineCoords.y, (int)mine.mineCoords.z] = mine;
	}

	Mine GetRandomMine() {
		return field [GetRandomMineIndex (), GetRandomMineIndex (), GetRandomMineIndex ()];
	}

	int GetRandomMineIndex() {
		return UnityEngine.Random.Range (0, Size);
	}

	public void OnMineSelect(Mine mine) {
		Vector3 mineCoords = mine.mineCoords;

		if (Input.GetKey (KeyCode.LeftShift)) {
			FocusAround (mineCoords, true);
			FocusCamera (mine.gameObject.transform.position);
		} else if (mine.isFlagged) {
			return;
		} else if (mine.isMine) {
			// if this is the user's first click, it must _never_ be a mine
			if (!hadFirstClick) {
				// so we need to de-mine this guy, set something else as the bomb,
				// and then update counts
				OnMineSelect (DeMineIfy (mine));
				return;
			}

			BombExploded ();
		} else if (!mine.isExposed) {
			if (!hadFirstClick) {
				hadFirstClick = true;
				isPlaying = true;
				timeStart = Time.time;
			}

			mine.Reveal();
			UpdateMine (mine);
	
			// if this node doesn't have any mines, we click around its neighbors until we hit a 'border'
			if (mine.GetMineCount() == 0) {
				List<Mine> neighbors = GetNeighborsOfPoint (mineCoords);
				foreach (Mine bor in neighbors) {
					OnMineSelect (bor);
				}
				Destroy (mine.gameObject);
			}

			CheckWinConditions ();
		}
	}

	void CheckWinConditions(){
		List<Mine> remainingSafe = GetRemainingSafeBoxes ();
		List<Mine> flaggedBoxes = GetFlaggedBoxes ();
		// if all of the safe boxes have been revealed, and the user has thrown down all the flags,
		// they win!
		if (remainingSafe.Count == 0 && flaggedBoxes.Count == BombCount){
			Win();
		}
	}

	void EndGame() {
		isPlaying = false;
		isGameOver = true;
	}

	void Win(){
		EndGame ();
		gameOverText.text = "you are winner ha ha ha";
	}

	void Lose(){
		EndGame ();
		gameOverText.text = "you lost, bummer!";
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

	List<Mine> GetFlaggedBoxes(){
		List<Mine> flagged = new List<Mine> ();

		for (int x = 0; x < Size; x++) {
			for (int y = 0; y < Size; y++) {
				for (int z = 0; z < Size; z++) {
					Mine mine = GetMineAtPos (new Vector3 (x, y, z));
					if (mine != null && !mine.isExposed && mine.isFlagged) {
						Debug.Log ("adding mine " + mine.mineCoords.ToString ());
						flagged.Add (mine);
					}
				}
			}
		}

		return flagged;
	}

	List<Mine> GetRemainingSafeBoxes(){
		List<Mine> remaining = new List<Mine> ();

		for (int x = 0; x < Size; x++) {
			for (int y = 0; y < Size; y++) {
				for (int z = 0; z < Size; z++) {
					Mine mine = GetMineAtPos (new Vector3 (x, y, z));
					if (mine != null && !mine.isExposed && !mine.isMine) {
						remaining.Add (mine);
					}
				}
			}
		}

		return remaining;
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

			if (hit.distance >= 4f && (includeInactive || target.isFocused)) {
				foundTarget = true;
				found = target;
			}
			i++;
		}

		return found;
	}

	void Update() {
		if (isGameOver) {
			if (Input.GetKeyDown (KeyCode.Return)) {
				Reset ();
			}
			return;
		}

		UpdateGUI ();

		Mine target = GetMousedMine ();
		if (target != lastHoverMine) {
			if (target != null) { 
				target.SetTargeted (true);
				UpdateMine (target);
			}

			if (lastHoverMine != null) {
				lastHoverMine.SetTargeted (false);
				UpdateMine (lastHoverMine);
			}
			lastHoverMine = target;
		}
		Mine inactiveTarget = GetMousedMine (true);
		if (inactiveTarget != lastHoverInactiveMine) {
			if (inactiveTarget != null) { 
				inactiveTarget.SetTargeted (true);
				UpdateMine (inactiveTarget);
			}

			if (lastHoverInactiveMine != null) {
				lastHoverInactiveMine.SetTargeted (false);
				UpdateMine (lastHoverInactiveMine);
			}
			lastHoverInactiveMine = inactiveTarget;
		}

		if (Input.GetKeyDown (KeyCode.Space)) {
			FocusAround (lastFocus, true);
			FocusCamera (lastTarget);
		} else if (Input.GetKeyDown (KeyCode.Return)) {
			Reset ();

			// User left clicked
		} else if (Input.GetMouseButtonUp (0) && !Input.GetKey (KeyCode.LeftShift)) {
			// And they had the left shift
			if (Input.GetKey (KeyCode.LeftControl) && inactiveTarget != null) {
				FocusAround (inactiveTarget.mineCoords, true);
				FocusCamera (inactiveTarget.GetWorldPosition ());
			} else if (target != null) {
				// And they did NOT have the left shift 
				OnMineSelect (target);
			}
		} else if (Input.GetMouseButtonUp (1) && target != null) {
			if (!target.isExposed && !target.hasExploded) {
				FlagCount += target.ToggleFlagged () ? 1 : -1;

				UpdateMine (target);

				CheckWinConditions ();
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

		Lose ();
	}
}
