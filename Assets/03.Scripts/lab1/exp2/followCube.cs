﻿using UnityEngine;

public class followCube : MonoBehaviour {
    private Transform target;

    private void Update() {
        if (GameObject.FindWithTag("Cube")) {
            target = GameObject.FindWithTag("Cube").transform;
        }
    }

    private void FixedUpdate() {
        transform.position = new Vector3(target.position.x - 0.1f, target.position.y + 0.1f, target.position.z);
    }
}