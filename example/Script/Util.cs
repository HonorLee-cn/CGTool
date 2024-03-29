using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CrossgateToolkit;

public class Util

{
    public static void Init()
    {
        Application.targetFrameRate = 60;
        CGTool.PATH = new CGTool.CGPath()
        {
            BIN = Application.persistentDataPath + "/bin",
            PAL = Application.persistentDataPath + "/pal",
            MAP = Application.persistentDataPath + "/map",
            BGM = Application.persistentDataPath + "/bgm",
            AUDIO = Application.persistentDataPath + "/se"
        };
        CGTool.Init();
    }
    // 获取距离目标单位直线方向一定距离的位置
    public static Vector2 GetTargetNearPosition(Vector2 fromPosition,Vector2 targetPosition,float distance)
    {
        Vector2 direction = targetPosition - fromPosition;
        Vector2 normalizedDirection = direction.normalized;

        // Vector2 point = targetPosition - normalizedDirection * distance;

        // 移动 C 点到圆上，使得 C 到 A 的距离等于半径 X
        // float distanceToMove = Vector2.Distance(point, fromPosition) - distance;
        Vector2 adjustedC = targetPosition - normalizedDirection * distance;

        return adjustedC;
    }
    
    //获取方向增量
    public static Vector2Int GetDirectionVector(Anime.DirectionType direction)
    {
        Vector2Int vector2 = Vector2Int.zero;
        switch (direction)
        {
            case Anime.DirectionType.North:
                vector2.x = -1;
                vector2.y = 1;
                break;
            case Anime.DirectionType.NorthEast:
                vector2.x = 0;
                vector2.y = 1;
                break;
            case Anime.DirectionType.East:
                vector2.x = 1;
                vector2.y = 1;
                break;
            case Anime.DirectionType.SouthEast:
                vector2.x = 1;
                vector2.y = 0;
                break;
            case Anime.DirectionType.South:
                vector2.x = 1;
                vector2.y = -1;
                break;
            case Anime.DirectionType.SouthWest:
                vector2.x = 0;
                vector2.y = -1;
                break;
            case Anime.DirectionType.West:
                vector2.x = -1;
                vector2.y = -1;
                break;
            case Anime.DirectionType.NorthWest:
                vector2.x = -1;
                vector2.y = 0;
                break;
        }

        return vector2;
    }
    
    // 获取方向上某距离的点
    public static Vector2 GetDirectionPoint(Vector2 fromPosition,Anime.DirectionType directionType,float distance)
    {
        Vector2 vector2 = GetDirectionVector(directionType);
        vector2 = vector2.normalized;
        Vector2 point = fromPosition + vector2 * distance;
        return point;
    }
    
    // 获取目标单位方向角度
    public static float GetTargetAngle(Vector2 fromPosition,Vector2 targetPosition)
    {
        Vector2 diff = targetPosition - fromPosition;
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        angle = angle - 90;
        return angle;
    }
    
    // 获取目标单位方向
    public static Anime.DirectionType GetTargetDirection(Vector2 fromPosition,Vector2 targetPosition)
    {
        Vector2 diff = targetPosition - fromPosition;
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        angle = angle - 90;
            
        if (angle <= 25 && angle > -25) return Anime.DirectionType.NorthEast;
        if (angle <= -25 && angle > -75) return Anime.DirectionType.East;
        if (angle <= -75 && angle > -105) return Anime.DirectionType.SouthEast;
        if (angle <= -105 && angle > -155) return Anime.DirectionType.South;
        if (angle <= -155 && angle > -205) return Anime.DirectionType.SouthWest;
        if (angle <= -205 && angle > -255) return Anime.DirectionType.West;
        if (angle <= -255 && angle > -285) return Anime.DirectionType.NorthWest;
        if (angle <= -285 && angle > -335) return Anime.DirectionType.North;
        if (angle <= -335 && angle > -385) return Anime.DirectionType.NorthEast;
            
        if (angle > 25 && angle < 75) return Anime.DirectionType.North;
        if (angle > 75 && angle < 105) return Anime.DirectionType.NorthWest;
        if (angle > 105 && angle < 155) return Anime.DirectionType.West;
        if (angle > 155 && angle < 205) return Anime.DirectionType.SouthWest;
        if (angle > 205 && angle < 255) return Anime.DirectionType.South;
        if (angle > 255 && angle < 285) return Anime.DirectionType.SouthEast;
        if (angle > 285 && angle < 335) return Anime.DirectionType.East;
        if (angle > 335 && angle < 385) return Anime.DirectionType.NorthEast;
            
        return Anime.DirectionType.East;
    }
    
