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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CGTool
{
    public class GraphicData
    {
        //版本号
        public int Version;
        //索引
        public uint Index;
        //地图编号
        public uint MapSerial;
        //图档宽度
        public uint Width;
        //图档高度
        public uint Height;
        //图档偏移X
        public int OffsetX;
        //图档偏移Y
        public int OffsetY;
        //Palet调色板Index
        public int PaletIndex;
        //图档Sprite
        public Sprite Sprite;
        //图档主色调,用于小地图绘制
        public Color32 PrimaryColor;
    }
    public class Graphic
    {
        //缓存Addr  Version -> Addr -> PaletIndex -> GraphicData
        private static Dictionary<int, Dictionary<uint, Dictionary<int, GraphicData>>> _cache =
            new Dictionary<int, Dictionary<uint, Dictionary<int, GraphicData>>>();
        //
        // //缓存Index映射 Version -> Index -> PaletIndex -> GraphicData
        // private static Dictionary<int, Dictionary<uint, Dictionary<int, GraphicData>>> _indexCache =
        //     new Dictionary<int, Dictionary<uint, Dictionary<int, GraphicData>>>();
        //
        // //缓存MapSerial映射 Version -> MapSerial -> PaletIndex -> GraphicData
        // private static Dictionary<int, Dictionary<uint, Dictionary<int, GraphicData>>> _serialCache =
        //     new Dictionary<int, Dictionary<uint, Dictionary<int, GraphicData>>>();
        
        private static List<string> _graphicPaths = new List<string>()
        {
            //龙之沙漏 之前版本前图档数据
            "Graphic_66.bin",
            //龙之沙漏 版本图档数据
            "GraphicEx_5.bin"
        };
        
        //根据地址获取GraphicData
        public static GraphicData GetGraphicData(GraphicInfoData graphicInfoData,int PaletIndex=0)
        {
            GraphicData graphicData = null;
            //缓存数据
            if (_cache.ContainsKey(graphicInfoData.Version))
            {
                if (_cache[graphicInfoData.Version].ContainsKey(graphicInfoData.Addr))
                {
                    if (_cache[graphicInfoData.Version][graphicInfoData.Addr].ContainsKey(PaletIndex))
                    {
                        graphicData = _cache[graphicInfoData.Version][graphicInfoData.Addr][PaletIndex];
                    }
                }
            }
            //无缓存则加载数据
            if (graphicData == null) graphicData = _loadGraphicData(graphicInfoData, PaletIndex);
            
            return graphicData;
        }

        //初始化加载GraphicInfo
        private static GraphicData _loadGraphicData(GraphicInfoData graphicInfoData, int PaletIndex = 0)
        {
            //查找图档文件
            string fileName = _graphicPaths[graphicInfoData.Version];
            FileInfo file = new FileInfo(CGTool.BaseFolder + "/" + fileName);
            if (!file.Exists) return null;

            //创建流读取器
            FileStream fileStream = file.OpenRead();
            BinaryReader fileReader = new BinaryReader(fileStream);

            //获取调色板
            List<Color32> palet = Palet.GetPalet(PaletIndex);

            GraphicData graphicData = new GraphicData();
            List<Color32> pixels = new List<Color32>();

            //调整流指针
            fileStream.Position = graphicInfoData.Addr;

            //读入目标字节集
            byte[] Content = fileReader.ReadBytes((int) graphicInfoData.Length);

            //关闭文件链接
            fileReader.Dispose();
            fileReader.Close();
            fileStream.Close();

            //读取缓存字节集
            BinaryReader contentReader = new BinaryReader(new MemoryStream(Content));

            //16字节头信息
            byte[] HEAD = contentReader.ReadBytes(2);
            int Version = contentReader.ReadByte();
            int Unknow = contentReader.ReadByte();
            uint Width = contentReader.ReadUInt32();
            uint Height = contentReader.ReadUInt32();
            uint Length = contentReader.ReadUInt32();

            //主色调色值
            int r = 0;
            int g = 0;
            int b = 0;
            //数据长度
            uint contentLen = Length - 16;
            //非压缩型数据
            if (Version == 0)
            {
                while (true)
                {
                    Color32 color32;
                    try
                    {
                        color32 = palet[contentReader.ReadByte()];
                    }
                    catch (Exception e)
                    {
                        break;
                    }
                    pixels.Add(color32);
                    r += color32.r;
                    g += color32.g;
                    b += color32.b;
                }
            }
            else
                //压缩型数据解压
            {
                int count = 0;
                while (true)
                {
                    count++;
                    int head;
                    try
                    {
                        head = contentReader.ReadByte();
                    }
                    catch (Exception e)
                    {
                        break;
                    }
                    
                    int repeat = 0;
                    Color32 color32;
                    if (head < 0x10)
                    {
                        repeat = head;
                        for (var i = 0; i < repeat; i++)
                        {
                            color32 = palet[contentReader.ReadByte()];
                            r += color32.r;
                            g += color32.g;
                            b += color32.b;
                            pixels.Add(color32);
                        }

                    }
                    else if (head < 0x20)
                    {
                        repeat = head % 0x10 * 0x100 + contentReader.ReadByte();
                        for (var i = 0; i < repeat; i++)
                        {
                            color32 = palet[contentReader.ReadByte()];
                            r += color32.r;
                            g += color32.g;
                            b += color32.b;
                            pixels.Add(color32);
                        }

                    }
                    else if (head < 0x80)
                    {
                        repeat = head % 0x20 * 0x10000 + contentReader.ReadByte() * 0x100 + contentReader.ReadByte();
                        for (var i = 0; i < repeat; i++)
                        {
                            color32 = palet[contentReader.ReadByte()];
                            r += color32.r;
                            g += color32.g;
                            b += color32.b;
                            pixels.Add(color32);
                        }

                    }
                    else if (head < 0x90)
                    {
                        repeat = head % 0x80;
                        color32 = palet[contentReader.ReadByte()];
                        for (var i = 0; i < repeat; i++)
                        {
                            r += color32.r;
                            g += color32.g;
                            b += color32.b;
                            pixels.Add(color32);
                        }

                    }
                    else if (head < 0xa0)
                    {
                        color32 = palet[contentReader.ReadByte()];
                        repeat = head % 0x90 * 0x100 + contentReader.ReadByte();
                        for (var i = 0; i < repeat; i++)
                        {
                            r += color32.r;
                            g += color32.g;
                            b += color32.b;
                            pixels.Add(color32);
                        }

                    }
                    else if (head < 0xc0)
                    {
                        color32 = palet[contentReader.ReadByte()];
                        repeat = head % 0xa0 * 0x10000 + contentReader.ReadByte() * 0x100 + contentReader.ReadByte();
                        for (var i = 0; i < repeat; i++)
                        {
                            r += color32.r;
                            g += color32.g;
                            b += color32.b;
                            pixels.Add(color32);
                        }

                    }
                    else if (head < 0xd0)
                    {
                        color32 = Color.clear;
                        repeat = head % 0xc0;
                        for (var i = 0; i < repeat; i++)
                        {
                            r += color32.r;
                            g += color32.g;
                            b += color32.b;
                            pixels.Add(color32);
                        }

                    }
                    else if (head < 0xe0)
                    {
                        color32 = Color.clear;
                        repeat = head % 0xd0 * 0x100 + contentReader.ReadByte();
                        for (var i = 0; i < repeat; i++)
                        {
                            r += color32.r;
                            g += color32.g;
                            b += color32.b;
                            pixels.Add(color32);
                        }

                    }
                    else
                    {
                        color32 = Color.clear;
                        repeat = head % 0xe0 * 0x10000 + contentReader.ReadByte() * 0x100 + contentReader.ReadByte();
                        for (var i = 0; i < repeat; i++)
                        {
                            r += color32.r;
                            g += color32.g;
                            b += color32.b;
                            pixels.Add(color32);
                        }
                    }
                }
            }

            //主色调计算及提亮
            r = r / pixels.Count * 2;
            g = g / pixels.Count * 2;
            b = b / pixels.Count * 2;
            if (r > 255) r = 255;
            if (g > 255) g = 255;
            if (b > 255) b = 255;
            //主色调
            Color32 primaryColor32 = new Color32((byte) r, (byte) g, (byte) b, 255);

            //释放连接
            contentReader.Dispose();
            contentReader.Close();

            //创建Sprite对象
            Texture2D texture2D = new Texture2D((int) graphicInfoData.Width, (int) graphicInfoData.Height,
                TextureFormat.RGBA32, false);
            
            int len = (int) (graphicInfoData.Width * graphicInfoData.Height);
            if (pixels.Count != len)
            {
                if (pixels.Count > len)
                {
                    pixels = pixels.GetRange(0, len);
                }
                else
                {
                    Color32[] temc = new Color32[len - pixels.Count];
                    ArrayList.Repeat(Color.clear, len - pixels.Count).CopyTo(temc);
                    pixels.AddRange(temc);
                }

            }

            texture2D.SetPixels32(pixels.ToArray());
            texture2D.filterMode = FilterMode.Point;
            // texture2D.Compress(true);
            texture2D.Apply();
            
            //直接通过Texture2D做偏移,并转为Sprite的偏移量
            Vector2 offset = new Vector2(0f, 1f);
            offset.x += -(graphicInfoData.OffsetX * 1f) / graphicInfoData.Width;
            offset.y -= (-graphicInfoData.OffsetY * 1f) / graphicInfoData.Height;

            Sprite sprite = Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height), offset, 1,1,SpriteMeshType.FullRect);

            //写入数据
            graphicData.Version = graphicInfoData.Version;
            graphicData.Index = graphicInfoData.Index;
            graphicData.MapSerial = graphicInfoData.MapSerial;
            graphicData.Width = graphicInfoData.Width;
            graphicData.Height = graphicInfoData.Height;
            graphicData.OffsetX = graphicInfoData.OffsetX;
            graphicData.OffsetY = graphicInfoData.OffsetY;
            graphicData.PaletIndex = PaletIndex;
            graphicData.Sprite = sprite;
            graphicData.PrimaryColor = primaryColor32;
            
            //缓存
            if (!_cache.ContainsKey(graphicInfoData.Version))
                _cache.Add(graphicInfoData.Version, new Dictionary<uint, Dictionary<int, GraphicData>>());
            if(!_cache[graphicInfoData.Version].ContainsKey(graphicInfoData.Addr)) _cache[graphicInfoData.Version].Add(graphicInfoData.Addr,new Dictionary<int, GraphicData>());
            if (!_cache[graphicInfoData.Version][graphicInfoData.Addr].ContainsKey(PaletIndex))
                _cache[graphicInfoData.Version][graphicInfoData.Addr].Add(PaletIndex, graphicData);
            
            return graphicData;
        }
        
    }
}
