/**
 * 魔力宝贝图档解析脚本 - CGTool
 * 
 * @Author  HonorLee (dev@honorlee.me)
 * @Version 1.0 (2023-04-15)
 * @License GPL-3.0
 *
 * Map.cs 服务端地图解析类
 */

using System;
using System.Collections.Generic;
using System.IO;

namespace CGTool
{
    //地图文件信息
    public class MapFileInfo
    {
        public uint Serial;
        public string Name;
        public string FileName;
    }
    //地图块数据
    public class MapBlockData
    {
        public GraphicInfoData GraphicInfo;
        // public uint GraphicIndex;
        public uint MapSerial;
    }
    //地图信息
    public class MapInfo
    {
        //地图编号
        public uint Serial;
        //地图宽度
        public uint Width;
        //地图高度
        public uint Height;
        // 地图名称
        public string Name;
        //未知数据
        public byte[] Unknow;
        //地面数据
        public List<MapBlockData> GroundDatas = new List<MapBlockData>();
        //地表数据
        public List<MapBlockData> ObjectDatas = new List<MapBlockData>();
        public bool[] BlockedIndexs;
        public bool[,] MapPoints;
    }
    
    public class Map
    {
        
        //缓存数据
        private static Dictionary<uint, MapInfo> _cache = new Dictionary<uint, MapInfo>();
        private static Dictionary<uint, MapFileInfo> _mapIndexFiles = new Dictionary<uint, MapFileInfo>(); 

        //初始化地图文件列表
        public static void Init()
        {
            DirectoryInfo mapDirectory = new DirectoryInfo(CGTool.MapFolder);
            FileInfo[] mapFiles = mapDirectory.GetFiles();
            foreach (var fileInfo in mapFiles)
            {
                string filename = fileInfo.Name;
                if(filename.Equals(".DS_Store")) continue;
                MapFileInfo _file = new MapFileInfo();
                string[] indexName = filename.Split(("_").ToCharArray());
                _file.Serial = uint.Parse(indexName[0]);
                _file.Name = indexName[1];
                _file.FileName = filename;
                _mapIndexFiles.Add(_file.Serial, _file);
            }
        }

        //获取全部地图列表
        public static List<MapFileInfo> GetMapList()
        {
            List<MapFileInfo> _list = new List<MapFileInfo>();
            foreach (var mapIndexFile in _mapIndexFiles)
            {
                _list.Add(mapIndexFile.Value);
            }

            return _list;
        }
        //获取地图数据
        public static MapInfo GetMap(uint serial)
        {
            
            //返回缓存数据
            if (_cache.ContainsKey(serial)) return _cache[serial];
            //加载数据
            MapInfo mapInfo = _loadMap(serial);
            return mapInfo;
        }

