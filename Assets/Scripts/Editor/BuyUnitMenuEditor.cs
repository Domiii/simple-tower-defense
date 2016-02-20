using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections;
using System.Linq;
using System.IO;

[CustomEditor(typeof(BuyUnitMenu))]
public class BuyUnitMenuEditor : Editor {
	public static readonly string PreviewFileName = "MenuButtonPreview";
	public static readonly string PreviewFileFolder = "Data/AutoGenerated/BuyUnitMenu";

	/// <summary>
	/// Copy given texture and remove all pixels of given color
	/// </summary>
	Texture2D RemoveColor(Texture2D textureOrig, Color c) {
		var result = new Texture2D (textureOrig.width, textureOrig.height, TextureFormat.ARGB32, false);
		var pixels = textureOrig.GetPixels ();

		// replace all matching pixels
		for (var i = 0; i < pixels.Length; ++i) {
			var px = pixels[i];
			if (px == c) {
				pixels[i] = new Color(0,0,0,0);
			}
		}

		// write back to result texture
		result.SetPixels (pixels);

		return result;
	}

	Texture2D StorePreviewAsAsset(Object asset, int index) {
		var folder = "Assets/" + PreviewFileFolder;
		var fileNamePrefix = PreviewFileName + index;
		var fileName = fileNamePrefix + ".asset";
		var completePath = folder + '/' + fileName;

		// get preview texture from cache
		var textureOrig = AssetPreview.GetAssetPreview(asset);

		// create new texture that is copy of original
		// remove background of preview (which is the given color)
		var texture = RemoveColor (textureOrig, new Color(0.3215686f, 0.3215686f, 0.3215686f, 1.0f));
		texture.name = folder + '/' + fileNamePrefix;

		// create folders
		if (!Directory.Exists (folder)) {
			Directory.CreateDirectory (folder);
		}

		// delete any existing version of it (if any)
		AssetDatabase.DeleteAsset (completePath);

		// add to AssetDatabase
		AssetDatabase.CreateAsset(texture, completePath);

		// load and from here on only reference the texture from the AssetDatabase
		var previewAsset = AssetDatabase.LoadAssetAtPath(completePath, typeof(Texture2D));
		return (Texture2D)EditorUtility.InstanceIDToObject (previewAsset.GetInstanceID ());
	}

	/// <summary>
	/// Loads the unit preview sprite.
	/// </summary>
	/// <see cref="http://answers.unity3d.com/questions/1139254/how-to-set-sprite-or-texture2d-assetpath-in-editor.html">TODO: Fix errors</see>
	Sprite CreateUnitPreview(int index, Object asset, Bounds worldBounds) {
		var previewTexture = StorePreviewAsAsset (asset, index);

		// compute pixels per unit size to fill given bounds while keeping aspect ratio
		var texW = previewTexture.width;
		var texH = previewTexture.height;
		var worldW = worldBounds.max.x - worldBounds.min.x;
		var worldH = worldBounds.max.y - worldBounds.min.y;
		var pixelsPerUnit = Mathf.Min (texW / worldW, texH / worldH);

		// create sprite
		Sprite sprite = new Sprite ();
		var previewSpriteRect = new Rect(new Vector2(0,0), new Vector2(texW, texH));
		var pivot = new Vector2 (0.5f, 0.5f);
		sprite = Sprite.Create (previewTexture, previewSpriteRect, pivot, pixelsPerUnit);

		return sprite;
	}

	void MoveAndScaleButton(GameObject btn, int index) {
		var menu = (BuyUnitMenu)target;
		var buttonRenderer = btn.GetComponent<SpriteRenderer> ();


		// first scale
		var buttonYMin = buttonRenderer.bounds.min.y;
		var buttonYMax = buttonRenderer.bounds.max.y;
		var currentHeight = buttonYMax - buttonYMin;
		var menuCoords = new Vector3[4];
		menu.GetComponent<RectTransform> ().GetWorldCorners (menuCoords);
		var targetHeight = menuCoords [1].y - menuCoords [0].y;
		var ratio = targetHeight / currentHeight;
		var scale = buttonRenderer.transform.localScale;
		buttonRenderer.transform.localScale = new Vector2(scale.x * ratio, scale.y * ratio);


		// then move
		var buttonXMin = buttonRenderer.bounds.min.x;
		var buttonXMax = buttonRenderer.bounds.max.x;
		var w = buttonXMax - buttonXMin;
		var xOffset = menu.transform.position.x - buttonXMin;
		btn.transform.Translate (new Vector2(xOffset + w * index, 0));
	}

