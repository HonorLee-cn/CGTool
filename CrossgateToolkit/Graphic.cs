/**
 * 魔力宝贝图档解析脚本 - CGTool
 * 
 * @Author  HonorLee (dev@honorlee.me)
 * @Version 1.0 (2023-04-15)
 * @License GPL-3.0
 *
 * Graphic.cs 图档解析类
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CrossgateToolkit
{
    //图档类
    public static class Graphic
    {
        private class FilePair
        {
            public FileInfo InfoFile;
            public FileInfo DataFile;
            public string Version;
        }
        
        // 临时文件列表缓存
        private static List<FilePair> _graphicFilePairs = new List<FilePair>();
        private static List<FilePair> _animeFilePairs = new List<FilePair>();
        
        // 初始化
        public static void Init()
        {
            // 解析Bin文件目录结构
            if(!Directory.Exists(CGTool.PATH.BIN)) throw new Exception("图档目录不存在,请检查CGTool中是否配置相应PATH路径");

            // 整理目录结构,生成对应待处理文件列表
            List<DirectoryInfo> _directorys = new List<DirectoryInfo>();
            _directorys.Add(new DirectoryInfo(CGTool.PATH.BIN));
            _directorys.AddRange(new DirectoryInfo(CGTool.PATH.BIN).GetDirectories().OrderBy(d => d.Name).ToList());
            foreach (DirectoryInfo directory in _directorys)
            {
                AnalysisDirectory(directory);
            }

            Debug.Log("[CGTool] 图档资源查找完毕,共找到: (" + _graphicFilePairs.Count + ") 个图档文件, (" + _animeFilePairs.Count +
                      ") 个动画文件");
            
            // 预加载 GraphicInfo
            foreach (FilePair graphicFilePair in _graphicFilePairs)
            {
                GraphicInfo.Init(graphicFilePair.Version,graphicFilePair.InfoFile, graphicFilePair.DataFile);
            }
            
            // 预加载 Anime
            foreach (FilePair animeFilePair in _animeFilePairs)
            {
                Anime.Init(animeFilePair.Version, animeFilePair.InfoFile, animeFilePair.DataFile);
            }
        }

        // 分析目录,并获取对应配对文件
        private static void AnalysisDirectory(DirectoryInfo directoryInfo)
        {
            Debug.Log("[CGTool] 开始分析目录: " + directoryInfo.FullName);
            string Version = directoryInfo.Name;
            FileInfo[] fileInfos = directoryInfo.GetFiles();
            fileInfos = fileInfos.OrderBy(f => f.Name).ToArray();
            foreach (FileInfo fileInfo in fileInfos)
            {
                if (!fileInfo.Name.EndsWith(".bin",StringComparison.OrdinalIgnoreCase)) continue;
                int suffixIndex = fileInfo.Name.LastIndexOf('.');
                string versionStr;
                if (fileInfo.Name.StartsWith("graphicinfo", StringComparison.OrdinalIgnoreCase))
                {
                    // 找到GraphicInfo文件
                    // 获取对应版本号 GraphicInfo(*).bin
                    versionStr = fileInfo.Name.Substring(11, suffixIndex - 11);
                    // 判断是否存在对应graphic文件,忽略大小写
                    string graphicFileName = "graphic" + versionStr + ".bin";
                    FileInfo graphicFileInfo = GetFileInfoByName(fileInfo.Directory, graphicFileName);
                    if (!graphicFileInfo.Exists)
                    {
                        throw new Exception("找不到对应的图档文件: " + fileInfo.FullName);
                    }
                    FilePair filePair = new FilePair()
                    {
                        InfoFile = fileInfo,
                        DataFile = graphicFileInfo,
                        Version = Version
                    };
                    _graphicFilePairs.Add(filePair);
                }else if (fileInfo.Name.StartsWith("animeinfo", StringComparison.OrdinalIgnoreCase))
                {
                    // 找到AnimeInfo文件
                    // 获取对应版本号 AnimeInfo(*).bin
                    versionStr = fileInfo.Name.Substring(9, suffixIndex - 9);
                    // 判断是否存在对应anime文件
                    string animeFileName = "anime" + versionStr + ".bin";
                    FileInfo animeFileInfo = GetFileInfoByName(fileInfo.Directory, animeFileName);
                    if (!animeFileInfo.Exists)
                    {
                        throw new Exception("找不到对应的动画文件: " + fileInfo.FullName);
                    }
                    FilePair filePair = new FilePair()
                    {
                        InfoFile = fileInfo,
                        DataFile = animeFileInfo,
                        Version = Version
                    };
                    _animeFilePairs.Add(filePair);
                }
            }
        }
        
        // 忽略大小写获取对应文件
        private static FileInfo GetFileInfoByName(DirectoryInfo directoryInfo,string fileName)
        {
            FileInfo[] fileInfos = directoryInfo.GetFiles();
            foreach (FileInfo fileInfo in fileInfos)
            {
                if (fileInfo.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                {
                    return fileInfo;
                }
            }

            return null;
        }
        
        // 获取图档数据
        public static GraphicDetail GetGraphicDetail(uint serial,int palet = 0)
        {
            GraphicInfoData graphicInfoData = GraphicInfo.GetGraphicInfoData(serial);
            return GraphicData.GetGraphicDetail(graphicInfoData, palet);
        }
        
        // 获取图档数据
        public static GraphicDetail GetGraphicDetailByIndex(string Version,uint index,int palet = 0)
        {
            GraphicInfoData graphicInfoData = GraphicInfo.GetGraphicInfoDataByIndex(Version, index);
            return GraphicData.GetGraphicDetail(graphicInfoData, palet);
        }
    }
}
