/**
 * 魔力宝贝图档解析脚本 - CGTool
 * 
 * @Author  HonorLee (dev@honorlee.me)
 * @Version 1.0 (2023-08-26)
 * @License GPL-3.0
 *
 * AudioTool.cs 音频工具
 * 本工具用于加载音频AudioClip，音频文件位于Assets/Resources/Audio目录下并使用Resources.Load加载
 * 请将Crossgate的音频目录bgm、se拷贝到Assets/Resources/Audio目录下
 * 如有其他需要可调整加载方式
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;

namespace CrossgateToolkit
{
    public class EffectPlayer : MonoBehaviour
    {
        public AudioSource audioSource;
        public delegate void OnAudioPlayEnd();
        public OnAudioPlayEnd onAudioPlayEnd;
        private void Awake()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = false;
            audioSource.playOnAwake = false;
        }

        public void Play(AudioClip clip)
        {
            audioSource.clip = clip;
            audioSource.Play();
            StartCoroutine(wait(clip.length));
        }
        
        
        
        IEnumerator wait(float length)
        {
            yield return new WaitForSeconds(length + 0.3f);
            onAudioPlayEnd?.Invoke();
        }
        
        
    }
    public static class Audio
    {

        // 背景音频缓存
        private static Dictionary<int, AudioClip> _bgmDic = new Dictionary<int, AudioClip>();
        // 声效音频缓存
        private static Dictionary<int, AudioClip> _effectDic = new Dictionary<int, AudioClip>();
        private static Queue<EffectPlayer> _effectPlayerPool = new Queue<EffectPlayer>();
        private static GameObject _effectPlayerContainer;
        private static int lastEffectId = -1;
        private static int lastEffectTime = -1;
        public static float EffectVolume = 1f;
        public enum Type
        {
            BGM,
            EFFECT
        }
        // 播放指定类型、编号的音频AudioClip
        public static void Play(AudioSource audioSource,Type type, int id)
        {
            // 对于同时间大量同一音效的播放，只播放一次
            if (type == Type.EFFECT && id == lastEffectId && Time.time - lastEffectTime < 0.1f) return;
            AudioClip audioClip;
            Dictionary<int,AudioClip> dic = type == Type.BGM ? _bgmDic : _effectDic;
            if (dic.TryGetValue(id, out audioClip))
            {
                _playAudio(type,audioSource, audioClip);
            }
            else
            {
                Dictionary<int,string> map = type == Type.BGM ? _bgmMap : _effectMap;
                if(map.TryGetValue(id, out string audioName))
                {
                    string path = type == Type.BGM ? CGTool.PATH.BGM : CGTool.PATH.AUDIO;
                    if (string.IsNullOrEmpty(path))
                    {
                        path = type == Type.BGM ? "Audio/bgm" : "Audio/se";
                        audioClip = Resources.Load<AudioClip>(path + "/" + audioName);
                        if (audioClip == null) return;
                        
                        dic[id] = audioClip;
                        _playAudio(type,audioSource, audioClip);
                    }
                    else
                    {
                        DirectoryInfo directoryInfo = new DirectoryInfo(path);
                        if(!directoryInfo.Exists) return;
                        FileInfo[] files = directoryInfo.GetFiles(audioName + ".wav", SearchOption.AllDirectories);
                        if (files.Length > 0)
                        {
                            string filePath = files[0].FullName;
                            
                            CoroutineRunner.instance.StartCoroutine(LoadAudioClipAsync(filePath, loadedAudioClip =>
                            {
                                if (loadedAudioClip != null)
                                {
                                    dic[id] = loadedAudioClip;
                                    _playAudio(type,audioSource, loadedAudioClip);
                                }
                            }));
                        }
                    }
                    
                }
            }
        }

        private static void _playAudio(Type type,AudioSource audioSource, AudioClip audioClip)
        {
            if (type == Type.EFFECT && audioSource == null)
            {
                if(_effectPlayerContainer==null) _effectPlayerContainer = new GameObject("EffectPlayerContainer");
                EffectPlayer effectPlayer = null;
                if (_effectPlayerPool.Count > 0)
                {
                    effectPlayer = _effectPlayerPool.Dequeue();
                }
                else
                {
                    effectPlayer = new GameObject("EffectPlayer").AddComponent<EffectPlayer>();
                    effectPlayer.transform.SetParent(_effectPlayerContainer.transform);
                    effectPlayer.onAudioPlayEnd = () =>
                    {
                        effectPlayer.audioSource.clip = null;
                        _effectPlayerPool.Enqueue(effectPlayer);
                    };
                }
                effectPlayer.audioSource.volume = EffectVolume;
                effectPlayer.Play(audioClip);
            }
            else
            {
                audioSource.Stop();
                audioSource.clip = audioClip;
                audioSource.Play();
            }
        }
        
        private delegate void AudioClipLoaded(AudioClip audioClip);
        private static IEnumerator LoadAudioClipAsync(string filePath, AudioClipLoaded onAudioLoaded)
        {
            if (File.Exists(filePath))
            {
                string audioURL = "file://" + filePath;

                using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(audioURL, AudioType.UNKNOWN))
                {
                    yield return www.SendWebRequest();
                    if (www.result == UnityWebRequest.Result.Success)
                    {
                        AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
                        onAudioLoaded?.Invoke(audioClip);
                    }
                    else
                    {
                        onAudioLoaded?.Invoke(null);
                    }
                }
            }
            else
            {
                onAudioLoaded?.Invoke(null);
            }
        }

        private static Dictionary<int, string> _bgmMap = new Dictionary<int, string>()
        {
            [200] = "cgbgm_m0",
            [201] = "cgbgm_m1",
            [202] = "cgbgm_m2",
            [203] = "cgbgm_m3",
            [204] = "cgbgm_m4",
            [209] = "cgbgm_f0",
            [210] = "cgbgm_f1",
            [211] = "cgbgm_f2",
            [212] = "cgbgm_d0",
            [213] = "cgbgm_d1",
            [214] = "cgbgm_d2",
            [215] = "cgbgm_d3",
            [216] = "cgbgm_d4",
            [205] = "cgbgm_b0",
            [206] = "cgbgm_b1",
            [207] = "cgbgm_b2",
            [208] = "cgbgm_b3",
            [217] = "cgbgm_t0",
            [219] = "exbgm_s0",
            [220] = "exbgm_f0",
            [221] = "exbgm_m0",
            [222] = "v2bgm_f0",
            [223] = "v2bgm_m0",
            [224] = "v2bgm_ex",
            [225] = "v2bgm_ex",
            [226] = "puk2_battle1",
            [227] = "puk2_battle2",
            [228] = "puk2_field1",
            [229] = "puk2_mati",
            [230] = "puk2_sinden",
            [231] = "puk2_yama",
            [232] = "puk2_haikyo",
            [233] = "puk2_m_town",
            [234] = "puk2_OP",
            [235] = "puk3_battle1",
            [236] = "puk3_battle2",
            [237] = "puk3_dungeon",
            [238] = "puk3_kame",
            [239] = "puk3_kujira",
            [240] = "puk3_kumo",
            [241] = "puk3_love",
            [242] = "puk3_playerbattle",
            [243] = "PUK3_title",
        };

        private static Dictionary<int, string> _effectMap = new Dictionary<int, string>()
        {
            [1] = "cgnat00",//环境音 流水
            [2] = "cgnat01",//环境音 流水
            [3] = "cgnat02",//环境音 港口船边
            [4] = "cgnat03",//环境音 风声（火山顶那种）
            [5] = "cgnat04",//环境音 （火山）
            [6] = "cgnat05a",//环境音 洞穴滴水
            [7] = "cgnat05b",//环境音 洞穴滴水
            [8] = "cgnat06a",//环境音 草丛
            [9] = "cgnat06b",//环境音 草丛
            [10] = "cgnat07",//环境音 树林
            [11] = "cgnat08",//环境音 树林
            [12] = "cgnat09",//环境音 瀑布
            [13] = "cgnat10",//环境音 篝火
            [14] = "cgnat11",//环境音 火旁
            [15] = "exnat00",//环境音 穿行驶时甲板上
            [16] = "v2mon150a",//环境音 雷声
            [17] = "34sand_clock",//环境音 不知道
            [18] = "35sand_clock",//环境音 不知道
            [19] = "36wind",//环境音 不知道
            [20] = "37bird",//环境音 鸟叫
            [21] = "puk3_Wind01",// 无
            [22] = "puk3_Wind02",// 无
            [23] = "puk3_Wind03",// 无
            [24] = "puk3_gaya01",//无
            [25] = "puk3_drop01",//无
            [26] = "puk3_drop02",//无
            [51] = "cgsys00",//鼠标左键点击
            [52] = "cgsys01",//鼠标左键点击道具
            [53] = "cgsys02",//点击登录
            [54] = "cgsys03",//点击返回
            [55] = "cgsys04",//点选不能使用的道具
            [56] = "cgsys05",//弹窗
            [57] = "cgsys06",//选择道具使用目标
            [58] = "cgsys07",//翻页
            [59] = "cgsys08",//登录角色
            [60] = "cgsys09",//登出
            [61] = "cgsys10a",//暗杀
            [62] = "cgsys10b",//暗杀
            [63] = "cgsys11",//邮件
            [64] = "cgsys12",//宠物升级
            [65] = "cgsys13a",//未知
            [66] = "cgsys13b",//丢钱在地上
            [67] = "cgsys13c",//丢钱在地上
            [68] = "cgsys14",//捡起地上物品
            [69] = "cgsys15",//扔地上物品
            [71] = "cgsys17",//加入队伍
            [72] = "cgsys18",//退出队伍
            [73] = "cgsys19",//换名片
            [74] = "cgsys20",//频道说话
            [75] = "cgsys21",//未知
            [76] = "cgsys22",//打开道具如果里面没东西
            [77] = "cgsys23",//使用料理
            [78] = "cgsys24",//使用道具给队友
            [79] = "cgsys25",//使用道具给队友
            [101] = "cgply00a",
            [102] = "cgply00b",
            [103] = "cgply01a",
            [104] = "cgply01b",
            [105] = "cgply02a",
            [106] = "cgply02b",
            [107] = "cgply03a",//空手MIS
            [108] = "cgply03b",
            [109] = "cgply04a",
            [110] = "cgply04b",
            [111] = "cgply05a",
            [112] = "cgply05b",
            [113] = "cgply06a1",
            [114] = "cgply06b1",
            [115] = "cgply06a2",
            [116] = "cgply06b2",//冰冻5-7（冰的声音先出 然后是这个）
            [117] = "cgply07a",
            [118] = "cgply07b",
            [131] = "cgply06a2",
            [132] = "cgply06b2",
            [133] = "cgply11a",
            [134] = "cgply11b",
            [135] = "cgply12a",
            [136] = "cgply12b",
            [137] = "cgply13a",
            [138] = "cgply13b",
            [139] = "cgply14a",
            [140] = "cgply14b",
            [141] = "cgply15",
            [142] = "cgply16",
            [143] = "cgply17",
            [147] = "cgply00a",
            [150] = "cgply11b",
            [151] = "cgmon00a",
            [152] = "cgmon00b",
            [153] = "cgmon01",
            [154] = "cgmon02a",
            [155] = "cgmon02b",
            [156] = "cgmon03b",
            [157] = "cgmon10",
            [158] = "cgmon20",
            [159] = "cgmon24",
            [160] = "cgmon30",
            [161] = "cgmon31",
            [162] = "cgmon41",
            [163] = "cgmon43",
            [164] = "cgmon50a",
            [165] = "cgmon50b",
            [166] = "cgmon51",
            [167] = "cgmon52",
            [168] = "cgmon60",
            [169] = "cgmon61",
            [171] = "cgmon63",
            [172] = "cgmon90",
            [173] = "cgmon91",
            [174] = "cgmon92",
            [175] = "cgmon93",
            [180] = "cgmon_bs1",
            [181] = "cgmon_bs2",
            [182] = "cgmon_bs3",
            [183] = "cgmon_bs4",
            [184] = "cgmon_bh1",
            [185] = "cgmon_bh2",
            [186] = "cgmon_bh3",
            [187] = "cgmon_bh4",
            [190] = "cgmon_m00",
            [191] = "cgmon_m01",
            [192] = "cgmon_m02",
            [198] = "cgmon_sample01",
            [199] = "cgmon_sample02",
            [200] = "cgmon_sample03",
            [201] = "cgbtl00",//遇敌进入战斗
            [202] = "cgbtl01",//倒数快到时
            [204] = "cgbtl03",//召唤宠物
            [205] = "cgbtl04",//收回宠物
            [206] = "cgbtl05",//战斗中技能升级
            [207] = "cgbtl06",//击飞
            [208] = "cgbtl07",//击飞过程碰壁
            [209] = "cgbtl08",//战斗中换武器
            [210] = "cgbtl09",//战斗中换位置
            [211] = "cgbtl10",//逃跑
            [212] = "cgbtl11",//封印扔卡
            [213] = "cgbtl12",//封印捕捉中
            [214] = "cgbtl13",//装备击碎
            [215] = "cgbtl14",//死亡
            [216] = "cgbtl15",//未知
            [217] = "cgbtl16",//使用道具前置
            [218] = "cgbtl17",//怪物死亡气泡
            [251] = "cgefc00",//物理技能使用前置（绿光）
            [252] = "cgefc01",//未知
            [253] = "cgefc02",//反击
            [254] = "cgefc03",//气功弹飞的过程中
            [255] = "cgefc04",//气功弹打到身上
            [256] = "cgefc05",//明镜止水出恢复数字时
            [257] = "cgefc06",//偷窃成功
            [258] = "cgefc07",//偷窃失败
            [259] = "cgefc08",//魔法施法
            [260] = "cgefc09",//状态技能 各种吸无反施法
            [261] = "cgefc10",//传教施法
            [262] = "cgefc11",//陨石魔法下落
            [263] = "cgefc12",//10级陨石打身上时
            [264] = "cgefc13",//冰冻魔法为造成伤害的前置动画声音
            [266] = "cgefc15",//1-4火
            [267] = "cgefc16",//5-7火
            [268] = "cgefc17",//1-4风
            [269] = "cgefc18",//5-7风
            [270] = "cgefc19",//吸血造成伤害时
            [271] = "cgefc20",//吸血回复血量时
            [272] = "cgefc21",//咒术怪物时气泡
            [273] = "cgefc22",//毒咒掉血时
            [274] = "cgefc23",//酒醉掉魔时
            [275] = "cgefc24",//祈祷魔封动画展开时
            [276] = "cgefc25",//属性反转
            [277] = "cgefc26",//功无攻反等套身上时
            [278] = "cgefc27",//补血魔法补血时
            [279] = "cgefc28",//恢复魔法恢复时
            [280] = "cgefc29",//气绝
            [281] = "cgefc30",//洁净
            [282] = "cgefc31",//10火
            [283] = "cgefc32",//跳舞10级
            [284] = "cgefc33",//祭死
            [285] = "cgefc34",//未知
            [286] = "cgefc35",//功无魔无抵挡时
            [287] = "cgefc36",//正常反击
            [288] = "cgefc37a",//自爆变大中
            [289] = "cgefc37b",//自爆准备爆炸
            [290] = "cgefc37c",//自爆爆炸
            [291] = "cgefc38",//恢复套的过程
            [296] = "v2monex1",
            [297] = "v2monex2",
            [298] = "v2monex3",
            [300] = "v2mon100",
            [301] = "v2mon110",
            [302] = "v2mon111a",
            [303] = "v2mon111b",
            [304] = "v2mon120",
            [305] = "v2mon121a",
            [306] = "v2mon121b",
            [307] = "v2mon121c",
            [308] = "v2mon130",
            [309] = "v2mon140",
            [310] = "v2mon150a",
            [311] = "v2mon150b",
            [312] = "v2mon161",
            [313] = "v2mon170a",
            [314] = "v2mon170b",
            [315] = "v2mon171a",
            [316] = "v2mon171b",
            [317] = "v2mon190",
            [318] = "v2mon191",
            [319] = "v2monex0",
            [400] = "01small_amae_new",
            [401] = "02small_normal",
            [402] = "02small_normal_new",
            [403] = "03small_iyaiya",
            [404] = "03small_iyaiya_new",
            [405] = "04fish_normal",
            [406] = "05fish_shout",
            [407] = "06fish_amae",
            [408] = "09kame_amae",
            [409] = "10yagi_shout",
            [410] = "11yagi_normal",
            [411] = "12yagi_amae",
            [412] = "13bird_normal",
            [413] = "14bird_shout",
            [414] = "15bird_amae",
            [415] = "16fish_normal",
            [416] = "17fish_shout",
            [417] = "18kame_normal",
            [418] = "19kame_shout",
            [419] = "20animal_normal",
            [420] = "21animal_shout",
            [421] = "22bird_normal",
            [422] = "23bird_shout",
            [423] = "24Monstor",
            [424] = "25_1_off",
            [425] = "26ground_on",
            [426] = "27ground_off",
            [427] = "28_water_on",
            [428] = "29_water_of",
            [429] = "30fire_on",
            [430] = "31fire_of",
            [431] = "32_wind_on",
            [432] = "32_wind_on2",
            [433] = "33_wind_off",
            [435] = "36wind",
            [436] = "37bird",
            [437] = "38make_gild",//升级
            [438] = "39levelup",
            [500] = "cgply10a",
            [501] = "cgply10b",
        };
    }
}