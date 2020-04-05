﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LaunchLevel : MonoBehaviour
{
    public int actualLevel = 0;
    /*[HideInInspector]*/ public Vector3 checkpoint;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void LoadLevel(int level)
    {
        checkpoint = Vector3.zero;
        actualLevel = level;
        SceneManager.LoadSceneAsync(actualLevel);
    }

    public void LoadnextLevel()
    {
        checkpoint = Vector3.zero;
        actualLevel = actualLevel + 1;
        SceneManager.LoadSceneAsync(actualLevel);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene(actualLevel);
    }

    public void LoadMainMenu()
    {
        actualLevel = 0;
        checkpoint = Vector3.zero;
        
        SceneManager.LoadSceneAsync(0);
        Destroy(gameObject);
    }
}
