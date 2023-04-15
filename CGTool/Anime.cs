/**
 * 魔力宝贝图档解析脚本 - CGTool
 * 
 * @Author  HonorLee (dev@honorlee.me)
 * @Version 1.0 (2023-04-15)
 * @License GPL-3.0
 *
 * Anime.cs 动画基础类
 */

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CGTool
{
    //动画信息
    public class AnimeInfo
    {
        public int Version;
        //4 bytes   动画索引
        public uint Index;
        //4 bytes   动画文件地址
        public uint Addr;
        //2 bytes   动作数量
        public int ActionCount;
        //2 bytes   未知字节
        public byte[] Unknow;
        //动画数据  Direction -> ActionType -> AnimeData
        public Dictionary<int, Dictionary<int, AnimeDetail>> AnimeDatas = new Dictionary<int, Dictionary<int, AnimeDetail>>();
    }

    //动画帧数据
    public class AnimeFrameInfo
    {
        //图档编号
        public uint GraphicIndex;
        //偏移X
        public int OffsetX;
        //偏移Y
        public int OffsetY;
        //音效编号
        public int AudioIndex;
        //动效编号
        public Anime.EffectType Effect;
    }

    //动画数据
    public class AnimeDetail
    {
        public uint Index;
        public int Version;
        public int Direction;
        public int ActionType;
        public uint CycleTime;
        public uint FrameCount;
        public AnimeFrameInfo[] AnimeFrameInfos;
        
        // public byte[] unknown;
    }
    //动画相关Enum类型
    public class Anime : MonoBehaviour
    {
        //方向
        public enum DirectionType
        {
            NULL=-1,
            North=0,
            NorthEast=1,
            East=2,
            SouthEast=3,
            South=4,
            SouthWest=5,
            West=6,
            NorthWest=7
        }
        //动作(未补全)
        public enum ActionType
        {
            Stand=0,
            Walk=1,
            BeforeRun=2,
            Run=3,
            AfterRun=4,
            Attack=5,
        }
        //动效
        public enum EffectType
        {
            Hit=1,
            HitOver=2
        }
        //动画列表缓存    Index -> AnimeInfo
        private static Dictionary<uint, AnimeInfo> _animeInfoCache = new Dictionary<uint, AnimeInfo>();

        //动画序列缓存    Direction -> Action -> AnimeData
        // private static Dictionary<uint, Dictionary<int, AnimeData>> _animeCache =
        //     new Dictionary<uint, Dictionary<int, AnimeData>>();
        
        private static List<string> _animeInfoFiles = new List<string>()
        {
            //龙之沙漏 之前版本前Info数据
            "AnimeInfo_4.bin",
            //龙之沙漏 版本Info数据
            "AnimeInfoEx_1.Bin"
        };

        private static List<string> _animeDataFiles = new List<string>()
        {
            "Anime_4.bin", "AnimeEx_1.Bin"
        };

        //获取动画数据信息
        public static AnimeInfo GetAnimeInfo(uint Index)
        {
            //返回缓存
            if (_animeInfoCache.ContainsKey(Index)) return _animeInfoCache[Index];
            //动画编号大于105000的属于 龙之沙漏 版本
            int Version = 0;
            if (Index >= 105000) Version = 1;
            Dictionary<uint, AnimeInfo> animeInfos = _loadAnimeInfo(Version);
            if (animeInfos.ContainsKey(Index)) return animeInfos[Index];
            return null;
        }

        //获取动画数据
        public static AnimeDetail GetAnimeDetail(uint serial,DirectionType Direction,ActionType Action)
        {
            AnimeInfo animeInfo = GetAnimeInfo(serial);
            if (animeInfo == null) return null;
            if (animeInfo.AnimeDatas.ContainsKey((int)Direction))
            {
                if (animeInfo.AnimeDatas[(int)Direction].ContainsKey((int) Action))
                    return animeInfo.AnimeDatas[(int)Direction][(int) Action];
            }

            return null;
        }
        
        //加载动画数据
        private static Dictionary<uint, AnimeInfo> _loadAnimeInfo(int Version)
        {
            //查找Info文件
            string infoFileName = _animeInfoFiles[Version];
            string dataFileName = _animeDataFiles[Version];
            FileInfo infoFile = new FileInfo(CGTool.BaseFolder + "/" + infoFileName);
            FileInfo dataFile = new FileInfo(CGTool.BaseFolder + "/" + dataFileName);
            if (!infoFile.Exists || !dataFile.Exists) return null;

            //创建流读取器
            FileStream infoFileStream = infoFile.OpenRead();
            FileStream dataFileStream = dataFile.OpenRead();
            BinaryReader infoFileReader = new BinaryReader(infoFileStream);
            BinaryReader dataFileReader = new BinaryReader(dataFileStream);

            // Dictionary<uint, AnimeInfo> animeInfos = new Dictionary<uint, AnimeInfo>();
            long DataLength = infoFileStream.Length / 12;
            for (int i = 0; i < DataLength; i++)
            {
                //初始化对象
                AnimeInfo animeInfo = new AnimeInfo();
                animeInfo.Version = Version;
                animeInfo.Index = BitConverter.ToUInt32(infoFileReader.ReadBytes(4),0);
                animeInfo.Addr = BitConverter.ToUInt32(infoFileReader.ReadBytes(4),0);
                animeInfo.ActionCount = infoFileReader.ReadUInt16();
                animeInfo.Unknow = infoFileReader.ReadBytes(2);
                // print(JsonUtility.ToJson(animeInfo));
                dataFileStream.Position = animeInfo.Addr;
                for (int j = 0; j < animeInfo.ActionCount; j++)
                {
                    AnimeDetail animeData = new AnimeDetail();
                    animeData.Index = animeInfo.Index;
                    animeData.Version = Version;
                    animeData.Direction = dataFileReader.ReadUInt16();
                    animeData.ActionType = dataFileReader.ReadUInt16();
                    animeData.CycleTime = BitConverter.ToUInt32(dataFileReader.ReadBytes(4),0);
                    animeData.FrameCount = BitConverter.ToUInt32(dataFileReader.ReadBytes(4),0);
                    animeData.AnimeFrameInfos = new AnimeFrameInfo[animeData.FrameCount];
                    // if (animeInfo.Index == 101201) Debug.Log("----------------------------------");
                    for (int k = 0; k < animeData.FrameCount; k++)
                    {
                        animeData.AnimeFrameInfos[k] = new AnimeFrameInfo();
                        //GraphicIndex序号
                        animeData.AnimeFrameInfos[k].GraphicIndex = BitConverter.ToUInt32(dataFileReader.ReadBytes(4),0);
                        //未知字节
                        // animeData.unknown = dataFileReader.ReadBytes(6);
                        // if (animeInfo.Index == 101201)
                        // {
                        //     byte[] tt = dataFileReader.ReadBytes(6);
                        //     
                        //     Debug.Log(tt[0]+" "+tt[1]+" "+tt[2]+" "+tt[3]+" "+tt[4]+" "+tt[5]);
                        // }
                        // else
                        // {
                        //     dataFileReader.ReadBytes(6);
                        // }
                        animeData.AnimeFrameInfos[k].OffsetX = BitConverter.ToInt16(dataFileReader.ReadBytes(2),0);
                        animeData.AnimeFrameInfos[k].OffsetY = BitConverter.ToInt16(dataFileReader.ReadBytes(2),0);
                        animeData.AnimeFrameInfos[k].AudioIndex = dataFileReader.ReadByte();
                        int effect = dataFileReader.ReadByte();
                        if (effect == 0x27 || effect == 0x28)
                        {
                            animeData.AnimeFrameInfos[k].Effect = EffectType.Hit;
                        }
                        else if(effect == 0x4E || effect == 0x4F)
                        {
                            animeData.AnimeFrameInfos[k].Effect = EffectType.HitOver;
                        }
                        // animeData.AnimeFrameInfos[k].Effect = dataFileReader.ReadByte();
                    }
                    
                    if (!animeInfo.AnimeDatas.ContainsKey(animeData.Direction))
                        animeInfo.AnimeDatas.Add(animeData.Direction, new Dictionary<int, AnimeDetail>());

                    if (animeInfo.AnimeDatas[animeData.Direction].ContainsKey(animeData.ActionType))
                    {
                        animeInfo.AnimeDatas[animeData.Direction][animeData.ActionType] = animeData;
                    }
                    else
                    {
                        animeInfo.AnimeDatas[animeData.Direction].Add(animeData.ActionType, animeData);
                    }

                    if (_animeInfoCache.ContainsKey(animeInfo.Index))
                    {
                        _animeInfoCache[animeInfo.Index] = animeInfo;
                    }
                    else
                    {
                        _animeInfoCache.Add(animeInfo.Index, animeInfo);
                    }
                }

            }

            infoFileReader.Dispose();
            infoFileReader.Close();
            dataFileReader.Dispose();
            dataFileReader.Close();
            infoFileStream.Close();
            dataFileStream.Close();

            return _animeInfoCache;
        }
    }
}