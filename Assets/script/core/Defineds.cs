using UnityEngine;

public static class Defines
{

    public static int NumPlayer = 6;//玩家数目
    public static int[] GunLayoutTypeToNumPlay = new int[] {1,2,3,4,4,6,8,10
,6,6,8,8,8,8,12,16,20
,2,2,4,4,6,6,8,8,8,8,10,10,12,14,16};//炮台布局类型与玩家数目关系

    public static int MaxNumPlayer = 20;//最大玩家数目
    public static float TimeBackGroundJumpSelect = 0.5F;//后台按死按键 光标跳动 间隔
    public static int NumScoreUpMax = 10000000;//最大上分数1000w
    public static int[] ChangeGunNeedScore = new int[] { 1, 50, 1001 ,10000};//达到该分数改变枪的外型,由类型GunLevelType决定数组大小

    public static float ClearFishRadius = 1.1F;//鱼离开场景距离达到 KillFishRadius*自己的半径 则清除


    //public static UnityEngine.Rect WorldDimensionUnit = new UnityEngine.Rect(-1.777778F, -1F, 3.555556F, 2F);//单位屏幕区域(Unity世界单位)
    public static UnityEngine.Rect WorldDimensionUnit = new UnityEngine.Rect(-683F, -384F, 1366F, 768F);//单位屏幕区域(Unity世界单位)

    public static int OriginWidthUnit = 1366;
    public static int OriginHeightUnit = 768;

    public static int ScreenWidthUnit = 854;
    public static int ScreenHeightUnit = 480;

    public static int SoundVolumeLevelNum = 16;//音量分多少级
    public static int PswLength = 6;//密码长度
    public static float FreezeBombTime = 10F;//定屏炸弹持续时间 单位:秒

    public static float OffsetAdv_FishGlobleDepth = 0.05F;//鱼深度偏移量
    //深度
    //炮台-9.3~-9
    //public static float GlobleDepth_PrepareInBG = -9.9F;//准备进后台
    //public static float GlobleDepth_GameDataViewer = -9.8F;//数据框显示
    //public static float GlobleDepth_BombParticle = -9.5F;//炸弹粒子
    //public static float GlobleDepth_DieFish = -9.4F;//死亡鱼
    //public static float GlobleDepth_PlayerTargeter = -9.3F;//玩家瞄准框框
    //public static float GlobleDepth_Web = -8.9F;//渔网
    //public static float GlobleDepth_WaterWave = -8F;//一直在场景上面的波浪效果
    //public static float GlobleDepth_ChangeNewScene = -7.99F;//换场景时的新场景
    //public static float GlobleDepth_SceneSweeper = -7.5F;//场景切换时扫过的波浪
    //public static float GlobleDepth_SceneBubblePar = -0.5F;//场景粒子



    public static float GlobleDepth_PrepareInBG = 90F;//准备进后台
    public static float GlobleDepth_GameDataViewer = 100F;//数据框显示
    

    public static float GlobleDepth_ChangeNewScene = 200F;//换场景时的新场景

    public static float GlobleDepth_DieFishPopDigit = 210F;//鱼死亡弹出数字
    public static float GlobleDepth_BombParticle = 300F;//炸弹粒子
    public static float GlobleDepth_LiziKa = 400F;//粒子卡
    
    public static float GlobleDepth_DieFish = 420F;//死亡鱼动画

    public static float GlobleDepth_PlayerTargeter = 540F;//玩家瞄准框框
    public static float GlobleDepth_Web = 560F;//渔网
    
    public static float GlobleDepth_WaterWave = 580F;//一直在场景上面的波浪效果
    public static float GlobleDepth_SceneSweeper = 590F;//场景切换时扫过的波浪
    public static float GlobleDepth_FishBase = 801F;//鱼基本深度

    public static float GlobleDepth_SceneBubblePar = 910F;//场景粒子(泡泡)
    public static float GlobleDepth_TempSceneShake = 940F;//临时震动创建场景的深度


    public static float GMDepth_Fish = 850F;//鱼主要所在深度 
    public static float GlobleDepth_SceneBackground = 950F;
    
