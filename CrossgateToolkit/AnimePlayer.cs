/**
 * 魔力宝贝图档解析脚本 - CGTool
 * 
 * @Author  HonorLee (dev@honorlee.me)
 * @Version 1.0 (2023-04-15)
 * @License GPL-3.0
 *
 * AnimePlayer.cs 动画播放器-挂载类
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CrossgateToolkit
{
    //动画周期回调
    public delegate void AnimeCallback(Anime.ActionType actionType);
    
    //动画动作帧监听
    public delegate void AnimeEffectListener(Anime.EffectType effect);
    
    //动画音频帧监听
    public delegate void AnimeAudioListener(int audioIndex);
    
    //鼠标移入事件监听
    public delegate void MouseListener(AnimePlayer animePlayer);
    
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
    public class AnimePlayer : MonoBehaviour
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
        }
        
        //当前播放
        private uint _currentSerial;
        private AnimeOption _currentAnime;
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
        private readonly List<AnimeOption> _animeQueue = new List<AnimeOption>();
        
        //计时器
        private float _timer;
        //下一帧延迟
        private float _delay;
        
        //绑定渲染对象
        [SerializeField,Header("Image渲染")] public bool isRenderByImage = false;
        [SerializeField,Header("序列帧合批")] public bool isFrameBatch = false;
        [SerializeField,Header("线性过滤")] public bool isLinearFilter = false;
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
                if (_currentAnime != null) _play(_currentAnime);
            }
        }
        
        //绑定RectTransform
        private RectTransform _rectTransform;
        
        //动画动作帧监听
        // public AnimeEffectListener onEffectListener;
        public AnimeAudioListener onAudioListener;
        //鼠标移入事件监听
        public MouseListener onMouseEnterListener;
        //鼠标移出事件监听
        public MouseListener onMouseExitListener;

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

        //鼠标移入监听
        private void OnMouseEnter()
        {
            onMouseEnterListener?.Invoke(this);
        }

        //鼠标移出监听
        private void OnMouseExit()
        {
            onMouseExitListener?.Invoke(this);
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
            float Speed = 1f,AnimeEffectListener onEffectListener = null,AnimeCallback onFinishCallback = null)
        {
            AnimeOption animeOption = CreateAnimeOption(serial, Direction, actionType, playType, Speed,onEffectListener, onFinishCallback);
            if (animeOption == null)
            {
                onFinishCallback?.Invoke(actionType);
                // Debug.Log("AnimePlayer:AnimeOption create failed");
                return this;
            }
            //清空播放队列
            _animeQueue.Clear();
            _animeQueue.Add(animeOption);
            _play(animeOption);
            
            //链式调用,后续可通过nextPlay方法添加动画到播放队列
            return this;
        }

        //播放动画
        public AnimePlayer play(uint serial, Anime.PlayType playType, float speed = 1f,AnimeEffectListener onEffectListener = null,AnimeCallback onFinishCallback = null)
        {
            return play(serial,Anime.DirectionType.North,Anime.ActionType.Idle,playType,speed,onEffectListener,onFinishCallback);
        }
        
        //不改变Serial情况下播放动画
        public AnimePlayer play(Anime.DirectionType directionType,Anime.ActionType actionType,Anime.PlayType playType,float Speed=1f,AnimeEffectListener onEffectListener=null,AnimeCallback onFinishCallback=null)
        {
            return play(_currentSerial, directionType, actionType, playType,
                Speed,onEffectListener, onFinishCallback);
        }

        //播放一次
        public AnimePlayer playOnce(Anime.DirectionType directionType,Anime.ActionType actionType,float Speed=1f,AnimeEffectListener onEffectListener=null,AnimeCallback onFinishCallback=null)
        {
            return play(_currentSerial, directionType, actionType, Anime.PlayType.Once,
                Speed, onEffectListener,onFinishCallback);
        }
        
        //播放循环
        public AnimePlayer playLoop(Anime.DirectionType directionType,Anime.ActionType actionType,float Speed=1f,AnimeEffectListener onEffectListener=null,AnimeCallback onFinishCallback=null)
        {
            return play(_currentSerial, directionType, actionType, Anime.PlayType.Loop,
                Speed, onEffectListener,onFinishCallback);
        }

        //调整动画方向
        public void changeDirection(Anime.DirectionType directionType)
        {
            if (directionType == _currentAnime.Direction || directionType == Anime.DirectionType.NULL) return;
            // _currentAnime = CreateAnimeOption(_currentAnime.AnimeSerial, directionType, _currentAnime.actionType,
            //     _currentAnime.playType, _currentAnime.Speed, _currentAnime.onEffectListener,_currentAnime.onFinishCallback);
            _currentAnime = CreateAnimeOption(_currentAnime.AnimeSerial, directionType, _currentAnime.actionType,
                _currentAnime.playType, _currentAnime.Speed);
            _play(_currentAnime);
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
        
        //调整动画动作类型
        public void changeActionType(Anime.ActionType actionType)
        {
            if (actionType == _currentAnime.actionType) return;
            // _currentAnime = CreateAnimeOption(_currentAnime.AnimeSerial, _currentAnime.Direction,actionType,
            //     _currentAnime.playType, _currentAnime.Speed, _currentAnime.onEffectListener,_currentAnime.onFinishCallback);
            _currentAnime = CreateAnimeOption(_currentAnime.AnimeSerial, _currentAnime.Direction,actionType,
                _currentAnime.playType, _currentAnime.Speed);
            _play(_currentAnime);
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
        private void _play(AnimeOption animeOption)
        {
            isPlayable = false;
            _currentAnime = null;
            
            // Debug.Log("AnimePlayer:play " + animeOption.AnimeSerial + "  " + animeOption.actionType);
            
            AnimeFrame[] frames = new AnimeFrame[animeOption.AnimeDetail.FrameCount];

            if (isFrameBatch)
            {
                Anime.BakeAnimeFrames(animeOption.AnimeDetail, _paletIndex, isLinearFilter);
                //获取动画帧数据
                for (int i = 0; i < animeOption.AnimeDetail.AnimeFrameInfos.Count; i++)
                {
                    if(!animeOption.AnimeDetail.AnimeFrameInfos[i].AnimeSprites.ContainsKey(_paletIndex)) continue;
                    if(animeOption.AnimeDetail.AnimeFrameInfos[i].AnimeSprites[_paletIndex] == null) continue;
                    //创建帧数据
                    frames[i] = new AnimeFrame();
                    frames[i].Index = i;
                    frames[i].GraphicInfo = animeOption.AnimeDetail.AnimeFrameInfos[i].GraphicInfo;
                    frames[i].Sprite = animeOption.AnimeDetail.AnimeFrameInfos[i].AnimeSprites[_paletIndex][isLinearFilter];
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

                    int subPaletIndex = 0;
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
                    frames[i].Sprite = graphicData.Sprite;
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

        //停止播放
        public void Stop()
        {
            isPlayable = false;
            _currentAnime = null;
            _frames = null;
            gameObject.SetActive(false);
        }

        //暂停播放
        public void Pause()
        {
            isPlayable = false;
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
            Anime.PlayType playType=Anime.PlayType.Once, float Speed = 1f,AnimeEffectListener onEffectListener = null, AnimeCallback onFinishCallback = null)
        {
            AnimeDetail animeDetail = Anime.GetAnimeDetail(serial, Direction, actionType);
            if (animeDetail == null)
            {
                // Debug.Log("AnimePlayer:AnimeDetail [" + serial + "] is null");
                return null;
            }
            AnimeOption animeOption = new AnimeOption()
            {
                AnimeSerial = serial,
                Direction = Direction,
                actionType = actionType,
                playType = playType,
                Speed = Speed,
                FrameRate = animeDetail.CycleTime / Speed / animeDetail.FrameCount,
                AnimeDetail = animeDetail,
                onEffectListener = onEffectListener,
                onFinishCallback = onFinishCallback,
            };
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
            if (_animeQueue.Count == 0)
            {
                _play(animeOption);
            }
            else
            {
                _animeQueue.Add(animeOption);    
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
            _currentAnime = CreateAnimeOption(_currentAnime.AnimeSerial, directionType, _currentAnime.actionType,
                _currentAnime.playType, _currentAnime.Speed);
            _currentAnime.CurrentFrame = --currentFrame;
                
            _currentAnime.KeepFrame = true;
            _play(_currentAnime);
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
            if (!isPlayable || _frames.Length == 0) return;
            
            //动画结束
            if (_currentAnime.CurrentFrame >= _currentAnime.AnimeDetail.FrameCount)
            {
                //循环播放
                if (_currentAnime.playType == Anime.PlayType.Loop)
                {
                    _currentAnime.onFinishCallback?.Invoke(_currentAnime.actionType);
                    _currentAnime.CurrentFrame = 0;
                }else if (_currentAnime.playType is Anime.PlayType.Once or Anime.PlayType.OnceAndDestroy)
                {
                    _animeQueue.RemoveAt(0);
                    if (_currentAnime.playType == Anime.PlayType.OnceAndDestroy)
                    {
                        _spriteRenderer.sprite = null;
                        _imageRenderer.sprite = null;
                        _rectTransform.sizeDelta = Vector2.zero;
                        _currentAnime.onFinishCallback?.Invoke(_currentAnime.actionType);
                        gameObject.SetActive(false);
                        return;
                    }
                    //播放下一个动画
                    if(_animeQueue.Count>0)
                    {
                        _currentAnime.onFinishCallback?.Invoke(_currentAnime.actionType);
                        AnimeOption animeOption = _animeQueue[0];
                        _play(animeOption);
                        return;
                    }else
                    {
                        if (_currentAnime.KeepFrame)
                        {
                            _currentAnime.onFinishCallback?.Invoke(_currentAnime.actionType);
                            // _currentAnime.CurrentFrame--;
                        }
                        else
                        {
                            isPlayable = false;
                            _currentAnime.onFinishCallback?.Invoke(_currentAnime.actionType);
                            return;
                        }
                        
                    }
                    
                }
                
            }
            
            //问题帧自动跳过
            if (_currentAnime.CurrentFrame < _frames.Length && _frames[_currentAnime.CurrentFrame] == null)
            {
                _currentAnime.CurrentFrame++;
                return;
            }
            
            //根据当前帧Sprite动态调整对象大小
            float width = _frames[_currentAnime.CurrentFrame].Sprite.rect.width * 1f;
            float height = _frames[_currentAnime.CurrentFrame].Sprite.rect.height * 1f;

            Vector3 pos = Vector3.zero;
            pos.x = _frames[_currentAnime.CurrentFrame].GraphicInfo.OffsetX;
            pos.y = -_frames[_currentAnime.CurrentFrame].GraphicInfo.OffsetY;
            
            if (isRenderByImage)
            {
                _imageRenderer.sprite = _frames[_currentAnime.CurrentFrame].Sprite;
                _imageRenderer.SetNativeSize();
                if (_currentAnime.AnimeDetail.FLAG!=null)
                {
                    if (_currentAnime.AnimeDetail.FLAG.REVERSE_X)
                    {
                        _imageRenderer.transform.localScale = new Vector3(-1, 1, 1);
                        pos.x = -pos.x;
                    }

                    if (_currentAnime.AnimeDetail.FLAG.REVERSE_Y)
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
                _spriteRenderer.sprite = _frames[_currentAnime.CurrentFrame].Sprite;
                _rectTransform.sizeDelta = new Vector2(width, height);
                _spriteRenderer.size = new Vector2(width, height);
                _rectTransform.pivot = new Vector2(0.5f,0f);
                if (_currentAnime.AnimeDetail.FLAG!=null)
                {
                    if (_currentAnime.AnimeDetail.FLAG.REVERSE_X)
                    {
                        _spriteRenderer.flipX = true;
                    }
                    
                    if (_currentAnime.AnimeDetail.FLAG.REVERSE_Y)
                    {
                        _spriteRenderer.flipY = true;
                    }
                }
                else
                {
                    _spriteRenderer.flipX = false;
                    _spriteRenderer.flipY = false;
                }
                _rectTransform.localPosition = Vector3.zero;
            }
            frameTexture = _frames[_currentAnime.CurrentFrame].Sprite.texture;
            
            _timer = Time.time * 1000;
            
            //动画事件帧监听
            if (_frames[_currentAnime.CurrentFrame].AnimeFrameInfo.Effect > 0)
                _currentAnime.onEffectListener?.Invoke(_frames[_currentAnime.CurrentFrame].AnimeFrameInfo.Effect);
            //音频事件帧监听
            if (_frames[_currentAnime.CurrentFrame].AnimeFrameInfo.AudioIndex > 0)
                onAudioListener?.Invoke(_frames[_currentAnime.CurrentFrame].AnimeFrameInfo.AudioIndex);
            
            _currentAnime.CurrentFrame++;
        }
    }
}