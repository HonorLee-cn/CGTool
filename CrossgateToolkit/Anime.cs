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
using System.Linq;
using UnityEngine;

namespace CrossgateToolkit
{
    //动画信息
    public class AnimeInfo
    {
        // 版本
        public string Version;
        //4 bytes   动画序号
        public uint Serial;
        //4 bytes   动画文件地址
        public uint Addr;
        //2 bytes   动作数量
        public int ActionCount;
        //2 bytes   未知字节
        public byte[] Unknow;
        //动画数据  Direction -> ActionType -> AnimeData
        public Dictionary<int, Dictionary<int, AnimeDetail>> AnimeDatas;
        // 数据读取对象
        public BinaryReader DataReader;
    }

    //动画帧数据
    public class AnimeFrameInfo
    {
        //图档编号
        public uint GraphicIndex;
        //宽度
        public int Width;
        //高度
        public int Height;
        //偏移X
        public int OffsetX;
        //偏移Y
        public int OffsetY;
        //音效编号
        public int AudioIndex;
        //动效编号
        public Anime.EffectType Effect;
        //GraphicInfo;
        public GraphicInfoData GraphicInfo;
        //动画Sprite
        // public Dictionary<int,Sprite> AnimeSprites = new Dictionary<int, Sprite>();
        public Dictionary<int,Dictionary<bool,GraphicDetail>> AnimeSprites = new Dictionary<int, Dictionary<bool, GraphicDetail>>();
    }

    //动画数据
    public class AnimeDetail
    {
        // 动画编号
        public uint Serial;
        // 动画版本
        public string Version;
        // 方向
        public int Direction;
        // 动作
        public int ActionType;
        // 动画循环时间
        public uint CycleTime;
        // 帧数
        public uint FrameCount;

        // 高版本 - 标识
        public bool IsHighVersion;
        // 高版本 - 调色板
        public int Palet;
        // 高版本 - 图像反转
        public AnimeFlag FLAG = AnimeFlag.NULL;
        // 高版本 - 结束标识
        public byte[] FLAG_END;
        // public Dictionary<int,Texture2D> AnimeTextures = new Dictionary<int, Texture2D>();
        public Dictionary<int,Dictionary<bool,Texture2D>> AnimeTextures = new Dictionary<int, Dictionary<bool, Texture2D>>();
        // public Texture2D AnimeTexture;
        public List<AnimeFrameInfo> AnimeFrameInfos;
        // public byte[] unknown;
    }

    [Flags]
    public enum AnimeFlag
    {
        NULL = 0,
        REVERSE_X = 1<<0,
        REVERSE_Y = 1<<1,
        LOCK_PAL = 1<<2,
        LIGHT_THROUGH = 1<<3
    }
    
    //动画相关Enum类型
    public class Anime : MonoBehaviour
    {
        //方向
        public enum DirectionType
        {
            NULL        = -1,
            North       = 0,
            NorthEast   = 1,
            East        = 2,
            SouthEast   = 3,
            South       = 4,
            SouthWest   = 5,
            West        = 6,
            NorthWest   = 7
        }
        //方向九宫映射表
        public static DirectionType[,] DirectionTypeMap = new DirectionType[3,3]
        {
            {DirectionType.North,DirectionType.NorthEast,DirectionType.East},
            {DirectionType.NorthWest,DirectionType.NULL,DirectionType.SouthEast},
            {DirectionType.West,DirectionType.SouthWest,DirectionType.South}
        };
        //动作
        public enum ActionType
        {
            NULL        = -1,
            Idle       = 0,
            Walk        = 1,
            BeforeRun   = 2,
            Run         = 3,
            AfterRun    = 4,
            Attack      = 5,
            Magic       = 6,
            Throw       = 7,
            Hurt        = 8,
            Defence     = 9,
            Dead        = 10,
            Sit         = 11,
            Hi          = 12,
            Happy       = 13,
            Angry       = 14,
            Sad         = 15,
            Shake       = 16,
            Rock        = 17,
            Scissors    = 18,
            Paper       = 19,
            Fishing     = 20,
            
        }
        //动效
        public enum EffectType
        {
            Hit     =1,
            HitOver =2
        }

        public enum PlayType
        {
            Loop,
            Once,
            OnceAndDestroy
        }
        private static byte[] highVersionFlag = { 0xFF, 0xFF, 0xFF, 0xFF };
        
        //动画列表缓存    Serial -> AnimeInfo
        private static Dictionary<uint, AnimeInfo> _animeInfoCache = new Dictionary<uint, AnimeInfo>();
        
