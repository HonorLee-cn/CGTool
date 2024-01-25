/**
 * 魔力宝贝图档解析脚本 - CGTool
 * 
 * @Author  HonorLee (dev@honorlee.me)
 * @Version 1.0 (2023-04-15)
 * @License GPL-3.0
 *
 * AnimePlayer.cs 动画播放器-挂载类
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

namespace CrossgateToolkit
{
    //动画周期回调
    public delegate void AnimeCallback(Anime.ActionType actionType);
    
    //动画动作帧监听
    public delegate void AnimeEffectListener(Anime.EffectType effect);
    
    //动画音频帧监听
    public delegate void AnimeAudioListener(int audioIndex);

    public enum MouseType
    {
        Enter,
        Exit,
        Click
    }
    //鼠标移入事件监听
    public delegate void MouseListener(MouseType mouseType);
    
    /**
     * 动画播放器,用于播放CG动画,支持多动画队列播放
     * 脚本需绑定至挂载了SpriteRenderer、Image和RectTransform的对象上
     * ########除此之外,还需绑定BoxCollider2D(可选),用于监听鼠标的移入移出事件#####此条删除
     *
     * 当动画播放完成后会自动调用onFinishCallback回调函数
     * 另外可指定onActionListener和onAudioListener监听动画动作帧和音频帧
     * 目前已知的动作帧有:
     * 击中 伤害结算
     */
    public class AnimePlayer : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerClickHandler
    {
        //动画帧数据
        private class AnimeFrame
        {
            public int Index;
            public GraphicInfoData GraphicInfo;
            public Sprite Sprite;
            public AnimeFrameInfo AnimeFrameInfo;
        }

        //播放配置数据
        private class AnimeOption
        {
            public uint AnimeSerial;
            public Anime.DirectionType Direction;
            public Anime.ActionType actionType;
            public Anime.PlayType playType;
            public float Speed;
            public float FrameRate;
            public AnimeDetail AnimeDetail;
            public AnimeCallback onFinishCallback;
            public AnimeEffectListener onEffectListener;
            public int CurrentFrame = 0;
            public bool KeepFrame = false;
            public bool _effectOverCalled = false;
            public bool _finishedCalled = false;
            public bool _keepCallback = false;
        }
        
        //当前播放
        private uint _currentSerial;
        private AnimeOption _currentAnime = null;
        private AnimeFrame[] _frames;
        // private int _currentFrame;
        
        //是否播放
        private bool isPlayable;
        // private bool isPlayable
        // {
        //     get => _isPlayable;
        //     set
        //     {
        //         _isPlayable = value;
        //         if (_isPlayable)
        //         {
        //             _timer = Time.time * 1000;
        //         }
        //         else
        //         {
        //             Debug.Log("set isPlayable  " + isPlayable + "  "+_currentAnime?.actionType);
        //         }
        //     }
        // }
        
        //待播放队列
        private readonly List<AnimeOption> _animeQueue = new List<AnimeOption>(10);
        
        //计时器
        private float _timer;
        //下一帧延迟
        private float _delay;
        
        //绑定渲染对象
        [SerializeField,Header("Image渲染")] public bool isRenderByImage = false;
        [SerializeField,Header("序列帧合批")] public bool isFrameBatch = false;
        [SerializeField, Header("合批压缩")] public bool isBatchCompress;
        [SerializeField,Header("线性过滤")] public bool isLinearFilter = false;
        [SerializeField,Header("PPU100模式")] public bool isPPU100 = false;
        
        [Header("序列帧Texture")] public Texture2D frameTexture;
        
        private SpriteRenderer _spriteRenderer;
        private Image _imageRenderer;
        
        private int _paletIndex = 0;
        public int PaletIndex
        {
            get { return _paletIndex; }
            set
            {
                _paletIndex = value;
                if (_currentAnime != null && value != _paletIndex)
                {
                    _play();
                }
            }
        }
        
        //绑定RectTransform
        private RectTransform _rectTransform;
        
        //动画动作帧监听
        // public AnimeEffectListener onEffectListener;
        public AnimeAudioListener onAudioListener;
        //鼠标事件监听
        public MouseListener onMouseListener;

        //获取偏移量(无用)
        public Vector2 offset
        {
            get
            {
                float offsetX = -_frames[_currentAnime.CurrentFrame].AnimeFrameInfo.OffsetX;
                float offsetY = _frames[_currentAnime.CurrentFrame].AnimeFrameInfo.OffsetY;
                return new Vector2(offsetX, offsetY);
            }
        }

        //实例初始化时获取相关绑定
        private void Awake()
        {
            //调整渲染
            _imageRenderer = GetComponent<Image>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _rectTransform = GetComponent<RectTransform>();
            
            if(_imageRenderer == null) _imageRenderer = gameObject.AddComponent<Image>();
            if(_spriteRenderer == null) _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            if(_rectTransform == null) _rectTransform = gameObject.AddComponent<RectTransform>();
            
            gameObject.SetActive(false);
        }

        private void Start()
        {
            _updateRenderMode();
        }

        private void OnDisable()
        {
            // 被隐藏后及时清理数据
            // Stop();
            Pause();
        }

        private void OnEnable()
        {
            if(_currentAnime!=null) Resume();
        }

        // 使用Image模式渲染
        public bool RenderByImage
        {
            get => isRenderByImage;
            set
            {
                isRenderByImage = value;
                _updateRenderMode();
            }
        }
        
        // 设置当前播放序列,默认方向North,动作Stand,播放类型Loop,播放速度1f
        public uint Serial
        {
            get => _currentSerial;
            set
            {
                if (value == 0)
                {
                    Stop();
                    return;
                }
                Anime.DirectionType direction =
                    _currentAnime?.Direction ?? Anime.DirectionType.North;
                Anime.ActionType actionType = _currentAnime?.actionType ?? Anime.ActionType.Idle;
                Anime.PlayType playType = _currentAnime?.playType ?? Anime.PlayType.Loop;
                float speed = _currentAnime?.Speed ?? 1f;
                AnimeCallback onFinishCallback = _currentAnime?.onFinishCallback;
                AnimeEffectListener onEffectListener = _currentAnime?.onEffectListener;
                play(value, direction, actionType, playType, speed, onEffectListener,onFinishCallback);
            }
        }
        
        // 动态调整播放类型
        public Anime.PlayType PlayType
        {
            get => _currentAnime?.playType ?? Anime.PlayType.Loop;
            set
            {
                if (_currentAnime != null)
                {
                    _currentAnime.playType = value;
                }
            }
        }
        
        // 动态调整播放回调
        public AnimeCallback OnFinishCallback
        {
            get => _currentAnime?.onFinishCallback;
            set
            {
                if (_currentAnime != null)
                {
                    _currentAnime.onFinishCallback = value;
                }
            }
        }
        
        public AnimeCallback OnCycleCallback;

        // 更新渲染模式
        private void _updateRenderMode()
        {
            if (isRenderByImage)
            {
                _imageRenderer.enabled = true;
                _spriteRenderer.enabled = false;
            }
            else
            {
                _imageRenderer.enabled = false;
                _spriteRenderer.enabled = true;
            }
        }

        /// <summary>
        /// 播放动画。调用此方法将会清空当前播放队列，调用完成可通过链式调用 <c>nextPlay</c> 方法添加动画到播放队列。
        /// </summary>
        /// <param name="Serial">动画序列号</param>
        /// <param name="Direction">动画方向</param>
        /// <param name="ActionType">动画动作</param>
        /// <param name="PlayType">播放类型</param>
        /// <param name="Speed">播放速度，以 1s 为基准，根据动画帧率计算实际播放周期时长</param>
        /// <param name="onEffectListener">动画动作帧监听</param>
        /// <param name="onFinishCallback">动画结束回调</param>
        /// <returns>AnimePlayer</returns>
        public AnimePlayer play(uint serial, Anime.DirectionType Direction = Anime.DirectionType.North, 
            Anime.ActionType actionType = Anime.ActionType.Idle, Anime.PlayType playType = Anime.PlayType.Once,
            float Speed = 1f,AnimeEffectListener onEffectListener = null,AnimeCallback onFinishCallback = null,bool keepCallback = false)
        {
            AnimeOption animeOption = CreateAnimeOption(serial, Direction, actionType, playType, Speed,onEffectListener, onFinishCallback,keepCallback);
            if (animeOption == null)
            {
                onFinishCallback?.Invoke(actionType);
                // Debug.Log("AnimePlayer:AnimeOption create failed");
                return this;
            }
            
            if (_currentAnime!=null && _currentAnime._keepCallback && isPlayable)
            {
                Pause();
                if(!_currentAnime._effectOverCalled) _currentAnime.onEffectListener?.Invoke(Anime.EffectType.HitOver);
                if(!_currentAnime._finishedCalled) _currentAnime.onFinishCallback?.Invoke(_currentAnime.actionType);
            }
            //清空播放队列
            _animeQueue.Clear();
            _animeQueue.Add(animeOption);
            _currentAnime = null;
            _play();
            
            //链式调用,后续可通过nextPlay方法添加动画到播放队列
            return this;
        }

        //播放动画
        public AnimePlayer play(uint serial, Anime.PlayType playType, float speed = 1f,AnimeEffectListener onEffectListener = null,AnimeCallback onFinishCallback = null,bool keepCallback = false)
        {
            return play(serial,Anime.DirectionType.North,Anime.ActionType.Idle,playType,speed,onEffectListener,onFinishCallback,keepCallback);
        }
        
        //不改变Serial情况下播放动画
        public AnimePlayer play(Anime.DirectionType directionType,Anime.ActionType actionType,Anime.PlayType playType,float Speed=1f,AnimeEffectListener onEffectListener=null,AnimeCallback onFinishCallback=null,bool keepCallback = false)
        {
            return play(_currentSerial, directionType, actionType, playType,
                Speed,onEffectListener, onFinishCallback,keepCallback);
        }

        //播放一次
        public AnimePlayer playOnce(Anime.DirectionType directionType,Anime.ActionType actionType,float Speed=1f,AnimeEffectListener onEffectListener=null,AnimeCallback onFinishCallback=null,bool keepCallback = false)
        {
            return play(_currentSerial, directionType, actionType, Anime.PlayType.Once,
                Speed, onEffectListener,onFinishCallback,keepCallback);
        }
        
        //播放循环
        public AnimePlayer playLoop(Anime.DirectionType directionType,Anime.ActionType actionType,float Speed=1f,AnimeEffectListener onEffectListener=null,AnimeCallback onFinishCallback=null,bool keepCallback = false)
        {
            return play(_currentSerial, directionType, actionType, Anime.PlayType.Loop,
                Speed, onEffectListener,onFinishCallback,keepCallback);
        }

        //调整动画方向
        public void changeDirection(Anime.DirectionType directionType)
        {
            if (directionType == _currentAnime.Direction || directionType == Anime.DirectionType.NULL) return;
            // _currentAnime = CreateAnimeOption(_currentAnime.AnimeSerial, directionType, _currentAnime.actionType,
            //     _currentAnime.playType, _currentAnime.Speed, _currentAnime.onEffectListener,_currentAnime.onFinishCallback);
            AnimeOption animeOption = CreateAnimeOption(_currentAnime.AnimeSerial, directionType, _currentAnime.actionType,
                _currentAnime.playType, _currentAnime.Speed);
            _currentAnime = CreateAnimeOption(_currentAnime.AnimeSerial, directionType, _currentAnime.actionType,
                _currentAnime.playType, _currentAnime.Speed);
            if (animeOption == null) return;
            _currentAnime = animeOption;
            if(_animeQueue.Count>0) _animeQueue[0] = _currentAnime;
            else _animeQueue.Add(_currentAnime);
            _play();
        }
        public Anime.DirectionType DirectionType
        {
            get => _currentAnime?.Direction ?? Anime.DirectionType.NULL;
            set
            {
                if (_currentAnime != null)
                {
                    changeDirection(value);
                }
            }
        }

        public void Rotate(bool clockwise = true)
        {
            if (_currentAnime == null) return;
            int direction = (int)_currentAnime.Direction;
            if (clockwise)
            {
                direction += 1;
                if(direction>7) direction = 0;
            }
            else
            {
                direction -= 1;
                if(direction<0) direction = 7;
            }
            changeDirection((Anime.DirectionType)direction);
        }
        
        //调整动画动作类型
        public void changeActionType(Anime.ActionType actionType)
        {
            if (actionType == _currentAnime.actionType) return;
            // _currentAnime = CreateAnimeOption(_currentAnime.AnimeSerial, _currentAnime.Direction,actionType,
            //     _currentAnime.playType, _currentAnime.Speed, _currentAnime.onEffectListener,_currentAnime.onFinishCallback);
            AnimeOption animeOption = CreateAnimeOption(_currentAnime.AnimeSerial, _currentAnime.Direction,actionType,
                _currentAnime.playType, _currentAnime.Speed);
            if (animeOption == null) return;
            _currentAnime = animeOption;
            if(_animeQueue.Count>0) _animeQueue[0] = _currentAnime;
            else _animeQueue.Add(_currentAnime);
            _play();
        }
        public Anime.ActionType ActionType
        {
            get => _currentAnime?.actionType ?? Anime.ActionType.NULL;
            set
            {
                if (_currentAnime != null)
                {
                    changeActionType(value);
                }
            }
        }

        //播放
        private void _play()
        {
            isPlayable = false;
            _currentAnime = null;
         
            AnimeOption animeOption = _animeQueue[0];
            // Debug.Log("AnimePlayer:play " + animeOption.AnimeSerial + "  " + animeOption.actionType);
            
            AnimeFrame[] frames = new AnimeFrame[animeOption.AnimeDetail.FrameCount];

            if (isFrameBatch)
            {
                Anime.BakeAnimeFrames(animeOption.AnimeDetail, _paletIndex, isLinearFilter, isBatchCompress);
                //获取动画帧数据
                for (int i = 0; i < animeOption.AnimeDetail.AnimeFrameInfos.Count; i++)
                {
                    if(!animeOption.AnimeDetail.AnimeFrameInfos[i].AnimeSprites.ContainsKey(_paletIndex)) continue;
                    if(animeOption.AnimeDetail.AnimeFrameInfos[i].AnimeSprites[_paletIndex] == null) continue;
                    //创建帧数据
                    frames[i] = new AnimeFrame();
                    frames[i].Index = i;
                    frames[i].GraphicInfo = animeOption.AnimeDetail.AnimeFrameInfos[i].GraphicInfo;
                    GraphicDetail graphicDetail =
                        animeOption.AnimeDetail.AnimeFrameInfos[i].AnimeSprites[_paletIndex][isLinearFilter];
                    frames[i].Sprite = isPPU100 ? graphicDetail.SpritePPU100 : graphicDetail.Sprite;
                    frames[i].AnimeFrameInfo = animeOption.AnimeDetail.AnimeFrameInfos[i];
                }
            }
            else
            {
                //获取动画帧数据
                for (int i = 0; i < animeOption.AnimeDetail.AnimeFrameInfos.Count; i++)
                {
                    AnimeFrameInfo animeFrameInfo = animeOption.AnimeDetail.AnimeFrameInfos[i];
                    GraphicInfoData graphicInfoData = GraphicInfo.GetGraphicInfoDataByIndex(
                        animeOption.AnimeDetail.Version, animeOption.AnimeDetail.AnimeFrameInfos[i].GraphicIndex);
                    if (graphicInfoData == null)
                    {
                        Debug.Log("GraphicInfo Serial:" +
                                  animeOption.AnimeDetail.AnimeFrameInfos[i].GraphicIndex + " is null");
                        continue;
                    }

                    int subPaletIndex = -1;
                    if (animeOption.AnimeDetail.IsHighVersion) subPaletIndex = (int)animeOption.AnimeDetail.Serial;
                    GraphicDetail graphicData =
                        GraphicData.GetGraphicDetail(graphicInfoData, _paletIndex, subPaletIndex, isLinearFilter);
                    if (graphicData == null)
                    {
                        Debug.Log("GraphicData Serial:" +
                                  animeOption.AnimeDetail.AnimeFrameInfos[i].GraphicIndex + " is null");
                        continue;
                    }
                
                    //创建帧数据
                    frames[i] = new AnimeFrame();
                    frames[i].Index = i;
                    frames[i].GraphicInfo = graphicInfoData;
                    frames[i].Sprite = isPPU100 ? graphicData.SpritePPU100 : graphicData.Sprite;
                    frames[i].AnimeFrameInfo = animeFrameInfo;
                }
            }
            
            
            _currentAnime = animeOption;
            _currentSerial = animeOption.AnimeSerial;
            _frames = frames;
            // _currentAnime.CurrentFrame = _currentAnime.CurrentFrame;
            _delay = 0;
            gameObject.SetActive(true);
            
            isPlayable = true;
            UpdateFrame();
        }

        //播放延时
        public void DelayPlay(float delayTime)
        {
            _delay = delayTime*1000;
        }

        // 设置速度
        public void SetSpeed(float speed)
        {
            if (_currentAnime == null) return;
            _currentAnime.Speed = speed;
            _currentAnime.FrameRate =
                _currentAnime.AnimeDetail.CycleTime * 1f / speed / _currentAnime.AnimeDetail.FrameCount;
        }

        //停止播放
        public void Stop()
        {
            isPlayable = false;
            _currentAnime = null;
            _frames = null;
            gameObject.SetActive(false);
            
            //清理缓存
            if(_imageRenderer!=null) _imageRenderer.sprite = null;
            if(_spriteRenderer!=null) _spriteRenderer.sprite = null;
            
        }

        //暂停播放
        public void Pause()
        {
            isPlayable = false;
        }

        // 恢复播放
        public void Play()
        {
            if(_currentAnime!=null) isPlayable = true;
        }
        
        //恢复播放
        public void Resume()
        {
            isPlayable = true;
        }

        //修改播放类型---重复方法--考虑删掉
        public void ChangePlayType(Anime.PlayType playType)
        {
            if (_currentAnime == null) return;
            _currentAnime.playType = playType;
        }

        //创建动画配置
        private AnimeOption CreateAnimeOption(uint serial, Anime.DirectionType Direction, Anime.ActionType actionType,
            Anime.PlayType playType=Anime.PlayType.Once, float Speed = 1f,AnimeEffectListener onEffectListener = null, AnimeCallback onFinishCallback = null,bool keepCallback = false)
        {
            AnimeDetail animeDetail = Anime.GetAnimeDetail(serial, Direction, actionType);
            
            if (animeDetail == null)
            {
                // 动画不存在,尝试查找图档
                GraphicInfoData graphicInfoData = GraphicInfo.GetGraphicInfoData(serial);
                if (graphicInfoData != null)
                {
                    // 图档存在情况下,不创建动画,直接更新图像显示并返回
                    GraphicDetail graphicDetail = GraphicData.GetGraphicDetail(graphicInfoData, _paletIndex, 0, isLinearFilter);
                    if (graphicDetail != null)
                    {
                        if (isRenderByImage)
                        {
                            _imageRenderer.sprite = isPPU100 ? graphicDetail.SpritePPU100 : graphicDetail.Sprite;
                            _imageRenderer.SetNativeSize();
                        }
                        else
                        {
                            _spriteRenderer.sprite = isPPU100 ? graphicDetail.SpritePPU100 : graphicDetail.Sprite;
                        }
                        _rectTransform.sizeDelta = new Vector2(graphicDetail.Sprite.rect.width, graphicDetail.Sprite.rect.height);
                        gameObject.SetActive(true);
                        return null;
                    }
                }
                else
                {
                    return null;    
                }
                // Debug.Log("AnimePlayer:AnimeDetail [" + serial + "] is null");
            }
            
            AnimeOption animeOption = new AnimeOption()
            {
                AnimeSerial = serial,
                Direction = Direction,
                actionType = actionType,
                playType = playType,
                Speed = Speed,
                FrameRate = animeDetail.CycleTime * 1f / (float)animeDetail.FrameCount /Speed,
                AnimeDetail = animeDetail,
                onEffectListener = onEffectListener,
                onFinishCallback = onFinishCallback,
                _keepCallback = keepCallback
            };
            // Debug.Log("AnimePlayer:CreateAnimeOption " + animeOption.AnimeSerial + "  " + animeOption.actionType +" speed:"+Speed+" framerate:"+animeOption.FrameRate);
            return animeOption;
        }

        //加入链式动画播放队列
        public AnimePlayer nextPlay(uint serial, Anime.DirectionType Direction, Anime.ActionType actionType,
            Anime.PlayType playType=Anime.PlayType.Once, float Speed = 1f, AnimeEffectListener onEffectListener=null, AnimeCallback onFinishCallback = null)
        {
            AnimeOption animeOption = CreateAnimeOption(serial, Direction, actionType, playType, Speed, onEffectListener,onFinishCallback);
            if (animeOption == null)
            {
                onFinishCallback?.Invoke(actionType);
                return this;
            }
            _animeQueue.Add(animeOption);    
            if (_animeQueue[0] == animeOption)
            {
                _play();
            }
            
            return this;
        }
        
        //加入链式动画播放队列
        public AnimePlayer nextPlay(Anime.DirectionType Direction, Anime.ActionType actionType,
            Anime.PlayType playType=Anime.PlayType.Once, float Speed = 1f,  AnimeEffectListener onEffectListener=null, AnimeCallback onFinishCallback = null)
        {
            return nextPlay(_currentSerial, Direction, actionType, playType, Speed, onEffectListener, onFinishCallback);
        }
        
        // 保持当前帧并切换动画方向(特殊情况如被暴击或击飞后保持受伤帧旋转)
        public void changeDirectionKeepFrame(Anime.DirectionType directionType)
        {
            if (directionType == _currentAnime.Direction || directionType == Anime.DirectionType.NULL) return;
            int currentFrame = _currentAnime.CurrentFrame;
            // _currentAnime = CreateAnimeOption(_currentAnime.AnimeSerial, directionType, _currentAnime.actionType,
            //     _currentAnime.playType, _currentAnime.Speed, _currentAnime.onEffectListener,_currentAnime.onFinishCallback);
            AnimeOption animeOption = CreateAnimeOption(_currentAnime.AnimeSerial, directionType, _currentAnime.actionType,
                _currentAnime.playType, _currentAnime.Speed);
            if (animeOption == null) return;
            animeOption.CurrentFrame = --currentFrame;
                
            animeOption.KeepFrame = true;
            if(_animeQueue.Count>0) _animeQueue[0] = animeOption;
            else _animeQueue.Add(animeOption);
            _play();
        }
        
        //更新计算
        private void Update()
        {
            
            if (!isPlayable || _currentAnime==null) return;
            float now = Time.time * 1000;
            if ((now - _timer - _delay) >= _currentAnime.FrameRate) UpdateFrame();
        }

        //更新帧
        private void UpdateFrame()
        {
            _delay = 0;
            AnimeOption playingAnime = _currentAnime;
            if (!isPlayable || _frames.Length == 0) return;
            
            //动画结束
            if (playingAnime.CurrentFrame >= playingAnime.AnimeDetail.FrameCount)
            {
                OnCycleCallback?.Invoke(playingAnime.actionType);
                //循环播放
                if (playingAnime.playType == Anime.PlayType.Loop)
                {
                    if(playingAnime==_currentAnime)  playingAnime.onFinishCallback?.Invoke(playingAnime.actionType);
                    playingAnime._finishedCalled = true;
                    playingAnime.CurrentFrame = 0;
                }else if (playingAnime.playType is Anime.PlayType.Once or Anime.PlayType.OnceAndDestroy)
                {
                    if (playingAnime.playType == Anime.PlayType.OnceAndDestroy)
                    {
                        _animeQueue.Clear();
                        _spriteRenderer.sprite = null;
                        _imageRenderer.sprite = null;
                        _rectTransform.sizeDelta = Vector2.zero;
                        if(playingAnime==_currentAnime) playingAnime.onFinishCallback?.Invoke(playingAnime.actionType);
                        gameObject.SetActive(false);
                        return;
                    }
                    if (playingAnime.KeepFrame)
                    {
                        if(playingAnime==_currentAnime) playingAnime.onFinishCallback?.Invoke(playingAnime.actionType);
                        playingAnime.CurrentFrame--;
                    }
                    else
                    {
                        _animeQueue.RemoveAt(0);
                        //播放下一个动画
                        if (_animeQueue.Count > 0)
                        {
                            AnimeCallback callback = playingAnime.onFinishCallback;
                            Anime.ActionType actionType = playingAnime.actionType;
                            // playingAnime.onFinishCallback?.Invoke(playingAnime.actionType);
                            _play();
                            callback?.Invoke(actionType);
                            return;
                        }
                        else
                        {
                            Pause();
                            // 回调在Pause之后避免时序问题导致影响下一个动画
                            playingAnime.onFinishCallback?.Invoke(playingAnime.actionType);
                            return;
                        }
                    }
                }
                
            }
            
            //问题帧自动跳过
            if (playingAnime.CurrentFrame < _frames.Length && _frames[playingAnime.CurrentFrame] == null)
            {
                playingAnime.CurrentFrame++;
                return;
            }
            
            //根据当前帧Sprite动态调整对象大小
            float width = _frames[playingAnime.CurrentFrame].Sprite.rect.width * 1f;
            float height = _frames[playingAnime.CurrentFrame].Sprite.rect.height * 1f;
            if (isPPU100)
            {
                width = width / 100f;
                height = height / 100f;
            }

            Vector3 pos = Vector3.zero;
            pos.x = _frames[playingAnime.CurrentFrame].GraphicInfo.OffsetX;
            pos.y = -_frames[playingAnime.CurrentFrame].GraphicInfo.OffsetY;
            
            if (isRenderByImage)
            {
                _imageRenderer.sprite = _frames[playingAnime.CurrentFrame].Sprite;
                _imageRenderer.SetNativeSize();
                if (playingAnime.AnimeDetail.FLAG.HasFlag(AnimeFlag.REVERSE_X) || playingAnime.AnimeDetail.FLAG.HasFlag(AnimeFlag.REVERSE_Y))
                {
                    if (playingAnime.AnimeDetail.FLAG.HasFlag(AnimeFlag.REVERSE_X))
                    {
                        _imageRenderer.transform.localScale = new Vector3(-1, 1, 1);
                        pos.x = -pos.x;
                    }

                    if (playingAnime.AnimeDetail.FLAG.HasFlag(AnimeFlag.REVERSE_Y))
                    {
                        _imageRenderer.transform.localScale = new Vector3(1, -1, 1);
                        pos.y = -pos.y;
                    }
                }
                else
                {
                    _imageRenderer.transform.localScale = new Vector3(1, 1, 1);
                }
                
                _rectTransform.localPosition = pos;
                _rectTransform.pivot = new Vector2(0f,1f);
            }
            else
            {
                _spriteRenderer.sprite = _frames[playingAnime.CurrentFrame].Sprite;
                _rectTransform.sizeDelta = new Vector2(width, height);
                _spriteRenderer.size = new Vector2(width, height);
                _rectTransform.pivot = new Vector2(0.5f,0f);
                // Vector3 scale = isPPU100 ? new Vector3(100f, 100f, 100f) : Vector3.one;
                // _rectTransform.localScale = scale;
                if (playingAnime.AnimeDetail.FLAG.HasFlag(AnimeFlag.REVERSE_X) || playingAnime.AnimeDetail.FLAG.HasFlag(AnimeFlag.REVERSE_Y))
                {
                    if (playingAnime.AnimeDetail.FLAG.HasFlag(AnimeFlag.REVERSE_X))
                    {
                        _spriteRenderer.flipX = true;
                    }else
                    {
                        _spriteRenderer.flipX = false;
                    }
                    
                    if (playingAnime.AnimeDetail.FLAG.HasFlag(AnimeFlag.REVERSE_Y))
                    {
                        _spriteRenderer.flipY = true;
                    }else
                    {
                        _spriteRenderer.flipY = false;
                    }
                }
                else
                {
                    _spriteRenderer.flipX = false;
                    _spriteRenderer.flipY = false;
                }
                _rectTransform.localPosition = Vector3.zero;
            }
            frameTexture = _frames[playingAnime.CurrentFrame].Sprite.texture;
            
            _timer = Time.time * 1000;
            
            //动画事件帧监听
            if (playingAnime==_currentAnime && _frames[playingAnime.CurrentFrame].AnimeFrameInfo.Effect > 0)
                playingAnime.onEffectListener?.Invoke(_frames[playingAnime.CurrentFrame].AnimeFrameInfo.Effect);
            if (playingAnime==_currentAnime && _frames[playingAnime.CurrentFrame].AnimeFrameInfo.Effect == Anime.EffectType.HitOver)
                playingAnime._effectOverCalled = true;
            //音频事件帧监听
            if (playingAnime==_currentAnime && _frames[playingAnime.CurrentFrame].AnimeFrameInfo.AudioIndex > 0)
                onAudioListener?.Invoke(_frames[playingAnime.CurrentFrame].AnimeFrameInfo.AudioIndex);
            
            playingAnime.CurrentFrame++;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            onMouseListener?.Invoke(MouseType.Enter);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            onMouseListener?.Invoke(MouseType.Exit);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            onMouseListener?.Invoke(MouseType.Click);
        }
    }
}