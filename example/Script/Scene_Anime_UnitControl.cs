using System.Collections;
using System.Collections.Generic;
using CrossgateToolkit;
using UnityEngine;

public class Scene_Anime_UnitControl : MonoBehaviour
{
    [Header("单位列表")]
    [Header("跑步单位")] public Prefeb_Unit Unit_Runner;
    [Header("攻击单位")] public Prefeb_Unit Unit_Attacker;
    [Header("防御单位")] public Prefeb_Unit Unit_Defender;
    [Header("特效单位")] public Prefeb_Unit Unit_Effect;
    [Header("动作单位")] public Prefeb_Unit Unit_Action;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void Awake()
    {
        Util.Init();
    }

    public void Play()
    {
        RunnerRun();
        AttackerRun();
        StartCoroutine(PlayControl());
    }

    private void RunnerRun()
    {
        // 动画队列一般用于对动画流程没有严格要求的场景
        // 动画播放过程中,如果某个动画数据缺失或不存在,则会自动跳过该动画并自动播放下一动画
        // 如果需要控制动画的具体播放时机和顺序或控制其他变量行为，建议使用动画回调监听方式处理
        
        // 以队列方式播放特殊三段式跑步动画
        Unit_Runner.animePlayer.Serial = 105000;
        Unit_Runner.animePlayer.DirectionType = Anime.DirectionType.South;
        Unit_Runner.animePlayer.playOnce(Unit_Runner.animePlayer.DirectionType,Anime.ActionType.BeforeRun)
            .nextPlay(Unit_Runner.animePlayer.DirectionType,Anime.ActionType.Run,Anime.PlayType.Once)
            .nextPlay(Unit_Runner.animePlayer.DirectionType,Anime.ActionType.AfterRun,Anime.PlayType.Once)
            .nextPlay(Unit_Runner.animePlayer.DirectionType,Anime.ActionType.Idle,Anime.PlayType.Loop);
    }

    private void AttackerRun()
    {
        // 动画帧事件监听
        // 普通的攻击动画正常会有击中和攻击完成两种事件回调
        // 如:小石像怪,有多段击中效果,最后一段击中则为攻击完成
        // 又如:冰冻魔法,有多段击中效果,最后一段击中则为攻击完成
        
        // 但可以肯定有部分动画因图档版本问题或打包过程中出现动作帧缺失的情况,则有可能无法触发击中或是攻击完成事件
        // 这时可能需要通过使用动画播放完成事件回调来处理

        Unit_Attacker.animePlayer.Serial = 101200;
        Unit_Attacker.animePlayer.DirectionType = Anime.DirectionType.SouthEast;
        
        Unit_Defender.animePlayer.Serial = 105052;
        Unit_Defender.animePlayer.DirectionType = Anime.DirectionType.NorthWest;

        Unit_Attacker.animePlayer.playOnce(Unit_Attacker.animePlayer.DirectionType, Anime.ActionType.Attack, 1f, 
            (effectType) =>
            {
                // 动画事件回调
                
                // 击中或攻击结束效果时,出现被击中动画
                // Unit_Effect.animePlayer.Serial = 110001;
                Unit_Effect.animePlayer.play(110001,Anime.DirectionType.North,Anime.ActionType.Idle,Anime.PlayType.OnceAndDestroy);
                Unit_Effect.RectTransform.position = Unit_Defender.RectTransform.position + new Vector3(0, 0.5f, 0);
                
                // 击中效果
                if (effectType == Anime.EffectType.Hit)
                {
                    // 对于多段攻击可以延迟下一帧的帧率以达到子弹时间或是其他效果
                    Unit_Attacker.animePlayer.DelayPlay(0.5f);
                    // 可以在击中效果中处理受击方的受击动画效果,同样也可以控制帧率速度倍率
                    Unit_Defender.animePlayer.playOnce(Unit_Defender.animePlayer.DirectionType, Anime.ActionType.Hurt,
                        0.2f);
                    // 可以给受击方增加抖动处理等等,这里不再做演示
                }

                // 攻击完成
                if (effectType == Anime.EffectType.HitOver)
                {
                    // 攻击结束阶段可以处理实际的伤害数值显示等
                    Unit_Defender.animePlayer.playOnce(Unit_Defender.animePlayer.DirectionType, Anime.ActionType.Hurt);
                }
            }, (act) =>
            {
                // 动画播放完成回调
                // 此处可以处理动画播放完成后的逻辑处理
                Unit_Attacker.animePlayer.ActionType = Anime.ActionType.Idle;
            });
    }

    IEnumerator PlayControl()
    {
        // 通过协程以及动画播放完成监听的方式控制动画播放
        // 也可通过非协程的多级回调方式控制动画播放,看具体应用场景和编码习惯
        bool runAble = false;
        Unit_Action.animePlayer.Serial = 106270;
        Unit_Action.animePlayer.DirectionType = Anime.DirectionType.SouthWest;
        Unit_Action.animePlayer.playOnce(Anime.DirectionType.SouthWest, Anime.ActionType.Rock, 1f, null,
            (act) => runAble = true);
        yield return new WaitUntil(() => runAble);
        yield return new WaitForSeconds(0.5f);
        runAble = false;
        Unit_Action.animePlayer.playOnce(Anime.DirectionType.SouthWest, Anime.ActionType.Scissors, 1f, null,
            (act) => runAble = true);
        yield return new WaitUntil(() => runAble);
        yield return new WaitForSeconds(0.5f);
        Unit_Action.animePlayer.playOnce(Anime.DirectionType.SouthWest, Anime.ActionType.Paper, 1f, null,
            (act) => runAble = true);
    }
}
