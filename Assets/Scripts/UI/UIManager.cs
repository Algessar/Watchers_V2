using System;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject isPlaying;

    public TextMeshProUGUI idleTimer;

    private void Awake()
    {
        isPlaying.SetActive(true);
    }

    void PlayMode()
    {
        
    }
}
