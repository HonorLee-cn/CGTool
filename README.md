# Crossgate-魔力宝贝
## Unity C# 图档解析库

## 1、开源目的
本脚本旨在学习研究```Crossgate``` ```魔力宝贝``` 图档bin文件的解压与使用，相关资料来源于互联网文章与相关技术大佬指导

再次感谢```阿伍```等在互联网上分享相关解压、算法解析等教学文章的技术大佬所提供的帮助

本脚本遵照GPL共享协议，可随意修改、调整代码学习使用

请务删除或修改文档或源代码中相关版权声明信息，且严禁用于商业项目

因利用本脚本盈利或其他行为导致的任何商业版权纠纷，脚本作者不承担任何责任或损失

本解析库原始发布地址: [https://git.honorlee.me/HonorLee/CGTool](https://git.honorlee.me/HonorLee/CGTool)

同步更新至Github: [https://github.com/HonorLee-cn/CGTool](https://github.com/HonorLee-cn/CGTool)

如有任何问题请至Github提交Issues或联系作者

![预览](Preview/Preview.png)

## 2、功能支持
> 当前版本目前已支持 魔力宝贝3.7-龙之沙漏 及以下版本的图档解析，以及 4.0 版本以上图档、动画数据解析
>
> <font color="red">注意：当前版本 4.0 及以上地图暂不支持</font>
>
> 目前版本支持以下功能：
> 
> * `GraphicInfo` [图档索引解析](#获取图档索引数据)
> * `GraphicData` [图档数据解析](#获取图档实体数据)
> * `Palet` 调色板数据解析
> * `Map` [服务端/客户端 图数据解析](#获取地图数据)
> * `Audio` [音频索引及加载](#播放音频)
> * `AnimeInfo` 动画索引解析
> * `Anime` 动画数据解析
> * `AnimePlayer` [动画播放器挂载组件](#动画播放) 
>   * `AnimePlayer` 动画关键帧(攻击/攻击完成)事件回调
>   * `AnimePlayer` 音频帧事件回调
> * `DynamicGraphic` [动态图集](#动态图集)

<div style="display: flex;flex-direction: column;align-items: center;">
<div><img style="" src="Preview/AnimeSupport.gif"><img style="" src="Preview/AnimeHSupport.gif"></div>
<div><img style="" src="Preview/AnimePlayer.png"></div>
<div><img style="" src="Preview/PUK3Graphic.jpg"></div>

</div>

## 3、使用说明

### 基本环境

请确认Unity已安装以下Package包:
```
"dependencies": {
    "com.unity.burst": "1.6.0",
    "com.unity.visualscripting": "1.9.0"
}
```

克隆当前仓库或下载zip包解压，将 CrossgateToolkit 文件夹放置于Unity项目文件夹内引用

可直接下载Release中unitypackage直接导入

### 工具库说明

最新 V2.0 版本已移除对魔力宝贝原版本的强绑定，初始化程序将根据目标路径进行自动分析

规划图档目录结构时，图档根目录下建议以数字版本号方式命名不同版本图档子目录

CGTool在初始化时对所配置图档根目录进行扫描，并按照``根目录优先``、``子目录按字符排序``方式依次加载并初始化图档数据

所涉及的所有Index则为图档序号、Serial为图档或动画的具体编号而非索引档中序号，实际使用时请注意区分



其他相关部分会逐渐更新完善

### 框架初始化
在入口或初始化脚本头部引入CGTool初始化文件
```csharp
using CrossgateToolkit;
```
并在相关初始化位置对CGTool进行初始化
```csharp
// 配置Crossgate相关资源路径，如跳过则默认为 Environment.CurrentDirectory 下相关 目录
CGTool.PATH = new CGTool.CGPath()
{
    // 调色板目录，不可省略
    PAL = Application.persistentDataPath + "/pal",
    // BIN图档目录，包含图档索引、图档文件、动画索引、动画文件等根目录
    // 初始化时会自动便利查询分析所有文件数据，不可省略
    BIN = Application.persistentDataPath + "/bin",
    // 地图文件目录，省略则不对地图数据初始化
    MAP = Application.persistentDataPath + "/map",
    // 音频文件目录，省略则不对音频初始化
    BGM = Application.persistentDataPath + "/bgm",
    AUDIO = Application.persistentDataPath + "/se"
};
// 初始化
CGTool.Init();
```
CGTool初始化时，会自动对相关索引Info文件进行解析，请根据实际所采用版本情况，对脚本代码中解析相关的文件名称进行修改调整


### 获取图档索引数据
(图档基本索引数据属性信息)
```csharp
// 正常通过编号获取图档信息,无需版本号(常规图档)
GraphicInfoData graphicInfoData = GraphicInfo.GetGraphicInfoData(uint Serial);

// 通过索引获取图档信息,带版本号(特殊无编号图档获取,如动画获取每帧图档时)
GraphicInfoData graphicInfoData = GraphicInfo.GetGraphicInfoDataByIndex(string version, uint Serial);
```

### 获取图档实体数据
(图档实际数据,包含图像Sprite资源)
```csharp
// 直接通过编号获取
GraphicDetail graphicDetail = Graphic.GetGraphicDetail(uint serial,int palet = 0);
// 或 通过GraphicInfoData获取
GraphicDetail graphicDetail = GraphicData.GetGraphicDetail(GraphicInfoData graphicInfoData, int palet = 0);
/**
 * 使用说明:
 * 所有通过Graphic获取的图档Sprite均已做偏移处理,可直接使用
 * 需自行判断图档是否存在,并处理图档不存在的情况
 * 1.获取指定编号的图档索引信息
 * 2.根据获取到的索引取得图档数据
 * 3.使用图档数据中的Sprite资源进行绘制
 */

GraphicInfoData graphicInfoData = GraphicInfo.GetGraphicInfoData(Serial);
GraphicDetail graphicDetail = GraphicData.GetGraphicDetail(graphicInfoData ,0);
SpriteRenderer(Image).sprite = graphicDetail.Sprite;
```

### 获取地图数据
```csharp
// 通过编号获取地图数据,地图数据中包含地面及地上物件数据以及寻路用的二维数组
// 可自行阅读源码查询相关属性成员和使用方式
Map.MapInfo mapInfo = Map.GetMap(uint Serial);
```

### 图档合批

为减少渲染时的Drawcall和Batchs动态合批数量提高性能并降低内存消耗，工具库提供了大量图档合批（并）处理方法

图档合并时，会自动根据每个图档图像由小到大进行排序处理，以最大限度将多个图档合并至一个或多个稍大的Texture2D中

```csharp
/**
* 合批图档
* 通过指定图档序列，对图档进行合批处理，并返回合批后的图档数据
* @param graphicInfoDatas 图档索引数据序列
* @param AsSerialBatch 以图档编号而非索引号返回合批数据,默认为true,反之则以图档索引号返回数据
* @param palet 调色板序号
* @param subPalet 副调色板序号,此项针对动画数据
* @param linear 以线性过滤方式生成图集
* @param maxTextureSize 单个Texture最大尺寸，地面数据建议2048，物件数据建议低于4096
* @param padding 图档间隔，可以有效避免图档渲染时出现多余的黑边或像素黏连
* @param compress 启用压缩
* @return Dictionary 合批后的图档数据，Key(unit)为图档数据编号(或AsSerialBatch为false时为图档序号)，Value为图档数据
*/
GraphicData.BakeGraphics(
    List<GraphicInfoData> graphicInfoDatas,
    int palet = 0,
    int maxTextureSize = 2048,
    int padding = 0)
```

### 地图地面/物件图档合批数据
针对地图图档部分合批提供了相应的简化方法，可以酌情使用
```csharp
/**
 * * 地面数据将拼合成一个或多个2048*2048尺寸Texture2D
 * * 物件(建筑等)拼合成一个或多个不大于4096*4096尺寸Texture2D
 * 另: 由于4.0后地图模式变动,部分地图图档过大,所以这个方法可能不适用于4.0后的地图
 */

// 地面合批
Dictionary<int, GraphicDetail> MapGroundSerialDic =
    Map.BakeGrounds(    // <= 合并地面图形
        List<GraphicInfoData> graphicInfoDatas,
        int PaletIndex = 0
    );

// 物件合批
Dictionary<int, GraphicDetail> MapObjectSerialDic =
    Graphic.BakeObjects(    // <= 合并物件图形
        List<GraphicInfoData> graphicInfoDatas,
        int PaletIndex = 0
    );
```
![地面合并效果](Preview/MapGroundMix.png)
![物件合并效果](Preview/MapObjectMix.png)
![资源合并后效果](Preview/batches.png)

### 播放音频
```csharp
CGTool.Audio.Play(AudioSource audioSource,Type type, int serial)

//播放背景音乐
CGTool.Audio.Play(AudioSource audioSource,Audio.Type.BGM,int serial);
//获取音效音频
CGTool.Audio.Play(AudioSource audioSource,Audio.Type.EFFECT,int serial);
```

### 动画播放
```csharp
/**
* 动画播放器,用于播放CG动画,支持多动画队列播放
* 1.3更新后已无需手动挂载SpriteRenderer、Image组件，程序会自动处理
* 只需将AnimePlayer挂载到任意GameObject上即可
* 可手动指定渲染方式是否以Image组件渲染
* 可选择是否对序列帧图像进行合批(建议开启)
*
* 动画解析Anime类中包含以下多个枚举预设:
* DirectionType 方向类型,含8个不同方向
* ActionType    动作类型,含20个不同动作
* EffectType    动作效果,含Hit和HitOver两种判定
* PlayType      播放类型,含Loop、Once、OnceAndDestory三种类型
*
* 当动画播放完成后会自动调用onFinishCallback回调函数
* 另外可指定onActionListener和onAudioListener监听动画动作帧和音频帧相关判定
*
* 目前已知的动作帧有:
* 击中 Hit (未结束攻击动作,如小石像、黄蜂、绿螳螂等单次攻击动作中有多次击中效果)
* 伤害结算 HitOver
*/

AnimePlayer player = GetComponent<AnimePlayer>();

/**
* 播放动画,调用此方法将会清空当前播放队列,调用完成可通过链式调用nextPlay方法添加动画到播放队列
* @param Serial     动画序列号
* @param Direction  动画方向
* @param ActionType 动画动作
* @param PlayType   播放类型 Loop / Once / OnceAndDestory
* @param Speed      播放速度倍率,以 1s 为单位基准,根据动画帧率计算实际播放周期时长
* @param onFinishCallback 动画结束回调
* @return AnimePlayer
*/

player.play(
    uint Serial,
    Anime.DirectionType Direction = Anime.DirectionType.North,
    Anime.ActionType actionType = Anime.ActionType.Stand,
    Anime.PlayType playType = Anime.PlayType.Once,
    float Speed = 1f,
    AnimeCallback onFinishCallback = null
);

// 简化播放: 此方法大多数情况下用以播放特效动画，没有方向和动作类型
player.play(
    uint Serial,
    Anime.PlayType playType,
    float speed = 1f,
    AnimeCallback onFinishCallback = null
);

// 播放一次
player.playOnce(
    Anime.DirectionType directionType,
    Anime.ActionType actionType,
    float Speed=1f,
    AnimeCallback onFinishCallback=null
    );

// 循环播放
player.playLoop(
    Anime.DirectionType directionType,
    Anime.ActionType actionType,
    float Speed=1f,
    AnimeCallback onFinishCallback=null
    );

/**
 * 可通过setter方法设置动画
 * 通过setter方法设置的动画将使用以下默认配置
 * DirectionType = DirectionType.North
 * ActionType = ActionType.Stand
 * PlayType = PlayType.Loop
 * Speed = 1f
 */
player.Serial = uint AnimeSerial;

// 设置帧动效反馈监听
player.onEffectListener = effect => {...};

// 设置帧音效反馈监听
player.onAudioListener = audioIndex => {...};

// 链式调用添加动作队列
player.play(...params).nextPlay(...params);

// 动态修改动画所用的调色板索引
player.PaletIndex = int PaletIndex;

// 动态修改动画播放类型
player.PlayType = Anime.PlayType;

// 动态调整当前动画方向
player.DirectionType = Anime.DirectionType;

// 动态调整当前动画动作
player.ActionType = Anime.ActionType;

// 动画帧延时,动态修改当前动画,延长每帧之间的时间间隔(此方法定义有歧义,后续更新调整)
player.DelayPlay(float delayTime)

// 停止动画
player.Stop();

```

### 动态图集
比```图档合批```更加灵活的动态图集功能，在运行时进行图档图集的写入处理操作

主要针对网游地图的动态获取增加的可扩展动态图集，包含图集建立、自维护和清理

每个动态图集对象内部会根据图档情况进行动态扩充，并采用相对高效的可用空间搜索方式进行图集填充，尽量减少空间浪费

动态图集固定采用 ```图集缓存 + 二级缓存模式```，并采用Unity的Graphic异步复制纹理方式写入图集，最大限度降低图集使用过程中CPU的计算量

```csharp
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

DynamicGraphic(
    int GraphicWidth,
    int GraphicHeight,
    int GraphicPadding = 0,
    int palet = 0,
    bool linear = false,
    bool ppu100 = false,
    bool compressWhenFull = false)

```

```csharp
// 创建一个1024*1024尺寸,间距 0 ,调色板编号 0,非线性过滤,输出PPU为100的的动态图集,且进行压缩
DynamicGraphic dynamicGraphic = new DynamicGraphic(1024, 1024, 0, 0, false, true, true);

// 通过动态图集获取图像
Sprite sprite = dynamicGraphic.GetSprite(uint Serial);

// 通过协程方式获取图像
StartCoroutine(dynamicGraphic.GetSpriteSync(uint Serial,(sprite)=>{
    // 使用回调Sprite
}));

// 图集清理
dynamicGraphic.Clear(bool GC = false);
```

### 其他
请根据情况自行探索修改代码适应应用场景



## 4、更新日志
### v 3.0
> `ADD` 本次大更新主要是性能优化及功能性更新,部分方法调用参数发生变化
>
> `ADD` 图档Sprite获取以及动画使用增加线性过滤参数
>
> `ADD` 动画使用增加PPU100、线性过滤、图集压缩等参数选项
>
> `ADD` 图档获取Sprite时可通过GraphicDetail.SpritePPU100获取PPU为100的Sprite对象
>
> `ADD` 增加动态图集 `DynamicGraphic` 类,具体使用方法请参考README示例或查阅代码
>
> `UPD` 调整部分代码和图档获取，性能增强，降低CPU压力，减少内存占用等

### v 2.6
> `UPD` 修改了Anime初始化过程，将预载全部动画帧修改为读取时加载,减少初始化时间
>
> `UPD` Anime微调部分逻辑处理,并<font color="red">将动作类型中```Stand```调整为```Idle```</font>
>
> `FIX` Anime修正回调和nextplay链式调用队列控制

### v 2.5
> `FIX` Audio播放代码忘了补充
>
> `UPD` 加入BLiner过滤支持,可以在获取图档时获取对应过滤图像,同时对AnimePlayer增加Liner可选项

### v 2.4
> `FIX` 继续调整目录分析，处理部分特殊情况
>
> `FIX` 对于修改过的Anime进行适配，修复高低版本动画数据混合情况解析报错问题

### v 2.3
> `FIX` 调整高版本动画播放标记、翻转问题

### v 2.2
> `ADD` 增加 <font color="red">4.0 版本(乐园之卵)及后续版本图档、动画解析</font>
>
> `UPD` 调整图档目录版本号分析过程，以更好的适应多种图档目录结构

### v 2.1
> `UPD` 图档合并方法进行统一处理，并增加地图相关简便方法

### v 2.0
> `ADD` 修改初始化方法以支持更复杂的图档文件建构
>
> `UPD` 图档解析支持多层目录结构并能自动识别加载，方便对图档进行扩充升级，建议根据版本号对图档子目录进行命名
>
> `UPD` 图档获取和加载方法优化，减少无用参数
>
> `UPD` 根据新的加载方式调整相关工具库代码以适应新的加载方式

### v 1.7
> `ADD` 加入地图物件合批处理

### v 1.6
> `ADD` 加入<font color="red">客户端</font>地图读取支持，同时附加了客户端地图文件缺失的名字和调色版映射表
>
> `UPD` Anime合批处理图形之间增加间隔,避免出现像素黏连
>
> `UPD` AnimePlayer挂载件预留FrameTexture属性可在Editor中查看合批的图像
>
> `UPD` CGTool入口调整，可指定bin目录位置

### v 1.5
> `UPD` Anime序列帧合批忘了考虑不同调色板的问题，现已增加相关处理

### v 1.4
> `ADD` 加入Anime序列帧合批，挂载AnimePlayer后可手动设置是否开启合批(建议开启)

### v 1.3
> `UPD` 优化AnimePlayer组件的挂载和使用方式
> 
> `UPD` 优化AnimePlayer动画播放器，增加动画相关处理方法
>
> `UPD` 更新README说明文档,增加、调整使用说明
> 

### v 1.2

> `FIX` 修正文件加载时判断为正则，避免因使用不同版本bin导致加载报错问题

### v 1.1
> `ADD` 音频索引及加载AudioTool
> 
> `ADD` 动画播放器添加对Image渲染支持，用以支持GUI动画播放
> 
> `ADD` Graphic增加地面图档动态合批
> 
> `ADD` Anime增加动画帧动态合批方法
> 
> `UPD` 优化Graphic解析，统一改为RGBA4444模式，以减少内存占用
> 
> `UPD` 重新整理初始化代码，优化初始化流程
> 
> `UPD` 优化动画播放器，增加动画相关处理方法
> 
> `UPD` 动画播放器添加对Image渲染支持，用以支持GUI动画播放
> 
> `FIX` 修复动画序列中攻击判定、音频序列解析方式错误的问题

### v 1.0

> `ADD` 脚本初始化
> 
> `ADD` 图档索引GraphicInfo文件解析
> 
> `ADD` 图档Graphic文件数据解析
> 
> `ADD` 调色板Palet文件解析
> 
> `ADD` 动画索引AnimeInfo文件解析
> 
> `ADD` 动画Anime文件数据解析
> 
> `ADD` <font color="red">服务端</font>地图文件解析



## 5、待处理

- 支援 4.0 以上版本图档解析
- 其他未知
- 优化

## LICENSE
This project is licensed under the GPL license. Copyrights are respective of each contributor listed at the beginning of each definition file.


