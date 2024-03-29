using System;
using System.Collections;
using System.Collections.Generic;
using CrossgateToolkit;
using UnityEngine;

public class Prefeb_Unit : MonoBehaviour
{
    public AnimePlayer animePlayer;

    public RectTransform RectTransform;

    private void Awake()
    {
        RectTransform = GetComponent<RectTransform>();
        // 可以监听动画播放帧中的音频事件并播放音效
        animePlayer.onAudioListener = (audioID) =>
        {
            // 播放音效
            // 1.获取AudioClip并自行交给AudioSource播放,推荐背景音乐等长时间循环音频交给AudioSource播放
            // AudioClip audioClip = CrossgateToolkit.Audio.Play([Your AudioSource],Audio.Type.BGM,audioID);
            
            // 2.直接播放音效,推荐技能、人物等一次性特效音频直接交给Audio类播放和管控,即不指定AudioSource
            CrossgateToolkit.Audio.Play(null,Audio.Type.EFFECT,audioID);
        };
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
