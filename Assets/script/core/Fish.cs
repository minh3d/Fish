using UnityEngine;
using System.Collections;

public class Fish : MonoBehaviour/* ,IPoolObj*/{
 
    public GameMain.Event_FishKilled EvtFishKilled;//鱼死亡事件

    public int Odds = 1;//赔率

    [System.NonSerialized]
    public int OddBonus = 1;//用于奖励的赔率(有hitprocess赋值,该值应该由kill传入,因为不想复杂化代码,而做的临时方案)
    [System.NonSerialized]
    public float TimeDieAnimation = 1.38F;//死亡动画持续时间(默认:1.35秒)

    /// <summary>
    /// //鱼序列说明
    ///  普通鱼:    0~49
    ///  范围炸弹:  100~149
    ///  同类炸弹:  70~99
    /// </summary>
    public int TypeIndex = 0;//类型索引,顺序递增,不重复,用于放入数组.需要手动赋值.为了效率
    //public HittableType HittableType_ = HittableType.Normal;
    public bool HitByBulletOnly = false;//只能被子弹击中攻击

    public float AreaBombRadius = 1.6F;//如果是范围炸弹的话.爆炸的范围半径
    public int AreaBombOddLimit = 300;//炸弹倍数限制,超过倍数之后的鱼不参与赔率计算
    public Fish[] Prefab_SameTypeBombAffect;//如果是同类鱼炸弹,是炸那个类型的

    public bool IsLockable = true;//是否可被锁定

    public int FishTypeIdx;
    //[HideInInspector]
    public string HittableTypeS;
    //位移标识,awake处复制
    public uint ID
    {
        get
        {
            if (mID == 0)
            {
                mID = mIDGenerateNow;
                ++mIDGenerateNow;
                if (mIDGenerateNow == 0)//保证不存在0的ID
                    ++mIDGenerateNow;
            }
            return mID;
        }
    }

    
    //public tk2dAnimatedSprite Prefab_AniSwim;
    //public tk2dAnimatedSprite Prefab_AniDead;//pre,漏写

    public GameObject Prefab_GoAniSwim;
    public GameObject Prefab_GoAniDead;//pre,漏写

    public AudioClip[] Snds_Die;
 

    [System.NonSerialized]
    public bool Attackable = true;//是否可攻击
   
    private GameObject mPrefab;
    public GameObject Prefab
    {
        get { return mPrefab; }
        set { mPrefab = value; /*Debug.Log("Fish set prefab."); */}

    }
    private Transform mTs;

    private Renderer mRenderer;
    private uint mID = 0;
    private static uint mIDGenerateNow = 1;// 用于计算当前鱼id
    private Swimmer mSwimmer;
    private tk2dSpriteAnimator mAnimationSprite;//mGoAniSprite的tk2dAnimatedSprite或者其子对象的首个tk2dAnimatedSprite
    private GameObject mGoAniSprite;
    public Swimmer swimmer
    {
        get
        {
            if (mSwimmer == null)
                mSwimmer = GetComponent<Swimmer>();
            return mSwimmer;
        }
    }

    public bool VisiableFish
    {
        set
        {
            if (mRenderer == null)
            {
                mRenderer = GetComponentInChildren<Renderer>();
            }
            mRenderer.enabled = value;
        }

    }

    public tk2dSpriteAnimator AniSprite
    {
        get
        {
            if (mAnimationSprite == null)
            {
                mGoAniSprite = Pool_GameObj.GetObj(Prefab_GoAniSwim);
                if(mGoAniSprite != null)
                    mGoAniSprite.SetActive(true);

                mAnimationSprite = mGoAniSprite.GetComponent<tk2dSpriteAnimator>();
                if (mAnimationSprite == null)
                    mAnimationSprite = mGoAniSprite.GetComponentInChildren<tk2dSpriteAnimator>();

                 
                
                Component[] renderers = mGoAniSprite.GetComponentsInChildren(typeof(Renderer));
                foreach (Component r in renderers)
                {
                    ((Renderer)r).enabled = true;
                }

                Transform tsAni = mGoAniSprite.transform;
                tsAni.parent = transform;
                tsAni.localPosition = Vector3.zero;
                tsAni.localRotation = Quaternion.identity;
               // tsAni.localScale.x = 1;
            }
            return mAnimationSprite;
        }
    }
    public void CopyDataTo(Fish tar)
    {
        
        tar.Attackable = Attackable;
        tar.mID = mID;
        
    }

// 
//     public void On_Reuse(GameObject prefab)
//     {
//         gameObject.SetActive(true);
//         prefab.GetComponent<Fish>().CopyDataTo(this);
//         VisiableFish = true;
//         collider.enabled = true;
//         ++GameMain.Singleton.NumFishAlive;
//         mAnimationSprite = AniSprite;//调用一下初始化函数
//  
//     }
// 
// 
//     public void On_Recycle()
//     {
//         StopAllCoroutines();
//         gameObject.SetActive(false);
//  
//         swimmer.CurrentState = Swimmer.State.Stop;
//        
//     }
    void Awake()
    {
        mTs = transform;
        swimmer.EvtSwimOutLiveArea += Handle_SwimOutLiveArea;
        ++GameMain.Singleton.NumFishAlive;
        mAnimationSprite = AniSprite;//调用一下初始化函数 
        if (GameMain.EvtFishInstance != null)
            GameMain.EvtFishInstance(this);
    }

