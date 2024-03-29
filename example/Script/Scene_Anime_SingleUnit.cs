using System;
using System.Collections;
using System.Collections.Generic;
using CrossgateToolkit;
using UnityEngine;
using UnityEngine.UI;

public class Scene_SingleUnit : MonoBehaviour
{
    public AnimePlayer animePlayer;

    public Anime.DirectionType DirectionType;
    
    public Anime.ActionType ActionType;

    public uint Serial;

    public Text[] Texts;
    // Start is called before the first frame update
    private void Awake()
    {
        Util.Init();
        Serial = Util.GetRandomSerial();
        DirectionType = Anime.DirectionType.SouthWest;
        ActionType = Anime.ActionType.Idle;
    }

    void Start()
    {
        animePlayer.play(Serial,DirectionType,ActionType,Anime.PlayType.Loop);
        UpdateText();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ChangeUint()
    {
        Serial = Util.GetRandomSerial();
        animePlayer.Serial = Serial;
        UpdateText();
    }

    public void ChangeDirection()
    {
        DirectionType = Util.GetNextDirection(DirectionType);
        animePlayer.DirectionType = DirectionType;
        UpdateText();
    }

    public void ChangeAction()
    {
        // 注意不是所有角色都有 BeforeRun/AfterRun 或其他动作
        ActionType = Util.GetNextAction(ActionType);
        animePlayer.ActionType = ActionType;
        UpdateText();
    }

    public void UpdateText()
    {
        Texts[0].text = Serial.ToString();
        Texts[1].text = DirectionType.ToString();
        Texts[2].text = ActionType.ToString();
    }
}
