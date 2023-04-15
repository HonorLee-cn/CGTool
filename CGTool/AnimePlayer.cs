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

namespace CGTool
{
    //动画周期回调
    public delegate void AnimeCallback();
    
    //动画动作帧监听
    public delegate void AnimeEffectListener(Anime.EffectType effect);
    
    //动画音频帧监听
    public delegate void AnimeAudioListener(int audioIndex);
    
    //鼠标移入事件监听
    public delegate void MouseListener(AnimePlayer animePlayer);
    
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
            public Anime.ActionType ActionType;
            public bool Infinity;
            public float Speed;
            public float FrameRate;
            public AnimeDetail AnimeDetail;
            public AnimeCallback onFinishCallback;
        }
        
        //当前播放
        private AnimeOption _currentAnime;
        private AnimeFrame[] _frames;
        private int _currentFrame;
        
        //是否播放
        private bool isPlayable;
        
        //待播放队列
        private Queue<AnimeOption> _animeQueue = new Queue<AnimeOption>();
        
        //计时器
        private float _timer;
        
        //绑定SpriteRenderer
        private SpriteRenderer _spriteRenderer;
        //绑定RectTransform
        private RectTransform _rectTransform;
        //绑定BoxCollider2D(可选)
        private BoxCollider2D _boxCollider2D;
        
        //动画动作帧监听
        public AnimeEffectListener onEffectListener;
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
                float offsetX = -_frames[_currentFrame].GraphicInfo.OffsetX;
                float offsetY = _frames[_currentFrame].GraphicInfo.OffsetY;
                return new Vector2(offsetX, offsetY);
            }
        }

        //实例初始化时获取相关绑定
        private void Awake()
        {
            _spriteRenderer = GetComponentInParent<SpriteRenderer>();
            _rectTransform = GetComponentInParent<RectTransform>();
            //碰撞盒,仅当需要添加鼠标事件时使用
            _boxCollider2D = GetComponent<BoxCollider2D>();
        }

        //鼠标移入监听
        private void OnMouseEnter()
        {
            if(onMouseEnterListener!=null) onMouseEnterListener(this);
        }

        //鼠标移出监听
        private void OnMouseExit()
        {
            if(onMouseExitListener!=null) onMouseExitListener(this);
        }

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
        public AnimePlayer play(uint Serial,Anime.DirectionType Direction,Anime.ActionType ActionType,bool Infinity = false,float Speed=1f,AnimeCallback onFinishCallback=null)
        {
            if (_spriteRenderer == null)
            {
                Debug.Log("AnimePlayer:SpriteRenderer is null");
                return this;
            }
            AnimeOption animeOption = CreateAnimeOption(Serial, Direction, ActionType, Infinity, Speed, onFinishCallback);
            if (animeOption == null)
            {
                Debug.Log("AnimePlayer:AnimeOption create failed");
                return this;
            }
            //清空播放队列
            _animeQueue.Clear();
            //播放
            _play(animeOption);
            
            //链式调用,后续可通过nextPlay方法添加动画到播放队列
            return this;
        }

        //调整动画方向
        public void changeDirection(Anime.DirectionType directionType)
        {
            _currentAnime = CreateAnimeOption(_currentAnime.AnimeSerial, directionType, _currentAnime.ActionType,
                _currentAnime.Infinity, _currentAnime.Speed, _currentAnime.onFinishCallback);
            _play(_currentAnime);
        }
        
        //调整动画动作类型
        public void changeActionType(Anime.ActionType actionType)
        {
            _currentAnime = CreateAnimeOption(_currentAnime.AnimeSerial, _currentAnime.Direction,actionType,
                _currentAnime.Infinity, _currentAnime.Speed, _currentAnime.onFinishCallback);
            _play(_currentAnime);
        }

        //播放
        private void _play(AnimeOption animeOption)
        {
            isPlayable = false;
            _currentAnime = animeOption;
            _frames = new AnimeFrame[animeOption.AnimeDetail.FrameCount];
            
            //获取动画帧数据
            for (int i = 0; i < animeOption.AnimeDetail.AnimeFrameInfos.Length; i++)
            {
                GraphicInfoData graphicInfoData = GraphicInfo.GetGraphicInfoDataByIndex(animeOption.AnimeDetail.Version, animeOption.AnimeDetail.AnimeFrameInfos[i].GraphicIndex);
                if (graphicInfoData == null)
                {
                    Debug.Log("GraphicInfo Version:" + animeOption.AnimeDetail.Version + " Index:" +
                              animeOption.AnimeDetail.AnimeFrameInfos[i] + " is null");
                    continue;
                }
                GraphicData graphicData = Graphic.GetGraphicData(graphicInfoData);
                if (graphicData == null)
                {
                    Debug.Log("GraphicData Version:" + animeOption.AnimeDetail.Version + " Index:" +
                              animeOption.AnimeDetail.AnimeFrameInfos[i] + " is null");
                    continue;
                }
                
                //创建帧数据
                _frames[i] = new AnimeFrame();
                _frames[i].Index = i;
                _frames[i].GraphicInfo = graphicInfoData;
                _frames[i].Sprite = graphicData.Sprite;
                _frames[i].AnimeFrameInfo = animeOption.AnimeDetail.AnimeFrameInfos[i];
            }

            _currentFrame = -1;
            isPlayable = true;
            UpdateFrame();
        }

        //创建动画配置
        private AnimeOption CreateAnimeOption(uint Serial, Anime.DirectionType Direction, Anime.ActionType ActionType,
            bool Infinity = false, float Speed = 1f, AnimeCallback onFinishCallback = null)
        {
            AnimeDetail animeDetail = Anime.GetAnimeDetail(Serial, Direction, ActionType);
            if (animeDetail == null)
            {
                Debug.Log("AnimePlayer:AnimeDetail is null");
                return null;
            }
            AnimeOption animeOption = new AnimeOption()
            {
                AnimeSerial = Serial,
                Direction = Direction,
                ActionType = ActionType,
                Infinity = Infinity,
                Speed = Speed,
                FrameRate = animeDetail.CycleTime / Speed / animeDetail.FrameCount,
                AnimeDetail = animeDetail,
                onFinishCallback = onFinishCallback,
            };
            return animeOption;
        }

        //加入链式动画播放队列
        public AnimePlayer nextPlay(uint Serial, Anime.DirectionType Direction, Anime.ActionType ActionType,
            bool Infinity = false, float Speed = 1f, AnimeCallback onFinishCallback = null)
        {
            AnimeOption animeOption = CreateAnimeOption(Serial, Direction, ActionType, Infinity, Speed, onFinishCallback);
            if (animeOption == null) return this;
            _animeQueue.Enqueue(animeOption);
            return this;
        }
        
        //更新计算
        private void Update()
        {
            float now = Time.time * 1000;
            if (_currentAnime != null && (now - _timer) >= _currentAnime.FrameRate) UpdateFrame();
        }

        //更新帧
        private void UpdateFrame()
        {
            if (!isPlayable || _frames.Length == 0) return;
            
            _currentFrame++;
            
            //动画结束
            if (_currentFrame >= _currentAnime.AnimeDetail.FrameCount)
            {
                if(_currentAnime.onFinishCallback!=null) _currentAnime.onFinishCallback();
                //循环播放
                if (_currentAnime.Infinity)
                {
                    _currentFrame = 0;
                }
                //播放下一个动画
                else if(_animeQueue.Count>0)
                {
                    AnimeOption animeOption = _animeQueue.Dequeue();
                    _play(animeOption);
                    return;
                }
            }
            
            //问题帧自动跳过
            if (_frames[_currentFrame] == null) return;
            //自动偏移
            // float graphicWidth = _frames[_currentFrame].Sprite.rect.width;
            // float graphicHeight = _frames[_currentFrame].Sprite.rect.height;
            // float offsetX = -_frames[_currentFrame].GraphicInfo.OffsetX;
            // float offsetY = _frames[_currentFrame].GraphicInfo.OffsetY;
            
            //根据当前帧Sprite动态调整对象大小
            float width = _frames[_currentFrame].Sprite.rect.width * 1f;
            float height = _frames[_currentFrame].Sprite.rect.height * 1f;
            
            _spriteRenderer.sprite = 
                _frames[_currentFrame].Sprite;
            _rectTransform.sizeDelta = new Vector2(width, height);
            _spriteRenderer.size = new Vector2(width, height);
            _rectTransform.pivot = new Vector2(0.5f,0f);
            
            // 2D碰撞器自动调整,但是动态碰撞器反而会导致重叠大物体选中效果不稳定,效果不如固定大小碰撞器好
            // if (_boxCollider2D != null)
            // {
            //     Vector2 newSize =_boxCollider2D.size 
            //     _boxCollider2D.size = new Vector2(width, height);
            // }
            // _rectTransform.pivot = new Vector2(offsetX,offsetY);
            // _rectTransform.localPosition = new Vector3(0f,  0f);
            
            
            _timer = Time.time * 1000;
            
            //动画事件帧监听
            if(_frames[_currentFrame].AnimeFrameInfo.Effect >0 && onEffectListener!=null) onEffectListener(_frames[_currentFrame].AnimeFrameInfo.Effect);
            //音频事件帧监听
            if(_frames[_currentFrame].AnimeFrameInfo.AudioIndex >0 && onAudioListener!=null) onAudioListener(_frames[_currentFrame].AnimeFrameInfo.AudioIndex);
        }
    }
}