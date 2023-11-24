/**
 * 魔力宝贝图档解析脚本 - CGTool
 * 
 * @Author  HonorLee (dev@honorlee.me)
 * @Version 1.0 (2023-11-20)
 * @License GPL-3.0
 *
 * GraphicData.cs 图档解析类
 */

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace CrossgateToolkit
{
    //图档数据详情
    public class GraphicDetail
    {
        //索引
        public uint Index;
        //编号
        public uint Serial;
        //图档宽度
        public uint Width;
        //图档高度
        public uint Height;
        //图档偏移X
        public int OffsetX;
        //图档偏移Y
        public int OffsetY;
        //Palet调色板Index
        public int Palet;
        //图档Sprite
        public Sprite Sprite;
        //图档主色调,用于小地图绘制
        public Color32 PrimaryColor;
    }
    // 图档数据
    public static class GraphicData
    {
        // // 图档缓存  Serial -> Palet -> GraphicDetail
        // public static Dictionary<uint,Dictionary<int,GraphicDetail>> _cache = new Dictionary<uint, Dictionary<int, GraphicDetail>>();
        //
        // // 图档索引缓存 Index -> Palet -> GraphicDetail
        // public static Dictionary<uint,Dictionary<int,GraphicDetail>> _indexCache = new Dictionary<uint, Dictionary<int, GraphicDetail>>();
        
        public static Dictionary<GraphicInfoData,Dictionary<int,GraphicDetail>> _cache = new Dictionary<GraphicInfoData, Dictionary<int, GraphicDetail>>();
        
        // 获取图档
        public static GraphicDetail GetGraphicDetail(GraphicInfoData graphicInfoData, int palet = 0)
        {
            GraphicDetail graphicDetail = null;
            if (_cache.ContainsKey(graphicInfoData))
            {
                if (_cache[graphicInfoData].ContainsKey(palet))
                {
                    graphicDetail = _cache[graphicInfoData][palet];
                }
                else
                {
                    graphicDetail = _loadGraphicDetail(graphicInfoData, palet);
                    _cache[graphicInfoData].Add(palet, graphicDetail);
                }
            }
            else
            {
                graphicDetail = _loadGraphicDetail(graphicInfoData, palet);
                _cache.Add(graphicInfoData, new Dictionary<int, GraphicDetail>());
                _cache[graphicInfoData].Add(palet, graphicDetail);
            }
            
            return graphicDetail;
        }
        
        // 解析图档
        private static GraphicDetail _loadGraphicDetail(GraphicInfoData graphicInfoData,int palet = 0)
        {
            GraphicDetail graphicDetail = new GraphicDetail();
            
            //获取图像数据
            List<Color32> pixels = UnpackGraphic(graphicInfoData, palet);
            graphicDetail.PrimaryColor = pixels.Last();
            pixels.RemoveAt(pixels.Count - 1);

            //直接通过Texture2D做偏移,并转为Sprite的偏移量
            Vector2 offset = new Vector2(0f, 1f);
            offset.x += -(graphicInfoData.OffsetX * 1f) / graphicInfoData.Width;
            offset.y -= (-graphicInfoData.OffsetY * 1f) / graphicInfoData.Height;
            
            //创建Texture2D对象
            Texture2D texture2D;
            Sprite sprite;

            // RGBA4444 减少内存占用
            texture2D = new Texture2D((int) graphicInfoData.Width, (int) graphicInfoData.Height,
                TextureFormat.RGBA4444, false, false);
            // 固定点过滤
            texture2D.filterMode = FilterMode.Point;
            texture2D.SetPixels32(pixels.ToArray());
            // texture2D.LoadRawTextureData(rawTextureData);
            texture2D.Apply();
            
            sprite = Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height), offset, 1,1,SpriteMeshType.FullRect);

            //写入数据
            graphicDetail.Index = graphicInfoData.Index;
            graphicDetail.Serial = graphicInfoData.Serial;
            graphicDetail.Width = graphicInfoData.Width;
            graphicDetail.Height = graphicInfoData.Height;
            graphicDetail.OffsetX = graphicInfoData.OffsetX;
            graphicDetail.OffsetY = graphicInfoData.OffsetY;
            graphicDetail.Palet = palet;
            graphicDetail.Sprite = sprite;
            return graphicDetail;
        }

        #region 地图合批
        private class BatchData
        {
            public int BatchOffsetX;
            public int BatchOffsetY;
            public GraphicDetail GraphicDetail;
        }
        //预备地图缓存
        public static Dictionary<uint, GraphicDetail> BakeAsGround(List<GraphicInfoData> groundInfos,int palet=0)
        {
            
            Dictionary<uint, GraphicDetail> graphicDataDic = new Dictionary<uint, GraphicDetail>();
            // _mapSpriteMap[MapID].Add(PaletIndex, graphicDataDic);
            
            List<BatchData> batchDatas = new List<BatchData>();
            Texture2D texture2D = null;
            
            for (var i = 0; i < groundInfos.Count; i++)
            {
                //每1344个图像合并一次,即不超过2048*2048尺寸
                if (i % 1344 == 0)
                {
                    //合并
                    if (i != 0) Combine(texture2D, batchDatas);
                    
                    //清空
                    batchDatas.Clear();
                    int height = 2048;

                    if (i + 1344 > groundInfos.Count-1)
                    {
                        height = Mathf.CeilToInt((groundInfos.Count - i) / 32f) * 48;
                    }
                    texture2D = new Texture2D(2048, height, TextureFormat.RGBA4444, false, true);
                    texture2D.filterMode = FilterMode.Point;
                    //默认填充全透明
                    Color32[] colors = Enumerable.Repeat(new Color32(0, 0, 0, 0), 2048 * height).ToArray();
                    texture2D.SetPixels32(colors);
                }
                
                GraphicInfoData graphicInfoData = groundInfos[i];
                GraphicDetail graphicData = GetGraphicDetail(graphicInfoData, palet);
                
                int x = i % 32 * 64;
                int y = i / 32 * 48;

                if(graphicData!=null && graphicData.Sprite!=null)
                {
                    Color32[] pixels = graphicData.Sprite.texture.GetPixels32();
                    texture2D.SetPixels32(x, y, (int) graphicInfoData.Width, (int) graphicInfoData.Height,
                        pixels.ToArray());    
                }
                BatchData batchData = new BatchData();
                

                batchData.BatchOffsetX = x;
                batchData.BatchOffsetY = y;
                batchData.GraphicDetail = graphicData;

                batchDatas.Add(batchData);
            }
            
            //最后一次合并
            if (batchDatas.Count > 0)
            {
                Combine(texture2D, batchDatas);
                batchDatas.Clear();
            }

            void Combine(Texture2D texture2D,List<BatchData> batchDatas)
            {
                texture2D.Apply();
                for (var i = 0; i < batchDatas.Count; i++)
                {
                    GraphicDetail graphicDataPiece = batchDatas[i].GraphicDetail;
                    //直接通过Texture2D做偏移,并转为Sprite的偏移量
                    Vector2 offset = new Vector2(0f, 1f);
                    offset.x += -(graphicDataPiece.OffsetX * 1f) / graphicDataPiece.Width;
                    offset.y -= (-graphicDataPiece.OffsetY * 1f) / graphicDataPiece.Height;
                        
                    int X = i % 32 * 64;
                    int Y = i / 32 * 48;

                    Sprite sprite = Sprite.Create(texture2D,
                        new Rect(X, Y, (int)graphicDataPiece.Width, (int)graphicDataPiece.Height), offset, 1, 1,
                        SpriteMeshType.FullRect);
                    
                    GraphicDetail graphicData = new GraphicDetail()
                    {
                        Index = graphicDataPiece.Index,
                        Serial = graphicDataPiece.Serial,
                        Width = graphicDataPiece.Width,
                        Height = graphicDataPiece.Height,
                        OffsetX = graphicDataPiece.OffsetX,
                        OffsetY = graphicDataPiece.OffsetY,
                        Palet = graphicDataPiece.Palet,
                        Sprite = sprite,
                        PrimaryColor = graphicDataPiece.PrimaryColor
                    };

                    graphicDataDic.Add(graphicData.Serial, graphicData);
                }
            }

            return graphicDataDic;
        }
        
        //预备地图物件缓存
        private class TextureData
        {
            public int MaxHeight;
            public int MaxWidth;
            public List<BatchData> BatchDatas = new List<BatchData>();
            public List<GraphicInfoData> GraphicInfoDatas = new List<GraphicInfoData>();
        }
        public static Dictionary<uint, GraphicDetail> BakeAsObject(List<GraphicInfoData> objectInfos,int palet = 0)
        {
            // 单个Texture最大尺寸
            int maxWidth = 4096;
            int maxHeight = 4096;
            
            List<TextureData> textureDatas = new List<TextureData>();
            Dictionary<uint, GraphicDetail> graphicDataDic = new Dictionary<uint, GraphicDetail>();

            // 根据objectInfos的内,GraphicInfoData的Width,Height进行排序,优先排序Width,使图档从小到大排列
            objectInfos = objectInfos.OrderBy(obj => obj.Width).ThenBy(obj => obj.Height).ToList();

            int offsetX = 0;    // X轴偏移量
            int offsetY = 0;    // Y轴偏移量
            int maxRowHeight = 0;   // 当前行最大高度
            
            TextureData textureData = new TextureData();
            
            for (var i = 0; i < objectInfos.Count; i++)
            {
                GraphicInfoData graphicInfoData = objectInfos[i];
                // 如果宽度超过4096,则换行
                if((graphicInfoData.Width + offsetX) > maxWidth)
                {
                    offsetX = 0;
                    offsetY = offsetY + maxRowHeight + 5;
                    maxRowHeight = 0;
                }
                // 如果高度超过2048,则生成新的Texture2D
                if ((graphicInfoData.Height + offsetY) > maxHeight)
                {
                    offsetX = 0;
                    offsetY = 0;
                    maxRowHeight = 0;
                    textureDatas.Add(textureData);
                    textureData = new TextureData();
                }
                
                BatchData batchData = new BatchData();
                batchData.BatchOffsetX = offsetX;
                batchData.BatchOffsetY = offsetY;
                batchData.GraphicDetail = GetGraphicDetail(graphicInfoData, palet);

                // graphicDatas.Add(graphicData);
                
                textureData.BatchDatas.Add(batchData);
                textureData.GraphicInfoDatas.Add(graphicInfoData);
                
                
                maxRowHeight = Mathf.Max(maxRowHeight, (int) graphicInfoData.Height);
                textureData.MaxHeight = Mathf.Max(textureData.MaxHeight, offsetY + maxRowHeight);
                textureData.MaxWidth = Mathf.Max(textureData.MaxWidth, offsetX + (int) graphicInfoData.Width);
                offsetX += (int) graphicInfoData.Width + 5;
            }
            
            //最后一次合并
            if (textureData.BatchDatas.Count > 0) textureDatas.Add(textureData);
            
            //合并Texture2D
            for (var i = 0; i < textureDatas.Count; i++)
            {
                TextureData textureDataPiece = textureDatas[i];
                // Debug.Log($"合并第{i}个Texture2D,最大高度:{textureDataPiece.MaxHeight},图像数量:{textureDataPiece.GraphicDatas.Count}");
                Color32[] colors = Enumerable.Repeat(new Color32(0,0,0,0), textureDataPiece.MaxWidth * textureDataPiece.MaxHeight).ToArray();
                Texture2D texture2DPiece = new Texture2D(textureDataPiece.MaxWidth, textureDataPiece.MaxHeight, TextureFormat.RGBA4444, false, false);
                texture2DPiece.filterMode = FilterMode.Point;
                texture2DPiece.SetPixels32(colors);
                for (var n = 0; n < textureDataPiece.BatchDatas.Count; n++)
                {
                    BatchData batchData = textureDataPiece.BatchDatas[n];
                    GraphicInfoData graphicInfoData = textureDataPiece.GraphicInfoDatas[n];

                    if (batchData.GraphicDetail!=null)
                    {
                        Color32[] pixels = batchData.GraphicDetail.Sprite.texture.GetPixels32();
                        texture2DPiece.SetPixels32(batchData.BatchOffsetX, batchData.BatchOffsetY, (int) graphicInfoData.Width, (int) graphicInfoData.Height,
                            pixels.ToArray());
                    }
                }
                texture2DPiece.Apply();
                Combine(texture2DPiece, textureDataPiece.BatchDatas);
            }

            void Combine(Texture2D texture2D,List<BatchData> batchDatas)
            {
                for (var i = 0; i < batchDatas.Count; i++)
                {
                    BatchData batchData = batchDatas[i];
                    //直接通过Texture2D做偏移,并转为Sprite的偏移量
                    Vector2 offset = new Vector2(0f, 1f);
                    offset.x += -(batchData.GraphicDetail.OffsetX * 1f) / batchData.GraphicDetail.Width;
                    offset.y -= (-batchData.GraphicDetail.OffsetY * 1f) / batchData.GraphicDetail.Height;

                    Sprite sprite = Sprite.Create(texture2D, new Rect(batchData.BatchOffsetX, batchData.BatchOffsetY, (int)batchData.GraphicDetail.Width, (int)batchData.GraphicDetail.Height),offset, 1, 1, SpriteMeshType.FullRect);
                    GraphicDetail graphicDetail = new GraphicDetail()
                    {
                        Index = batchData.GraphicDetail.Index,
                        Serial = batchData.GraphicDetail.Serial,
                        Width = batchData.GraphicDetail.Width,
                        Height = batchData.GraphicDetail.Height,
                        OffsetX = batchData.GraphicDetail.OffsetX,
                        OffsetY = batchData.GraphicDetail.OffsetY,
                        Palet = batchData.GraphicDetail.Palet,
                        Sprite = sprite,
                        PrimaryColor = batchData.GraphicDetail.PrimaryColor
                    };
                    
                    // graphicDataPiece.Sprite = sprite;
                    graphicDataDic.Add(graphicDetail.Serial, graphicDetail);
                }
            }

            return graphicDataDic;
        }
        #endregion
        
        //解压图像数据
        private static List<Color32> UnpackGraphic(GraphicInfoData graphicInfoData,int PaletIndex=0){
            List<Color32> pixels = new List<Color32>();
            //获取调色板
            List<Color32> palet = Palet.GetPalet(PaletIndex);

            //调整流指针
            BinaryReader fileReader = graphicInfoData.GraphicReader;
            fileReader.BaseStream.Position = graphicInfoData.Addr;

            //读入目标字节集
            byte[] Content = fileReader.ReadBytes((int) graphicInfoData.Length);

            //读取缓存字节集
            BinaryReader contentReader = new BinaryReader(new MemoryStream(Content));

            //16字节头信息
            byte[] HEAD = contentReader.ReadBytes(2);
            int Version = contentReader.ReadByte();
            int Unknow = contentReader.ReadByte();
            uint Width = contentReader.ReadUInt32();
            uint Height = contentReader.ReadUInt32();
            uint Length = contentReader.ReadUInt32();

            
            //数据长度
            uint contentLen = Length - 16;
            int pixelLen = (int) (graphicInfoData.Width * graphicInfoData.Height);

            int[] paletIndex;
            if (graphicInfoData.UnpackedPaletIndex == null)
            {
                //解压数据
                byte[] contentBytes = contentReader.ReadBytes((int) contentLen);
                NativeArray<byte> bytes = new NativeArray<byte>((int) contentBytes.Length, Allocator.TempJob);
                
                bytes.CopyFrom(contentBytes);

                // Debug.Log(contentBytes.Length + "   " + bytes.Length);
                NativeArray<int> colorIndexs =
                    new NativeArray<int>(pixelLen, Allocator.TempJob);


                DecompressJob decompressJob = new DecompressJob()
                {
                    bytes = bytes,
                    compressd = Version != 0,
                    colorIndexs = colorIndexs
                };
                // decompressJob.Execute();
                decompressJob.Schedule().Complete();
                bytes.Dispose();
                paletIndex = colorIndexs.ToArray();
                graphicInfoData.UnpackedPaletIndex = paletIndex;
                colorIndexs.Dispose();
            }
            else
            {
                paletIndex = graphicInfoData.UnpackedPaletIndex;
            }

            //释放连接
            contentReader.Dispose();
            contentReader.Close();
            
            //主色调色值
            int r = 0;
            int g = 0;
            int b = 0;
            foreach (int index in paletIndex)
            {
                // Debug.Log(index);
                Color32 color32 = palet[index];
                pixels.Add(color32);
                r += color32.r;
                g += color32.g;
                b += color32.b;
            }
            //主色调计算及提亮
            r = r / pixels.Count * 3;
            g = g / pixels.Count * 3;
            b = b / pixels.Count * 3;
            if (r > 255) r = 255;
            if (g > 255) g = 255;
            if (b > 255) b = 255;

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

            
            //主色调加入最后
            pixels.Add(new Color32((byte) r, (byte) g, (byte) b, 255));
            return pixels;
        }
    }
    
        //解压缩交给IJob处理
    [BurstCompile]
    public struct DecompressJob : IJob
    {
        [ReadOnly]
        public NativeArray<byte> bytes;
        public bool compressd;
        public NativeArray<int> colorIndexs;
    
        private int _maxIndex;
        private int _index;
        private int _colorIndex;
    
        private int NextByte()
        {
            _index++;
            if (_index > _maxIndex) return -1;
            return bytes[_index];
        }
        private void AddColorIndex(int index)
        {
            colorIndexs[_colorIndex] = index;
            _colorIndex++;
        }
        [BurstCompile]
        public void Execute()
        {
            _maxIndex = bytes.Length - 1;
            _index = -1;
            _colorIndex = 0;
            
            if (!compressd)
            {
                while (_index<=_maxIndex)
                {
                    int index = NextByte();
                    if(index==-1) break;
                    AddColorIndex(index);
                }
            }
            else
                //压缩型数据解压
            {
                // int count = 0;
                while (_index<=_maxIndex)
                {
                    // count++;
                    int head = NextByte();
                    if(head==-1) break;

                    int repeat = 0;
                    if (head < 0x10)
                    {
                        repeat = head;
                        for (var i = 0; i < repeat; i++)
                        {
                            AddColorIndex(NextByte());
                        }
    
                    }
                    else if (head < 0x20)
                    {
                        repeat = head % 0x10 * 0x100 + NextByte();
                        for (var i = 0; i < repeat; i++)
                        {
                            AddColorIndex(NextByte());
                        }
    
                    }
                    else if (head < 0x80)
                    {
                        repeat = head % 0x20 * 0x10000 + NextByte() * 0x100 + NextByte();
                        for (var i = 0; i < repeat; i++)
                        {
                            AddColorIndex(NextByte());
                        }
    
                    }
                    else if (head < 0x90)
                    {
                        repeat = head % 0x80;
                        int index = NextByte();
                        for (var i = 0; i < repeat; i++)
                        {
                            AddColorIndex(index);
                        }
    
                    }
                    else if (head < 0xa0)
                    {
                        int index = NextByte();
                        repeat = head % 0x90 * 0x100 + NextByte();
                        for (var i = 0; i < repeat; i++)
                        {
                            AddColorIndex(index);
                        }
    
                    }
                    else if (head < 0xc0)
                    {
                        int index = NextByte();
                        repeat = head % 0xa0 * 0x10000 + NextByte() * 0x100 + NextByte();
                        for (var i = 0; i < repeat; i++)
                        {
                            AddColorIndex(index);
                        }
    
                    }
                    else if (head < 0xd0)
                    {
                        repeat = head % 0xc0;
                        for (var i = 0; i < repeat; i++)
                        {
                            AddColorIndex(256);
                        }
    
                    }
                    else if (head < 0xe0)
                    {
                        repeat = head % 0xd0 * 0x100 + NextByte();
                        for (var i = 0; i < repeat; i++)
                        {
                            AddColorIndex(256);
                        }
    
                    }
                    else
                    {
                        repeat = head % 0xe0 * 0x10000 + NextByte() * 0x100 + NextByte();
                        for (var i = 0; i < repeat; i++)
                        {
                            AddColorIndex(256);
                        }
                    }
                }
            }
        }
    }
}