    // 获取反方向
    public static Anime.DirectionType GetReverseDirection(Anime.DirectionType directionType)
    {
        switch (directionType)
        {
            case Anime.DirectionType.North:
                return Anime.DirectionType.South;
            case Anime.DirectionType.NorthEast:
                return Anime.DirectionType.SouthWest;
            case Anime.DirectionType.East:
                return Anime.DirectionType.West;
            case Anime.DirectionType.SouthEast:
                return Anime.DirectionType.NorthWest;
            case Anime.DirectionType.South:
                return Anime.DirectionType.North;
            case Anime.DirectionType.SouthWest:
                return Anime.DirectionType.NorthEast;
            case Anime.DirectionType.West:
                return Anime.DirectionType.East;
            case Anime.DirectionType.NorthWest:
                return Anime.DirectionType.SouthEast;
            default:
                return Anime.DirectionType.East;
        }
    }
    
    public static Anime.DirectionType GetRandomDirection()
    {
        int random = Random.Range(0, 8);
        switch (random)
        {
            case 0:
                return Anime.DirectionType.North;
            case 1:
                return Anime.DirectionType.NorthEast;
            case 2:
                return Anime.DirectionType.East;
            case 3:
                return Anime.DirectionType.SouthEast;
            case 4:
                return Anime.DirectionType.South;
            case 5:
                return Anime.DirectionType.SouthWest;
            case 6:
                return Anime.DirectionType.West;
            case 7:
                return Anime.DirectionType.NorthWest;
            default:
                return Anime.DirectionType.East;
        }
    }
    
    // 获取上一个方向 左旋
    public static Anime.DirectionType GetPrevDirection(Anime.DirectionType directionType)
    {
        switch (directionType)
        {
            case Anime.DirectionType.North:
                return Anime.DirectionType.NorthWest;
            case Anime.DirectionType.NorthEast:
                return Anime.DirectionType.North;
            case Anime.DirectionType.East:
                return Anime.DirectionType.NorthEast;
            case Anime.DirectionType.SouthEast:
                return Anime.DirectionType.East;
            case Anime.DirectionType.South:
                return Anime.DirectionType.SouthEast;
            case Anime.DirectionType.SouthWest:
                return Anime.DirectionType.South;
            case Anime.DirectionType.West:
                return Anime.DirectionType.SouthWest;
            case Anime.DirectionType.NorthWest:
                return Anime.DirectionType.West;
            default:
                return Anime.DirectionType.East;
        }
    }
    
    public static Anime.DirectionType GetDoublePrevDirection(Anime.DirectionType directionType)
    {
        switch (directionType)
        {
            case Anime.DirectionType.North:
                return Anime.DirectionType.West;
            case Anime.DirectionType.NorthEast:
                return Anime.DirectionType.NorthWest;
            case Anime.DirectionType.East:
                return Anime.DirectionType.North;
            case Anime.DirectionType.SouthEast:
                return Anime.DirectionType.NorthEast;
            case Anime.DirectionType.South:
                return Anime.DirectionType.East;
            case Anime.DirectionType.SouthWest:
                return Anime.DirectionType.SouthEast;
            case Anime.DirectionType.West:
                return Anime.DirectionType.South;
            case Anime.DirectionType.NorthWest:
                return Anime.DirectionType.SouthWest;
            default:
                return Anime.DirectionType.East;
        }
    }
    
    // 获取下一个方向 右旋
    public static Anime.DirectionType GetNextDirection(Anime.DirectionType directionType)
    {
        switch (directionType)
        {
            case Anime.DirectionType.North:
                return Anime.DirectionType.NorthEast;
            case Anime.DirectionType.NorthEast:
                return Anime.DirectionType.East;
            case Anime.DirectionType.East:
                return Anime.DirectionType.SouthEast;
            case Anime.DirectionType.SouthEast:
                return Anime.DirectionType.South;
            case Anime.DirectionType.South:
                return Anime.DirectionType.SouthWest;
            case Anime.DirectionType.SouthWest:
                return Anime.DirectionType.West;
            case Anime.DirectionType.West:
                return Anime.DirectionType.NorthWest;
            case Anime.DirectionType.NorthWest:
                return Anime.DirectionType.North;
            default:
                return Anime.DirectionType.East;
        }
    }
    
