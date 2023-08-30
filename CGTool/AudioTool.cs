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
using System.Collections.Generic;
using UnityEngine;

namespace CGTool
{
    public static class AudioTool
    {

        // 背景音频缓存
        private static Dictionary<int, AudioClip> _bgmDic = new Dictionary<int, AudioClip>();
        // 声效音频缓存
        private static Dictionary<int, AudioClip> _effectDic = new Dictionary<int, AudioClip>();

        public enum Type
        {
            BGM,
            EFFECT
        }
        // 获取指定类型、编号的音频AudioClip
        public static AudioClip GetAudio(Type type, int id)
        {
            AudioClip audioClip;
            Dictionary<int,AudioClip> dic = type == Type.BGM ? _bgmDic : _effectDic;
            if (dic.TryGetValue(id, out audioClip))
            {
                return audioClip;
            }
            else
            {
                Dictionary<int,string> map = type == Type.BGM ? _bgmMap : _effectMap;
                if(map.TryGetValue(id, out string audioPath))
                {
                    audioClip = Resources.Load<AudioClip>(audioPath);
                    return audioClip;
                }
                else
                {
                    // Debug.LogError("Audio not found: " + id);
                }
            }

            return null;
        }

        private static Dictionary<int, string> _bgmMap = new Dictionary<int, string>()
        {
            [200] = "Audio/bgm/cgbgm_m0",
            [201] = "Audio/bgm/cgbgm_m1",
            [202] = "Audio/bgm/cgbgm_m2",
            [203] = "Audio/bgm/cgbgm_m3",
            [204] = "Audio/bgm/cgbgm_m4",
            [209] = "Audio/bgm/cgbgm_f0",
            [210] = "Audio/bgm/cgbgm_f1",
            [211] = "Audio/bgm/cgbgm_f2",
            [212] = "Audio/bgm/cgbgm_d0",
            [213] = "Audio/bgm/cgbgm_d1",
            [214] = "Audio/bgm/cgbgm_d2",
            [215] = "Audio/bgm/cgbgm_d3",
            [216] = "Audio/bgm/cgbgm_d4",
            [205] = "Audio/bgm/cgbgm_b0",
            [206] = "Audio/bgm/cgbgm_b1",
            [207] = "Audio/bgm/cgbgm_b2",
            [208] = "Audio/bgm/cgbgm_b3",
            [217] = "Audio/bgm/cgbgm_t0",
            [219] = "Audio/bgm/exbgm_s0",
            [220] = "Audio/bgm/exbgm_f0",
            [221] = "Audio/bgm/exbgm_m0",
            [222] = "Audio/bgm/v2bgm_f0",
            [223] = "Audio/bgm/v2bgm_m0",
            [224] = "Audio/bgm/v2bgm_ex",
            [225] = "Audio/bgm/v2bgm_ex",
            [226] = "Audio/bgm/puk2_battle1",
            [227] = "Audio/bgm/puk2_battle2",
            [228] = "Audio/bgm/puk2_field1",
            [229] = "Audio/bgm/puk2_mati",
            [230] = "Audio/bgm/puk2_sinden",
            [231] = "Audio/bgm/puk2_yama",
            [232] = "Audio/bgm/puk2_haikyo",
            [233] = "Audio/bgm/puk2_m_town",
            [234] = "Audio/bgm/puk2_OP",
            [235] = "Audio/bgm/puk3_battle1",
            [236] = "Audio/bgm/puk3_battle2",
            [237] = "Audio/bgm/puk3_dungeon",
            [238] = "Audio/bgm/puk3_kame",
            [239] = "Audio/bgm/puk3_kujira",
            [240] = "Audio/bgm/puk3_kumo",
            [241] = "Audio/bgm/puk3_love",
            [242] = "Audio/bgm/puk3_playerbattle",
            [243] = "Audio/bgm/PUK3_title",
        };

