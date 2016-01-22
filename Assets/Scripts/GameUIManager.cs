﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameUIManager : MonoBehaviour {
	public static GameUIManager Instance;
	
	public GameObject DimmerPrefab;
	public GameObject AttackerHighlighterPrefab;

	GameObject dimmerObject;
	Selectable currentSelection;
	int currentSelectionSortingLayerID;
	

	public GameUIManager() {
		Instance = this;
	}


	// Use this for initialization
	void Start () {
		// create invisible highlighter
		dimmerObject = Instantiate (DimmerPrefab);
		dimmerObject.SetActive (false);
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	#region Selection + Highlighting
	public bool IsSelected(Selectable obj) {
		return currentSelection == obj;
	}

	public void ToggleSelect(Selectable obj) {
		Debug.Log (obj);
		if (IsSelected(obj)) {
			ClearSelection();
		} else {
			Select (obj);
		}
	}

	public void Select(Selectable obj) {
		if (obj == currentSelection) {
			return;
		}

		if (currentSelection != null) {
			ClearSelection();
		}

		// select object
		currentSelection = obj;

		// move object on top of dimmer
		var renderer = currentSelection.GetComponent<SpriteRenderer> ();
		if (renderer != null) {
			currentSelectionSortingLayerID = renderer.sortingLayerID;
			renderer.sortingLayerName = "Highlight";
		}

		// make highlighter visible
		dimmerObject.SetActive (true);

		// send message
		currentSelection.SendMessage ("OnSelect", SendMessageOptions.DontRequireReceiver);
	}

	public void ClearSelection() {
		if (currentSelection != null) {
			// reset rendering options
			var renderer = currentSelection.GetComponent<SpriteRenderer> ();
			if (renderer != null) {
				renderer.sortingLayerID = currentSelectionSortingLayerID;
			}

			// send message
			currentSelection.SendMessage ("OnUnselect", SendMessageOptions.DontRequireReceiver);

			// unset
			currentSelection = null;
		}

		// make highlighter invisible
		dimmerObject.SetActive (false);
	}
	#endregion
	


	#region Text
	public void UpdateText() {

	}
	#endregion
}
