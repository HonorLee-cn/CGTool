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

## 2、使用说明

克隆当前仓库或下载zip包解压，将CGTool文件夹放置于Unity项目文件夹内引用


下文示例中,所涉及到的版本号均对应:

```0``` 龙之沙漏前版本

```1``` 龙之沙漏

所涉及的所有index、serial均指代图档或动画的具体编号而非索引档中序号

其他相关部分会逐渐更新完善

### 框架初始化
在入口或初始化脚本头部引入CGTool初始化文件
```csharp
using CGTool;
```
并在相关初始化位置对CGTool进行初始化
```csharp
CGTool.CGTool.Init();
```
CGTool初始化时，会自动对相关索引Info文件进行解析，请根据实际所采用版本情况，对脚本代码中解析相关的文件名称进行修改调整


### 获取图档索引数据(图档基本索引数据属性信息)
```csharp
// 通过编号获取图档,无需版本号(推荐方法)
GraphicInfo.GetGraphicInfoDataBySerial(uint Serial);

// 通过编号获取图档,带版本号
GraphicInfo.GetGraphicInfoDataBySerial(int version, uint Serial);

// 通过地面编号获取GraphicInfo数据
GraphicInfo.GetGraphicInfoDataByMapSerial(int Version, uint MapSerial);

// 通过索引获取GraphicInfo数据
GraphicInfo.GetGraphicInfoDataByIndex(int Version, uint Index);
```

### 获取指定索引图档数据(图档实际数据,包含图像Sprite资源)
```csharp
// 通过图档索引编号获取GraphicData数据
Graphic.GetGraphicData(GraphicInfoData graphicInfoData,int PaletIndex=0);

/**
 * 使用说明:
 * 所有通过Graphic获取的图档Sprite均已做偏移处理,可直接使用
 * 需自行判断图档是否存在,并处理图档不存在的情况
 * 1.获取指定编号的图档索引信息
 * 2.根据获取到的索引取得图档数据
 * 3.使用图档数据中的Sprite资源进行绘制
 */

GraphicInfoData graphicInfoData = GraphicInfo.GetGraphicInfoDataBySerial(Serial);
GraphicData graphicData = Graphic.GetGraphicData(graphicInfoData);
SpriteRenderer(Image).sprite = graphicData.Sprite;
```

### 获取地图数据
```csharp
// 通过编号获取地图数据,地图数据中包含地面及地上物件数据以及寻路用的二维数组
// 可自行阅读源码查询相关属性成员和使用方式
Map.MapInfo mapInfo = Map.GetMap(uint Serial);
```

### 获取地图地面图档合批图档数据
```csharp
/**
 * 针对地面数据将地面图档自动进行拼合成一个或多个2048*2048尺寸Texture2D
 * 并将拼合后的Texture2D数据拆分为对应的Sprite资源
 * 这样可以大幅降低地面图档的内存占用和Drawcall数量,提高渲染的动态合批性能
 * 另:
 * 代码中暂时禁用了已合并地面Texture2D的缓存功能,如需使用请取消相关代码注释或自行修改
 * 由于4.0后地图模式变动,所以这个方法可能不适用于4.0后的地图
 */
Dictionary<int, GraphicData> MapGroundSerialDic =
    Graphic.PrepareMapGroundTexture(
        int MapID,
        int PaletIndex,
        List<GraphicInfoData> graphicInfoDataList
    ); 
```

### 获取并播放动画数据
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
* @param PlayType   播放类型
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

// 简化方式: 此方法大多数情况下用以播放特效动画，没有方向和动作类型
player.play(
    uint Serial,
    Anime.PlayType playType,
    float speed = 1f,
    AnimeCallback onFinishCallback = null
);

// 播放一次
player.playOnce(Anime.DirectionType directionType,Anime.ActionType actionType,float Speed=1f,AnimeCallback onFinishCallback=null);

// 循环播放
player.playLoop(Anime.DirectionType directionType,Anime.ActionType actionType,float Speed=1f,AnimeCallback onFinishCallback=null);

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

### 其他
请根据情况自行探索修改代码适应应用场景

## 3、版本及功能概述
> 当前版本目前仅支持 魔力宝贝3.7-龙之沙漏 及以下版本的图档解析
> 
> 目前版本支持以下功能：
> 
> * `GraphicInfo` 图档索引解析
> * `Graphic` 图档数据解析
> * `Palet` 调色板数据解析
> * `AnimeInfo` 动画索引解析
> * `Anime` 动画数据解析
> * `AudioTool` 音频索引及加载
> * `AnimePlayer` 动画播放器挂载组件
> * `Map` 服务端/客户端 图数据解析



## 4、更新日志
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


