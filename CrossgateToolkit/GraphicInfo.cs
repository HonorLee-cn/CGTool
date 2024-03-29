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
        public byte VERSION_FLAG;
        //已解压的调色板索引
        public byte[] UnpackedPaletIndex;
        public List<Color32> InnerPalet;

        public bool IsEncrypted;
        public EncryptInfo EncryptInfo;
    }

    public class EncryptInfo
    {
        public long PwdIndex;
        public int PwdLen;
        public byte[] Pwd;
        
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
            
            byte[] head = graphicFileReader.ReadBytes(3);
            bool isEncrypted = false;
            EncryptInfo encryptInfo = null;
            if (head[0] == 0x52 && head[1] == 0x44) isEncrypted = false;
            else
            {
                isEncrypted = true;
                bool fromHead = head[0] % 2 == 0;
                encryptInfo = new EncryptInfo();
                encryptInfo.PwdLen = head[2];
                
                // 密码索引
                if (fromHead)
                {
                    encryptInfo.PwdIndex = 3 + head[1];
                }
                else
                {
                    // 获取文件大小
                    long fileSize = graphicFile.Length;
                    encryptInfo.PwdIndex = fileSize - encryptInfo.PwdLen - 3 - head[1] + 3;
                }

                // 获取密码
                graphicFileStream.Position = encryptInfo.PwdIndex;
                encryptInfo.Pwd = graphicFileReader.ReadBytes(encryptInfo.PwdLen);
                
                // 密码解密
                byte[] keyCodes = new byte[CGTool.ENCRYPT_KEY.Length];
                for (var i = 0; i < CGTool.ENCRYPT_KEY.Length; i++)
                {
                    // 获取秘钥的ASCII码
                    byte code = (byte)(CGTool.ENCRYPT_KEY[i]);
                    keyCodes[i] = code;
                }

                // 解密密码
                for (int i = 0; i < encryptInfo.PwdLen; i++)
                {
                    for (var i1 = 0; i1 < keyCodes.Length; i1++)
                    {
                        encryptInfo.Pwd[i] = (byte)(encryptInfo.Pwd[i] ^ keyCodes[i1]);

                    }
                }
            }
            
            //解析Info数据表
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
                graphicInfoData.Blocked =  fileReader.ReadByte() % 2 == 0;
                graphicInfoData.AsGround = fileReader.ReadByte() == 1;
                graphicInfoData.Unknow = fileReader.ReadBytes(4);
                graphicInfoData.Serial = BitConverter.ToUInt32(fileReader.ReadBytes(4),0);
                graphicInfoData.GraphicReader = graphicFileReader;
                graphicInfoData.IsEncrypted = isEncrypted;
                graphicInfoData.EncryptInfo = encryptInfo;
                // if (graphicInfoData.Serial >= 220000 && graphicInfoData.Width == 64 && graphicInfoData.Height == 47 && graphicInfoData.Blocked)
                // {
                //     Debug.LogError(graphicInfoData.Serial + "穿越" + Blocked);
                // }
                
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

            Debug.Log("[CGTool] 加载GraphicInfo - 文件: [" +
                      // (Graphic.Flag_HighVersion[Version] ? "H" : "N") + "] [" +
                      Version + "] " +
                      graphicInfoFile.Name + " 贴图总量: " + DataLength + (isEncrypted ? " 加密图档" : ""));
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