        private static Dictionary<int, string> _effectMap = new Dictionary<int, string>()
        {
            [1] = "Audio/se/cgnat00",
            [2] = "Audio/se/cgnat01",
            [3] = "Audio/se/cgnat02",
            [4] = "Audio/se/cgnat03",
            [5] = "Audio/se/cgnat04",
            [6] = "Audio/se/cgnat05a",
            [7] = "Audio/se/cgnat05b",
            [8] = "Audio/se/cgnat06a",
            [9] = "Audio/se/cgnat06b",
            [10] = "Audio/se/cgnat07",
            [11] = "Audio/se/cgnat08",
            [12] = "Audio/se/cgnat09",
            [13] = "Audio/se/cgnat10",
            [14] = "Audio/se/cgnat11",
            [15] = "Audio/se/exnat00",
            [16] = "Audio/se/v2mon150a",
            [17] = "Audio/se/34sand_clock",
            [18] = "Audio/se/35sand_clock",
            [19] = "Audio/se/36wind",
            [20] = "Audio/se/37bird",
            [21] = "Audio/se/puk3_Wind01",
            [22] = "Audio/se/puk3_Wind02",
            [23] = "Audio/se/puk3_Wind03",
            [24] = "Audio/se/puk3_gaya01",
            [25] = "Audio/se/puk3_drop01",
            [26] = "Audio/se/puk3_drop02",
            [51] = "Audio/se/cgsys00",
            [52] = "Audio/se/cgsys01",
            [53] = "Audio/se/cgsys02",
            [54] = "Audio/se/cgsys03",
            [55] = "Audio/se/cgsys04",
            [56] = "Audio/se/cgsys05",
            [57] = "Audio/se/cgsys06",
            [58] = "Audio/se/cgsys07",
            [59] = "Audio/se/cgsys08",
            [60] = "Audio/se/cgsys09",
            [61] = "Audio/se/cgsys10a",
            [62] = "Audio/se/cgsys10b",
            [63] = "Audio/se/cgsys11",
            [64] = "Audio/se/cgsys12",
            [65] = "Audio/se/cgsys13a",
            [66] = "Audio/se/cgsys13b",
            [67] = "Audio/se/cgsys13c",
            [68] = "Audio/se/cgsys14",
            [69] = "Audio/se/cgsys15",
            [71] = "Audio/se/cgsys17",
            [72] = "Audio/se/cgsys18",
            [73] = "Audio/se/cgsys19",
            [74] = "Audio/se/cgsys20",
            [75] = "Audio/se/cgsys21",
            [76] = "Audio/se/cgsys22",
            [77] = "Audio/se/cgsys23",
            [78] = "Audio/se/cgsys24",
            [79] = "Audio/se/cgsys25",
            [101] = "Audio/se/cgply00a",
            [102] = "Audio/se/cgply00b",
            [103] = "Audio/se/cgply01a",
            [104] = "Audio/se/cgply01b",
            [105] = "Audio/se/cgply02a",
            [106] = "Audio/se/cgply02b",
            [107] = "Audio/se/cgply03a",
            [108] = "Audio/se/cgply03b",
            [109] = "Audio/se/cgply04a",
            [110] = "Audio/se/cgply04b",
            [111] = "Audio/se/cgply05a",
            [112] = "Audio/se/cgply05b",
            [113] = "Audio/se/cgply06a1",
            [114] = "Audio/se/cgply06b1",
            [115] = "Audio/se/cgply06a2",
            [116] = "Audio/se/cgply06b2",
            [117] = "Audio/se/cgply07a",
            [118] = "Audio/se/cgply07b",
            [131] = "Audio/se/cgply06a2",
            [132] = "Audio/se/cgply06b2",
            [133] = "Audio/se/cgply11a",
            [134] = "Audio/se/cgply11b",
            [135] = "Audio/se/cgply12a",
            [136] = "Audio/se/cgply12b",
            [137] = "Audio/se/cgply13a",
            [138] = "Audio/se/cgply13b",
            [139] = "Audio/se/cgply14a",
            [140] = "Audio/se/cgply14b",
            [141] = "Audio/se/cgply15",
            [142] = "Audio/se/cgply16",
            [143] = "Audio/se/cgply17",
            [147] = "Audio/se/cgply00a",
            [150] = "Audio/se/cgply11b",
            [151] = "Audio/se/cgmon00a",
            [152] = "Audio/se/cgmon00b",
            [153] = "Audio/se/cgmon01",
            [154] = "Audio/se/cgmon02a",
            [155] = "Audio/se/cgmon02b",
            [156] = "Audio/se/cgmon03b",
            [157] = "Audio/se/cgmon10",
            [158] = "Audio/se/cgmon20",
            [159] = "Audio/se/cgmon24",
            [160] = "Audio/se/cgmon30",
            [161] = "Audio/se/cgmon31",
            [162] = "Audio/se/cgmon41",
            [163] = "Audio/se/cgmon43",
            [164] = "Audio/se/cgmon50a",
            [165] = "Audio/se/cgmon50b",
            [166] = "Audio/se/cgmon51",
            [167] = "Audio/se/cgmon52",
            [168] = "Audio/se/cgmon60",
            [169] = "Audio/se/cgmon61",
            [171] = "Audio/se/cgmon63",
            [172] = "Audio/se/cgmon90",
            [173] = "Audio/se/cgmon91",
            [174] = "Audio/se/cgmon92",
            [175] = "Audio/se/cgmon93",
            [180] = "Audio/se/cgmon_bs1",
            [181] = "Audio/se/cgmon_bs2",
            [182] = "Audio/se/cgmon_bs3",
            [183] = "Audio/se/cgmon_bs4",
            [184] = "Audio/se/cgmon_bh1",
            [185] = "Audio/se/cgmon_bh2",
            [186] = "Audio/se/cgmon_bh3",
            [187] = "Audio/se/cgmon_bh4",
            [190] = "Audio/se/cgmon_m00",
            [191] = "Audio/se/cgmon_m01",
            [192] = "Audio/se/cgmon_m02",
            [198] = "Audio/se/cgmon_sample01",
            [199] = "Audio/se/cgmon_sample02",
            [200] = "Audio/se/cgmon_sample03",
            [201] = "Audio/se/cgbtl00",
            [202] = "Audio/se/cgbtl01",
            [204] = "Audio/se/cgbtl03",
            [205] = "Audio/se/cgbtl04",
            [206] = "Audio/se/cgbtl05",
            [207] = "Audio/se/cgbtl06",
            [208] = "Audio/se/cgbtl07",
            [209] = "Audio/se/cgbtl08",
            [210] = "Audio/se/cgbtl09",
            [211] = "Audio/se/cgbtl10",
            [212] = "Audio/se/cgbtl11",
            [213] = "Audio/se/cgbtl12",
            [214] = "Audio/se/cgbtl13",
            [215] = "Audio/se/cgbtl14",
            [216] = "Audio/se/cgbtl15",
            [217] = "Audio/se/cgbtl16",
            [218] = "Audio/se/cgbtl17",
            [251] = "Audio/se/cgefc00",
            [252] = "Audio/se/cgefc01",
            [253] = "Audio/se/cgefc02",
            [254] = "Audio/se/cgefc03",
            [255] = "Audio/se/cgefc04",
            [256] = "Audio/se/cgefc05",
            [257] = "Audio/se/cgefc06",
            [258] = "Audio/se/cgefc07",
            [259] = "Audio/se/cgefc08",
            [260] = "Audio/se/cgefc09",
            [261] = "Audio/se/cgefc10",
            [262] = "Audio/se/cgefc11",
            [263] = "Audio/se/cgefc12",
            [264] = "Audio/se/cgefc13",
            [266] = "Audio/se/cgefc15",
            [267] = "Audio/se/cgefc16",
            [268] = "Audio/se/cgefc17",
            [269] = "Audio/se/cgefc18",
            [270] = "Audio/se/cgefc19",
            [271] = "Audio/se/cgefc20",
            [272] = "Audio/se/cgefc21",
            [273] = "Audio/se/cgefc22",
            [274] = "Audio/se/cgefc23",
            [275] = "Audio/se/cgefc24",
            [276] = "Audio/se/cgefc25",
            [277] = "Audio/se/cgefc26",
            [278] = "Audio/se/cgefc27",
            [279] = "Audio/se/cgefc28",
            [280] = "Audio/se/cgefc29",
            [281] = "Audio/se/cgefc30",
            [282] = "Audio/se/cgefc31",
            [283] = "Audio/se/cgefc32",
            [284] = "Audio/se/cgefc33",
            [285] = "Audio/se/cgefc34",
            [286] = "Audio/se/cgefc35",
            [287] = "Audio/se/cgefc36",
            [288] = "Audio/se/cgefc37a",
            [289] = "Audio/se/cgefc37b",
            [290] = "Audio/se/cgefc37c",
            [291] = "Audio/se/cgefc38",
            [296] = "Audio/se/v2monex1",
            [297] = "Audio/se/v2monex2",
            [298] = "Audio/se/v2monex3",
            [300] = "Audio/se/v2mon100",
            [301] = "Audio/se/v2mon110",
            [302] = "Audio/se/v2mon111a",
            [303] = "Audio/se/v2mon111b",
            [304] = "Audio/se/v2mon120",
            [305] = "Audio/se/v2mon121a",
            [306] = "Audio/se/v2mon121b",
            [307] = "Audio/se/v2mon121c",
            [308] = "Audio/se/v2mon130",
            [309] = "Audio/se/v2mon140",
            [310] = "Audio/se/v2mon150a",
            [311] = "Audio/se/v2mon150b",
            [312] = "Audio/se/v2mon161",
            [313] = "Audio/se/v2mon170a",
            [314] = "Audio/se/v2mon170b",
            [315] = "Audio/se/v2mon171a",
            [316] = "Audio/se/v2mon171b",
            [317] = "Audio/se/v2mon190",
            [318] = "Audio/se/v2mon191",
            [319] = "Audio/se/v2monex0",
            [400] = "Audio/se/01small_amae_new",
            [401] = "Audio/se/02small_normal",
            [402] = "Audio/se/02small_normal_new",
            [403] = "Audio/se/03small_iyaiya",
            [404] = "Audio/se/03small_iyaiya_new",
            [405] = "Audio/se/04fish_normal",
            [406] = "Audio/se/05fish_shout",
            [407] = "Audio/se/06fish_amae",
            [408] = "Audio/se/09kame_amae",
            [409] = "Audio/se/10yagi_shout",
            [410] = "Audio/se/11yagi_normal",
            [411] = "Audio/se/12yagi_amae",
            [412] = "Audio/se/13bird_normal",
            [413] = "Audio/se/14bird_shout",
            [414] = "Audio/se/15bird_amae",
            [415] = "Audio/se/16fish_normal",
            [416] = "Audio/se/17fish_shout",
            [417] = "Audio/se/18kame_normal",
            [418] = "Audio/se/19kame_shout",
            [419] = "Audio/se/20animal_normal",
            [420] = "Audio/se/21animal_shout",
            [421] = "Audio/se/22bird_normal",
            [422] = "Audio/se/23bird_shout",
            [423] = "Audio/se/24Monstor",
            [424] = "Audio/se/25_1_off",
            [425] = "Audio/se/26ground_on",
            [426] = "Audio/se/27ground_off",
            [427] = "Audio/se/28_water_on",
            [428] = "Audio/se/29_water_of",
            [429] = "Audio/se/30fire_on",
            [430] = "Audio/se/31fire_of",
            [431] = "Audio/se/32_wind_on",
            [432] = "Audio/se/32_wind_on2",
            [433] = "Audio/se/33_wind_off",
            [435] = "Audio/se/36wind",
            [436] = "Audio/se/37bird",
            [437] = "Audio/se/38make_gild",
            [438] = "Audio/se/39levelup",
        };
    }
}