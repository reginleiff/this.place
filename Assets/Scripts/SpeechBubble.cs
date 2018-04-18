﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpeechBubble : MonoBehaviour
{

    public GameObject SpeechCanvasRoot;
    public float FadeOutTime = 5;

    private GameObject _player;

    void Start()
    {
        SpeechCanvasRoot.SetActive(false);
    }

    void Update()
    {
        if (SpeechCanvasRoot.activeSelf)
        {
            SpeechCanvasRoot.transform.forward = Camera.main.transform.forward;
			if (_player != null)
			{
				SpeechCanvasRoot.transform.position = _player.transform.position;
			}
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (SpeechCanvasRoot.activeSelf || Menu.Instance != null && Menu.Instance._menuShowing)
        {
            return;
        }

        if (other.CompareTag("Player"))
        {
            SpeechCanvasRoot.SetActive(true);
            _player = other.gameObject;
            StartCoroutine(WaitThenDestroy());
        }
    }

    IEnumerator WaitThenDestroy()
    {
        yield return new WaitForSeconds(FadeOutTime);
        Destroy(gameObject);
        Destroy(SpeechCanvasRoot);
    }
}
