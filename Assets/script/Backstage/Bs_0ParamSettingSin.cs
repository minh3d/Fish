using UnityEngine;
using System.Collections;

public class Bs_0ParamSettingSin : MonoBehaviour {

    public enum Option
    {
        Difficult,
        CoinTicketRatio,
        ScoreChange,
        MaxGunScore,
        _OutBountyType,
        _GunLayoutType,
        LaguageSelect,//可隐藏
        RecoverSetting,
        SaveSetting,
        Quit

    }

    public enum State
    {
        Main,
        Sub 
    }

    public CursorDimLocation[] CursorLocals;
    public Ctrl_OptionText Ctrl_GameDifficul;//游戏难度

    public Ctrl_OptionNum Ctrl_CoinTicketRatio_Coin;//币票比例
    public Ctrl_OptionNum Ctrl_CoinTicketRatio_Ticket;//币票比例

    //public Ctrl_OptionText Ctrl_IsBulletCross;//联屏时子弹是否可穿
    public Ctrl_OptionNum Ctrl_ScoreChangeVal;//最小分数切换
    public Ctrl_OptionNum Ctrl_MaxScore;//最大分值
    //public Ctrl_OptionNum Ctrl_MinScore;//最小分值
    public Ctrl_OptionText Ctrl_OutBountyType;
    public Ctrl_OptionText Ctrl_GunLayouType;//机台位置
    public Ctrl_OptionText Ctrl_Language;//语言选择
    //public tk2dSprite Spr_GunLayoutHint;//机台布局提示图精灵
    public string[] StrGunLayoutHintSprs;//机台布局提示图精灵名称
    public GameObject[] goGunLayoutSprs;
    public Transform TsLocal_GunLayoutHint;//机台布局提示图定位
    
    private GameObject mGOGunLayoutHint;
    private State mState;
    private int mCurCursorIdx;
    private bool mIsMainMenuMovable = true;

    private Ctrl_OptionNum mCurrentCoinTicketRotioSelect;//当前选择的coinTicket
    //private bool mEditingCoinRatio = false;//币比例 是否修改中 
    //private bool mEditingTicketRatio = false;//票比例 是否修改中 


    private GameDifficult mEnterVal_GameDifficult;
    private int mEnterVal_CoinTicketRatio_Coin;
    private int mEnterVal_CoinTicketRatio_Ticket;

    //private bool mEnterVal_IsBulletCross;
    private int mEnterVal_ScoreChangeValue;
    private int mEnterVal_Scoremax;
    //private int mEnterVal_Scoremin;

    private OutBountyType mEnterVal_OutBountyType;
    private GunLayoutType mEnterVal_GunLayoutType;
    private Language mEnterVal_Language;
 
    void Awake()
    {
        
        //mEditingCoinRatio = mEditingTicketRatio = false;
        GameMain.EvtLanguageChange += Handle_LanguageChanged;
    }
 

