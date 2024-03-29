/**
 * 魔力宝贝图档解析脚本 - CGTool
 * 
 * @Author  HonorLee (dev@honorlee.me)
 * @Version 1.0 (2023-11-20)
 * @License GPL-3.0
 *
 * GraphicData.cs 图档解析类
 */

using System;
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
        //图档Sprite(100%PPU)
        public Sprite SpritePPU100;
        //图档主色调,用于小地图绘制
        public Color32 PrimaryColor;
    }
    
    // 图档数据
    public static class GraphicData
    {
        // 常规图档缓存
        public static Dictionary<GraphicInfoData,Dictionary<int,GraphicDetail>> _cache = new Dictionary<GraphicInfoData, Dictionary<int, GraphicDetail>>();
        
        // 线性图档缓存
        public static Dictionary<GraphicInfoData,Dictionary<int,GraphicDetail>> _linearCache = new Dictionary<GraphicInfoData, Dictionary<int, GraphicDetail>>();
        
        // 获取图档
        public static GraphicDetail GetGraphicDetail(GraphicInfoData graphicInfoData, int palet = 0,int subPalet = -1,bool asLinear = false,bool cache = true)
        {
            GraphicDetail graphicDetail = null;

            if (cache)
            {
                var checkCache = asLinear ? _linearCache : _cache;
            
                if (checkCache.ContainsKey(graphicInfoData))
                {
                    if (checkCache[graphicInfoData].ContainsKey(palet))
                    {
                        graphicDetail = checkCache[graphicInfoData][palet];
                    }
                    else
                    {
                        graphicDetail = _loadGraphicDetail(graphicInfoData, palet, subPalet, asLinear);
                        checkCache[graphicInfoData].Add(palet, graphicDetail);
                    }
                }
                else
                {
                    graphicDetail = _loadGraphicDetail(graphicInfoData, palet, subPalet, asLinear);
                    checkCache.Add(graphicInfoData, new Dictionary<int, GraphicDetail>());
                    checkCache[graphicInfoData].Add(palet, graphicDetail);
                }
            }
            else
            {
                graphicDetail = _loadGraphicDetail(graphicInfoData, palet, subPalet, asLinear);
            }
            
            return graphicDetail;
        }

        public static void ClearCache(uint serial,int palet =0,bool asLinear = false)
        {
            var checkCache = asLinear ? _linearCache : _cache;
            GraphicInfoData graphicInfoData = GraphicInfo.GetGraphicInfoData(serial);
            if (graphicInfoData == null) return;
            if (checkCache.ContainsKey(graphicInfoData))
            {
                if (checkCache[graphicInfoData].ContainsKey(palet))
                {
                    GraphicDetail graphicDetail = checkCache[graphicInfoData][palet];
                    if (graphicDetail != null)
                    {
                        UnityEngine.Object.Destroy(graphicDetail.Sprite.texture);
                        if (graphicDetail.Sprite != null)
                        {
                            UnityEngine.Object.Destroy(graphicDetail.Sprite);
                            UnityEngine.Object.Destroy(graphicDetail.SpritePPU100);
                        }
                        graphicDetail = null;
                    }
                    checkCache[graphicInfoData].Remove(palet);
                }
            }
        }
        
        // 解析图档
        private static GraphicDetail _loadGraphicDetail(GraphicInfoData graphicInfoData,int palet = 0,int subPalet = -1,bool asLinear = false)
        {
            GraphicDetail graphicDetail = new GraphicDetail();
            
            //获取图像数据
            List<Color32> pixels = UnpackGraphic(graphicInfoData, palet, subPalet);
            if(pixels==null) return null;
            
            graphicDetail.PrimaryColor = pixels.Last();
            pixels.RemoveAt(pixels.Count - 1);

            //直接通过Texture2D做偏移,并转为Sprite的偏移量
            Vector2 offset = new Vector2(0f, 1f);
            offset.x += -(graphicInfoData.OffsetX * 1f) / graphicInfoData.Width;
            offset.y -= (-graphicInfoData.OffsetY * 1f) / graphicInfoData.Height;
            
            //创建Texture2D对象
            Texture2D texture2D;
            Sprite sprite;

            // RGBA4444 减少内存占用-对边缘增加1px空白,避免黑线
            texture2D = new Texture2D((int) graphicInfoData.Width, (int) graphicInfoData.Height,
                TextureFormat.RGBA4444, false, asLinear);
            // 固定点过滤
            if (asLinear) texture2D.filterMode = FilterMode.Bilinear;
            else texture2D.filterMode = FilterMode.Point;
            texture2D.wrapMode = TextureWrapMode.Clamp;
            texture2D.SetPixels32(pixels.ToArray());
            texture2D.Apply();
            
            sprite = Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height), offset, 1,0,SpriteMeshType.FullRect);

            // 创建PPU为100的sprite
            Sprite spritePPU100 = Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height), offset, 100,0,SpriteMeshType.FullRect);
            
            //写入数据
            graphicDetail.Index = graphicInfoData.Index;
            graphicDetail.Serial = graphicInfoData.Serial;
            graphicDetail.Width = graphicInfoData.Width;
            graphicDetail.Height = graphicInfoData.Height;
            graphicDetail.OffsetX = graphicInfoData.OffsetX;
            graphicDetail.OffsetY = graphicInfoData.OffsetY;
            graphicDetail.Palet = palet;
            graphicDetail.Sprite = sprite;
            graphicDetail.SpritePPU100 = spritePPU100;
            return graphicDetail;
        }

        #region 地图合批
        // 合批数据
        private class BatchData
        {
            public int BatchOffsetX;
            public int BatchOffsetY;
            public GraphicInfoData GraphicInfoData;
            // public List<Color32> Pixels;
            public Color32 PrimaryColor;
            public GraphicDetail GraphicDetail;
        }
        // 图档合批
        private class TextureData
        {
            public int MaxHeight;
            public int MaxWidth;
            public List<BatchData> BatchDatas = new List<BatchData>();
            public List<GraphicInfoData> GraphicInfoDatas = new List<GraphicInfoData>();
        }
        
        /// <summary>
        /// 合批图档
        /// 通过指定图档序列，对图档进行合批处理，并返回合批后的图档数据
        /// </summary>
        /// <param name="graphicInfoDatas">图档索引数据序列</param>
        /// <param name="AsSerialBatch">以图档编号而非索引号返回合批数据</param>
        /// <param name="palet">调色板序号</param>
        /// <param name="subPalet">副调色板编号(针对动画)</param>
        /// <param name="linear">线性过滤</param>
        /// <param name="maxTextureSize">单个Texture最大尺寸，地面数据建议2048，物件数据建议4096</param>
        /// <param name="padding">图档间隔，可以有效避免图档渲染时出现多余的黑边或像素黏连</param>
        /// <param name="compress">启用压缩</param>
        /// <returns>合批后的图档数据，Key(unit)为图档数据编号(或AsSerialBatch为false时为图档序号)，Value为图档数据</returns>
        public static Dictionary<uint, GraphicDetail> BakeGraphics(List<GraphicInfoData> graphicInfoDatas,bool AsSerialBatch = true,int palet = 0,int subPalet = -1,bool linear = false,int maxTextureSize = 2048,int padding = 0,bool compress = false)
        {
            // 单个Texture最大尺寸
            int maxWidth = maxTextureSize;
            int maxHeight = maxTextureSize;
            
            List<TextureData> textureDatas = new List<TextureData>();

            // 根据objectInfos的内,GraphicInfoData的Width,Height进行排序,优先排序Width,使图档从小到大排列
            graphicInfoDatas = graphicInfoDatas.OrderBy(obj => obj.Width).ThenBy(obj => obj.Height).ToList();
            // 去重
            graphicInfoDatas = graphicInfoDatas.Distinct().ToList();

            int offsetX = 0;    // X轴偏移量
            int offsetY = 0;    // Y轴偏移量
            int maxRowHeight = 0;   // 当前行最大高度
            
            TextureData textureData = new TextureData();
            
            for (var i = 0; i < graphicInfoDatas.Count; i++)
            {
                GraphicInfoData graphicInfoData = graphicInfoDatas[i];
                // 如果宽度超过4096,则换行
                if((graphicInfoData.Width + offsetX) > maxWidth)
                {
                    offsetX = 0;
                    offsetY = offsetY + maxRowHeight + padding;
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
                batchData.GraphicDetail = GetGraphicDetail(graphicInfoData, palet, subPalet, linear, true);
                batchData.GraphicInfoData = graphicInfoData;
                batchData.PrimaryColor = batchData.GraphicDetail.PrimaryColor;
                
                textureData.BatchDatas.Add(batchData);
                textureData.GraphicInfoDatas.Add(graphicInfoData);
                
                
                maxRowHeight = Mathf.Max(maxRowHeight, (int) graphicInfoData.Height);
                textureData.MaxHeight = Mathf.Max(textureData.MaxHeight, offsetY + maxRowHeight);
                textureData.MaxWidth = Mathf.Max(textureData.MaxWidth, offsetX + (int) graphicInfoData.Width);
                offsetX += (int) graphicInfoData.Width + padding;
            }
            
            //最后一次合并
            if (textureData.BatchDatas.Count > 0) textureDatas.Add(textureData);
            
            Dictionary<uint, GraphicDetail> graphicDataDic = new Dictionary<uint, GraphicDetail>();
            //合并Texture2D
            for (var i = 0; i < textureDatas.Count; i++)
            {
                TextureData textureDataPiece = textureDatas[i];
                // 将最大高宽调整为4的倍数,提高渲染效率及适配压缩
                textureDataPiece.MaxWidth = (int) Math.Ceiling(textureDataPiece.MaxWidth / 4f) * 4;
                textureDataPiece.MaxHeight = (int) Math.Ceiling(textureDataPiece.MaxHeight / 4f) * 4;
                
                // Debug.Log($"合并第{i}个Texture2D,最大高度:{textureDataPiece.MaxHeight},图像数量:{textureDataPiece.GraphicDatas.Count}");
                Color32[] colors = Enumerable.Repeat(new Color32(0,0,0,0), textureDataPiece.MaxWidth * textureDataPiece.MaxHeight).ToArray();
                Texture2D texture2DPiece = new Texture2D(textureDataPiece.MaxWidth, textureDataPiece.MaxHeight, TextureFormat.RGBA4444, false, linear);
                texture2DPiece.filterMode = linear ? FilterMode.Bilinear : FilterMode.Point;
                texture2DPiece.wrapMode = TextureWrapMode.Clamp;
                texture2DPiece.SetPixels32(colors);
                texture2DPiece.Apply();
                for (var n = 0; n < textureDataPiece.BatchDatas.Count; n++)
                {
                    BatchData batchData = textureDataPiece.BatchDatas[n];
                    GraphicInfoData graphicInfoData = textureDataPiece.GraphicInfoDatas[n];
                    Graphics.CopyTexture(batchData.GraphicDetail.Sprite.texture, 0, 0, 0, 0, (int) graphicInfoData.Width,
                        (int) graphicInfoData.Height, texture2DPiece, 0, 0, batchData.BatchOffsetX,
                        batchData.BatchOffsetY);
                }
                
                Combine(texture2DPiece, textureDataPiece.BatchDatas);
            }
            
            void Combine(Texture2D texture2D,List<BatchData> batchDatas)
            {
                for (var i = 0; i < batchDatas.Count; i++)
                {
                    BatchData batchData = batchDatas[i];
                    //直接通过Texture2D做偏移,并转为Sprite的偏移量
                    Vector2 offset = new Vector2(0f, 1f);
                    offset.x += -(batchData.GraphicInfoData.OffsetX * 1f) / batchData.GraphicInfoData.Width;
                    offset.y -= (-batchData.GraphicInfoData.OffsetY * 1f) / batchData.GraphicInfoData.Height;
                    
                    Sprite sprite = Sprite.Create(texture2D,
                        new Rect(batchData.BatchOffsetX, batchData.BatchOffsetY, (int)batchData.GraphicInfoData.Width,
                            (int)batchData.GraphicInfoData.Height), offset, 1, 0, SpriteMeshType.FullRect);
                    Sprite spritePPU100 = Sprite.Create(texture2D,
                        new Rect(batchData.BatchOffsetX, batchData.BatchOffsetY, (int)batchData.GraphicInfoData.Width,
                            (int)batchData.GraphicInfoData.Height), offset, 100, 0, SpriteMeshType.FullRect);
                    GraphicDetail graphicDetail = new GraphicDetail()
                    {
                        Index = batchData.GraphicInfoData.Index,
                        Serial = batchData.GraphicInfoData.Serial,
                        Width = batchData.GraphicInfoData.Width,
                        Height = batchData.GraphicInfoData.Height,
                        OffsetX = batchData.GraphicInfoData.OffsetX,
                        OffsetY = batchData.GraphicInfoData.OffsetY,
                        Palet = palet,
                        Sprite = sprite,
                        SpritePPU100 = spritePPU100,
                        PrimaryColor = batchData.PrimaryColor
                    };
                    // graphicDataPiece.Sprite = sprite;
                    if (AsSerialBatch) graphicDataDic[graphicDetail.Serial] = graphicDetail; 
                    else graphicDataDic[graphicDetail.Index] = graphicDetail;
                    ClearCache(graphicDetail.Serial);
                    if(compress) texture2D.Compress(true);
                }

                
            }
            return graphicDataDic;
        }
        #endregion
        
        //解压图像数据
        public static List<Color32> UnpackGraphic(GraphicInfoData graphicInfoData, int PaletIndex = 0,
            int SubPaletIndex = -1)
        {
            List<Color32> pixels = new List<Color32>();
            //获取调色板
            List<Color32> palet = null;

            //调整流指针
            BinaryReader fileReader = graphicInfoData.GraphicReader;
            BinaryReader contentReader = null;
            
            if(graphicInfoData.IsEncrypted)
            {
                // 解密
                long position = graphicInfoData.Addr + 3;
                byte[] content;
                // 由于秘钥被嵌入到数据流中,先检查当前位与秘钥位置和长度关系,且图档文件最开始多3个字节的头信息
                // 1.不包含:位置+数据长度小于秘钥索引
                if (position + graphicInfoData.Length < graphicInfoData.EncryptInfo.PwdIndex)
                {
                    // Debug.Log($"不包含秘钥:{graphicInfoData.Index}");
                    // 不处理
                    fileReader.BaseStream.Position = position;
                    content = fileReader.ReadBytes((int)graphicInfoData.Length);
                    
                }else // 2.包含秘钥:位置小于秘钥索引,但是位置+数据长度大于秘钥索引,秘钥将数据分割
                if (position < graphicInfoData.EncryptInfo.PwdIndex && // 数据索引小于秘钥索引
                    position + graphicInfoData.Length > graphicInfoData.EncryptInfo.PwdIndex) // 数据索引+数据长度大于秘钥索引 
                {
                    // Debug.Log($"包含秘钥:{graphicInfoData.Index}");
                    // 读取秘钥前数据,注意这里的长度是秘钥索引-数据索引+3
                    int preLen = (int)(graphicInfoData.EncryptInfo.PwdIndex - position + 3);
                    fileReader.BaseStream.Position = position;
                    byte[] preContent = fileReader.ReadBytes(preLen);
                    // 读取秘钥后数据
                    int nextLen = (int)(graphicInfoData.Length - preLen);
                    fileReader.BaseStream.Position = graphicInfoData.EncryptInfo.PwdIndex + graphicInfoData.EncryptInfo.PwdLen;
                    byte[] nextContent = fileReader.ReadBytes(nextLen);
                    // 合并数据
                    content = preContent.Concat(nextContent).ToArray();
                }
                else // 3.秘钥之后:位置大于秘钥索引,数据位置需要加上秘钥长度
                {
                    // Debug.Log($"秘钥之后:{graphicInfoData.Index}");
                    fileReader.BaseStream.Position = position + graphicInfoData.EncryptInfo.PwdLen;
                    content = fileReader.ReadBytes((int)graphicInfoData.Length);
                }
                // 读取缓存字节集
                contentReader = new BinaryReader(new MemoryStream(content));
                
                int pwdIndex = 0;
                byte[] head = contentReader.ReadBytes(2);
                // 寻找解密头数据的密码索引
                for (int i = 0; i < graphicInfoData.EncryptInfo.Pwd.Length; i++)
                {
                    if((head[0]^graphicInfoData.EncryptInfo.Pwd[i]) == 0x52)
                    {
                        int next = i + 1;
                        if (i == graphicInfoData.EncryptInfo.Pwd.Length - 1) next = 0;
                        if ((head[1] ^ graphicInfoData.EncryptInfo.Pwd[next]) == 0x44)
                        {
                            pwdIndex = i;
                            break;    
                        }
                    }
                }
                contentReader.Dispose();
                
                // 解密数据
                for (int i = 0; i < content.Length; i++)
                {
                    content[i] = (byte)(content[i] ^ graphicInfoData.EncryptInfo.Pwd[pwdIndex]);
                    pwdIndex++;
                    if(pwdIndex >= graphicInfoData.EncryptInfo.Pwd.Length) pwdIndex = 0;
                }
                //读取缓存字节集
                contentReader = new BinaryReader(new MemoryStream(content));
            }
            else
            {
                fileReader.BaseStream.Position = graphicInfoData.Addr;
                //读入目标字节集
                byte[] Content = fileReader.ReadBytes((int)graphicInfoData.Length);

                //读取缓存字节集
                contentReader = new BinaryReader(new MemoryStream(Content));
            }
            
            //16字节头信息
            byte[] RD = contentReader.ReadBytes(2);
            // 研究了几个图档数据,这个字节分别有 0~3 不同类型
            // 猜想一下的话
            // 0:图档无压缩无内置图档
            // 1:图档压缩无内置图档
            // 2:图档无压缩有内置图档
            // 3:图档压缩有内置图档
            int Version = contentReader.ReadByte();
            int Unknow = contentReader.ReadByte();
            uint Width = contentReader.ReadUInt32();
            uint Height = contentReader.ReadUInt32();
            uint DataLen = contentReader.ReadUInt32();
            uint innerPaletLen = 0;

            // 低版本头部长度为16,高版本为20
            int headLen = 16;
            if (Version > 1)
            {
                headLen = 20;
                innerPaletLen = contentReader.ReadUInt32();
            }

            //数据长度
            int contentLen = (int)(DataLen - headLen);
            int pixelLen = (int)(graphicInfoData.Width * graphicInfoData.Height);

            byte[] paletIndexs = graphicInfoData.UnpackedPaletIndex;
            if (paletIndexs == null)
            {
                //解压数据
                byte[] contentBytes =
                    contentReader.ReadBytes((int)Version % 2 == 0 ? (int)(pixelLen + innerPaletLen) : contentLen);
                NativeArray<byte> bytes = new NativeArray<byte>((int)contentBytes.Length, Allocator.TempJob);
                bytes.CopyFrom(contentBytes);
                long decompressLen = pixelLen + innerPaletLen;

                NativeArray<byte> colorIndexs =
                    new NativeArray<byte>((int)decompressLen, Allocator.TempJob);

                DecompressJob decompressJob = new DecompressJob()
                {
                    bytes = bytes,
                    compressd = Version % 2 != 0,
                    colorIndexs = colorIndexs
                };
                // decompressJob.Execute();
                decompressJob.Schedule().Complete();
                paletIndexs = colorIndexs.ToArray();
                graphicInfoData.UnpackedPaletIndex = paletIndexs;
                
                // 如果存在内置调色板,则读取内置调色板
                if (innerPaletLen > 0 && graphicInfoData.InnerPalet == null)
                {
                    byte[] innerPaletIndex = paletIndexs.Skip(pixelLen).Take((int)innerPaletLen).ToArray();
                    graphicInfoData.InnerPalet = AnalysisInnerPalet(innerPaletIndex).ToList();
                    
                }
                
                bytes.Dispose();
                colorIndexs.Dispose();
            }
            
            paletIndexs = paletIndexs.Take(pixelLen).ToArray();

            // palet = Palet.GetPalet(PaletIndex);
            // Debug.Log($"PaletIndex:{PaletIndex},SubPaletIndex:{SubPaletIndex},palet:{palet!=null},InnerPalet:{graphicInfoData.InnerPalet!=null}");
            // 如果指定了外置调色板
            if (SubPaletIndex >= 0)
            {
                palet = Palet.GetPalet(SubPaletIndex);
                if (palet == null)
                {
                    GraphicInfoData subPaletInfoData = GraphicInfo.GetGraphicInfoData((uint)SubPaletIndex);
                    if (subPaletInfoData != null)
                    {
                        Graphic.GetGraphicDetail((uint)SubPaletIndex);
                        if (subPaletInfoData.InnerPalet != null)
                        {
                            palet = subPaletInfoData.InnerPalet;
                            Palet.AddPalet(SubPaletIndex, palet);
                        }
                    }
                }
            }
            
            if (palet == null)
            {
                if (graphicInfoData.InnerPalet != null)
                {
                    // 没有指定外置调色板,存在内置调色板,则读取内置调色板
                    palet = graphicInfoData.InnerPalet;
                }
                else
                {
                    palet = Palet.GetPalet(PaletIndex);
                    if (palet == null) palet = Palet.GetPalet(0);
                }
            }
            
            //释放连接
            contentReader.Dispose();
            contentReader.Close();
            
            //主色调色值
            int r = 0;
            int g = 0;
            int b = 0;
            foreach (int index in paletIndexs)
            {
                Color32 color32;
                if (index == 0 || (index > palet.Count - 1))
                {
                    color32 = Color.clear;
                }
                else
                {
                    color32 = palet[index];
                }
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

        //分析高版本内部调色板
        private static Color32[] AnalysisInnerPalet(byte[] bytes)
        {
            int colorLen = bytes.Length / 3;
            Color32[] palet = new Color32[colorLen + 1];
            for (var i = 0; i < colorLen; i++)
            {
                byte[] paletBytes = bytes.Skip(i * 3).Take(3).ToArray();
                Color32 color32 = new Color32();
                color32.r = (byte)paletBytes[2];
                color32.g = (byte)paletBytes[1];
                color32.b = (byte)paletBytes[0];
                color32.a = (byte)(i == 0 ? 0x00 : 0xFF);
                palet[i] = color32;
            }
            palet[colorLen] = Color.clear;
            return palet;
        }

        #region 测试解压
        private static int[] TestDecompress(byte[] bytes)
        {
            List<int> colorIndexs = new List<int>();
            int _index = -1;

            int next()
            {
                _index++;
                if (_index > bytes.Length - 1) return -1;
                return bytes[_index];
            }

            while (_index < (bytes.Length - 1))
            {
                int head = next();
                if (head == -1) break;

                int repeat = 0;
                if (head < 0x10)
                {
                    repeat = head;
                    for (var i = 0; i < repeat; i++)
                    {
                        colorIndexs.Add(next());
                    }

                }
                else if (head < 0x20)
                {
                    repeat = head % 0x10 * 0x100 + next();
                    for (var i = 0; i < repeat; i++)
                    {
                        colorIndexs.Add(next());
                    }

                }
                else if (head < 0x80)
                {
                    repeat = head % 0x20 * 0x10000 + next() * 0x100 + next();
                    for (var i = 0; i < repeat; i++)
                    {
                        colorIndexs.Add(next());
                    }

                }
                else if (head < 0x90)
                {
                    repeat = head % 0x80;
                    int index = next();
                    for (var i = 0; i < repeat; i++)
                    {
                        colorIndexs.Add(index);
                    }

                }
                else if (head < 0xa0)
                {
                    int index = next();
                    repeat = head % 0x90 * 0x100 + next();
                    for (var i = 0; i < repeat; i++)
                    {
                        colorIndexs.Add(index);
                    }

                }
                else if (head < 0xc0)
                {
                    int index = next();
                    repeat = head % 0xa0 * 0x10000 + next() * 0x100 + next();
                    for (var i = 0; i < repeat; i++)
                    {
                        colorIndexs.Add(index);
                    }

                }
                else if (head < 0xd0)
                {
                    repeat = head % 0xc0;
                    for (var i = 0; i < repeat; i++)
                    {
                        colorIndexs.Add(0);
                    }

                }
                else if (head < 0xe0)
                {
                    repeat = head % 0xd0 * 0x100 + next();
                    for (var i = 0; i < repeat; i++)
                    {
                        colorIndexs.Add(0);
                    }

                }
                else if (head < 0xff)
                {
                    repeat = head % 0xe0 * 0x10000 + next() * 0x100 + next();
                    for (var i = 0; i < repeat; i++)
                    {
                        colorIndexs.Add(0);
                    }
                }
            }

            return colorIndexs.ToArray();


        }

        #endregion 测试解压
        
    }
    
        //解压缩交给IJob处理
    [BurstCompile]
    public struct DecompressJob : IJob
    {
        [ReadOnly]
        public NativeArray<byte> bytes;
        public bool compressd;
        public NativeArray<byte> colorIndexs;
    
        private int _maxIndex;
        private int _index;
        private int _colorIndex;
    
        private int NextByte()
        {
            _index++;
            if (_index > _maxIndex) return -1;
            return bytes[_index];
        }
        private void AddColorIndex(byte index)
        {
            if (_colorIndex > colorIndexs.Length - 1) return;
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
                    int pindex = NextByte();
                    if(pindex==-1) break;
                    AddColorIndex((byte)pindex);
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
                            AddColorIndex((byte)NextByte());
                        }
    
                    }
                    else if (head < 0x20)
                    {
                        repeat = head % 0x10 * 0x100 + NextByte();
                        for (var i = 0; i < repeat; i++)
                        {
                            AddColorIndex((byte)NextByte());
                        }
    
                    }
                    else if (head < 0x80)
                    {
                        repeat = head % 0x20 * 0x10000 + NextByte() * 0x100 + NextByte();
                        for (var i = 0; i < repeat; i++)
                        {
                            AddColorIndex((byte)NextByte());
                        }
    
                    }
                    else if (head < 0x90)
                    {
                        repeat = head % 0x80;
                        byte index = (byte)NextByte();
                        for (var i = 0; i < repeat; i++)
                        {
                            AddColorIndex(index);
                        }
    
                    }
                    else if (head < 0xa0)
                    {
                        byte index = (byte)NextByte();
                        repeat = head % 0x90 * 0x100 + NextByte();
                        for (var i = 0; i < repeat; i++)
                        {
                            AddColorIndex(index);
                        }
    
                    }
                    else if (head < 0xc0)
                    {
                        byte index = (byte)NextByte();
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
                            AddColorIndex(0);
                        }
    
                    }
                    else if (head < 0xe0)
                    {
                        repeat = head % 0xd0 * 0x100 + NextByte();
                        for (var i = 0; i < repeat; i++)
                        {
                            AddColorIndex(0);
                        }
    
                    }
                    else
                    {
                        repeat = head % 0xe0 * 0x10000 + NextByte() * 0x100 + NextByte();
                        for (var i = 0; i < repeat; i++)
                        {
                            AddColorIndex(0);
                        }
                    }
                }
            }
        }
    }
}