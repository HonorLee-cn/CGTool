using System;
using System.Collections;
using System.Collections.Generic;
using CrossgateToolkit;
using UnityEngine;
using Random = UnityEngine.Random;

public class Scene_MultiUnit : MonoBehaviour
{
    public Transform UnitContainer;
    public Prefeb_Unit unitPrefeb;
    List<Prefeb_Unit> units = new List<Prefeb_Unit>();

    private void Awake()
    {
        Util.Init();
    }

    void Start()
    {
        int x = 18;
        int y = 5;
        int width = x * 60;
        int height = y * 100;
        for (int i = 0; i < x; i++)
        {
            for (int j = 0; j < y; j++) 
            {
                Prefeb_Unit unit = Instantiate(unitPrefeb, UnitContainer);
                unit.name = "Unit_" + i + "_" + j;
                unit.RectTransform.anchoredPosition =
                    new Vector2(-width / 2f + i * 60, -height / 2f + j * 100 + 10);
                unit.animePlayer.Serial = Util.GetRandomSerial();
                unit.animePlayer.DirectionType = Util.GetRandomDirection();
                unit.animePlayer.ActionType = (Anime.ActionType)Random.Range(0, 21);
                units.Add(unit);
            }
        }
    }
    
    public void ChangeSerial()
    {
        foreach (var unit in units)
        {
            unit.animePlayer.Serial = Util.GetRandomSerial();
        }
    }
    
    public void ChangeDirection()
    {
        foreach (var unit in units)
        {
            unit.animePlayer.DirectionType = Util.GetRandomDirection();
        }
    }
    
    public void ChangeAction()
    {
        foreach (var unit in units)
        {
            unit.animePlayer.ActionType = (Anime.ActionType)Random.Range(0, 21);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