    void OnEnable()
    {
        GameMain.EvtInputKey += Handle_InputKey;

        BackStageSetting bsSetting = GameMain.Singleton.BSSetting;
        mEnterVal_GameDifficult = bsSetting.GameDifficult_.Val;
        mEnterVal_CoinTicketRatio_Coin = bsSetting.CoinTicketRatio_Coin.Val; 
        mEnterVal_CoinTicketRatio_Ticket= bsSetting.CoinTicketRatio_Ticket.Val;
        //mEnterVal_IsBulletCross = bsSetting.IsBulletCrossWhenScreenNet.Val;
        mEnterVal_ScoreChangeValue = bsSetting.ScoreChangeValue.Val;
        mEnterVal_Scoremax = bsSetting.ScoreMax.Val;
        //mEnterVal_Scoremin = bsSetting.ScoreMin.Val;
        mEnterVal_OutBountyType = bsSetting.OutBountyType_.Val;
        mEnterVal_GunLayoutType = bsSetting.GunLayoutType_.Val;
        mEnterVal_Language = bsSetting.LaguageUsing.Val;

        Ctrl_GameDifficul.ViewIdx = (int)mEnterVal_GameDifficult;
        Ctrl_CoinTicketRatio_Coin.ViewTextFormat = "{0:d}  " + BackstageMain.Singleton.Unit_Coin.CurrentText;
        Ctrl_CoinTicketRatio_Coin.NumViewing = mEnterVal_CoinTicketRatio_Coin;
        Ctrl_CoinTicketRatio_Ticket.ViewTextFormat = "{0:d}  " + BackstageMain.Singleton.Unit_Ticket.CurrentText;
        Ctrl_CoinTicketRatio_Ticket.NumViewing = mEnterVal_CoinTicketRatio_Ticket;
        
        //Ctrl_IsBulletCross.ViewIdx
        //Ctrl_IsBulletCross.ViewIdx = mEnterVal_IsBulletCross ? 1 : 0;
        Ctrl_ScoreChangeVal.ViewTextFormat = "{0:d}  " + BackstageMain.Singleton.Unit_Score.CurrentText;
        Ctrl_ScoreChangeVal.NumViewing = mEnterVal_ScoreChangeValue;
        Ctrl_MaxScore.ViewTextFormat = "{0:d}  " + BackstageMain.Singleton.Unit_Score.CurrentText;
        Ctrl_MaxScore.NumViewing = mEnterVal_Scoremax;
        //Ctrl_MinScore.ViewTextFormat = "{0:d}  " + BackstageMain.Singleton.Unit_Score.CurrentText;
        //Ctrl_MinScore.NumViewing = mEnterVal_Scoremin;
        Ctrl_Language.ViewIdx = (int)mEnterVal_Language;

        if (!bsSetting.Dat_GameShowLanguageSetup.Val)
        {
            Renderer r = Ctrl_Language.GetComponentInChildren<Renderer>();
            if (r != null)
                r.enabled = false;
            Transform ts = transform.FindChild("OptionText/OptionText6");
            if (ts != null)
            {
                if (ts.renderer != null)
                    ts.renderer.enabled = false;
            }
        }
        else
        {
            Renderer r = Ctrl_Language.GetComponentInChildren<Renderer>();
            if (r != null)
                r.enabled = true;
            Transform ts = transform.FindChild("OptionText/OptionText6");
            if (ts != null)
            {
                if (ts.renderer != null)
                    ts.renderer.enabled = true;
            }
        }
        

        Ctrl_OutBountyType.ViewIdx = (int)mEnterVal_OutBountyType;

         Ctrl_GunLayouType.ViewIdx = (int)mEnterVal_GunLayoutType;
        if (CursorLocals != null && CursorLocals.Length != 0)
            BackstageMain.Singleton.UpdateCursor(CursorLocals[mCurCursorIdx]);

        //Spr_GunLayoutHint.renderer.enabled = false;
        Ctrl_GunLayouType.EvtAdvanceing += Handle_Ctrl_GunLayoutType;

    }

   
    void OnDisable()
    {
        GameMain.EvtInputKey -= Handle_InputKey;
        Ctrl_GunLayouType.EvtAdvanceing -= Handle_Ctrl_GunLayoutType;
    }

    void Handle_LanguageChanged(Language l)
    {
        Ctrl_CoinTicketRatio_Coin.ViewTextFormat = "{0:d}  " + BackstageMain.Singleton.Unit_Coin.CurrentText;
        Ctrl_CoinTicketRatio_Coin.UpdateText();

        Ctrl_CoinTicketRatio_Ticket.ViewTextFormat = "{0:d}  " + BackstageMain.Singleton.Unit_Ticket.CurrentText;
        Ctrl_CoinTicketRatio_Ticket.UpdateText();

        Ctrl_ScoreChangeVal.ViewTextFormat = "{0:d}  " + BackstageMain.Singleton.Unit_Score.CurrentText;
        Ctrl_ScoreChangeVal.UpdateText();

        Ctrl_MaxScore.ViewTextFormat = "{0:d}  " + BackstageMain.Singleton.Unit_Score.CurrentText;
        Ctrl_MaxScore.UpdateText();

        //Ctrl_MinScore.ViewTextFormat = "{0:d}  " + BackstageMain.Singleton.Unit_Score.CurrentText;
        //Ctrl_MinScore.UpdateText();

        Ctrl_GunLayouType.UpdateText();
    }

