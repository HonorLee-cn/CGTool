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
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Object = System.Object;

namespace CrossgateToolkit
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
        public int MapIndex;
        public uint MapSerial;
        public float ObjectZIndex = 0;
        public int x;
        public int y;
    }
    //地图信息
    public class MapInfo
    {
        //地图编号
        public uint Serial;
        //地图宽度
        public int Width;
        //地图高度
        public int Height;
        // 地图名称
        public string Name;
        // 调色板号 - 默认 -1 表示自动
        public int Palet = -1;
        //未知数据
        public byte[] Unknow;
        //地面数据
        public List<MapBlockData> GroundDatas = new List<MapBlockData>();
        //地表数据
        public List<MapBlockData> ObjectDatas = new List<MapBlockData>();
        public bool[] BlockedIndexs;
        public float[] FixPlayerZs;
        //地图坐标二维数组,用以记录可行走区域并作为自动寻路的数据参考
        public bool[,] MapNodes;
    }
    
    
    public class Map
    {
        //缓存数据
        private static Dictionary<uint, MapInfo> _cache = new Dictionary<uint, MapInfo>();
        private static Dictionary<uint, MapFileInfo> _mapIndexFiles = new Dictionary<uint, MapFileInfo>(); 
        
        private static Dictionary<uint,Dictionary<uint,GraphicDetail>> _mapGroundGraphicBatch = new Dictionary<uint, Dictionary<uint, GraphicDetail>>();
        private static Dictionary<uint,Dictionary<uint,GraphicDetail>> _mapObjectGraphicBatch = new Dictionary<uint, Dictionary<uint, GraphicDetail>>();

        //初始化地图文件列表
        public static void Init()
        {
            DirectoryInfo mapDirectory = new DirectoryInfo(CGTool.PATH.MAP);
            FileInfo[] mapFiles = mapDirectory.GetFiles();
            string match = @"^(\d+)_?(.+)?$";
            foreach (var fileInfo in mapFiles)
            {
                string filename = fileInfo.Name;
                Match matchRet = Regex.Match(filename, match);
                if(!matchRet.Success) continue;
                
                MapFileInfo _file = new MapFileInfo();
                _file.Serial = uint.Parse(matchRet.Groups[1].Value);
                if(matchRet.Groups.Count > 1) _file.Name = matchRet.Groups[1].Value;
                _file.FileName = filename;
                _mapIndexFiles.Add(_file.Serial, _file);
            }
            Debug.Log("[CGTool] 地图列表初始化完成,共" + _mapIndexFiles.Count + "个地图文件");
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
        
        // 地面数据合批
        public static Dictionary<uint,GraphicDetail> BakeGrounds(uint mapID,List<GraphicInfoData> graphicInfoDatas,int palet = 0,int subPalet = 0,bool linear = false)
        {
            _mapGroundGraphicBatch.TryGetValue(mapID, out var graphicDataDict);
            if (graphicDataDict == null)
            {
                graphicDataDict = GraphicData.BakeGraphics(graphicInfoDatas, true, palet, subPalet, linear, 2048, 0);
                _mapGroundGraphicBatch[mapID] = graphicDataDict;
            }
            return graphicDataDict;
        }
        
        // 物件数据合批
        public static Dictionary<uint,GraphicDetail> BakeObjects(uint mapID,List<GraphicInfoData> graphicInfoDatas,int palet = 0,int subPalet = 0,bool linear = false)
        {
            _mapObjectGraphicBatch.TryGetValue(mapID, out var graphicDataDict);
            if (graphicDataDict == null)
            {
                graphicDataDict = GraphicData.BakeGraphics(graphicInfoDatas, true, palet, subPalet, linear, 4096, 0);
                _mapObjectGraphicBatch[mapID] = graphicDataDict;
            }
            return graphicDataDict;
        }
        
        public static void ClearMapBatch(uint mapID)
        {
            Dictionary<uint,GraphicDetail> graphicDataDict;
            List<GraphicDetail> graphicDetails;
            List<Texture> textures = new List<Texture>();
            _mapGroundGraphicBatch.TryGetValue(mapID, out graphicDataDict);
            if (graphicDataDict != null)
            {
                graphicDetails = graphicDataDict.Values.ToList();
                foreach (var graphicDetail in graphicDetails)
                {
                    Texture texture = graphicDetail.Sprite.texture;
                    if (!textures.Contains(texture)) textures.Add(graphicDetail.Sprite.texture);
                    graphicDetail.SpritePPU100 = null;
                    graphicDetail.Sprite = null;
                }
            }
            _mapObjectGraphicBatch.TryGetValue(mapID, out graphicDataDict);
            if (graphicDataDict != null)
            {
                graphicDetails = graphicDataDict.Values.ToList();
                foreach (var graphicDetail in graphicDetails)
                {
                    Texture texture = graphicDetail.Sprite.texture;
                    if (!textures.Contains(texture)) textures.Add(graphicDetail.Sprite.texture);
                    graphicDetail.SpritePPU100 = null;
                    graphicDetail.Sprite = null;
                }
            }
            foreach (var texture in textures)
            {
                Resources.UnloadAsset(texture);
            }
            
            _mapGroundGraphicBatch.Remove(mapID);
            _mapObjectGraphicBatch.Remove(mapID);
        }

        //加载地图数据
        private static MapInfo _loadMap(uint serial)
        {
            // CGTool.Logger.Write("开始加载时间:" + DateTime.Now);
            if (!_mapIndexFiles.ContainsKey(serial)) return null;
            
            // print("找到地图文件: " + mapFileInfo.Name);
            FileStream mapFileStream = new FileStream(CGTool.PATH.MAP + "/" + _mapIndexFiles[serial].FileName, FileMode.Open);
            BinaryReader mapFileReader = new BinaryReader(mapFileStream);
            
            MapInfo mapInfo = new MapInfo();
            mapInfo.Serial = serial;

            bool isClientMapFile = false;

            //地图文件头
            byte[] mapHeader = mapFileReader.ReadBytes( 6);
            if(mapHeader[0]==0x4C && mapHeader[1]==0x53 && mapHeader[2]==0x32 && mapHeader[3]==0x4D && mapHeader[4]==0x41 && mapHeader[5]==0x50){
                isClientMapFile = false;
                Debug.Log("[CGTool] 地图文件头: 服务端地图");
            }else if (mapHeader[0]==0x4D && mapHeader[1]==0x41 && mapHeader[2]==0x50){
                isClientMapFile = true;
                Debug.Log("[CGTool] 地图文件头: 客户端地图");
            }
            else
            {
                Debug.LogError("[CGTool] 地图文件头错误: " + _mapIndexFiles[serial].FileName);
                return null;
            }
            byte[] bytes;
            if (isClientMapFile)
            {
                // 无用信息
                mapFileReader.ReadBytes(6);
                //读取地图宽度
                bytes = mapFileReader.ReadBytes(4);
                mapInfo.Width = BitConverter.ToUInt16(bytes,0);
                //读取地图高度
                bytes = mapFileReader.ReadBytes(4);
                mapInfo.Height = BitConverter.ToUInt16(bytes,0);
                if (MapExtra.ClientMapExtraDatas.ContainsKey(serial))
                {
                    mapInfo.Name = MapExtra.ClientMapExtraDatas[serial].Name;
                    mapInfo.Palet = MapExtra.ClientMapExtraDatas[serial].Palet;
                }
                else
                {
                    mapInfo.Name = "未知领域";
                    mapInfo.Palet = -1;
                }
            }
            else
            {
                // 无用信息
                mapFileReader.ReadBytes(2);
                //地图名称
                byte[] mapNameBytes = mapFileReader.ReadBytes(32);
                string[] mapHead = System.Text.Encoding.GetEncoding("GBK").GetString(mapNameBytes).Split('|');
                mapInfo.Name = mapHead[0];
            
                // 调色板
                if (mapHead.Length>1){
                    if(mapHead[1] != null || mapHead[1] != "") mapInfo.Palet = int.Parse(mapHead[1]);
                }
                
                //读取地图宽度
                bytes = mapFileReader.ReadBytes(2);
                Array.Reverse(bytes);
                mapInfo.Width = BitConverter.ToUInt16(bytes,0);
                //读取地图高度
                bytes = mapFileReader.ReadBytes(2);
                Array.Reverse(bytes);
                mapInfo.Height = BitConverter.ToUInt16(bytes,0);
            }
            
            

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
            //原始数据为反转数据,即坐标起点为 1,1 排序方向为 y : 1=>0 x: 1=>0
            int len = mapInfo.Width * mapInfo.Height;
            for (int i = 0; i < len; i++)
            {
                //地面数据
                MapBlockData mapTile = null;
                bytes = mapReader.ReadBytes(2);
                if(!isClientMapFile) Array.Reverse(bytes);
                uint mapGraphicSerial = BitConverter.ToUInt16(bytes,0);
                GraphicInfoData graphicInfoData = GraphicInfo.GetGraphicInfoData(mapGraphicSerial);
                if (graphicInfoData != null)
                {
                    mapTile = new MapBlockData();
                    mapTile.GraphicInfo = graphicInfoData;
                    mapTile.MapSerial = mapGraphicSerial;
                }
                tempGroundTiles.Add(mapTile);
                
                MapBlockData mapCoverTile = null;
                bytes = mapCoverReader.ReadBytes(2);
                if(!isClientMapFile) Array.Reverse(bytes);
                
                uint mapCoverGraphicSerial = BitConverter.ToUInt16(bytes,0);
                graphicInfoData = GraphicInfo.GetGraphicInfoData(mapCoverGraphicSerial);
                if (graphicInfoData != null)
                {
                    mapCoverTile = new MapBlockData();
                    mapCoverTile.GraphicInfo = graphicInfoData;
                    mapCoverTile.MapSerial = mapCoverGraphicSerial;
                }
                tempObjectTiles.Add(mapCoverTile);
            }

            MapBlockData[] GroundTiles = new MapBlockData[len];
            MapBlockData[] ObjectTiles = new MapBlockData[len];
            bool[] blockedIndexs = new bool[len];
            bool[,] nodes = new bool[mapInfo.Width, mapInfo.Height];
            mapInfo.FixPlayerZs = new float[len];

            // CGTool.Logger.Write("开始排序时间:" + DateTime.Now);
            //重新排序
            for (int y = 0; y < mapInfo.Height; y++)
            {
                for (int x = 0; x < mapInfo.Width; x++)
                {
                    // int index = i * (int) mapInfo.Width + ((int) mapInfo.Width - j - 1);
                    int _tmpindex = x + (mapInfo.Height - y - 1) * mapInfo.Width;
                    int index = x + y * mapInfo.Width;
                    
                    MapBlockData mapTile = tempGroundTiles[_tmpindex];
                    MapBlockData ObjectTile = tempObjectTiles[_tmpindex];
                    
                    GroundTiles[index] = mapTile;
                    ObjectTiles[index] = ObjectTile;
                    
                    if (mapTile==null || mapTile.GraphicInfo.Blocked) blockedIndexs[index] = true;

                    //角色默认层级
                    float Z = x * 9 + y * 11;
                    mapInfo.FixPlayerZs[index] = Z;

                    if (ObjectTile != null)
                    {
                        ObjectTile.MapIndex = index;
                        if (!ObjectTile.GraphicInfo.AsGround)
                        {
                            ObjectTile.ObjectZIndex = Z + Math.Min(ObjectTile.GraphicInfo.East,ObjectTile.GraphicInfo.South) * 10 - 10;
                            ObjectTile.x = x;
                            ObjectTile.y = y;
                            
                            if(ObjectTile.GraphicInfo !=null && ObjectTile.GraphicInfo.Blocked) blockedIndexs[index] = true;
                        }
                    }
                    
                    nodes[x, y] = !blockedIndexs[index];
                }
            }

            // Z轴修正
            void resetObjectZ(MapBlockData blockData,float Z = 0f)
            {
                if (!blockData.GraphicInfo.Blocked)
                {
                    blockData.ObjectZIndex = blockData.ObjectZIndex + 0.1f;
                    return;
                }
                int x = blockData.x;
                int y = blockData.y;
                if (Z != 0f)
                {
                    mapInfo.FixPlayerZs[(int)(y * mapInfo.Width + x)] = Z;
                    blockData.ObjectZIndex = Z;
                }
                else
                {
                    Z = blockData.ObjectZIndex;
                }
                int ox = x - 1;
                if (ox >= 0 && (blockData.GraphicInfo.South>2 || blockData.GraphicInfo.Serial==17644))
                {
                    int maxHeight = Math.Min(y + blockData.GraphicInfo.South, mapInfo.Height);
                    float leftZ = Z - 10f;
                    for(int n = y;n<maxHeight;n++)
                    {
                        int _index = (int)(n * mapInfo.Width + ox);
                        mapInfo.FixPlayerZs[_index] = leftZ + (n-y) * 0.1f;
                        if (ObjectTiles[_index] != null) if(blockData.GraphicInfo.Serial != 17644 && ObjectTiles[_index].GraphicInfo.Serial== 17644) resetObjectZ(ObjectTiles[_index],leftZ);
                    }
                }
                
                int oy = y - 1;
                if (oy >= 0 && (blockData.GraphicInfo.East>2 || blockData.GraphicInfo.Serial==17644))
                {
                    int maxWidth = Math.Min(x + blockData.GraphicInfo.East, mapInfo.Width);
                    float rightZ = Z - 10f;
                    for(int n = x;n<maxWidth;n++)
                    {
                        int _index = (int)(oy * mapInfo.Width + n);
                        mapInfo.FixPlayerZs[_index] = rightZ + (n-x) * 0.1f;
                        if (ObjectTiles[_index] != null) if(blockData.GraphicInfo.Serial != 17644 && ObjectTiles[_index].GraphicInfo.Serial == 17644) resetObjectZ(ObjectTiles[_index],rightZ);
                    }
                }
                if (blockData.GraphicInfo.Serial == 17644)
                {
                    x = blockData.x - 1;
                    y = blockData.y - 1;
                    mapInfo.FixPlayerZs[(int)(y * mapInfo.Width + x)] = Z;
                }
            }
            
            //整理Object Z轴层级遮挡及角色遮挡问题
            for (int y = 0; y < mapInfo.Height; y++)
            {
                for (int x = 0; x < mapInfo.Width; x++)
                {
                    int index = x + y * mapInfo.Width;
                    // int objectTileZIndex = index * FixZIndex;

                    MapBlockData ObjectTile = ObjectTiles[index];
                    if(ObjectTile==null || ObjectTile.GraphicInfo==null) continue;
                    
                    //地图单位Z轴补正
                    if (!ObjectTile.GraphicInfo.AsGround && ObjectTile.GraphicInfo.Serial != 17644)
                    {
                        resetObjectZ(ObjectTile, ObjectTile.ObjectZIndex);
                    }

                    //如果物件占地范围大于1x1,则需要处理行走限制
                    if (ObjectTile.GraphicInfo.Blocked && (ObjectTile.GraphicInfo.East > 1 || ObjectTile.GraphicInfo.South > 1))
                    {
                        for (int i = x; i < (x + ObjectTile.GraphicInfo.East); i++)
                        {
                            for (int j = y; j < (y+ ObjectTile.GraphicInfo.South); j++)
                            {
                                if(i>=mapInfo.Width || j>=mapInfo.Height) continue;
                                int _index = (int) (j * mapInfo.Width + i);
                                blockedIndexs[_index] = true;
                                nodes[i, j] = false;
                            }
                        }
                    }
                }
            }

            mapInfo.GroundDatas = GroundTiles.ToList();
            mapInfo.ObjectDatas = ObjectTiles.ToList();
            mapInfo.BlockedIndexs = blockedIndexs;
            mapInfo.MapNodes = nodes;
            _cache[serial] = mapInfo;
            
            Debug.Log("[CGTool] 读取地图: " + mapInfo.Name);
            Debug.Log("地图宽度: " + mapInfo.Width + " 地图高度: " + mapInfo.Height);
            // CGTool.Logger.Write("地图解析完成时间:" + DateTime.Now);
            return mapInfo;
        }
        
    }
}