	void CreateButton(int index, BuyUnitStatus status) {
		var menu = (BuyUnitMenu)target;

		// create button
		var btn = (GameObject)PrefabUtility.InstantiatePrefab (menu.ButtonPrefab);
		var btnSettings = btn.GetComponent<BuyUnitButton> ();
		btnSettings.Menu = menu;
		btnSettings.UnitStatusIndex = index;

		// add button to pivot point in menu canvas
		btn.transform.SetParent (menu.transform, false);
		btn.transform.position = menu.transform.position;

		var previewObject = btn.transform.FindChild ("Preview");
		if (previewObject == null) {
			// just ignore it, for now
			//Debug.LogError("ButtonPrefab is missing \"Preview\" child in object hierarchy.", this);
		}
		else {
			// create preview image
			var previewRenderer = previewObject.GetComponent<SpriteRenderer> ();
			var previewBounds = previewRenderer.bounds;
			previewRenderer.sprite = CreateUnitPreview(index, status.Config.UnitPrefab, previewBounds);
			
			// resize (make sure, new sprite fills out the entire space)
			var previewSize = previewBounds.max - previewBounds.min;
			var newSize = previewRenderer.bounds.max - previewRenderer.bounds.min;
			var scale = previewRenderer.transform.localScale;
			previewRenderer.transform.localScale =  new Vector2(scale.x * previewSize.x / newSize.x, scale.y * previewSize.y / newSize.y);
		}
		
		// scale then move to correct position
		MoveAndScaleButton (btn, index);

		// make sure, canvas sorting is overridden
		for (var i = 0; i < btn.transform.childCount; ++i) {
			var child = btn.transform.GetChild(i);
			var canvas = child.GetComponent<Canvas>();
			
			if (canvas != null) {
				canvas.overrideSorting = true;
			}
		}

		btnSettings.Start ();
	}

	void CreateButtons(BuyUnitStatus[] statuses) {
		var menu = (BuyUnitMenu)target;

		var buttonPrefab = menu.ButtonPrefab;
		var buttonPrefabRenderer = buttonPrefab != null ? buttonPrefab.GetComponent<SpriteRenderer> () : null;
		
		if (menu.GetComponent<RectTransform> () == null) {
			Debug.LogError("BuyUnitMenu is missing RectTransform component. Make sure it is a canvas!", this);
			return;
		}

		if (buttonPrefabRenderer == null) {
			Debug.LogError("ButtonPrefab of BuyUnitMenu is missing SpriteRenderer component.", this);
			return;
		}

		if (statuses == null) {
			return;
		}

		for (int i = 0; i < statuses.Length; ++i) {
			var status = statuses [i];
			CreateButton (i, status);
		}
	}

	void DeleteAllButtons() {
		var menu = (BuyUnitMenu)target;
		for (var i = menu.transform.childCount-1; i >= 0; --i) {
			var child = menu.transform.GetChild(i);
			GameObject.DestroyImmediate(child.gameObject);
		}
	}

	public override void OnInspectorGUI () {
		DrawDefaultInspector();

		var menu = (BuyUnitMenu)target;

		if (GUILayout.Button ("Create Buttons")) {
			var statuses = menu.UnitManager.ResetUnitStatuses();

			// wait until all asset previews are ready
			int readyCount;
			do {
				readyCount = 0;
				for (int i = 0; i < statuses.Length; ++i) {
					var status = statuses [i];
					var result = AssetPreview.GetAssetPreview (status.Config.UnitPrefab);
					if (result != null) {
						++readyCount;
					}
				}
				if (readyCount < statuses.Length) {
					System.Threading.Thread.Sleep(0);
					Debug.Log ("Waiting for assets...");
				}
				else {
					break;
				}
			}
			while (true);


			// delete all existing children
			DeleteAllButtons();

			// create buttons
			CreateButtons(statuses);
		}
		
		if (GUILayout.Button ("Delete Buttons")) {
			DeleteAllButtons();
		}

		EditorUtility.SetDirty(target);
	}

	// Use this for initialization
	void OnEnable  () {
		
	}
}