    void Handle_SwimOutLiveArea()
    {
        if (Attackable)
        {
            Attackable = false;
            Clear();
        }
    }

    //private bool mIsCleaned = false;
    /// <summary>
    /// 清除,从屏幕上消失
    /// </summary>
    public void Clear()
    {
        //if (mIsCleaned)
        //    return;

        //mIsCleaned = true;
 
        if (GameMain.Singleton != null)
            --GameMain.Singleton.NumFishAlive;

        if (GameMain.EvtFishClear != null)
            GameMain.EvtFishClear(this);

        Attackable = false;

        Pool_GameObj.RecycleGO(Prefab_GoAniSwim, mGoAniSprite);
        mGoAniSprite.SetActive(false);
        Transform tsAniSwim = mGoAniSprite.transform;
        tsAniSwim.position = new Vector3(1000F, 0F, 0F);
        tsAniSwim.rotation = Quaternion.identity;
        tsAniSwim.localScale = Vector3.one;

        mGoAniSprite = null;
        mAnimationSprite = null;
 
//         if (!Pool_GameObj.RecycleGO(null, gameObject))
//         {
            Destroy(gameObject);


        //}
    }
 

  

    public void Kill(Player killer,Bullet b,float delayVisiableAnimation)
    {
        if (!Attackable)
            return;
        if (EvtFishKilled != null)
            EvtFishKilled(killer, b, this);

        if (GameMain.EvtFishKilled != null)
            GameMain.EvtFishKilled(killer,b,this);

        Die(killer,delayVisiableAnimation,b.FishOddsMulti);
    }

    void Die(Player killer,float delay,int oddsMulti)
    {
 
        Attackable = false;
        Vector3 deadWorldPos = mTs.position;

        //if (EvtDieStart != null)
        //    EvtDieStart = null;

        //消除碰撞框
        collider.enabled = false;

        //隐藏原来鱼
        //AniSprite.renderer.enabled = false;
        //Destroy(AniSprite.renderer.gameObject);
        //foreach ()
        
        Component[] renderers = GetComponentsInChildren(typeof(Renderer));
        foreach (Component r in renderers)
        {
            ((Renderer)r).enabled = false;
        }

        float delayTotal = delay + TimeDieAnimation;
        //播放死亡动画
        if (Prefab_GoAniDead != null)
        {

            GameObject goDieAnimation= Pool_GameObj.GetObj(Prefab_GoAniDead);
            goDieAnimation.SetActive(true);
            goDieAnimation.transform.parent = GameMain.Singleton.FishGenerator.transform;
            goDieAnimation.transform.position = new Vector3(mTs.position.x, mTs.position.y, Defines.GlobleDepth_DieFish);
            goDieAnimation.transform.rotation = mTs.rotation;

            RecycleDelay fishRecycleDelay = goDieAnimation.AddComponent<RecycleDelay>();
            fishRecycleDelay.delay = delayTotal;
            fishRecycleDelay.Prefab = Prefab_GoAniDead;
        }


        //飞币
        if (Odds != 0)
            killer.Ef_FlyCoin.FlyFrom(deadWorldPos, oddsMulti * Odds, delayTotal);

        //声音
        if(Odds <= 10)
        {
            if(GameMain.Singleton.SoundMgr.snd_Score != null)
                GameMain.Singleton.SoundMgr.PlayOneShot( GameMain.Singleton.SoundMgr.snd_Score[0] );
        }
        else if(Odds > 10 && Odds < 25)
        {
            if(GameMain.Singleton.SoundMgr.snd_Score != null)
                GameMain.Singleton.SoundMgr.PlayOneShot( GameMain.Singleton.SoundMgr.snd_Score[1] );
        }
        else
        {
            if(GameMain.Singleton.SoundMgr.snd_Score != null)
                GameMain.Singleton.SoundMgr.PlayOneShot( GameMain.Singleton.SoundMgr.snd_Score[2] );

        }
        if (Snds_Die != null && Snds_Die.Length != 0)
        {
            GameMain.Singleton.SoundMgr.PlayOneShot(Snds_Die[Random.Range(0, Snds_Die.Length)]);
        }

        //yield return new WaitForSeconds(delayTotal);

        //删除本对象
        Clear();
        
    } 

    public void ClearAI()
    {
        Component[] fishAIs = GetComponents(typeof(IFishAI));
        for (int i = 0; i != fishAIs.Length; ++i )
        {
            Destroy(fishAIs[i]);
        }
    }


}