    void Handle_Ctrl_GunLayoutType(bool advance)
    {
        //Spr_GunLayoutHint.spriteId = Spr_GunLayoutHint.GetSpriteIdByName(StrGunLayoutHintSprs[Ctrl_GunLayouType.ViewIdx]);

        if (mGOGunLayoutHint != null)
        {
            Destroy(mGOGunLayoutHint);
            mGOGunLayoutHint = null;
        }

        mGOGunLayoutHint = Instantiate(goGunLayoutSprs[Ctrl_GunLayouType.ViewIdx]) as GameObject;
        mGOGunLayoutHint.transform.parent = TsLocal_GunLayoutHint;
        mGOGunLayoutHint.transform.localPosition = Vector3.zero;
    }

    void SelectPrev()
    {
        //音效-后台
        if (GameMain.Singleton.SoundMgr.snd_bkBtn != null)
            GameMain.Singleton.SoundMgr.PlayOneShot(GameMain.Singleton.SoundMgr.snd_bkBtn);

        //可在主菜单移动
        if (mIsMainMenuMovable)
        {
            --mCurCursorIdx;
            if (mCurCursorIdx < 0)
                mCurCursorIdx = CursorLocals.Length - 1;

            //如果不显示语言项，则跳过语言选择
            if (!GameMain.Singleton.BSSetting.Dat_GameShowLanguageSetup.Val && mCurCursorIdx == (int)Option.LaguageSelect)
            {
                --mCurCursorIdx;
                if (mCurCursorIdx < 0)
                    mCurCursorIdx = CursorLocals.Length - 1;
            }

            BackstageMain.Singleton.UpdateCursor(CursorLocals[mCurCursorIdx]);


            if (mCurCursorIdx == (int)Option._GunLayoutType)//机型选择
            {
                if (mGOGunLayoutHint != null)
                {
                    Destroy(mGOGunLayoutHint);
                    mGOGunLayoutHint = null;
                }

                mGOGunLayoutHint = Instantiate(goGunLayoutSprs[Ctrl_GunLayouType.ViewIdx]) as GameObject;
                mGOGunLayoutHint.transform.parent = TsLocal_GunLayoutHint;
                mGOGunLayoutHint.transform.localPosition = Vector3.zero;
            }
            else
            {
                if (mGOGunLayoutHint != null)
                {
                    Destroy(mGOGunLayoutHint);
                    mGOGunLayoutHint = null;
                }
            }
        }
        else
        {
            if (mCurCursorIdx == (int)Option.CoinTicketRatio)//币票比率
            {
                if (mCurrentCoinTicketRotioSelect != null)//切换到票选择
                {
                    mCurrentCoinTicketRotioSelect.GetComponent<Ef_RendererFlash>().StopFlash();
                    mCurrentCoinTicketRotioSelect
                        = mCurrentCoinTicketRotioSelect == Ctrl_CoinTicketRatio_Coin ? Ctrl_CoinTicketRatio_Ticket : Ctrl_CoinTicketRatio_Coin;
                    mCurrentCoinTicketRotioSelect.GetComponent<Ef_RendererFlash>().StartFlash();
                }
            }
        }
    }

    IEnumerator _Coro_SelectPrev()
    {
        while (true)
        {
            SelectPrev();
            yield return new WaitForSeconds(Defines.TimeBackGroundJumpSelect);
        }
    }

