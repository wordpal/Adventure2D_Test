using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Event/PlayAudioEventSO")]
public class PlayAudioEventSO : ScriptableObject
{
    public UnityAction<AudioClip> OnEventRaised;    //Ö´ÐÐ

    public void RaiseEvent(AudioClip audioClip)     //¹ã²¥
    {
        OnEventRaised?.Invoke(audioClip);
    }
}
