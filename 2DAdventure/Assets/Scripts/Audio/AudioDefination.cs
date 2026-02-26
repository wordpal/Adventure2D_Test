using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioDefination : MonoBehaviour
{
    public PlayAudioEventSO playAudioEventSO;
    public AudioClip audioClip;
    public bool playOnEnable;   //是否在激活时播放

    private void OnEnable()
    {
        if (playOnEnable)
            PlayAudioClip();
    }

    public void PlayAudioClip()
    {
//        Debug.Log($"PlayAudioClip called on {gameObject.name}");
        playAudioEventSO.RaiseEvent(audioClip);
    }
}
