using UnityEngine;
using System.Collections;

public class BackstageMain : MonoBehaviour {

    public Bs_Cursor Cursor;

    public Bs_Mainmenu WndMainMenu;
    public GameObject WndParamSetting;
    public Bs_1DecodeSetting WndDecodeSetting;
    public Bs_2CheckBill WndCheckBill;
    public Bs_3_1CodePrint WndCodePrint;
    public Bs_3_2CodePrintSelect WndCodePrintSelect;
    public Bs_4Reboot WndReboot;
    public Bs_5SystemSetting WndSystemSetting;

    public Ctrl_InputNum Ctrl_SystemSettingPswInput
    {
        get
        {
            if (mCtrl_SystemSettingPswInput == null)
            {
                mCtrl_SystemSettingPswInput = Instantiate(Prefab_CtrlSystemSettingPswInput) as Ctrl_InputNum;
                mCtrl_SystemSettingPswInput.transform.parent = transform;
                mCtrl_SystemSettingPswInput.transform.position = TsLocal_CtrlSystemSettingPswInput.position;
                mCtrl_SystemSettingPswInput.gameObject.SetActiveRecursively(false);
            }
            return mCtrl_SystemSettingPswInput;
        }
    }
    public Ctrl_InputNum Prefab_CtrlSystemSettingPswInput;
    public Transform TsLocal_CtrlSystemSettingPswInput;
    private Ctrl_InputNum mCtrl_SystemSettingPswInput;
    

    public Ctrl_MsgBox Ctrl_CodePrintConfirm;//报账确认框
    public GameObject MainBG;

    public LanguageItem Unit_Day;//单位:天
    public LanguageItem Unit_Score;//单位:分
    public LanguageItem Unit_Coin;//单位:币
    public LanguageItem Unit_Ticket;//单位:张
    public LanguageItem Unit_Minute;//单位:分钟
    public LanguageItem Unit_Times;//单位:次
    public static BackstageMain Singleton
    {
        get {
            if (mSingleton == null)
            {
    
                mSingleton = Object.FindObjectOfType(typeof(BackstageMain)) as BackstageMain;
            }
            return mSingleton;
        }
    }
    private static BackstageMain mSingleton;

    void Awake()
    {
        gameObject.SetActiveRecursively(false);

        gameObject.active = true;
        MainBG.SetActiveRecursively(true);
        WndMainMenu.gameObject.SetActiveRecursively(true);
        Cursor.gameObject.SetActiveRecursively(true);

        Ctrl_SystemSettingPswInput.Num = Defines.PswLength;
        //创建多屏幕摄像头(并偏移当前摄像头)
        int screenNum = GameMain.Singleton.ScreenNumUsing;
        
        if (screenNum > 1)
        {
            //偏移当前摄像头
            Rect tmpRect = camera.rect;
            tmpRect.width = 1F / screenNum;
            camera.rect = tmpRect;
            for (int i = 0; i != screenNum - 1; ++i )
            {
                GameObject tmpGo = new GameObject("cameraCopy");
                tmpGo.transform.parent = transform;
                tmpGo.transform.localPosition = Vector3.zero;
                Camera c = tmpGo.AddComponent<Camera>();
                c.CopyFrom(camera);
                tmpRect = c.rect;
                tmpRect.x = (float)(i + 1) / screenNum;
                tmpRect.width = 1F / screenNum;
                c.rect = tmpRect;
            }
        }

        if (GameMain.Singleton.SoundMgr.snd_bkBtn != null)
            GameMain.Singleton.SoundMgr.PlayOneShot(GameMain.Singleton.SoundMgr.snd_bkBtn);
    }

    public void UpdateCursor(CursorDimLocation dimLocal)
    {
 
        BackstageMain.Singleton.Cursor.transform.position = dimLocal.transform.position;
        BackstageMain.Singleton.Cursor.SetDimens(dimLocal.Dimension);
    }

    public void EnterPrintCode()
    {
        WndMainMenu.gameObject.SetActiveRecursively(false);
        //WndCodePrint.gameObject.SetActiveRecursively(true);
        WndCodePrint.Enter();
    }
}