        //加载地图数据
        private static MapInfo _loadMap(uint serial)
        {
            // CGTool.Logger.Write("开始加载时间:" + DateTime.Now);
            if (!_mapIndexFiles.ContainsKey(serial)) return null;
            
            // print("找到地图文件: " + mapFileInfo.Name);
            FileStream mapFileStream = new FileStream(CGTool.MapFolder + "/" + _mapIndexFiles[serial].FileName, FileMode.Open);
            BinaryReader mapFileReader = new BinaryReader(mapFileStream);
            
            MapInfo mapInfo = new MapInfo();
            mapInfo.Serial = serial;

            //地图文件头
            byte[] mapHeader = mapFileReader.ReadBytes( 8);
            //地图名称
            byte[] mapNameBytes = mapFileReader.ReadBytes(32);
            mapInfo.Name = System.Text.Encoding.GetEncoding("GBK").GetString(mapNameBytes).Split('|')[0];

            //读取地图宽度
            byte[] bytes = mapFileReader.ReadBytes(2);
            Array.Reverse(bytes);
            mapInfo.Width = (uint)BitConverter.ToUInt16(bytes,0);
            //读取地图高度
            bytes = mapFileReader.ReadBytes(2);
            Array.Reverse(bytes);
            mapInfo.Height = (uint)BitConverter.ToUInt16(bytes,0);

            byte[] mapBytes = mapFileReader.ReadBytes((int) (mapInfo.Width * mapInfo.Height * 2));
            byte[] mapCoverBytes = mapFileReader.ReadBytes((int) (mapInfo.Width * mapInfo.Height * 2));

            mapFileReader.Dispose();
            mapFileReader.Close();
            mapFileStream.Close();

            // print(JsonUtility.ToJson(mapInfo));
            
            BinaryReader mapReader = new BinaryReader(new MemoryStream(mapBytes));
            BinaryReader mapCoverReader = new BinaryReader(new MemoryStream(mapCoverBytes));
            // BinaryReader mapInfoReader = new BinaryReader(new MemoryStream(mapInfoBytes));

            List<MapBlockData> tempGroundTiles = new List<MapBlockData>();
            List<MapBlockData> tempObjectTiles = new List<MapBlockData>();
            
            // CGTool.Logger.Write("开始解析时间:" + DateTime.Now);
            uint len = mapInfo.Width * mapInfo.Height;
            for (uint i = 0; i < len; i++)
            {
                //地面数据
                MapBlockData mapTile = null;
                bytes = mapReader.ReadBytes(2);
                Array.Reverse(bytes);
                uint mapGraphicSerial = BitConverter.ToUInt16(bytes,0);
                int Version = 0;
                if (mapGraphicSerial > 20000)
                {
                    mapGraphicSerial += 200000;
                    Version = 1;
                }
                GraphicInfoData graphicInfoData = GraphicInfo.GetGraphicInfoDataByMapSerial(Version, mapGraphicSerial);
                if (graphicInfoData != null)
                {
                    mapTile = new MapBlockData();
                    mapTile.GraphicInfo = graphicInfoData;
                    mapTile.MapSerial = mapGraphicSerial;
                }
                tempGroundTiles.Add(mapTile);
                
                MapBlockData mapCoverTile = null;
                bytes = mapCoverReader.ReadBytes(2);
                Array.Reverse(bytes);
                uint mapCoverGraphicSerial = BitConverter.ToUInt16(bytes,0);
                Version = 0;
                if (mapCoverGraphicSerial > 30000 || mapCoverGraphicSerial==25290)
                {
                    mapCoverGraphicSerial += 200000;
                    Version = 1;
                }
                graphicInfoData = GraphicInfo.GetGraphicInfoDataByMapSerial(Version, mapCoverGraphicSerial);
                if (graphicInfoData != null)
                {
                    mapCoverTile = new MapBlockData();
                    mapCoverTile.GraphicInfo = graphicInfoData;
                    mapCoverTile.MapSerial = mapCoverGraphicSerial;
                }
                tempObjectTiles.Add(mapCoverTile);
            }

            List<MapBlockData> GroundTiles = new List<MapBlockData>();
            List<MapBlockData> ObjectTiles = new List<MapBlockData>();
            bool[] blockedIndexs = new bool[mapInfo.Width * mapInfo.Height];
            // CGTool.Logger.Write("开始排序时间:" + DateTime.Now);
            for (int y = 0; y < mapInfo.Height; y++)
            {
                for (int x = 0; x < mapInfo.Width; x++)
                {
                    // int index = i * (int) mapInfo.Width + ((int) mapInfo.Width - j - 1);
                    int _tmpindex = x + (int)((mapInfo.Height - y - 1) * mapInfo.Width);
                    int index = x + y * (int)mapInfo.Width;
                    
                    MapBlockData mapTile = tempGroundTiles[_tmpindex]; 
                    MapBlockData ObjectTile = tempObjectTiles[_tmpindex];
                    
                    GroundTiles.Add(mapTile);
                    ObjectTiles.Add(ObjectTile);

                    if (mapTile==null || mapTile.GraphicInfo.Blocked) blockedIndexs[index] = true;
                    if (ObjectTile!=null && ObjectTile.GraphicInfo.Blocked)
                    {
                        blockedIndexs[index] = true;
                        if (ObjectTile.GraphicInfo.East > 0 || ObjectTile.GraphicInfo.South > 0)
                        {
                            for (int i = x; i < (x + ObjectTile.GraphicInfo.East); i++)
                            {
                                for (int j = y; j < (y+ ObjectTile.GraphicInfo.South); j++)
                                {

                                    if(i>=mapInfo.Width || j>=mapInfo.Height) continue;
                                    int _index = (int) (j * mapInfo.Width + i);
                                    blockedIndexs[_index] = true;
                                }
                            }
                        }
                    }
                }
            }

            bool[,] points = new bool[mapInfo.Width, mapInfo.Height];
            for (int y = 0; y < mapInfo.Height; y++)
            {
                for (int x = 0; x < mapInfo.Width; x++)
                {
                    int index = x + y * (int)mapInfo.Width;
                    points[x, y] = !blockedIndexs[index];
                }
            }

            mapInfo.GroundDatas = GroundTiles;
            mapInfo.ObjectDatas = ObjectTiles;
            mapInfo.BlockedIndexs = blockedIndexs;
            mapInfo.MapPoints = points;
            _cache[serial] = mapInfo;
            // CGTool.Logger.Write("地图解析完成时间:" + DateTime.Now);
            return mapInfo;
        }
    }
}
