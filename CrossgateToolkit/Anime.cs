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
        public Dictionary<int, Dictionary<int, AnimeDetail>> AnimeDatas = new Dictionary<int, Dictionary<int, AnimeDetail>>();
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
        public Dictionary<int,Sprite> AnimeSprites = new Dictionary<int, Sprite>();
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
        public AnimeFlag FLAG;
        // 高版本 - 结束标识
        public byte[] FLAG_END;
        public Dictionary<int,Texture2D> AnimeTextures = new Dictionary<int, Texture2D>();
        // public Texture2D AnimeTexture;
        public List<AnimeFrameInfo> AnimeFrameInfos;
        // public byte[] unknown;
    }

    public class AnimeFlag
    {
        public bool REVERSE_X;
        public bool REVERSE_Y;
        public bool LOCK_PAL;
        public bool LIGHT_THROUGH;
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
            Stand       = 0,
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
                AnimeInfo animeInfo = new AnimeInfo();
                animeInfo.Version = Version;
                animeInfo.Serial = BitConverter.ToUInt32(infoFileReader.ReadBytes(4),0);
                // Debug.Log(animeInfo.Serial);
                animeInfo.Addr = BitConverter.ToUInt32(infoFileReader.ReadBytes(4),0);
                animeInfo.ActionCount = infoFileReader.ReadUInt16();
                animeInfo.Unknow = infoFileReader.ReadBytes(2);
                if (animeInfo.Addr > dataFileStream.Length) break;
                dataFileStream.Position = animeInfo.Addr;
                for (int j = 0; j < animeInfo.ActionCount; j++)
                {
                    
                    // 高版本标识
                    bool isHighVersion = false;
                    dataFileStream.Position += 16;
                    if(dataFileReader.ReadBytes(4).SequenceEqual(highVersionFlag)) isHighVersion = true;
                    dataFileStream.Position -= 20;
                    
                    AnimeDetail animeData = new AnimeDetail();
                    animeData.Version = Version;
                    animeData.Serial = animeInfo.Serial;
                    animeData.Direction = dataFileReader.ReadUInt16();
                    animeData.ActionType = dataFileReader.ReadUInt16();
                    animeData.CycleTime = BitConverter.ToUInt32(dataFileReader.ReadBytes(4),0);
                    animeData.FrameCount = BitConverter.ToUInt32(dataFileReader.ReadBytes(4),0);
                    
                    // 高版本
                    if (isHighVersion)
                    {
                        animeData.IsHighVersion = true;
                        animeData.Palet = dataFileReader.ReadUInt16();
                        int flag = dataFileReader.ReadUInt16();
                        if (flag > 0)
                        {
                            animeData.FLAG = new AnimeFlag();
                            if ((flag & 1) == (1 << 0)) animeData.FLAG.REVERSE_X = true;
                            if ((flag & 2) == (1 << 1)) animeData.FLAG.REVERSE_Y = true;
                            if ((flag & 4) == (1 << 2)) animeData.FLAG.LOCK_PAL = true;
                            if ((flag & 8) == (1 << 3)) animeData.FLAG.LIGHT_THROUGH = true;    
                        }
                        
                        animeData.FLAG_END = dataFileReader.ReadBytes(4);
                    }
                    animeData.AnimeFrameInfos = new List<AnimeFrameInfo>();
                    
                    // if (animeInfo.Index == 101201) Debug.Log("----------------------------------");
                    for (int k = 0; k < animeData.FrameCount; k++)
                    {
                        byte[] frameBytes = dataFileReader.ReadBytes(10);
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
                    
                    _animeInfoCache[animeInfo.Serial] = animeInfo;
                }

            }
            infoFileReader.Dispose();
            infoFileReader.Close();
            dataFileReader.Dispose();
            dataFileReader.Close();
            infoFileStream.Close();
            dataFileStream.Close();
            
            Debug.Log("[CGTool] 加载AnimeInfo - 文件: [" +
                      // (Graphic.Flag_HighVersion[Version] ? "H" : "N") + "] [" +
                      Version + "] " +
                      animeInfoFile.Name +
                      " 动画总量: " + DataLength);
        }
        
        //获取动画数据信息
        public static AnimeInfo GetAnimeInfo(uint serial)
        {
            _animeInfoCache.TryGetValue(serial, out var animeInfo);
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
        public static void BakeAnimeFrames(AnimeDetail animeDetail,int palet = 0)
        {
            if(animeDetail.AnimeTextures.ContainsKey(palet)) return;
            //所有帧的图形数据
            GraphicDetail[] graphicDetails = new GraphicDetail[animeDetail.FrameCount];
            
            //合并后的Texture2D尺寸
            uint textureWidth = 0;
            uint textureHeight = 0;
            
            
            for (var i = 0; i < animeDetail.FrameCount; i++)
            {
                //载入图档
                GraphicInfoData graphicInfoData = GraphicInfo.GetGraphicInfoDataByIndex(animeDetail.Version,animeDetail.AnimeFrameInfos[i].GraphicIndex);
                if (graphicInfoData == null) continue;
                int subPaletIndex = 0;
                if (animeDetail.IsHighVersion) subPaletIndex = (int)animeDetail.Serial;
                GraphicDetail graphicDetail = GraphicData.GetGraphicDetail(graphicInfoData, palet, subPaletIndex);
                if(graphicDetail == null) continue;
                graphicDetails[i] = graphicDetail;
                if(graphicDetail.Height > textureHeight) textureHeight = graphicDetail.Height;
                textureWidth += graphicDetail.Width + 5;
                animeDetail.AnimeFrameInfos[i].Width = (int) graphicDetail.Width;
                animeDetail.AnimeFrameInfos[i].Height = (int) graphicDetail.Height;
                animeDetail.AnimeFrameInfos[i].OffsetX = (int) graphicInfoData.OffsetX;
                animeDetail.AnimeFrameInfos[i].OffsetY = (int) graphicInfoData.OffsetY;
                animeDetail.AnimeFrameInfos[i].GraphicInfo = graphicInfoData;
            }
            //合并图档
            Texture2D texture2dMix = new Texture2D((int) textureWidth, (int) textureHeight, TextureFormat.RGBA4444, false,false);
            texture2dMix.filterMode = FilterMode.Point;
            Color32 transparentColor = new Color32(0, 0, 0, 0);
            Color32[] transparentColors = new Color32[texture2dMix.width * texture2dMix.height];
            for (var i = 0; i < transparentColors.Length; i++)
            {
                transparentColors[i] = transparentColor;
            }
            texture2dMix.SetPixels32(transparentColors,0);
            
            int offsetX = 0;
            for (var i = 0; i < animeDetail.FrameCount; i++)
            {
                GraphicDetail graphicDetail = graphicDetails[i];
                if(graphicDetail == null) continue;
                texture2dMix.SetPixels32((int) offsetX, 0, (int) graphicDetail.Width,
                    (int) graphicDetail.Height,
                    graphicDetail.Sprite.texture.GetPixels32());
                offsetX += (int) graphicDetail.Width + 5;
            }
            texture2dMix.Apply();
            
            animeDetail.AnimeTextures.Add(palet,texture2dMix);
            
            //创建动画每帧Sprite
            offsetX = 0;
            for (var l = 0; l < animeDetail.FrameCount; l++)
            {
                if(graphicDetails[l] == null) continue;
                AnimeFrameInfo animeFrameInfo = animeDetail.AnimeFrameInfos[l];
                Vector2 pivot = new Vector2(0f, 1f);
                pivot.x += -(animeFrameInfo.OffsetX * 1f) / animeFrameInfo.Width;
                pivot.y -= (-animeFrameInfo.OffsetY * 1f) / animeFrameInfo.Height;
                Sprite sprite = Sprite.Create(texture2dMix, new Rect(offsetX, 0,
                        animeDetail.AnimeFrameInfos[l].Width, animeDetail.AnimeFrameInfos[l].Height),
                    pivot, 1, 1, SpriteMeshType.FullRect);
                offsetX += animeDetail.AnimeFrameInfos[l].Width + 5;
                animeFrameInfo.AnimeSprites.Add(palet, sprite);
            }
            
        }
        
        
    }
}