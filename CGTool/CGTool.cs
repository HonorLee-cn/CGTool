/**
 * 魔力宝贝图档解析脚本 - CGTool
 * 
 * @Author  HonorLee (dev@honorlee.me)
 * @Version 1.0 (2023-04-15)
 * @License GPL-3.0
 *
 * CGTool.cs 入口文件
 */

using UnityEngine;

namespace CGTool
{
    public class CGTool : MonoBehaviour
    {
        public readonly static bool DEBUG = true;
        //Bin基础目录
        public readonly static string BaseFolder = System.Environment.CurrentDirectory + "/bin";
        //Palet调色板目录
        public readonly static string PaletFolder = BaseFolder + "/pal";
        //Datas目录
        public readonly static string DataFolder = System.Environment.CurrentDirectory + "/data";
        //Map地图文件目录
        public readonly static string MapFolder = DataFolder + "/map";
        
        //日志工具
        // public readonly static Util.Logger Logger = new Util.Logger("CGTool", DEBUG);

        public static bool ShowMapUnitName = true;
        
        
        //初始化CGTool
        public static void Init()
        {
            //初始化加载并缓存 0-15 调色板文件
            for (int i = 0; i < 16; i++) Palet.GetPalet(i);
            
            //初始化加载并缓存GraphicInfo配置表
            for (int i = 0; i < 2; i++) GraphicInfo.GetGraphicInfo(i);

            //初始化加载动画序列信息
            Anime.GetAnimeInfo(0);
            Anime.GetAnimeInfo(105000);
            
            //地图索引初始化
            Map.Init();

            Debug.Log("CGTool初始化完成");
        }

    }
}
