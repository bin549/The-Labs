using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class changG : MonoBehaviour {
    private const float DEFAULT_GRAVITY = -9.8f;
    private const float ALTERNATE_GRAVITY = -10f;

    public void OnClick(bool isOn) {
        Physics.gravity = new Vector3(0, isOn ? ALTERNATE_GRAVITY : DEFAULT_GRAVITY, 0);
    }
}