/**
 * 魔力宝贝图档解析脚本 - CGTool
 * 
 * @Author  HonorLee (dev@honorlee.me)
 * @Version 1.0 (2023-04-15)
 * @License GPL-3.0
 *
 * GraphicInfo.cs 图档索引解析类
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CrossgateToolkit
{
    //GraphicInfo数据块
    public class GraphicInfoData
    {
        // GraphicInfo基础数据
        //4 bytes   索引
        public uint Index;
        //4 bytes   Graphic 地址
        public uint Addr;
        //4 bytes   Graphic 数据长度
        public uint Length;
        //4 bytes   Graphic 偏移 - X
        public int OffsetX;
        //4 bytes   Graphic 偏移 - Y
        public int OffsetY;
        //4 bytes   Graphic 宽
        public uint Width;
        //4 bytes   Graphic 高
        public uint Height;
        //4 bytes   Graphic East占地
        public int East;
        //4 bytes   Graphic South 占地
        public int South;
        //bool      穿越标识
        public bool Blocked;
        //1 byte    作为地面无层级遮挡[Test]
        public bool AsGround;
        //4 bytes   未知标识
        public byte[] Unknow;
        //4 bytes   编号
        public uint Serial;
        
        // GraphicInfo附加数据
        // GraphicInfo对应Graphic文件流读取器
        public BinaryReader GraphicReader;
        //已解压的调色板索引
        public int[] UnpackedPaletIndex;
    }

    public class GraphicInfo:MonoBehaviour
    {
        //索引字典    Serial -> GraphicInfoData
        private static readonly Dictionary<uint,GraphicInfoData> _cache = new Dictionary<uint, GraphicInfoData>();
        // private static readonly Dictionary<uint,GraphicInfoData> _indexCache = new Dictionary<uint, GraphicInfoData>();
        
        private static readonly Dictionary<string,Dictionary<uint,GraphicInfoData>> _indexCache = new Dictionary<string, Dictionary<uint, GraphicInfoData>>();
        public static void Init(string Version,FileInfo graphicInfoFile,FileInfo graphicFile)
        {
            if(!_indexCache.ContainsKey(Version)) _indexCache.Add(Version,new Dictionary<uint, GraphicInfoData>());
            //创建流读取器
            FileStream fileStream = graphicInfoFile.OpenRead();
            BinaryReader fileReader = new BinaryReader(fileStream);
            
            FileStream graphicFileStream = graphicFile.OpenRead();
            BinaryReader graphicFileReader = new BinaryReader(graphicFileStream);
            
            //解析Info数据表
            // List<GraphicInfoData> infoDatas = new List<GraphicInfoData>();
            long DataLength = fileStream.Length/40;
            for (int i = 0; i < DataLength; i++)
            {
                GraphicInfoData graphicInfoData = new GraphicInfoData();
                graphicInfoData.Index = BitConverter.ToUInt32(fileReader.ReadBytes(4),0);
                graphicInfoData.Addr = BitConverter.ToUInt32(fileReader.ReadBytes(4),0);
                graphicInfoData.Length = BitConverter.ToUInt32(fileReader.ReadBytes(4),0);
                graphicInfoData.OffsetX = BitConverter.ToInt32(fileReader.ReadBytes(4),0);
                graphicInfoData.OffsetY = BitConverter.ToInt32(fileReader.ReadBytes(4),0);
                graphicInfoData.Width = BitConverter.ToUInt32(fileReader.ReadBytes(4),0);
                graphicInfoData.Height = BitConverter.ToUInt32(fileReader.ReadBytes(4),0);
                graphicInfoData.East = fileReader.ReadByte();
                graphicInfoData.South = fileReader.ReadByte();
                graphicInfoData.Blocked =  fileReader.ReadByte() == 0;
                graphicInfoData.AsGround = fileReader.ReadByte() == 1;
                graphicInfoData.Unknow = fileReader.ReadBytes(4);
                graphicInfoData.Serial = BitConverter.ToUInt32(fileReader.ReadBytes(4),0);
                graphicInfoData.GraphicReader = graphicFileReader;

                //建立Index映射表
                _indexCache[Version][graphicInfoData.Index] = graphicInfoData;
                
                //建立Serial映射表
                if (graphicInfoData.Serial != 0) _cache[graphicInfoData.Serial] = graphicInfoData;



                // _logger.Write("Index: " + graphicInfoData.Index + " Addr: " + graphicInfoData.Addr + 
                //               " Width: " + graphicInfoData.Width + 
                //               " Height: " + graphicInfoData.Height +
                //               " OffsetX: " + graphicInfoData.OffsetX +
                //               " OffsetY: " + graphicInfoData.OffsetY +
                //               " East: " + graphicInfoData.East +
                //               " South: " + graphicInfoData.South +
                //               " Blocked: " + graphicInfoData.Blocked +
                //               " Unknow: " + BitConverter.ToString(graphicInfoData.Unknow).Replace("-", ",") +
                //               " MapSerial: " + graphicInfoData.MapSerial);
            }
            Debug.Log("[CGTool] 加载GraphicInfo - 文件: " + graphicInfoFile.Name + " 贴图总量: " + DataLength);
        }
        
        //获取GraphicInfoData
        public static GraphicInfoData GetGraphicInfoData(uint Serial)
        {
            _cache.TryGetValue(Serial, out var graphicInfoData);
            return graphicInfoData;
        }
        
        public static GraphicInfoData GetGraphicInfoDataByIndex(string Version,uint Index)
        {
            _indexCache.TryGetValue(Version, out var indexDict);
            if(indexDict == null) throw new Exception("找不到对应版本的GraphicInfo数据");
            indexDict.TryGetValue(Index, out var graphicInfoData);
            return graphicInfoData;
        }
    }
}