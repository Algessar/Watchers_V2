using System;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject isPlaying;

    private void Awake()
    {
        isPlaying.SetActive(true);
    }
}