    //
    //玩家:      501~600
    //游动鱼:    601~900
    //背景:      901~1000
    
}
public static class PubFunc
{
    public static Quaternion QuatRotateZ90 = new Quaternion(0F, 0F, 0.7F, 0.7F);//绕Z轴旋转90度

    /// <summary>
    /// 获得旋转right向量到指定向量的四元数
    /// </summary>
    /// <param name="right"></param>
    /// <returns></returns>
    public static Quaternion RightToRotation(Vector3 rightTarget)
    {
        return Quaternion.LookRotation(Vector3.forward, rightTarget) * QuatRotateZ90;
    }
}

public enum Language
{
    Cn,
    En
}

public enum HittableType
{
    Normal,         //普通鱼typeIndex:0~49
    AreaBomb,       //范围炸弹typeIndex 100~149
    SameTypeBomb,   //同类炸弹typeIndex:与Normal鱼一致
    FreezeBomb,      //定身炸弹
    SameTypeBombEx  //触发所有同类炸弹的炸弹
}

public struct FishOddsData
{
    public FishOddsData(uint id, int odds) { ID = id; Odds = odds; }
    public uint ID;
    public int Odds; 
}

public delegate void Event_Generic();
public enum HpyInputKey
{
    Up, Down, Left, Right, Fire, Advance ,ScoreUp,ScoreDown,OutBounty
    , BS_Up, BS_Down, BS_Left, BS_Right,BS_Confirm, BS_Cancel,BS_GameLite

}

[System.Serializable]
public class WebScoreScaleRatio
{
    public WebScoreScaleRatio()
    {
        Scale = PositionScale = /*ScaleCollider =*/ BubbleScale = 1F;
        
    }
    public int StartScore;//开始分值,不可超过下一开始分值
    //public float Size;//网大小unity中单位
    public float Scale;//标准缩放值-网缩放值
    public float PositionScale;//位置缩放值
    //public float ScaleCollider;//碰撞缩放值
    public float BubbleScale;//泡泡缩放
    public string NameSprite;//网sprite id
    public GameObject PrefabWeb;//网sprite prefab
    public GameObject PrefabWebBoom;//网效果,格局prefab
}

/// <summary>
/// 飞币种类
/// </summary>
public enum FlyingCoinType
{
    Sliver,Glod
}

/// <summary>
/// 枪总类型
/// </summary>
public enum GunType
{
    Normal,
    NormalTri,
    NormalQuad,
    Lizi,
    LiziTri, 
    LiziQuad
}

/// <summary>
/// 枪分级类型,二管.三管.四管
/// </summary>
public enum GunLevelType
{
    Dbl,
    Tri,
    Quad
}

/// <summary>
/// 功能分类,普通,离子
/// </summary>
public enum GunPowerType
{
    Normal,
    Lizi
}

/// <summary>
/// 游戏难度
/// </summary>
public enum GameDifficult
{
    VeryEasy,
    Easy,
    Hard,
    VeryHard,
    DieHard
}
/// <summary>
/// 出奖励类型
/// </summary>
public enum OutBountyType
{
    OutCoinButtom,
    OutTicketButtom,
    OutCoinImmediate,
    OutTicketImmediate
}

/// <summary>
/// 场地类型
/// </summary>
public enum ArenaType
{
    Small,Medium,Larger
}


/// <summary>
///  炮台布局类型(注意顺序,有地方使用该顺序判断屏幕数量)
/// </summary>
public enum GunLayoutType
{
    //W10,W8,W6,W4,L4,L3,
    L1, L2, L3, L4, W4, W6, W8, W10,                                                        //单屏
    S_L6S, S_L6D, S_L8S, S_L8D, S_W8S, S_W8D, S_W12S, S_W16S,S_W20S,                //双屏
    L_L2S, L_L2D, L_L4S, L_L4D, L_L6S, L_L6D, L_L8S, L_L8D, L_W8S, L_W8D, L_W10S, L_W10D, L_W12S, L_W14S, L_W16S//联屏
}

