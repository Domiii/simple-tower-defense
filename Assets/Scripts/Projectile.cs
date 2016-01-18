﻿using UnityEngine;
using System.Collections;

public class Projectile : MonoBehaviour {
	public float Damage = 10;
	public float Speed = 1;
	public float MaxLifeTimeSeconds = 10;

	public Vector3 FlightDirection;


	float startTime;


	// Use this for initialization
	void Start () {
		startTime = Time.time;

	}
	
	// Update is called once per frame
	void Update () {
		var now = Time.time;
		var lifeTime = now - startTime;

		if (lifeTime >= MaxLifeTimeSeconds) {
			// missed target
			Destroy(gameObject);
			return;
		}
	}

	void OnCollisionEnter2D (Collision2D col) {
		if (col.gameObject.CompareTag(Living.AliveTag)) {
			// when colliding with Living -> Cause damage
			var living = col.gameObject.GetComponent<Living>();
			if (living != null) {
				living.Damage(Damage);
			}
		}
		Destroy (gameObject);
	}
}
