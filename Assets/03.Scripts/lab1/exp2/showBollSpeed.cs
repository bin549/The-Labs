﻿using System;
using UnityEngine;
using UnityEngine.UI;

public class showBollSpeed : MonoBehaviour {
    private double speed;
    private Text text;

    private void Start() {
        text = GetComponent<Text>();
    }

    private void FixedUpdate() {
        if (!GameObject.FindWithTag("Ideal") && !GameObject.FindWithTag("ball")) {
            text.text = "实时速度为:";
            speed = GameObject.FindWithTag("ball").GetComponent<getBollSpeed>().getSpeed();
            text.text += Math.Round(speed, 2);
        }
    }
}