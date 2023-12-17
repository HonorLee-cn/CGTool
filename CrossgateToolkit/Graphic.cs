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
        
        // 高版本号标识
        // public static readonly Dictionary<string,bool> Flag_HighVersion = new Dictionary<string, bool>(); 
        
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

        // 版本号分析
        private class VersionInfo
        {
            public string Version;
            public int VersionCode;
            public string FullVersion;
        }
        private static VersionInfo AnalysisVersion(string prefix,FileInfo fileinfo)
        {
            VersionInfo versionInfo = new VersionInfo();
            int suffixIndex = fileinfo.Name.LastIndexOf('.');
                
            // 解析版本号
            string filename = fileinfo.Name.Substring(0, suffixIndex);
            versionInfo.FullVersion = filename.Substring(prefix.Length);
            if (string.IsNullOrEmpty(versionInfo.FullVersion))
            {
                string parentName = fileinfo.Directory != null ? fileinfo.Directory.Name : "";
                versionInfo.FullVersion = "";
                versionInfo.Version = parentName.ToUpper();
            }
            else
            {
                string[] versionArr = versionInfo.FullVersion.Split('_');
                if (String.IsNullOrEmpty(versionArr[0]))
                {
                    versionArr = versionArr.Skip(1).ToArray();
                }
            
                if (int.TryParse(versionArr[0], out int code))
                {
                    versionInfo.VersionCode = code;
                }
                else
                {
                    versionInfo.Version = versionArr[0].ToUpper();
                    if(int.TryParse(versionArr[^1], out int vcode))
                    {
                        versionInfo.VersionCode = vcode;
                    }
                }
            }

            
            return versionInfo;
        }
        // 分析目录,并获取对应配对文件
        private static void AnalysisDirectory(DirectoryInfo directoryInfo)
        {
            // Debug.Log("[CGTool] 开始分析目录: " + directoryInfo.FullName);
            string Version = directoryInfo.Name;
            FileInfo[] fileInfos = directoryInfo.GetFiles();
            fileInfos = fileInfos.OrderBy(f => f.Name).ToArray();
            foreach (FileInfo fileInfo in fileInfos)
            {
                if (!fileInfo.Name.EndsWith(".bin",StringComparison.OrdinalIgnoreCase)) continue;
                
                if (fileInfo.Name.StartsWith("graphicinfo", StringComparison.OrdinalIgnoreCase))
                {
                    // 找到GraphicInfo文件
                    // 获取对应版本号 GraphicInfo(*).bin
                    VersionInfo versionInfo = AnalysisVersion("graphicinfo", fileInfo);
                    // 判断是否存在对应graphic文件,忽略大小写
                    string graphicFileName = "graphic" + versionInfo.FullVersion + ".bin";
                    FileInfo graphicFileInfo = GetFileInfoByName(fileInfo.Directory, graphicFileName);
                    if (!graphicFileInfo.Exists)
                    {
                        throw new Exception("找不到对应的图档文件: " + fileInfo.FullName);
                    }
                    FilePair filePair = new FilePair()
                    {
                        InfoFile = fileInfo,
                        DataFile = graphicFileInfo,
                        Version = versionInfo.Version ?? Version
                    };
                    _graphicFilePairs.Add(filePair);
                }else if (fileInfo.Name.StartsWith("animeinfo", StringComparison.OrdinalIgnoreCase))
                {
                    // 找到AnimeInfo文件
                    // 获取对应版本号 AnimeInfo(*).bin
                    VersionInfo versionInfo = AnalysisVersion("animeinfo", fileInfo);
                    // 判断是否存在对应anime文件
                    string animeFileName = "anime" + versionInfo.FullVersion + ".bin";
                    FileInfo animeFileInfo = GetFileInfoByName(fileInfo.Directory, animeFileName);
                    if (!animeFileInfo.Exists)
                    {
                        throw new Exception("找不到对应的动画文件: " + fileInfo.FullName);
                    }
                    FilePair filePair = new FilePair()
                    {
                        InfoFile = fileInfo,
                        DataFile = animeFileInfo,
                        Version = versionInfo.Version ?? Version
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
        public static GraphicDetail GetGraphicDetail(uint serial,int palet = 0,bool linerFilter = false)
        {
            GraphicInfoData graphicInfoData = GraphicInfo.GetGraphicInfoData(serial);
            if (graphicInfoData == null) return null;
            return GraphicData.GetGraphicDetail(graphicInfoData, palet, 0, linerFilter);
        }
        
        // 获取图档数据
        public static GraphicDetail GetGraphicDetailByIndex(string Version,uint index,int palet = 0,bool linerFilter = false)
        {
            GraphicInfoData graphicInfoData = GraphicInfo.GetGraphicInfoDataByIndex(Version, index);
            if (graphicInfoData == null) return null;
            return GraphicData.GetGraphicDetail(graphicInfoData, palet, 0, linerFilter);
        }
    }
}