    void SelectNext()
    {
        //音效-后台
        if (GameMain.Singleton.SoundMgr.snd_bkBtn != null)
            GameMain.Singleton.SoundMgr.PlayOneShot(GameMain.Singleton.SoundMgr.snd_bkBtn);

        //可在主菜单移动
        if (mIsMainMenuMovable)
        {
            mCurCursorIdx = (mCurCursorIdx + 1) % CursorLocals.Length;

            //如果不显示语言项，则跳过语言选择
            if (!GameMain.Singleton.BSSetting.Dat_GameShowLanguageSetup.Val && mCurCursorIdx == (int)Option.LaguageSelect)
            {
                mCurCursorIdx = (mCurCursorIdx + 1) % CursorLocals.Length;
            }

            BackstageMain.Singleton.UpdateCursor(CursorLocals[mCurCursorIdx]);

            if (mCurCursorIdx == (int)Option._GunLayoutType)//机型选择
            {
                if (mGOGunLayoutHint != null)
                {
                    Destroy(mGOGunLayoutHint);
                    mGOGunLayoutHint = null;
                }

                mGOGunLayoutHint = Instantiate(goGunLayoutSprs[Ctrl_GunLayouType.ViewIdx]) as GameObject;
                mGOGunLayoutHint.transform.parent = TsLocal_GunLayoutHint;
                mGOGunLayoutHint.transform.localPosition = Vector3.zero;
            }
            else
            {
                if (mGOGunLayoutHint != null)
                {
                    Destroy(mGOGunLayoutHint);
                    mGOGunLayoutHint = null;
                }
            }


        }
        else
        {
            if (mCurCursorIdx == (int)Option.CoinTicketRatio)//币票比率
            {
                if (mCurrentCoinTicketRotioSelect != null)//切换到票选择
                {
                    mCurrentCoinTicketRotioSelect.GetComponent<Ef_RendererFlash>().StopFlash();
                    mCurrentCoinTicketRotioSelect
                        = mCurrentCoinTicketRotioSelect == Ctrl_CoinTicketRatio_Coin ? Ctrl_CoinTicketRatio_Ticket : Ctrl_CoinTicketRatio_Coin;
                    mCurrentCoinTicketRotioSelect.GetComponent<Ef_RendererFlash>().StartFlash();

                }
            }
        }
    }

