using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace CrossgateToolkit
{
    // 针对网游设置的可扩展动态图集
    // 包含图集建立、自动维护和清理
    public class DynamicGraphic
    {
        // 动态图集数据
        public class DynamicData
        {
            // 图集高度
            public int Width;
            // 图集宽度
            public int Height;
            // 图集Texture
            public Texture2D Texture2D;
            // 图集Sprite引用及编号银映射
            // public List<Sprite> RefSpriteList = new List<Sprite>();
            // public Dictionary<uint, int> RefSpriteMap = new Dictionary<uint, int>();
            // public Dictionary<uint,Color> PrimaryColor = new Dictionary<uint, Color>();
            // 二维数组存储当前所有像素点可用位置 - 此数组在4096*4096情况下,占用约16MB内存
            // public bool[,] PixelAvaliable;
            // 在二维空间中,记录当前可用的矩形区域
            public List<Rect> AvaliableRect = new List<Rect>();
            public bool avaliable = true;
            public bool CompressWhenFull = false;
            
            public void Init(bool Linear = false)
            {
                // 初始化时填补List的0位,避免序列号为0的图像无法获取
                // RefSpriteList.Add(null);
                AvaliableRect.Add(new Rect(0, 0, Width, Height));
				float time = Time.realtimeSinceStartup;
                Texture2D = new Texture2D(Width, Height, TextureFormat.RGBA4444, false);
                Texture2D.name = "DynamicGraphicTexture";
                Texture2D.filterMode = Linear ? FilterMode.Bilinear : FilterMode.Point;
                Texture2D.wrapMode = TextureWrapMode.Clamp;
                // Texture2D.Compress(true);
                // Texture2d填充透明像素
                // Color32[] transColor = new Color32[Width * Height];
                // for (int i = 0; i < Width * Height; i++)
                // {
                //     transColor[i] = Color.clear;
                // }
                // Texture2D.SetPixels32(transColor);
                // Texture2D.Apply();
                // transColor = null;
            }

            private int bestLongSideFit;
            private int bestShortSideFit;
            
            // 查找最佳位置
            public Rect FindBestFitRect(int width, int height)
            {
                Rect bestRect = default;
                bestShortSideFit = int.MaxValue;
                bool hasAvaliable = false;
                for (int i = 0; i < AvaliableRect.Count; ++i)
                {
	                if(AvaliableRect[i].width>64 && AvaliableRect[i].height>48) hasAvaliable = true;
                    if (AvaliableRect[i].width >= width && AvaliableRect[i].height >= height)
                    {
                        int leftoverHoriz = Mathf.Abs((int)AvaliableRect[i].width - width);
                        int leftoverVert = Mathf.Abs((int)AvaliableRect[i].height - height);
                        int shortSideFit = Mathf.Min(leftoverHoriz, leftoverVert);
                        int longSideFit = Mathf.Max(leftoverHoriz, leftoverVert);

                        if (shortSideFit < bestShortSideFit || (shortSideFit == bestShortSideFit && longSideFit < bestLongSideFit))
                        {
                            bestRect.x = AvaliableRect[i].x;
                            bestRect.y = AvaliableRect[i].y;
                            bestRect.width = width;
                            bestRect.height = height;
                            bestShortSideFit = shortSideFit;
                            bestLongSideFit = longSideFit;
                        }
                    }
                    
                }
                // 检测过程中判断是否还有有效空间
                avaliable = hasAvaliable;
                if (!avaliable)
                {
	                // 当有效空间为0时,对texture压缩
	                if(CompressWhenFull) Texture2D.Compress(true);
                }
                return bestRect;
            }
            
            // 插入位置
            public void InsertRect (Rect node)
            {
	            // float time = Time.time;
	            // Stopwatch stopwatch = new Stopwatch();
	            // stopwatch.Start();
                int numRectanglesToProcess = AvaliableRect.Count;
                for (int i = 0; i < numRectanglesToProcess; ++i)
                {
                    if (SplitFreeRect(AvaliableRect[i], ref node))
                    {
                        AvaliableRect.RemoveAt(i);
                        --i;
                        --numRectanglesToProcess;
                    }
                }

                // 重新整理可用区域,去除重叠区域
                OptFreeList();
                // stopwatch.Stop();
                // Debug.Log("[CGTool] 插入位置耗时: " + stopwatch.ElapsedMilliseconds + "ms");
            }
            
            // 分割区域
            bool SplitFreeRect (Rect freeRect, ref Rect useRect)
            {
                Rect rect = default;
		        if (useRect.x >= freeRect.x + freeRect.width || useRect.x + useRect.width <= freeRect.x ||
			        useRect.y >= freeRect.y + freeRect.height || useRect.y + useRect.height <= freeRect.y)
			        return false;

		        if (useRect.x < freeRect.x + freeRect.width && useRect.x + useRect.width > freeRect.x)
		        {
			        if (useRect.y > freeRect.y && useRect.y < freeRect.y + freeRect.height)
			        {
				        rect = freeRect;
				        rect.height = useRect.y - rect.y;
                        AvaliableRect.Add(rect);
			        }
                    
			        if (useRect.y + useRect.height < freeRect.y + freeRect.height)
			        {
				        rect = freeRect;
				        rect.y = useRect.y + useRect.height;
				        rect.height = freeRect.y + freeRect.height - (useRect.y + useRect.height);
                        AvaliableRect.Add(rect);
			        }
		        }

		        if (useRect.y < freeRect.y + freeRect.height && useRect.y + useRect.height > freeRect.y)
		        {
			        if (useRect.x > freeRect.x && useRect.x < freeRect.x + freeRect.width)
			        {
                        rect = freeRect;
				        rect.width = useRect.x - rect.x;
                        AvaliableRect.Add(rect);
			        }
                    
			        if (useRect.x + useRect.width < freeRect.x + freeRect.width)
			        {
				        rect = freeRect;
				        rect.x = useRect.x + useRect.width;
				        rect.width = freeRect.x + freeRect.width - (useRect.x + useRect.width);
                        AvaliableRect.Add(rect);
			        }
		        }

		        return true;
	        }

            // 重新整理可用区域,去除重叠区域
	        void OptFreeList ()
	        {
		        for (int i = 0; i < AvaliableRect.Count; ++i)
			        for (int j = i + 1; j < AvaliableRect.Count; ++j)
			        {
				        if (IsIntersect(AvaliableRect[i], AvaliableRect[j]))
				        {
					        AvaliableRect.RemoveAt(i);
					        --i;
					        break;
				        }
				        if (IsIntersect(AvaliableRect[j], AvaliableRect[i]))
				        {
					        AvaliableRect.RemoveAt(j);
					        --j;
				        }
			        }
	        }

            // 比对两个矩形是否重叠
	        bool IsIntersect (Rect a, Rect b)
	        {
		        return a.x >= b.x && a.y >= b.y
			        && a.x + a.width <= b.x + b.width
			        && a.y + a.height <= b.y + b.height;
	        }
        }
        
        private class ClearMono:MonoBehaviour
        {
	        public void Clear()
	        {
		        Destroy(gameObject);
	        }
        }

        // 动态图集最大宽度
        public int MaxGraphicWidth;
        // 动态图集最大高度
        public int MaxGraphicHeight;
        // 动态图集内部间隔
        public int Padding;
        // 动态图集是否使用线性采样
        public bool Linear;
        // 动态图集是否使用100ppu
        public bool PPU100;
        // 当动态图档无可用空间时进行压缩
        public bool CompressWhenFull;
        // 使用调色板编号
        public int PaletIndex;
        // 动态图集固定图像尺寸
        public Vector2Int FixedSize = default;
        // 当前动态图集包含的图集数据
        public List<DynamicData> DynamicDatas = new List<DynamicData>();
        // Sprite缓存
        public Dictionary<uint, Sprite> SpriteCache = new Dictionary<uint, Sprite>();
        // 主色缓存
        public Dictionary<uint, Color> PrimaryColorCache = new Dictionary<uint, Color>();

        private static ClearMono clearMono;
        
        /// <summary>
        /// 创建动态图集
        /// </summary>
        /// <param name="GraphicWidth">图集最大宽度</param>
        /// <param name="GraphicHeight">图集最大高度</param>
        /// <param name="GraphicPadding">图集各图档间隔</param>
        /// <param name="palet">调色板编号</param>
        /// <param name="linear">线性过滤</param>
        /// <param name="ppu100">以100的PPU方式生成Sprite对象</param>
        /// <param name="compressWhenFull">当图集对象无可用空间时,对Texture进行压缩</param>
        public DynamicGraphic(int GraphicWidth, int GraphicHeight, int GraphicPadding = 0,int palet = 0,bool linear = false,bool ppu100 = false,bool compressWhenFull = false)
        {
            MaxGraphicWidth = GraphicWidth;
            MaxGraphicHeight = GraphicHeight;
            Padding = GraphicPadding;
            PaletIndex = palet;
            Linear = linear;
            PPU100 = ppu100;
            CompressWhenFull = compressWhenFull;
            if (clearMono == null) clearMono = new GameObject("DynamicGraphicClear").AddComponent<ClearMono>();
        }
        
        
        // 清理图集图像
        public void Clear(bool autoGC = false)
        {
	        // clearMono.StartCoroutine(ClearCoroutine(DynamicDatas, autoGC));
	        ClearCoroutine(DynamicDatas, autoGC);
	        DynamicDatas = new List<DynamicData>();
        }
        
        // 获取图像
        public Sprite GetSprite(uint Serial)
        {
            Sprite sprite = null;
            // 检查缓存
            SpriteCache.TryGetValue(Serial, out sprite);
            if (sprite != null) return sprite;

            // 获取图档数据
            GraphicInfoData graphicInfoData = GraphicInfo.GetGraphicInfoData(Serial);
            if (graphicInfoData == null)
            {
	            Debug.LogError("[CGTool] 无法获取图档数据: " + Serial);
	            return null;
            }
            // Debug.Log("[CGTool] 获取图档数据: " + Serial + " " + graphicInfoData.Width + "x" + graphicInfoData.Height);

            // 更新尺寸,增加Padding
            int width = (int)graphicInfoData.Width + Padding;
            int height = (int)graphicInfoData.Height + Padding;
            
            // 首先检查最新的图集数据是否有空间
            DynamicData lastDynamicData = null;
            bool avaliable = true;
            Rect avaliableRect = default;

            // 检测是否有图集数据
            if (DynamicDatas.Count > 0)
            {
	            for (var i = 0; i < DynamicDatas.Count; i++)
	            {
		            lastDynamicData = DynamicDatas[i];
		            if (!lastDynamicData.avaliable)
		            {
			            lastDynamicData = null;
			            continue;
		            }
		            avaliableRect = lastDynamicData.FindBestFitRect(width, height);
		            if (avaliableRect != default) break;
	            }
                // lastDynamicData = DynamicDatas[^1];
                // avaliableRect = lastDynamicData.FindBestFitRect(width, height);
            }
            else avaliable = false;

            
            
            // 如果没有可用数据集或没有空间则创建新的图集
            if (!avaliable || avaliableRect==default)
            {
                DynamicData newDynamicData = new DynamicData();
                newDynamicData.Width = MaxGraphicWidth;
                newDynamicData.Height = MaxGraphicHeight;
                
                // 初始化
                newDynamicData.Init(Linear);
                newDynamicData.CompressWhenFull = CompressWhenFull;
                DynamicDatas.Add(newDynamicData);
                lastDynamicData = newDynamicData;
                avaliableRect = lastDynamicData.FindBestFitRect(width, height);
                Debug.Log("[CGTool] 创建新图集: " + newDynamicData.Width + "x" + newDynamicData.Height + " 当前图集数量: " +
                          DynamicDatas.Count);
            }
            
            
            // 获取图档像素
            List<Color32> color32s;
            Color PrimaryColor = Color.clear;
            Texture texture = null;
            bool useCache = true;

            // 填充图集,填充方式为可用区域的左下角开始
            if (useCache)
            {
	            // 二级缓存模式
	            GraphicDetail graphicDetail = GraphicData.GetGraphicDetail(graphicInfoData, PaletIndex, 0, Linear, true);
	            if (graphicDetail == null) return null;
	            texture = graphicDetail.Sprite.texture;
	            PrimaryColor = graphicDetail.PrimaryColor;
	            Graphics.CopyTexture(texture, 0, 0, 0,0,
		            (int)graphicInfoData.Width, (int)graphicInfoData.Height, lastDynamicData.Texture2D, 0, 0, (int)avaliableRect.x, (int)avaliableRect.y);
            }
            else
            {
	            // 非缓存模式
	            color32s = GraphicData.UnpackGraphic(graphicInfoData, PaletIndex);
	            if (color32s == null || color32s.Count == 0) return null;
            
	            // 去除最后一位主色
	            // lastDynamicData.PrimaryColor[Serial] = color32s[^1];
	            PrimaryColor = color32s[^1];
	            color32s.RemoveAt(color32s.Count - 1);
	            lastDynamicData.Texture2D.SetPixels32((int)avaliableRect.x, (int)avaliableRect.y,
	             (int)graphicInfoData.Width, (int)graphicInfoData.Height, color32s.ToArray(), 0);
	            lastDynamicData.Texture2D.Apply(false, false);
	            color32s = null;
            }
            
            //直接通过Texture2D做偏移,并转为Sprite的偏移量
            Vector2 offset = new Vector2(0f, 1f);
            offset.x += -(graphicInfoData.OffsetX * 1f) / graphicInfoData.Width;
            offset.y -= (-graphicInfoData.OffsetY * 1f) / graphicInfoData.Height;
            
            // 创建Sprite
            sprite = Sprite.Create(lastDynamicData.Texture2D,
	            new Rect(avaliableRect.x, avaliableRect.y, graphicInfoData.Width, graphicInfoData.Height),
	            offset, PPU100 ? 100 : 1, 0, SpriteMeshType.FullRect);
            sprite.name = "DG-" + Serial;
            
            SpriteCache[Serial] = sprite;
            PrimaryColorCache[Serial] = PrimaryColor;

            // 更新当前可用区域
            lastDynamicData.InsertRect(avaliableRect);
            return sprite;
        }

        public IEnumerator GetSpriteSync(uint Serial, Action<Sprite> callback)
        {
	        Sprite sprite = null;
			SpriteCache.TryGetValue(Serial, out sprite);
			if (sprite!=null)
			{
				callback?.Invoke(sprite);
				yield break;
			}
			yield return null;
	        sprite = GetSprite(Serial);
	        callback?.Invoke(sprite);
		}
        
        // 获取主色
        public Color GetPrimaryColor(uint Serial)
        {
	        SpriteCache.TryGetValue(Serial, out var sprite);
	        if(sprite==null) clearMono.StartCoroutine(GetSpriteSync(Serial, s => sprite = s));
	        PrimaryColorCache.TryGetValue(Serial, out var color);
	        // if(color==null) color = Color.clear;
	        return color;
		}
        
        private void ClearCoroutine(List<DynamicData> dynamicDatas, bool autoGC = false)
        {
	        PrimaryColorCache.Clear();
	        Dictionary<uint, Sprite> spriteCache = SpriteCache;
	        SpriteCache = new Dictionary<uint, Sprite>();
			     
	        List<Texture> textures = new List<Texture>();
			     
	        foreach (var keyValuePair in spriteCache)
	        {
		        Sprite sprite = keyValuePair.Value;
		        if (sprite == null) continue;
		        if(!textures.Contains(sprite.texture)) textures.Add(sprite.texture);
		        Object.Destroy(sprite);
		        sprite = null;
		        GraphicData.ClearCache(keyValuePair.Key);
	        }
	        foreach (var texture in textures)
	        {
		        Object.Destroy(texture);
		        // Resources.UnloadAsset(texture);
	        }
			     
	        DynamicDatas.Clear();
	        if (autoGC)
	        {
		        Resources.UnloadUnusedAssets();
		        // System.GC.Collect();
	        }
        }
        
        // 协程清理有可能导致无法清除干净,暂时不用
  //       IEnumerator ClearCoroutine(List<DynamicData> dynamicDatas, bool autoGC = false)
  //       {
	 //        PrimaryColorCache.Clear();
	 //        Dictionary<uint, Sprite> spriteCache = SpriteCache;
	 //        SpriteCache = new Dictionary<uint, Sprite>();
		// 	
		// 	List<Texture> textures = new List<Texture>();
		// 	
		// 	foreach (var keyValuePair in spriteCache)
		// 	{
		// 		Sprite sprite = keyValuePair.Value;
		// 		if (sprite == null) continue;
		// 		if(!textures.Contains(sprite.texture)) textures.Add(sprite.texture);
		// 		spriteCache.Remove(keyValuePair.Key);
		// 		sprite = null;
		// 		// Resources.UnloadAsset(sprite);
		// 	}
		// 	yield return null;
		// 	foreach (var texture in textures)
		// 	{
		// 		Object.Destroy(texture);
		// 		yield return null;
		// 	}
		// 	
		// 	DynamicDatas.Clear();
		// 	yield return null;
		// 	if (autoGC)
		// 	{
		// 		Resources.UnloadUnusedAssets();
		// 		// System.GC.Collect();
		// 	}
		// }
    }
}