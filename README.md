# 魔力宝贝Unity C#图档解析脚本

## 1、开源目的
本脚本旨在学习研究```Crossgate``` ```魔力宝贝``` 图档bin文件的解压与使用，相关资料来源于互联网文章与相关技术大佬指导

再次感谢```阿伍```等在互联网上分享相关解压、算法解析等教学文章的技术大佬所提供的帮助

本脚本遵照GPL共享协议，可随意修改、调整代码学习使用

请务删除或修改文档或源代码中相关版权声明信息，且严禁用于商业项目

因利用本脚本盈利或其他行为导致的任何商业版权纠纷，脚本作者不承担任何责任或损失

## 2、使用说明

克隆当前仓库或下载zip包解压，将CGTool文件夹放置于Unity项目文件夹内引用

### 框架初始化
在入口或初始化脚本头部引入CGTool初始化文件
```csharp
using CGTool;
```
并在关键位置对CGTool进行初始化
```csharp
CGTool.CGTool.Init();
```
CGTool初始化时，会自动对相关索引Info文件进行解析，请根据实际所采用版本情况，对脚本代码中解析相关的文件名称进行修改调整

### 获取图档索引数据
```csharp
//通过地面编号获取GraphicInfo数据
GraphicInfo.GetGraphicInfoDataByMapSerial(int Version, uint MapSerial);
//通过索引获取GraphicInfo数据
GraphicInfo.GetGraphicInfoDataByIndex(int Version, uint Index);
```

### 获取指定索引图档数据
```csharp
//通过图档索引编号获取GraphicData数据
Graphic.GetGraphicData(GraphicInfoData graphicInfoData,int PaletIndex=0);
```

### 获取并播放动画数据
```csharp
/**
* 动画播放器,用于播放CG动画,支持多动画队列播放
* 脚本需绑定至挂载了SpriteRenderer和RectTransform的对象上
* 除此之外,还需绑定BoxCollider2D(可选),用于监听鼠标的移入移出事件
*
* 当动画播放完成后会自动调用onFinishCallback回调函数
* 另外可指定onActionListener和onAudioListener监听动画动作帧和音频帧
* 目前已知的动作帧有:
* 击中 0x27 | 0x28
* 伤害结算 0x4E | 0x4F
*/

/**
* 播放动画,调用此方法将会清空当前播放队列,调用完成可通过链式调用nextPlay方法添加动画到播放队列
* @param Serial 动画序列号
* @param Direction 动画方向
* @param ActionType 动画动作
* @param Infinity 是否循环
* @param Speed 播放速度,以 1s 为基准,根据动画帧率计算实际播放周期时长
* @param onFinishCallback 动画结束回调
* @return AnimePlayer
*/
AnimePlayer player.play(uint AnimeSerial, Anime.DirectionType.North, Anime.ActionType.Stand, true,0.1f,AnimeCallback onFinishCallback=null);

//设置帧动效反馈监听
AnimePlayer player.onEffectListener = effect => { };

//设置帧音效反馈监听
AnimePlayer player.onAudioListener = audioIndex => { };

//链式调用添加动作队列
AnimePlayer player.play(...params).nextPlay(...params);
```

### 其他
请根据情况自行探索修改代码适应应用场景

## 3、版本及功能概述
### 1.0

当前版本目前仅支持 魔力宝贝3.7-龙之沙漏 及以下版本的图档解析

>`ADD` 脚本初始化
> 
>`ADD` 图档索引GraphicInfo文件解析
> 
>`ADD` 图档Graphic文件数据解析
> 
>`ADD` 调色板Palet文件解析
> 
>`ADD` 动画索引AnimeInfo文件解析
> 
>`ADD` 动画Anime文件数据解析
> 
>`ADD` <font color="red">服务端</font>地图文件解析



## 4、待处理

- 支援 4.0 以上版本图档解析
- 音频解析
- 其他未知
- 优化

## LICENSE
This project is licensed under the GPL license. Copyrights are respective of each contributor listed at the beginning of each definition file.