    IEnumerator _Coro_SelectNext()
    {
        while (true)
        {
            SelectNext();
            yield return new WaitForSeconds(Defines.TimeBackGroundJumpSelect);
        }
    }

      
    void Handle_InputKey(int control, HpyInputKey key, bool down)
    {
#region keydown

        if (down && key == HpyInputKey.BS_Up)
        {
            SelectPrev();
            //StartCoroutine("_Coro_SelectPrev");
        }
        else if (down && key == HpyInputKey.BS_Down)
        {
            SelectNext();
            //StartCoroutine("_Coro_SelectNext");
        }
        else if (down && key == HpyInputKey.BS_Left)
        {
            if (mCurCursorIdx == (int)Option.Difficult)//难易度
            {
                Ctrl_GameDifficul.StartChange(false);
            }
            else if (mCurCursorIdx == (int)Option.CoinTicketRatio)//币票比率
            {

                if (mCurrentCoinTicketRotioSelect == null)
                {
                    mCurrentCoinTicketRotioSelect = Ctrl_CoinTicketRatio_Coin;
                    mCurrentCoinTicketRotioSelect.GetComponent<Ef_RendererFlash>().StartFlash();
                }
                else
                {
                    mCurrentCoinTicketRotioSelect.GetComponent<Ef_RendererFlash>().StopFlash();
                    mCurrentCoinTicketRotioSelect.StartChangeNumViewing(-1);
                }
                mIsMainMenuMovable = false;//主菜单不能移动
            }
            else if (mCurCursorIdx == (int)Option._OutBountyType)
                Ctrl_OutBountyType.StartChange(false);
            else if (mCurCursorIdx == (int)Option._GunLayoutType)
                 Ctrl_GunLayouType.StartChange(false);
            //else if (mCurCursorIdx == 4)
            //    Ctrl_IsBulletCross.StartChange(false);
            else if (mCurCursorIdx == (int)Option.ScoreChange)
                Ctrl_ScoreChangeVal.StartChangeNumViewing((int cur) => { return -GetChangeValL(cur); });
            //else if (mCurCursorIdx == 6)
            //    Ctrl_MinScore.StartChangeNumViewing((int cur) => { return -GetChangeValL(cur); });
            else if (mCurCursorIdx == (int)Option.MaxGunScore)
                Ctrl_MaxScore.StartChangeNumViewing((int cur) => { return -GetChangeValL(cur); });
            else if (mCurCursorIdx == (int)Option.LaguageSelect)
                Ctrl_Language.StartChange(false);
        }
 
        else if (down && key == HpyInputKey.BS_Right)
        {
            if (mCurCursorIdx == (int)Option.Difficult)//难易度
            {
                Ctrl_GameDifficul.StartChange(true);
            }
            else if (mCurCursorIdx == (int)Option.CoinTicketRatio)//币票比率
            {

                if (mCurrentCoinTicketRotioSelect == null)
                {
                    mCurrentCoinTicketRotioSelect = Ctrl_CoinTicketRatio_Coin;
                    mCurrentCoinTicketRotioSelect.GetComponent<Ef_RendererFlash>().StartFlash();
                }
                else
                {
                    mCurrentCoinTicketRotioSelect.GetComponent<Ef_RendererFlash>().StopFlash();
                    mCurrentCoinTicketRotioSelect.StartChangeNumViewing(1);
                }
                mIsMainMenuMovable = false;//主菜单不能移动
            }
            else if (mCurCursorIdx == (int)Option._OutBountyType)
                Ctrl_OutBountyType.StartChange(true); 
            else if (mCurCursorIdx == (int)Option._GunLayoutType)
                Ctrl_GunLayouType.StartChange(true); 
            //else if (mCurCursorIdx == 4)
            //    Ctrl_IsBulletCross.StartChange(true);
            else if (mCurCursorIdx == (int)Option.ScoreChange)
                Ctrl_ScoreChangeVal.StartChangeNumViewing(GetChangeValR);
            //else if (mCurCursorIdx == 6)
            //    Ctrl_MinScore.StartChangeNumViewing(GetChangeValR);
            else if (mCurCursorIdx == (int)Option.MaxGunScore)
                Ctrl_MaxScore.StartChangeNumViewing(GetChangeValR);
            else if (mCurCursorIdx == (int)Option.LaguageSelect)
                Ctrl_Language.StartChange(true);
        }
  
        else if (down && key == HpyInputKey.BS_Confirm)
        {
            //音效-后台
            if (GameMain.Singleton.SoundMgr.snd_bkBtn != null)
                GameMain.Singleton.SoundMgr.PlayOneShot(GameMain.Singleton.SoundMgr.snd_bkBtn);

            if (mCurCursorIdx == (int)Option.CoinTicketRatio)//币票比率
            {
                if (mCurrentCoinTicketRotioSelect == null)
                {
                    mCurrentCoinTicketRotioSelect = Ctrl_CoinTicketRatio_Coin;
                    mCurrentCoinTicketRotioSelect.GetComponent<Ef_RendererFlash>().StartFlash();
                    mIsMainMenuMovable = false;
                }
                else
                {
                    mCurrentCoinTicketRotioSelect.GetComponent<Ef_RendererFlash>().StopFlash();
                    mCurrentCoinTicketRotioSelect = null;
                    mIsMainMenuMovable = true;
                }
            }
            else if (mCurCursorIdx == (int)Option.RecoverSetting)//恢复默认值
            {
                Ctrl_GameDifficul.ViewIdx = (int)BackStageSetting.Def_GameDifficult;
                Ctrl_CoinTicketRatio_Coin.NumViewing = BackStageSetting.Def_CoinTicketRatio_Coin;
                Ctrl_CoinTicketRatio_Ticket.NumViewing = BackStageSetting.Def_CoinTicketRatio_Ticket;
                //Ctrl_IsBulletCross.ViewIdx = BackStageSetting.Def_IsBulletCrossWhenScreenNet ? 1 : 0;
                Ctrl_ScoreChangeVal.NumViewing = BackStageSetting.Def_ScoreChangeValue;
                Ctrl_MaxScore.NumViewing = BackStageSetting.Def_ScoreMax;

                //Ctrl_MinScore.NumViewing = BackStageSetting.Def_ScoreMin;
                Ctrl_OutBountyType.ViewIdx = (int)BackStageSetting.Def_OutBountyType;
                Ctrl_GunLayouType.ViewIdx = (int)BackStageSetting.Def_GunLayoutType;
                Ctrl_Language.ViewIdx = (int)BackStageSetting.Def_LanguageUsing;
            }
            else if (mCurCursorIdx == (int)Option.SaveSetting)//保存退出
            {
                //检查 难度 和 币票 比例是否有改变,有的话就需要打码清0 ,后再退出
                bool needCodePrint = false;
                //保存
                BackStageSetting bsSetting = GameMain.Singleton.BSSetting;
                if ((int)mEnterVal_GameDifficult != Ctrl_GameDifficul.ViewIdx)
                {
                    bsSetting.GameDifficult_.Val = (GameDifficult)Ctrl_GameDifficul.ViewIdx;
                    needCodePrint = true;
                }
                if (mEnterVal_CoinTicketRatio_Coin != Ctrl_CoinTicketRatio_Coin.NumViewing)
                { 
                    bsSetting.CoinTicketRatio_Coin.Val = Ctrl_CoinTicketRatio_Coin.NumViewing;
                    needCodePrint = true;
                }
                if (mEnterVal_CoinTicketRatio_Ticket != Ctrl_CoinTicketRatio_Ticket.NumViewing)
                {
                    bsSetting.CoinTicketRatio_Ticket.Val = Ctrl_CoinTicketRatio_Ticket.NumViewing;
                    needCodePrint = true;
                }

                //if ((mEnterVal_IsBulletCross?1:0) != Ctrl_IsBulletCross.ViewIdx)
                //{
                //    bsSetting.IsBulletCrossWhenScreenNet.Val = Ctrl_IsBulletCross.ViewIdx == 1 ? true : false;
                //}

                if (mEnterVal_ScoreChangeValue != Ctrl_ScoreChangeVal.NumViewing)
                {
                    bsSetting.ScoreChangeValue.Val = Ctrl_ScoreChangeVal.NumViewing;
                    bsSetting.ScoreMin.Val = Ctrl_ScoreChangeVal.NumViewing;
                }

                if (mEnterVal_Scoremax != Ctrl_MaxScore.NumViewing)
                {
                    bsSetting.ScoreMax.Val = Ctrl_MaxScore.NumViewing;
                }

                //if (mEnterVal_Scoremin != Ctrl_MinScore.NumViewing)
                //{
                //    bsSetting.ScoreMin.Val = Ctrl_MinScore.NumViewing;
                //}

                if ((int)mEnterVal_OutBountyType != Ctrl_OutBountyType.ViewIdx)
                {
                    bsSetting.OutBountyType_.Val = (OutBountyType)Ctrl_OutBountyType.ViewIdx;
                }

                if((int)mEnterVal_GunLayoutType != Ctrl_GunLayouType.ViewIdx)
                {
                    bsSetting.GunLayoutType_.Val = (GunLayoutType)Ctrl_GunLayouType.ViewIdx;

                }

                if ((int)mEnterVal_Language != Ctrl_Language.ViewIdx)
                {
                    bsSetting.LaguageUsing.Val = (Language)Ctrl_Language.ViewIdx;
                    if (GameMain.EvtLanguageChange != null)
                        GameMain.EvtLanguageChange(bsSetting.LaguageUsing.Val);

                }
                if (needCodePrint)
                {
                    gameObject.SetActiveRecursively(false);
                    GameMain.Singleton.BSSetting.CodePrintCurrentAction.SetImmdiately(false);//归0状态
                    GameMain.Singleton.BSSetting.IsCodePrintClearAllData.SetImmdiately(true);
                    //BackstageMain.Singleton.WndCodePrint.gameObject.SetActiveRecursively(true); 
                    BackstageMain.Singleton.WndCodePrint.Enter(); 
                }
                else
                    BackToMainMenu();
            }
            else if (mCurCursorIdx == (int)Option.Quit)//不保存退出
            {
                //音效-后台
                if (GameMain.Singleton.SoundMgr.snd_bkBtn != null)
                    GameMain.Singleton.SoundMgr.PlayOneShot(GameMain.Singleton.SoundMgr.snd_bkBtn);

                BackToMainMenu();

            }
        }

#endregion

#region keyup         
        if (!down && key == HpyInputKey.BS_Up)
        {
            //StopCoroutine("_Coro_SelectPrev");
        }
        else if (!down && key == HpyInputKey.BS_Down)
        {
            //StopCoroutine("_Coro_SelectNext");
        }
        else if (!down && key == HpyInputKey.BS_Left)
        {
            if (mCurCursorIdx == (int)Option.Difficult)
            {
                Ctrl_GameDifficul.StopChange();
            }
            if (mCurCursorIdx == (int)Option.CoinTicketRatio)
            {
                if (mCurrentCoinTicketRotioSelect != null)
                {
                    mCurrentCoinTicketRotioSelect.StopChangeNumViewing();
                    mCurrentCoinTicketRotioSelect.GetComponent<Ef_RendererFlash>().StartFlash();
                }
            }
            else if (mCurCursorIdx == (int)Option._OutBountyType)
                Ctrl_OutBountyType.StopChange();
            else if (mCurCursorIdx == (int)Option._GunLayoutType)
                Ctrl_GunLayouType.StopChange();
            //else if (mCurCursorIdx == 4)
            //     Ctrl_IsBulletCross.StopChange();
            else if (mCurCursorIdx == (int)Option.ScoreChange)
                Ctrl_ScoreChangeVal.StopChangeNumViewing();
            //else if (mCurCursorIdx == 6)
            //    Ctrl_MinScore.StopChangeNumViewing();
            else if (mCurCursorIdx == (int)Option.MaxGunScore)
                Ctrl_MaxScore.StopChangeNumViewing();
            else if (mCurCursorIdx == (int)Option.LaguageSelect)
                Ctrl_Language.StopChange();

        }
        else if (!down && key == HpyInputKey.BS_Right)
        {
            if (mCurCursorIdx == (int)Option.Difficult)
            {
                Ctrl_GameDifficul.StopChange();
            }
            if (mCurCursorIdx == (int)Option.CoinTicketRatio)
            {
                if (mCurrentCoinTicketRotioSelect != null)
                {
                    mCurrentCoinTicketRotioSelect.StopChangeNumViewing();
                    mCurrentCoinTicketRotioSelect.GetComponent<Ef_RendererFlash>().StartFlash();
                }
                
            }
            else if (mCurCursorIdx == (int)Option._OutBountyType)
                Ctrl_OutBountyType.StopChange();
            else if (mCurCursorIdx == (int)Option._GunLayoutType)
                Ctrl_GunLayouType.StopChange();
            //else if (mCurCursorIdx == 4)
            //    Ctrl_IsBulletCross.StopChange();
            else if (mCurCursorIdx == (int)Option.ScoreChange)
                Ctrl_ScoreChangeVal.StopChangeNumViewing();
            //else if (mCurCursorIdx == 6)
            //    Ctrl_MinScore.StopChangeNumViewing();
            else if (mCurCursorIdx == (int)Option.MaxGunScore)
                Ctrl_MaxScore.StopChangeNumViewing();
            else if (mCurCursorIdx == (int)Option.LaguageSelect)
                Ctrl_Language.StopChange();

        }
#endregion

    }
     
