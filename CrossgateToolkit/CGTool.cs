/**
 * 魔力宝贝图档解析脚本 - CGTool
 * 
 * @Author  HonorLee (dev@honorlee.me)
 * @Version 2.0 (2023-11-19)
 * @License GPL-3.0
 *
 * CGTool.cs 入口文件
 */

using System;
using System.IO;
using UnityEngine;

namespace CrossgateToolkit
{
    public static class CGTool
    {
        // 路径配置
        public class CGPath
        {
            // 调色板目录
            public string PAL;
            // 图档目录
            public string BIN;
            // 地图目录
            public string MAP;
            // BGM目录
            public string BGM;
            // 音效目录
            public string AUDIO;
            
        }
        
        // 基础路径默认配置 
        public static CGPath PATH = new CGPath()
        {
            BIN = Environment.CurrentDirectory + "/bin",
            PAL = Environment.CurrentDirectory + "/pal",
            MAP = Environment.CurrentDirectory + "/map",
            BGM = Environment.CurrentDirectory + "/bgm",
            AUDIO = Environment.CurrentDirectory + "/se"
        };
        
        public static string ENCRYPT_KEY = "";

        /**
         * 初始化CGTool,并按顺序加载并初始化指定模块
         * Graphic加载顺序以Bin目录中的文件名排序
         * 其中Bin目录根目录下优先级最高，其次是Bin目录下的子目录
         */
        public static void Init(string encryptKey = "")
        {
            // 加密KEY
            ENCRYPT_KEY = encryptKey;
            
            // 初始化调色板
            if (PATH.PAL != null) Palet.Init();
            // 初始化图档解析器
            if (PATH.BIN != null) Graphic.Init();
            // 初始化地图索引
            if (PATH.MAP != null) Map.Init();
            
            
            Debug.Log("[CGTool] CGTool初始化完成");
        }

    }

}
