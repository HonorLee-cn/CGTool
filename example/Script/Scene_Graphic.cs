using System.Collections;
using System.Collections.Generic;
using CrossgateToolkit;
using UnityEngine;
using UnityEngine.UI;

public class Scene_Graphic : MonoBehaviour
{
    [SerializeField,Header("单张图档")]
    private Image SingleGraphic;
    [SerializeField,Header("合批图档")]
    private Image BatchGraphic;
    [SerializeField,Header("动态合批图档")]
    private Image DynamicBatchGraphic;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    private void Awake()
    {
        Util.Init();
        GetSingleGraphic();
        
        // 两种合批方式
        // 可以观察两种合批方式所形成的图像图集差异
        GetBatchGraphic();  // 一次性合批图像
        StartCoroutine(GetDynamicBatchGraphic());  // 动态合批图像
    }
    
    public void GetSingleGraphic()
    {
        // 获取单张图档
        GraphicDetail graphicDetail = CrossgateToolkit.Graphic.GetGraphicDetail(10302);
        SingleGraphic.sprite = graphicDetail.SpritePPU100;
        SingleGraphic.SetNativeSize();
        // 获取单张图档的PPU-100的图像
        // SingleGraphic.sprite = CrossgateToolkit.Graphic.GetGraphicDetail(4116).SpritePPU100;
    }
    
    public void GetBatchGraphic()
    {
        // 以合批方式获取并处理图像图集
        // 此处使用图档编号(非索引)获取图像
        uint[] serials = new uint[]
        {
            // 地面
            4116,4117,4118,4119,4120,4121,4122,4123,4124,4125,
            // 其他不同尺寸图像
            10561,10571,10573,10443,10403
        };
        
        // 获取合批图集前,需要手动获取整理图像信息数据
        List<GraphicInfoData> graphicInfoDatas = new List<GraphicInfoData>();
        for (int i = 0; i < serials.Length; i++)
        {
            graphicInfoDatas.Add(CrossgateToolkit.GraphicInfo.GetGraphicInfoData(serials[i]));
        }

        // 合批图像,并获取对应编号与图像映射
        Dictionary<uint,GraphicDetail> graphicDetails = GraphicData.BakeGraphics(graphicInfoDatas, true, 0, -1, false, 256, 5);
        
        // 获取合批的原图像
        Texture2D texture2D = graphicDetails[4116].SpritePPU100.texture;
        BatchGraphic.sprite = Sprite.Create(texture2D,new Rect(0,0,texture2D.width,texture2D.height),new Vector2(0.5f,0.5f));
        BatchGraphic.SetNativeSize();
        // 说明:合批图集定义最大图集尺寸为512,合批后的图像图集,尺寸不会超过512
        // 一次性合批的图像会根据图像尺寸由小到大,由低到高方式进行排列处理,直到达到最大尺寸限制
    }
    
    IEnumerator GetDynamicBatchGraphic()
    {
        // 以动态图集方式获取并处理图像图集
        // 此处使用图档编号(非索引)获取图像
        uint[] serials = new uint[]
        {
            // 地面
            4116,4117,4118,4119,4120,4121,4122,4123,4124,4125,
            // 其他不同尺寸图像
            10561,10571,10573,10443,10403
        };
        
        // 动态图集无需手动获取整理图像信息数据
        
        // 首先定义图集
        DynamicGraphic dynamicGraphic = new DynamicGraphic(256,256,5,0,false,true);
        // 动态图集为自动动态扩容图集
        // 当当前图集内无空余空间容纳当前填充图形时,会自动进行扩容,形成新的图集图像,但尺寸不会超过定义的图集尺寸

        // 可以直接通过图像编号填充图集并获取填充后的图像数据
        Sprite sprite = dynamicGraphic.GetSprite(4116);
        sprite = dynamicGraphic.GetSprite(4117);
        
        // 可以手动清除图集避免内存泄漏
        dynamicGraphic.Clear();
        
        // 亦可通过协程方式填充图集,避免大批量获取图像时造成主线程卡顿
        // 协程方式填充图集需要通过回调方式获取填充后的图像数据
        bool next = false;
        for (var i = 0; i < serials.Length; i++)
        {
            StartCoroutine(
                dynamicGraphic.GetSpriteSync(serials[i], (outputSprite) =>
                {
                    // 获取到图像数据后的处理
                    // 此处可以将图像数据赋值给Image等UI组件
                    // 也可以将图像数据保存到本地等
                    sprite = outputSprite;
                    next = true;
                }));
            yield return new WaitUntil(() => next);
        }
        
        Texture2D texture2D = sprite.texture;
        DynamicBatchGraphic.sprite = Sprite.Create(texture2D,new Rect(0,0,texture2D.width,texture2D.height),new Vector2(0.5f,0.5f));
        DynamicBatchGraphic.SetNativeSize();
    }
}
