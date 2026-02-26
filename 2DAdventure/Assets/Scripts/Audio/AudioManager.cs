using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    [Header("事件监听")]
    public PlayAudioEventSO FXEvent;
    public PlayAudioEventSO BGMEvent;
    public FloatEventSO volumeChangeEvent;
    public VoidEventSO pauseEvent;

    [Header("广播")]
    public FloatEventSO syncVolumeEvent;

    [Header("组件")]
    public AudioMixer Mixer;
    public AudioSource BGMSource;
    public AudioSource FXSource;

    private void OnEnable()
    {
        volumeChangeEvent.OnEventEaised += OnVoluneChangeEvent;
        FXEvent.OnEventRaised += OnFXEvent;
        BGMEvent.OnEventRaised += OnBGMEvent;
        pauseEvent.OnEventEaised += OnpauseEvent;
    }

    private void OnDisable()
    {
        volumeChangeEvent.OnEventEaised -= OnVoluneChangeEvent;
        FXEvent.OnEventRaised -= OnFXEvent;
        BGMEvent.OnEventRaised -= OnBGMEvent;
        pauseEvent.OnEventEaised -= OnpauseEvent;
    }

    private void OnVoluneChangeEvent(float amount)
    {
        Mixer.SetFloat("MasterVolume", VolumeMapper.SliderToDb(amount));
    }

    private void OnBGMEvent(AudioClip clip)
    {
        BGMSource.clip = clip;
        BGMSource.Play();
    }

    private void OnFXEvent(AudioClip clip)
    {
        FXSource.clip = clip;
        FXSource.PlayOneShot(clip);
    }

    private void OnpauseEvent()
    {
        float amount;
        Mixer.GetFloat("MasterVolume", out amount);

        syncVolumeEvent.RaiseEvent(amount); //Setting中的value与实际同步
    }
}