        //加载动画数据
        public static void Init(string Version,FileInfo animeInfoFile,FileInfo animeFile)
        {
            //创建流读取器
            FileStream infoFileStream = animeInfoFile.OpenRead();
            FileStream dataFileStream = animeFile.OpenRead();
            BinaryReader infoFileReader = new BinaryReader(infoFileStream);
            BinaryReader dataFileReader = new BinaryReader(dataFileStream);
            
            long DataLength = infoFileStream.Length / 12;
            
            // 循环初始化动画数据
            for (int i = 0; i < DataLength; i++)
            {
                //初始化对象
                AnimeInfo animeInfo = new AnimeInfo
                {
                    Version = Version,
                    Serial = BitConverter.ToUInt32(infoFileReader.ReadBytes(4),0),
                    Addr = BitConverter.ToUInt32(infoFileReader.ReadBytes(4),0),
                    ActionCount = infoFileReader.ReadUInt16(),
                    Unknow = infoFileReader.ReadBytes(2)
                };
                if (animeInfo.Addr > dataFileStream.Length) break;
                
                animeInfo.DataReader = dataFileReader;
                _animeInfoCache[animeInfo.Serial] = animeInfo;
            }
            infoFileReader.Dispose();
            infoFileReader.Close();
            infoFileStream.Close();
            
            Debug.Log("[CGTool] 加载AnimeInfo - 文件: [" +
                      // (Graphic.Flag_HighVersion[Version] ? "H" : "N") + "] [" +
                      Version + "] " +
                      animeInfoFile.Name +
                      " 动画总量: " + DataLength);
        }

        private static void ReadAnimeData(AnimeInfo animeInfo)
        {
            animeInfo.DataReader.BaseStream.Position = animeInfo.Addr;
            animeInfo.AnimeDatas = new Dictionary<int, Dictionary<int, AnimeDetail>>();
            for (int j = 0; j < animeInfo.ActionCount; j++)
            {
                // 高版本标识
                bool isHighVersion = false;
                animeInfo.DataReader.BaseStream.Position += 16;
                if(animeInfo.DataReader.ReadBytes(4).SequenceEqual(highVersionFlag)) isHighVersion = true;
                animeInfo.DataReader.BaseStream.Position -= 20;
                
                AnimeDetail animeData = new AnimeDetail();
                animeData.Version = animeInfo.Version;
                animeData.Serial = animeInfo.Serial;
                animeData.Direction = animeInfo.DataReader.ReadUInt16();
                animeData.ActionType = animeInfo.DataReader.ReadUInt16();
                animeData.CycleTime = BitConverter.ToUInt32(animeInfo.DataReader.ReadBytes(4),0);
                animeData.FrameCount = BitConverter.ToUInt32(animeInfo.DataReader.ReadBytes(4),0);
                
                // 高版本
                if (isHighVersion)
                {
                    animeData.IsHighVersion = true;
                    animeData.Palet = animeInfo.DataReader.ReadUInt16();
                    int flag = animeInfo.DataReader.ReadUInt16();
                    if (flag > 0)
                    {
                        if ((flag & 1) == (1 << 0)) animeData.FLAG |= AnimeFlag.REVERSE_X;
                        if ((flag & 2) == (1 << 1)) animeData.FLAG |= AnimeFlag.REVERSE_Y;
                        if ((flag & 4) == (1 << 2)) animeData.FLAG |= AnimeFlag.LOCK_PAL;
                        if ((flag & 8) == (1 << 3)) animeData.FLAG |= AnimeFlag.LIGHT_THROUGH;
                    }
                    
                    animeData.FLAG_END = animeInfo.DataReader.ReadBytes(4);
                }
                
                animeData.AnimeFrameInfos = new List<AnimeFrameInfo>();
                
                // if (animeInfo.Index == 101201) Debug.Log("----------------------------------");
                for (int k = 0; k < animeData.FrameCount; k++)
                {
                    byte[] frameBytes = animeInfo.DataReader.ReadBytes(10);
                    if (frameBytes.Length <10) break;
                    BinaryReader frameReader = new BinaryReader(new MemoryStream(frameBytes));
                    AnimeFrameInfo animeFrameInfo = new AnimeFrameInfo();
                    //GraphicIndex序号
                    animeFrameInfo.GraphicIndex = BitConverter.ToUInt32(frameReader.ReadBytes(4),0);
                    animeFrameInfo.OffsetX = BitConverter.ToInt16(frameReader.ReadBytes(2),0);
                    animeFrameInfo.OffsetY = BitConverter.ToInt16(frameReader.ReadBytes(2), 0);
                    
                    //标识位
                    int flag = BitConverter.ToInt16(frameReader.ReadBytes(2),0);
            
                    if (flag>20000)
                    {
                        //击打判定
                        animeFrameInfo.Effect = EffectType.Hit;
                        animeFrameInfo.AudioIndex = flag - 20000;
                    }
                    else if(flag>10000)
                    {
                        //攻击动作结束判定
                        animeFrameInfo.Effect = EffectType.HitOver;
                        animeFrameInfo.AudioIndex = flag - 10000;
                    }
                    else
                    {
                        animeFrameInfo.AudioIndex = flag;
                    }
                    animeData.AnimeFrameInfos.Add(animeFrameInfo);
                }
                animeData.FrameCount = (uint) animeData.AnimeFrameInfos.Count;
            
                if (!animeInfo.AnimeDatas.ContainsKey(animeData.Direction))
                    animeInfo.AnimeDatas.Add(animeData.Direction, new Dictionary<int, AnimeDetail>());
            
                animeInfo.AnimeDatas[animeData.Direction][animeData.ActionType] = animeData;
            }
        }
        