    /// <summary>
    /// 返回主菜单
    /// </summary>
    void BackToMainMenu()
    {
        BackstageMain.Singleton.WndParamSetting.SetActiveRecursively(false);
        BackstageMain.Singleton.WndMainMenu.gameObject.SetActiveRecursively(true);
    }

    IEnumerator _Coro_ChangingCoinTicketNum(int direct)
    {
        if(mCurrentCoinTicketRotioSelect == null)
            yield break;
        mCurrentCoinTicketRotioSelect.GetComponent<Ef_RendererFlash>().StopFlash();
        while (true)
        {
            mCurrentCoinTicketRotioSelect.NumViewing += direct;
            yield return new WaitForSeconds(0.3F);
        }
    }

    /// <summary>
    /// 由原来值获得改变的变化量
    /// </summary>
    /// <param name="oriVal"></param>
    static int GetChangeValR(int oriVal)
    {
        if (oriVal < 100)
            return 1;
        //else if (oriVal >= 50 && oriVal < 100)
        //    return 5;
        else if (oriVal >= 100 && oriVal < 500)
            return 10;
        else if (oriVal >= 500 && oriVal < 1000)
            return 50;
        else if (oriVal >= 1000 && oriVal < 10000)
            return 100;

        return 1;
    }

    static int GetChangeValL(int oriVal)
    {
        if (oriVal <= 100)
            return 1;
        //else if (oriVal > 50 && oriVal <= 100)
        //    return 5;
        else if (oriVal > 100 && oriVal <= 500)
            return 10;
        else if (oriVal > 500 && oriVal <= 1000)
            return 50;
        else if (oriVal > 1000 && oriVal <= 10000)
            return 100;

        return 1;
    }
}
