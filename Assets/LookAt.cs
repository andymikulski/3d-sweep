using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAt : MonoBehaviour {
	public Transform target;
//	private Quaternion intialRotation;
//
//	void Start () {
//		intialRotation = transform.rotation;
//	}
//
	// Update is called once per frame
	void Update () {
		Vector3 relativePos = transform.position - target.position;
		Quaternion rotation = Quaternion.LookRotation(relativePos);

//		rotation.x = intialRotation.x;
//		rotation.y = intialRotation.y;

//		rotation.w = transform.rotation.w;
//		rotation.x = transform.rotation.x;
//		rotation.y = transform.rotation.y;
//
		transform.rotation = rotation;
	}
}