    // 获取下第二个方向
    public static Anime.DirectionType GetDoubleNextDirection(Anime.DirectionType directionType)
    {
        switch (directionType)
        {
            case Anime.DirectionType.North:
                return Anime.DirectionType.East;
            case Anime.DirectionType.NorthEast:
                return Anime.DirectionType.SouthEast;
            case Anime.DirectionType.East:
                return Anime.DirectionType.South;
            case Anime.DirectionType.SouthEast:
                return Anime.DirectionType.SouthWest;
            case Anime.DirectionType.South:
                return Anime.DirectionType.West;
            case Anime.DirectionType.SouthWest:
                return Anime.DirectionType.NorthWest;
            case Anime.DirectionType.West:
                return Anime.DirectionType.North;
            case Anime.DirectionType.NorthWest:
                return Anime.DirectionType.NorthEast;
            default:
                return Anime.DirectionType.East;
        }
    }
    
    public static List<uint> TestSerial = new List<uint>()
        {
            // 旧版单位
            // 100000, 100001, 100002, 100003, 100004, 100005, 100006, 100007, 100008, 100009, 100010, 100011, 100012,
            // 100013, 100014, 100015, 100016, 100017, 100018, 100019, 100020, 100021, 100022, 100023, 100025, 100026,
            // 100027, 100028, 100029, 100030, 100031, 100032, 100033, 100034, 100035, 100036, 100037, 100038, 100039,
            // 100040, 100041, 100042, 100043, 100044, 100045, 100046, 100047, 100048, 100050, 100051, 100052, 100053,
            // 100054, 100055, 100056, 100057, 100058, 100059, 100060, 100061, 100062, 100063, 100064, 100065, 100066,
            // 100067, 100068, 100069, 100070, 100071, 100072, 100073, 100075, 100076, 100077, 100078, 100079, 100080,
            // 100081, 100082, 100083, 100084, 100085, 100086, 100087, 100088, 100089, 100090, 100091, 100092, 100093,
            // 100094, 100095, 100096, 100097, 100098, 100100, 100101, 100102, 100103, 100104, 100105, 100106, 100107,
            // 100108, 100109, 100110, 100111, 100112, 100113, 100114, 100115, 100116, 100117, 100118, 100119, 100120,
            // 100121, 100122, 100123, 100125, 100126, 100127, 100128, 100129, 100130, 100131, 100132, 100133, 100134,
            // 100135, 100136, 100137, 100138, 100139, 100140, 100141, 100142, 100143, 100144, 100145, 100146, 100147,
            // 100148, 100150, 100151, 100152, 100153, 100154, 100155, 100156, 100157, 100158, 100159, 100160, 100161,
            // 100162, 100163, 100164, 100165, 100166, 100167, 100168, 100169, 100170, 100171, 100172, 100173, 100250,
            // 100251, 100252, 100253, 100254, 100255, 100256, 100257, 100258, 100259, 100260, 100261, 100262, 100263,
            // 100264, 100265, 100266, 100267, 100268, 100269, 100270, 100271, 100272, 100273, 100275, 100276, 100277,
            // 100278, 100279, 100280, 100281, 100282, 100283, 100284, 100285, 100286, 100287, 100288, 100289, 100290,
            // 100291, 100292, 100293, 100294, 100295, 100296, 100297, 100298, 100300, 100301, 100302, 100303, 100304,
            // 100305, 100306, 100307, 100308, 100309, 100310, 100311, 100312, 100313, 100314, 100315, 100316, 100317,
            // 100318, 100319, 100320, 100321, 100322, 100323, 100325, 100326, 100327, 100328, 100329, 100330, 100331,
            // 100332, 100333, 100334, 100335, 100336, 100337, 100338, 100339, 100340, 100341, 100342, 100343, 100344,
            // 100345, 100346, 100347, 100348, 100350, 100351, 100352, 100353, 100354, 100355, 100356, 100357, 100358,
            // 100359, 100360, 100361, 100362, 100363, 100364, 100365, 100366, 100367, 100368, 100369, 100370, 100371,
            // 100372, 100373, 100375, 100376, 100377, 100378, 100379, 100380, 100381, 100382, 100383, 100384, 100385,
            // 100386, 100387, 100388, 100389, 100390, 100391, 100392, 100393, 100394, 100395, 100396, 100397, 100398,
            // 100400, 100401, 100402, 100403, 100404, 100405, 100406, 100407, 100408, 100409, 100410, 100411, 100412,
            // 100413, 100414, 100415, 100416, 100417, 100418, 100419, 100420, 100421, 100422, 100423, 100425, 100426,
            // 100427, 100428, 100429, 100430, 100450, 100451, 100452, 100453, 100454, 100455, 100475, 100476, 100477,
            // 100478, 100479, 100480, 100500, 100501, 100502, 100503, 100504, 100505, 100525, 100526, 100527, 100528,
            // 100529, 100530, 100550, 100551, 100552, 100553, 100554, 100555, 100575, 100576, 100577, 100578, 100579,
            // 100580, 100600, 100601, 100602, 100603, 100604, 100605, 100625, 100626, 100627, 100628, 100629, 100630,
            // 100650, 100651, 100652, 100653, 100654, 100655, 100675, 100676, 100677, 100678, 100679, 100680, 100700,
            // 100701, 100702, 100703, 100704, 100705, 100725, 100726, 100727, 100728, 100729, 100730, 100750, 100751,
            // 100752, 100753, 100754, 100755, 100800, 100801, 100802, 100803, 100804, 100805, 100900, 100901,

            // 新版单位
            105000, 105001, 105002, 105003, 105004, 105005, 105006, 105007, 105008, 105009, 105010, 105011, 105012,
            105013, 105014, 105014, 105016, 105017, 105018, 105019, 105020, 105021, 105022, 105023, 105025, 105026,
            105027, 105028, 105029, 105030, 105031, 105032, 105033, 105034, 105035, 105036, 105037, 105038, 105039,
            105040, 105041, 105042, 105043, 105044, 105045, 105046, 105047, 105048, 105050, 105051, 105052, 105053,
            105054, 105055, 105056, 105057, 105058, 105059, 105060, 105061, 105062, 105063, 105064, 105065, 105066,
            105067, 105068, 105069, 105070, 105071, 105072, 105073, 105075, 105076, 105077, 105078, 105079, 105080,
            105081, 105082, 105083, 105084, 105085, 105086, 105087, 105088, 105089, 105090, 105091, 105092, 105093,
            105094, 105095, 105096, 105097, 105098, 105100, 105101, 105102, 105103, 105104, 105105, 105106, 105107,
            105108, 105109, 105110, 105111, 105112, 105113, 105114, 105115, 105116, 105117, 105118, 105119, 105120,
            105121, 105122, 105123, 105125, 105126, 105127, 105128, 105129, 105130, 105131, 105132, 105133, 105134,
            105135, 105136, 105137, 105138, 105139, 105140, 105141, 105142, 105143, 105144, 105145, 105146, 105147,
            105148, 105150, 105151, 105152, 105153, 105154, 105155, 105156, 105157, 105158, 105159, 105160, 105161,
            105162, 105163, 105164, 105165, 105166, 105167, 105168, 105169, 105170, 105171, 105172, 105173, 105250,
            105251, 105252, 105253, 105254, 105255, 105256, 105257, 105258, 105259, 105260, 105261, 105262, 105263,
            105264, 105265, 105266, 105267, 105268, 105269, 105270, 105271, 105272, 105273, 105275, 105276, 105277,
            105278, 105279, 105280, 105281, 105282, 105283, 105284, 105285, 105286, 105287, 105288, 105289, 105290,
            105291, 105292, 105293, 105294, 105295, 105296, 105297, 105298, 105300, 105301, 105302, 105303, 105304,
            105305, 105306, 105307, 105308, 105309, 105310, 105311, 105312, 105313, 105314, 105315, 105316, 105317,
            105318, 105319, 105320, 105321, 105322, 105323, 105325, 105326, 105327, 105328, 105329, 105330, 105331, 105332,
            105333, 105334, 105335, 105336, 105337, 105338, 105339, 105340, 105341, 105342, 105343, 105344, 105345,
            105346, 105347, 105348, 105350, 105351, 105352, 105353, 105354, 105355, 105356, 105357, 105358, 105359,
            105360, 105361, 105362, 105363, 105364, 105365, 105366, 105367, 105368, 105369, 105370, 105371, 105372,
            105373, 105375, 105376, 105377, 105378, 105379, 105380, 105381, 105382, 105383, 105384, 105385, 105386,
            105387, 105388, 105389, 105390, 105391, 105392, 105393, 105394, 105395, 105396, 105397, 105398, 105400,
            105401, 105402, 105403, 105404, 105405, 105406, 105407, 105408, 105409, 105410, 105411, 105412, 105413,
            105414, 105415, 105416, 105417, 105418, 105419, 105420, 105421, 105422, 105423, 106000, 106001, 106002,
            106003, 106004, 106005, 106006, 106007, 106008, 106009, 106010, 106011, 106012, 106013, 106014, 106015,
            106016, 106017, 106018, 106019, 106020, 106021, 106022, 106023, 106025, 106026, 106027, 106028, 106029,
            106030, 106031, 106032, 106033, 106034, 106035, 106036, 106037, 106038, 106039, 106040, 106041, 106042,
            106043, 106044, 106045, 106046, 106047, 106048, 106050, 106051, 106052, 106053, 106054, 106055, 106056,
            106057, 106058, 106059, 106060, 106061, 106062, 106063, 106064, 106065, 106066, 106067, 106068, 106069,
            106070, 106071, 106072, 106073, 106075, 106076, 106077, 106078, 106079, 106080, 106081, 106082, 106083,
            106084, 106085, 106086, 106087, 106088, 106089, 106090, 106091, 106092, 106093, 106094, 106095, 106096,
            106097, 106098, 106100, 106101, 106102, 106103, 106104, 106105, 106106, 106107, 106108, 106109, 106110,
            106111, 106112, 106113, 106114, 106115, 106116, 106117, 106118, 106119, 106120, 106121, 106122, 106123,
            106125, 106126, 106127, 106128, 106129, 106130, 106131, 106132, 106133, 106134, 106135, 106136, 106137,
            106138, 106139, 106140, 106141, 106142, 106143, 106144, 106145, 106146, 106147, 106148, 106150, 106151,
            106152, 106153, 106154, 106155, 106156, 106157, 106158, 106159, 106160, 106161, 106162, 106163, 106164,
            106165, 106166, 106167, 106168, 106169, 106170, 106171, 106172, 106173, 106250, 106251, 106252, 106253,
            106254, 106255, 106256, 106257, 106258, 106259, 106260, 106261, 106262, 106263, 106264, 106265, 106266,
            106267, 106268, 106269, 106270, 106271, 106272, 106273, 106275, 106276, 106277, 106278, 106279, 106280,
            106281, 106282, 106283, 106284, 106285, 106286, 106287, 106288, 106289, 106290, 106291, 106292, 106293,
            106294, 106295, 106296, 106297, 106298, 106300, 106301, 106302, 106303, 106304, 106305, 106306, 106307,
            106308, 106309, 106310, 106311, 106312, 106313, 106314, 106315, 106316, 106317, 106318, 106319, 106320,
            106321, 106322, 106323, 106325, 106326, 106327, 106328, 106329, 106330, 106331, 106332, 106333, 106334,
            106335, 106336, 106337, 106338, 106339, 106340, 106341, 106342, 106343, 106344, 106345, 106346, 106347,
            106348, 106350, 106351, 106352, 106353, 106354, 106355, 106356, 106357, 106358, 106359, 106360, 106361,
            106362, 106363, 106364, 106365, 106366, 106367, 106368, 106369, 106370, 106371, 106372, 106373, 106375,
            106376, 106377, 106378, 106379, 106380, 106381, 106382, 106383, 106384, 106385, 106386, 106387, 106388,
            106389, 106390, 106391, 106392, 106393, 106394, 106395, 106396, 106397, 106398, 106400, 106401, 106402,
            106403, 106404, 106405, 106406, 106407, 106408, 106409, 106410, 106411, 106412, 106413, 106414, 106415,
            106416, 106417, 106418, 106419, 106420, 106421, 106422, 106423, 106425, 106426, 106427, 106428, 106429,
            106430, 106450, 106451, 106452, 106453, 106454, 106455, 106475, 106476, 106477, 106478, 106479, 106480,
            106500, 106501, 106502, 106503, 106504, 106505, 106525, 106526, 106527, 106528, 106529, 106530, 106550,
            106551, 106552, 106553, 106554, 106555, 106575, 106576, 106577, 106578, 106579, 106580, 106600, 106601,
            106602, 106603, 106604, 106605, 106625, 106626, 106627, 106628, 106629, 106630, 106650, 106651, 106652,
            106653, 106654, 106655, 106675, 106676, 106677, 106678, 106679, 106680, 106700, 106701, 106702, 106703,
            106704, 106705, 106725, 106726, 106727, 106728, 106729, 106730, 106750, 106751, 106752, 106753, 106754,
            106755
        };
    public static uint GetRandomSerial()
    {
        int random = Random.Range(0, TestSerial.Count);
        return TestSerial[random];
    }

    public static Anime.ActionType GetNextAction(Anime.ActionType actionType)
    {
        int action = (int)actionType;
        action++;
        if (action > 20) action = 0;
        return (Anime.ActionType)action;
    }
}
