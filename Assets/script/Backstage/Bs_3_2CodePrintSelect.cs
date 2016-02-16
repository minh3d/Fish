using UnityEngine;
using System.Collections;

/// <summary>
/// 打码选择,(直接显示,打码报账)
/// </summary>
public class Bs_3_2CodePrintSelect : MonoBehaviour {
    public CursorDimLocation[] CursorLocals;

    public Renderer Rd_AddTime;
    public tk2dTextMesh Text_AddTimeCountDown;
    private int mCurCursorIdx;

    private bool mIsInputable = true;
 

    void OnEnable()
    {
        

        Rd_AddTime.enabled = false;
        Text_AddTimeCountDown.renderer.enabled = false;

        mIsInputable = true;
        GameMain.EvtInputKey += Handle_InputKey;

        if (CursorLocals != null && CursorLocals.Length != 0)
            BackstageMain.Singleton.UpdateCursor(CursorLocals[mCurCursorIdx]);
    }
    void Start()
    {
        if (CursorLocals != null && CursorLocals.Length != 0)
            BackstageMain.Singleton.UpdateCursor(CursorLocals[mCurCursorIdx]);
    }
    void OnDisable()
    {
        GameMain.EvtInputKey -= Handle_InputKey;
    }

    void SelectNext()
    { 
        mCurCursorIdx = (mCurCursorIdx + 1) % CursorLocals.Length;
        BackstageMain.Singleton.UpdateCursor(CursorLocals[mCurCursorIdx]);

        //音效-后台
        if (GameMain.Singleton.SoundMgr.snd_bkBtn != null)
            GameMain.Singleton.SoundMgr.PlayOneShot(GameMain.Singleton.SoundMgr.snd_bkBtn);
    }

    IEnumerator _Coro_SelectNext()
    {
        while (true)
        {
            SelectNext();
            yield return new WaitForSeconds(Defines.TimeBackGroundJumpSelect);
        }
    }

    void SelectPrev()
    {
        --mCurCursorIdx;
        if (mCurCursorIdx < 0)
            mCurCursorIdx = CursorLocals.Length - 1;
        BackstageMain.Singleton.UpdateCursor(CursorLocals[mCurCursorIdx]);

        //音效-后台
        if (GameMain.Singleton.SoundMgr.snd_bkBtn != null)
            GameMain.Singleton.SoundMgr.PlayOneShot(GameMain.Singleton.SoundMgr.snd_bkBtn); 
    }

    IEnumerator _Coro_SelectPrev()
    {
        while (true)
        {
            SelectPrev();
            yield return new WaitForSeconds(Defines.TimeBackGroundJumpSelect);
        }
    }

    void Handle_InputKey(int control, HpyInputKey key, bool down)
    {
        if (!mIsInputable)
            return;

        if (down && key == HpyInputKey.BS_Up)
        {
            SelectPrev();
            //StartCoroutine("_Coro_SelectPrev");
        }
        else if (!down && key == HpyInputKey.BS_Up)
        {
            //StopCoroutine("_Coro_SelectPrev");
        }
        else if (down && key == HpyInputKey.BS_Down)
        {
            SelectNext();
            //StartCoroutine("_Coro_SelectNext");
        }
        else if (!down && key == HpyInputKey.BS_Down)
        {
            //StopCoroutine("_Coro_SelectNext");
        }
        else if (down && key == HpyInputKey.BS_Confirm)
        {
            if (mCurCursorIdx == 0)
            {
                //直接延时
                GameMain.Singleton.BSSetting.Dat_CodePrintDateTime.Val = System.DateTime.Now.Ticks;
                //效果显示 
                StartCoroutine(_Coro_RuntimeAdvanceAndDelayQuit());
                //禁止按键
                mIsInputable = false;

                //音效-后台
                if (GameMain.Singleton.SoundMgr.snd_bkBtn != null)
                    GameMain.Singleton.SoundMgr.PlayOneShot(GameMain.Singleton.SoundMgr.snd_bkBtn);
            }
            else if (mCurCursorIdx == 1)
            {
                gameObject.SetActiveRecursively(false);
                //BackstageMain.Singleton.WndCodePrint.gameObject.SetActiveRecursively(true);
                BackstageMain.Singleton.WndCodePrint.Enter(); 

                //音效-后台
                if (GameMain.Singleton.SoundMgr.snd_bkBtn != null)
                    GameMain.Singleton.SoundMgr.PlayOneShot(GameMain.Singleton.SoundMgr.snd_bkBtn);
            }
        }
    }

    IEnumerator _Coro_RuntimeAdvanceAndDelayQuit()
    {
        Rd_AddTime.enabled = true;
        
        Text_AddTimeCountDown.renderer.enabled = true;
        
        float useTime = 4F;//4秒
        
        while (useTime > 0)
        {
            Text_AddTimeCountDown.text = ((int)useTime).ToString();
            Text_AddTimeCountDown.Commit();
            useTime -= 1F;
            yield return new WaitForSeconds(1F);
        }

        BackToMainMenu();
    }

    /// <summary>
    /// 返回主菜单
    /// </summary>
    void BackToMainMenu()
    {
        gameObject.SetActiveRecursively(false);
        BackstageMain.Singleton.WndMainMenu.gameObject.SetActiveRecursively(true);
    }
}
