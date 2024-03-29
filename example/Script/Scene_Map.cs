using System.Collections;
using System.Collections.Generic;
using CrossgateToolkit;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Scene_Map : MonoBehaviour
{
    [SerializeField,Header("地图渲染用Camera")]
    private Camera MapCamera;
    [SerializeField,Header("地图地面TileMap")]
    private Tilemap TileMap_Ground;
    [SerializeField,Header("地图物件TileMap")]
    private Tilemap TileMap_Object;
    
    [SerializeField,Header("地图Grid")]
    private Grid MapGrid;

    [SerializeField, Header("地图背景音乐")]
    private AudioSource MapBGM;
    
    
    [SerializeField,Header("小地图TileMap")]
    private Tilemap TileMap_MiniMap;
    [SerializeField,Header("小地图TileSprite")]
    private Sprite MiniMapSprite;
    
    // 地图数据
    private MapInfo mapInfo;
    // 地图音乐
    private uint mapBGMID;
    // 地图图像数据
    private Dictionary<uint,GraphicDetail> GroundGraphicDetails;
    private Dictionary<uint,GraphicDetail> ObjectGraphicDetails;
    
    // Start is called before the first frame update
    void Start()
    {
        // 地图渲染相机需要注意调整排序方式
        MapCamera.transparencySortMode = TransparencySortMode.CustomAxis;
        MapCamera.transparencySortAxis = new Vector3(0, 1, -0.1f);
        
        LoadMap();
    }

    private void Awake()
    {
        Util.Init();
    }

    private void LoadMap()
    {
        // 获取地图信息,此处以编号为1000的法兰城地图为例
        mapInfo = Map.GetMap(1000);
        
        // 地图的数据已经过重新排序处理,无论加载的是服务端地图或是客户端地图,均按照场景中所示排序规则进行处理
        
        // 图集合批处理
        BakeMapGraphics();
        
        // 绘制地图,此处直接绘制全图
        // 实际使用中推荐以卡马克卷轴算法进行局部绘制,以提高性能
        DrawMapGround();
        DrawMapObject();
        
        // 调整位置
        int x = mapInfo.Width / 2 * 32 - mapInfo.Height / 2 * 32;
        int y = -mapInfo.Height / 2 * 24 - mapInfo.Width / 2 * 24;
        MapGrid.GetComponent<RectTransform>().localPosition = new Vector3(x, y, 0);
        
        // 绘制小地图
        DrawMiniMap();
    }

    // 对地图图像进行合批处理
    private void BakeMapGraphics()
    {
        // 以合批图集的形式缓存地图图像数据
        // 此处仅以一次性合批图集方式进行示范
        // 如若针对服务端动态地图方式,则需要动态合批图集,可参考Scene_Graphic.cs中的GetDynamicBatchGraphic()方法进行处理
        
        List<GraphicInfoData> graphicInfoDatas;
        // 获取地面数据并进行合批
        graphicInfoDatas = new List<GraphicInfoData>();
        for (var i = 0; i < mapInfo.GroundDatas.Count; i++)
        {
            MapBlockData mapBlockData = mapInfo.GroundDatas[i];
            if(mapBlockData==null || mapBlockData.GraphicInfo==null) continue;
            graphicInfoDatas.Add(mapBlockData.GraphicInfo);
        }
        GroundGraphicDetails = GraphicData.BakeGraphics(graphicInfoDatas, true, 0, -1, false, 2048);
        
        // 获取物件数据并进行合批
        graphicInfoDatas = new List<GraphicInfoData>();
        for (var i = 0; i < mapInfo.ObjectDatas.Count; i++)
        {
            MapBlockData mapBlockData = mapInfo.ObjectDatas[i];
            if(mapBlockData==null || mapBlockData.GraphicInfo==null) continue;
            graphicInfoDatas.Add(mapBlockData.GraphicInfo);
        }
        ObjectGraphicDetails = GraphicData.BakeGraphics(graphicInfoDatas, true, 0, -1, false, 2048);
    }

    // 绘制地图地面
    private void DrawMapGround()
    {
        int width = mapInfo.Width;
        int height = mapInfo.Height;
        List<Vector3Int> drawPositions = new List<Vector3Int>();
        List<Tile> drawTiles = new List<Tile>();
        for(int x = 0; x < width; x++)
        {
            for(int y = 0; y < height; y++)
            {
                MapBlockData mapBlockData = mapInfo.GroundDatas[y * width + x];
                if(mapBlockData==null || mapBlockData.GraphicInfo==null) continue;
                GraphicDetail graphicDetail = GroundGraphicDetails[mapBlockData.MapSerial];
                Tile groundTile = Tile.CreateInstance(typeof(Tile)) as Tile;
                groundTile.sprite = graphicDetail.Sprite;
                drawPositions.Add(new Vector3Int(x, y, 0));
                drawTiles.Add(groundTile);
            }
        }
        
        TileMap_Ground.SetTiles(drawPositions.ToArray(), drawTiles.ToArray());
    }
    
    // 绘制地图物件
    private void DrawMapObject()
    {
        int width = mapInfo.Width;
        int height = mapInfo.Height;
        List<Vector3Int> drawPositions = new List<Vector3Int>();
        List<Tile> drawTiles = new List<Tile>();
        for(int x = 0; x < width; x++)
        {
            for(int y = 0; y < height; y++)
            {
                MapBlockData mapBlockData = mapInfo.ObjectDatas[y * width + x];
                if(mapBlockData==null || mapBlockData.GraphicInfo==null) continue;
                if (mapBlockData.GraphicInfo.Serial < 1000)
                {
                    // 小于1000的编号为音频编号
                    if (mapBGMID == 0)
                    {
                        mapBGMID = mapBlockData.MapSerial;
                        // 播放背景音乐
                        Audio.Play(MapBGM, Audio.Type.BGM, (int)mapBGMID);
                    }
                }
                GraphicDetail graphicDetail = ObjectGraphicDetails[mapBlockData.MapSerial];
                Tile objectTile = Tile.CreateInstance(typeof(Tile)) as Tile;
                objectTile.sprite = graphicDetail.Sprite;
                // 注意排序的修正,mapBlockData提供了基础的Z轴修正,如需更精确的效果请自行计算处理
                drawPositions.Add(new Vector3Int(x, y, 0));
                drawTiles.Add(objectTile);
            }
        }
        
        TileMap_Object.SetTiles(drawPositions.ToArray(), drawTiles.ToArray());
    }
    
    // 绘制小地图
    private void DrawMiniMap()
    {
        // CGTool提供了简单的处理方式
        // 可以直接通过获取图档数据的主色调来处理小地图
        
        List<Vector3Int> drawPositions = new List<Vector3Int>();
        List<Tile> drawTiles = new List<Tile>();
        
        for(int x = 0; x < mapInfo.Width; x++)
        {
            for(int y = 0; y < mapInfo.Height; y++)
            {
                MapBlockData mapBlockData = mapInfo.GroundDatas[y * mapInfo.Width + x];
                MapBlockData mapObjectData = mapInfo.ObjectDatas[y * mapInfo.Width + x];
                if(mapBlockData==null && mapObjectData==null) continue;

                Tile tile = Tile.CreateInstance(typeof(Tile)) as Tile;
                tile.sprite = MiniMapSprite;
                
                
                if (mapObjectData == null)
                {
                    // 没有地面物件时使用地面主色
                    tile.color = GroundGraphicDetails[mapBlockData.MapSerial].PrimaryColor;
                }
                else
                {
                    // 有地面物件时使用物件主色,此处只做演示,正常情况下需要根据建筑物的占地尺寸对相应区块进行填充以提高小地图的视觉效果
                    tile.color = ObjectGraphicDetails[mapObjectData.MapSerial].PrimaryColor;
                }
                
                drawPositions.Add(new Vector3Int(x, y, 0));
                drawTiles.Add(tile);
            }
        }
        
        TileMap_MiniMap.SetTiles(drawPositions.ToArray(), drawTiles.ToArray());
    }
}