        //获取动画数据信息
        public static AnimeInfo GetAnimeInfo(uint serial)
        {
            _animeInfoCache.TryGetValue(serial, out AnimeInfo animeInfo);
            if (animeInfo is { AnimeDatas: null }) ReadAnimeData(animeInfo);
            return animeInfo;
        }

        //获取动画数据
        public static AnimeDetail GetAnimeDetail(uint serial,DirectionType Direction,ActionType Action)
        {
            AnimeInfo animeInfo = GetAnimeInfo(serial);
            if (animeInfo == null) return null;
            
            if (animeInfo.AnimeDatas.ContainsKey((int)Direction))
            {
                if (animeInfo.AnimeDatas[(int) Direction].ContainsKey((int) Action))
                {
                    AnimeDetail animeDetail = animeInfo.AnimeDatas[(int) Direction][(int) Action];
                    // if(animeDetail.AnimeTexture == null) prepareAnimeFrames(animeDetail);
                    return animeDetail;
                }
            }

            return null;
        }

        //预处理动画图形合批烘焙
        public static void BakeAnimeFrames(AnimeDetail animeDetail,int palet = 0, bool linear = false,bool compress = false)
        {
            // 查看是否存在缓存Texture数据
            animeDetail.AnimeTextures.TryGetValue(palet, out var textureDict);
            if(textureDict!=null)
            {
                if(textureDict.ContainsKey(linear)) return;
            }
            
            //所有帧的图形数据
            GraphicDetail[] graphicDetails = new GraphicDetail[animeDetail.FrameCount];
            
            //合并后的Texture2D尺寸
            List<GraphicInfoData> graphicInfoDatas = new List<GraphicInfoData>();
            Dictionary<uint,GraphicInfoData> graphicInfoDataDict = new Dictionary<uint, GraphicInfoData>();
            int subPaletIndex = -1;
            if (animeDetail.IsHighVersion) subPaletIndex = (int)animeDetail.Serial;
            for (var i = 0; i < animeDetail.FrameCount; i++)
            {
                //载入图档
                GraphicInfoData graphicInfoData = GraphicInfo.GetGraphicInfoDataByIndex(animeDetail.Version,animeDetail.AnimeFrameInfos[i].GraphicIndex);
                if (graphicInfoData == null) continue;
                graphicInfoDatas.Add(graphicInfoData);
                graphicInfoDataDict[graphicInfoData.Index] = graphicInfoData;
                animeDetail.AnimeFrameInfos[i].GraphicInfo = graphicInfoData;
                animeDetail.AnimeFrameInfos[i].OffsetX = (int) graphicInfoData.OffsetX;
                animeDetail.AnimeFrameInfos[i].OffsetY = (int) graphicInfoData.OffsetY;
            }

            Dictionary<uint, GraphicDetail> graphicDetailDict =
                GraphicData.BakeGraphics(graphicInfoDatas, false, palet, subPaletIndex, linear, 2048, 5, compress);
            Texture2D texture2dMix = null;
            for (var i = 0; i < animeDetail.FrameCount; i++)
            {
                graphicDetailDict.TryGetValue(animeDetail.AnimeFrameInfos[i].GraphicInfo.Index,out var graphicDetail);
                if(graphicDetail == null) continue;
                graphicDetails[i] = graphicDetail;
                if (texture2dMix == null) texture2dMix = graphicDetail.Sprite.texture;
                
                AnimeFrameInfo animeFrameInfo = animeDetail.AnimeFrameInfos[i];
                animeFrameInfo.Width = (int) graphicDetail.Width;
                animeFrameInfo.Height = (int) graphicDetail.Height;
                if(!animeFrameInfo.AnimeSprites.ContainsKey(palet)) animeFrameInfo.AnimeSprites[palet] = new Dictionary<bool, GraphicDetail>();
                if(!animeFrameInfo.AnimeSprites[palet].ContainsKey(linear)) animeFrameInfo.AnimeSprites[palet]
                    .Add(linear,graphicDetail);
            }
            
            if(!animeDetail.AnimeTextures.ContainsKey(palet)) animeDetail.AnimeTextures.Add(palet,new Dictionary<bool, Texture2D>());
            animeDetail.AnimeTextures[palet][linear] = texture2dMix;
            
        }
        
        
    }
}