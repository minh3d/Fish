using UnityEngine;
using System.Collections;
using System.Security.Cryptography;
using System.IO;

public class Bs_5SystemSetting : MonoBehaviour {
    enum UIState
    {
        Admin,//管理者界面
        Arena//场地界面
    }
    public Ctrl_InputNum Prefab_NumInputer;
    public Transform TsLocal_NumInputer;

    
    public tk2dTextMesh Text_Hint;//提示信息


    public Wnd_OptionMoveAble Prefab_WndOptionMoverAdmin;
    public Wnd_OptionMoveAble Prefab_WndOptionMoverArena;


    public LanguageItem LI_SetupLineIdSuccess;//本地化:设置线号成功
    public LanguageItem LI_Enter3DigitLineID;//本地化:请输入3位数线号

    public LanguageItem LI_SetupPswSucess;//本地化:设置密码成功

    public LanguageItem LI_EnteredPswNotSame;//本地化:两次输入密码不一致
    public LanguageItem LI_EnterNDigitPsw;//本地化:请输入n位密码,(带c#string.format{0:d})

    public LanguageItem LI_PlzEnterNewLineID;//本地化:请输入新的线号
    public LanguageItem LI_PlzEnterNewTableID;//本地化:请输入新的台号
    public LanguageItem LI_PlzEnterNewPsw;//本地化:请输入新的密码
    public LanguageItem LI_EnterPswAgain;//本地化:请再次输入密码

    public LanguageItem LI_PlzEnterFormulaCode;//本地化:输入公式码
    public LanguageItem LI_SetupFormulaCodeSucess;//本地化:公式码设置成功
    public LanguageItem LI_FormulaDigitNumWorng;//本地化:请输入8位公式码




    private Ctrl_InputNum mCurNumInputer;//当前的数字输入框
    private Wnd_OptionMoveAble mCurWndOptioner;
    //private Wnd_OptionMoveAble mWndOptionMoverArena;
    private static HMACMD5 mHmacMd5;

    private static UIState mUIState;
    private int mCurSelectIdx = 0;
    public static HMACMD5 Cryptor
    {
        get
        {
            if (mHmacMd5 == null)
            {
                mHmacMd5 = new HMACMD5(System.Text.Encoding.ASCII.GetBytes("yidingyaochang"));
            }
            return mHmacMd5;
        }
    }
    
    

    string ReadGamePswMD5(string pswName)
    {
        string filename = Directory.GetCurrentDirectory() + "/DataFiles/"+pswName;
        
        if (!File.Exists(filename))
            return "";

        string output;
        using (FileStream fs = new FileStream(filename, FileMode.OpenOrCreate))
        {
            using(BinaryReader br = new BinaryReader(fs))
                output = br.ReadString();
        }
        
        return output;
    }

    void WritePswMD5(string pswName, string psw)
    {
        string filename = Directory.GetCurrentDirectory() + "/DataFiles/" + pswName;
        using (FileStream fs = new FileStream(filename, FileMode.Create))
        {
            using (BinaryWriter br = new BinaryWriter(fs))
                br.Write(psw);
        }
    }


    

    public bool TryEnter(int[] psw)
    {
        string pswStr = Ctrl_InputNum.DigitToInt(psw).ToString();
        string pswInputCryptedStr = 
            System.Text.Encoding.ASCII.GetString(Cryptor.ComputeHash(System.Text.Encoding.ASCII.GetBytes(pswStr)));
        string pswAdminSavedStr = ReadGamePswMD5("adminPsw");
        
        //arena模式
        string pswArenaSavedStr = ReadGamePswMD5("arenaPsw");
        if (pswArenaSavedStr == "" && pswStr == "822228")//硬盘没存密码,
        {
            Enter(UIState.Arena);
            return true;
        }
        if (pswArenaSavedStr == pswInputCryptedStr) 
        {
            Enter(UIState.Arena);
            return true;
        }

        //进入admin模式
        if (pswAdminSavedStr == "" && pswStr == "911119")//硬盘没存密码 
        {
            Enter(UIState.Admin);
            return true;
        }

        if (pswAdminSavedStr == pswInputCryptedStr)
        {
            Enter(UIState.Admin);
            return true;
        }

        return false;
    }

    void Enter(UIState state)
    {
        


        if (mCurWndOptioner != null)
        {
            Debug.LogWarning("连续调用两次Enter?");
            Destroy(mCurWndOptioner.gameObject);
            mCurWndOptioner = null;
        }

        gameObject.SetActiveRecursively(true);
        mUIState = state;

        switch (state)
        {
            case UIState.Admin:
                {
                    mCurWndOptioner = Instantiate(Prefab_WndOptionMoverAdmin, transform.position, Quaternion.identity) as Wnd_OptionMoveAble;

                    Transform ts = mCurWndOptioner.transform.FindChild("option4/ctrl_当前显示状态");
                    Ctrl_OptionText ctrlOption = null;
                    if(ts!=null)
                        ctrlOption = ts.GetComponent<Ctrl_OptionText>();
                    if (ctrlOption != null)
                    {
                        ctrlOption.ViewIdx = GameMain.Singleton.BSSetting.Dat_GameShowLanguageSetup.Val ? 0 : 1;
                        ctrlOption.UpdateText();
                    }

                }
                break;
            case UIState.Arena:
                mCurWndOptioner = Instantiate(Prefab_WndOptionMoverArena, transform.position, Quaternion.identity) as Wnd_OptionMoveAble;
                break;
        }
        mCurWndOptioner.transform.parent = transform;
        mCurWndOptioner.transform.localPosition = Vector3.zero;
 
        mCurWndOptioner.EvtSelectChanged += Handle_OptionSelectorChanged;
        mCurWndOptioner.EvtConfirm += Handle_OptionSelectorConfirm;

        GameMain.EvtInputKey += Handle_Input;
 
        UpdateView();
        
    }

    void Exit()
    {
        if (GameMain.IsEditorShutdown)
            return;

        gameObject.SetActiveRecursively(false);
        if (mCurWndOptioner != null)
        {
            Destroy(mCurWndOptioner.gameObject);
            mCurWndOptioner = null;
        }
        

        if (mCurWndOptioner != null)
        { 
            Destroy(mCurWndOptioner.gameObject);
            mCurWndOptioner = null;
        }
         
        GameMain.EvtInputKey -= Handle_Input;
    }

    /// <summary>
    /// 更新显示
    /// </summary>
    void UpdateView()
    {
        if(mCurWndOptioner == null)
            return;

        Ui_lineIdTableIdLink idLink = mCurWndOptioner.GetComponent<Ui_lineIdTableIdLink>();
        if (idLink == null)
            return;

        BackStageSetting bss = GameMain.Singleton.BSSetting;
        switch (mUIState)
        {
            case UIState.Admin:
                idLink.Text_LineID.text = string.Format("{0:d"+BackStageSetting.Digit_IdLine.ToString()+"}",bss.Dat_IdLine.Val);
                idLink.Text_LineID.Commit();

                idLink.Text_TableID.text = string.Format("{0:d" + BackStageSetting.Digit_IdTable.ToString() + "}", bss.Dat_IdTable.Val);
                idLink.Text_TableID.Commit();
                break;
            case UIState.Arena:
                idLink.Text_TableID.text = string.Format("{0:d" + BackStageSetting.Digit_IdTable.ToString() + "}", bss.Dat_IdTable.Val);
                idLink.Text_TableID.Commit();

                if (bss.Dat_FormulaCode.Val == 0xffffffff)
                    idLink.Text_FormulaCode.text = "-";
                else
                    idLink.Text_FormulaCode.text = string.Format("{0:d" + BackStageSetting.Digit_FormulaCode.ToString() + "}", bss.Dat_FormulaCode.Val);
                idLink.Text_FormulaCode.Commit();
                break;
        }
    }
    void Handle_Input(int control, HpyInputKey key, bool down)
    {
        if (key == HpyInputKey.BS_Cancel && down)
        {
            if (mCurNumInputer != null)
            {
                Destroy(mCurNumInputer.gameObject);
                mCurNumInputer = null;

                mCurWndOptioner.IsControlable = true;
            }
        }
        else if ((key == HpyInputKey.BS_Left||key == HpyInputKey.BS_Right) && down)
        {
            if (mCurSelectIdx == 4)
            {
                Transform ts = mCurWndOptioner.transform.FindChild("option4/ctrl_当前显示状态");
                Ctrl_OptionText ctrlOption = null;
                if(ts!=null)
                    ctrlOption = ts.GetComponent<Ctrl_OptionText>();
                if (ctrlOption != null)
                {
                    PersistentData<bool,bool> isShowLanguageSetup = GameMain.Singleton.BSSetting.Dat_GameShowLanguageSetup;
                    isShowLanguageSetup.Val = !isShowLanguageSetup.Val;
                    ctrlOption.ViewIdx = isShowLanguageSetup.Val ? 0 : 1;
                    ctrlOption.UpdateText();

                }
                else
                {
                    Debug.LogError("ctrlOption == null");
                }
            }
        }
        
    }
 

    void Handle_OptionSelectorConfirm(int selectIdx)
    {
        switch (mUIState)
        {
            case UIState.Admin:
                if (selectIdx == 0)//线号设置
                {
                    SetupLineIDOrTableID(true, BackStageSetting.Digit_IdLine);
                }
                else if (selectIdx == 1)
                {
                    SetupLineIDOrTableID(false, BackStageSetting.Digit_IdTable);
                }
                else if (selectIdx == 2)//管理员密码设置
                {
                    SetupPsw("adminPsw");
                }
                else if (selectIdx == 3)//场地密码设置
                {
                    SetupPsw("arenaPsw");
                }
                else if (selectIdx == 5)//返回主菜单
                {
                    Exit();
                    BackstageMain.Singleton.WndMainMenu.gameObject.SetActiveRecursively(true);
                }
                break;
            case UIState.Arena:
                if (selectIdx == 0)//台号设置
                {
                    SetupLineIDOrTableID(false, BackStageSetting.Digit_IdTable);
                }
                else if (selectIdx == 1)//设置公式码
                {
                    SetupFormulaCode();
                }
                else if (selectIdx == 2)//场地密码设置
                {
                    SetupPsw("arenaPsw");
                }

                else if (selectIdx == 3)//返回主菜单
                {
                    Exit();
                    BackstageMain.Singleton.WndMainMenu.gameObject.SetActiveRecursively(true);
                }
                break;
        }
    }

    void SetupFormulaCode()
    {
        mCurWndOptioner.IsControlable = false;
        //创建一个输入框
        mCurNumInputer = Instantiate(Prefab_NumInputer) as Ctrl_InputNum;
        mCurNumInputer.Num = BackStageSetting.Digit_FormulaCode;
        mCurNumInputer.Text_Tile.text = LI_PlzEnterFormulaCode.CurrentText;
        mCurNumInputer.Text_Tile.Commit();
        mCurNumInputer.transform.parent = transform;
        mCurNumInputer.transform.localPosition = TsLocal_NumInputer.localPosition;


        mCurNumInputer.EvtConfirm = (int[] digits) =>
        {

            if (digits.Length == BackStageSetting.Digit_FormulaCode)
            {
                uint formulaCodeNew = (uint)Ctrl_InputNum.DigitToInt(digits);
				if (formulaCodeNew == 0)
					formulaCodeNew = 0xffffffff;
                GameMain.Singleton.BSSetting.Dat_FormulaCode.SetImmdiately(formulaCodeNew);
                Destroy(mCurNumInputer.gameObject);
                mCurNumInputer = null;
                mCurWndOptioner.IsControlable = true;
                ViewHint(LI_SetupFormulaCodeSucess.CurrentText);

                UpdateView();
               
            }
            else
                ViewHint(LI_FormulaDigitNumWorng.CurrentText);

        };
    }

    /// <summary>
    /// 设置线号或者台号
    /// </summary>
    /// <param name="lineIdOrTableID">true:线号,false:台号</param>
    /// <param name="numLen"></param>
    void SetupLineIDOrTableID(bool lineIdOrTableID,int numLen)
    {
        mCurWndOptioner.IsControlable = false;
        //创建一个输入框
        mCurNumInputer = Instantiate(Prefab_NumInputer) as Ctrl_InputNum;
        mCurNumInputer.Num = numLen;
        mCurNumInputer.Text_Tile.text = lineIdOrTableID ? LI_PlzEnterNewLineID.CurrentText: LI_PlzEnterNewTableID.CurrentText;
        mCurNumInputer.Text_Tile.Commit();
        mCurNumInputer.transform.parent = transform;
        mCurNumInputer.transform.localPosition = TsLocal_NumInputer.localPosition;


        mCurNumInputer.EvtConfirm = (int[] digits) =>
        {

            if (digits.Length == numLen)
            {
                if(lineIdOrTableID)
                    GameMain.Singleton.BSSetting.Dat_IdLine.SetImmdiately((int)Ctrl_InputNum.DigitToInt(digits));
                else
                    GameMain.Singleton.BSSetting.Dat_IdTable.SetImmdiately((int)Ctrl_InputNum.DigitToInt(digits));
                Destroy(mCurNumInputer.gameObject);
                mCurNumInputer = null;
                mCurWndOptioner.IsControlable = true;
                ViewHint(LI_SetupLineIdSuccess.CurrentText);
                UpdateView();
            }
            else
                ViewHint(LI_Enter3DigitLineID.CurrentText);

        };

    }


    void SetupPsw(string pswName)
    {
         mCurWndOptioner.IsControlable = false;
            //创建一个输入框
            mCurNumInputer = Instantiate(Prefab_NumInputer) as Ctrl_InputNum;
            mCurNumInputer.Num = Defines.PswLength;
            mCurNumInputer.Text_Tile.text = LI_PlzEnterNewPsw.CurrentText;
            mCurNumInputer.Text_Tile.Commit();
            mCurNumInputer.transform.parent = transform;
            mCurNumInputer.transform.localPosition = TsLocal_NumInputer.localPosition;

            ulong pswInputFirst = 0;
            int pswInputState = 1;//1第一次输入,2第二次输入
            mCurNumInputer.EvtConfirm = (int[] digits) =>
            {
                if (digits.Length == Defines.PswLength)
                {
                    if (pswInputState == 1)
                    {
                        pswInputFirst = Ctrl_InputNum.DigitToInt(digits);
                        mCurNumInputer.ResetDigits();
                        mCurNumInputer.Text_Tile.text = LI_EnterPswAgain.CurrentText;
                        mCurNumInputer.Text_Tile.Commit();
                        pswInputState = 2;
                    }
                    else if (pswInputState == 2)
                    {
                        ulong pswInputSecond = Ctrl_InputNum.DigitToInt(digits);//第二次输入密码
                        if (pswInputFirst == pswInputSecond)
                        {

                            byte[] md5Psw = Cryptor.ComputeHash(System.Text.Encoding.ASCII.GetBytes(pswInputSecond.ToString()));
                            string md5PswStr = System.Text.Encoding.ASCII.GetString(md5Psw);
                            WritePswMD5(pswName, md5PswStr);

                            Destroy(mCurNumInputer.gameObject);
                            mCurNumInputer = null;
                            mCurWndOptioner.IsControlable = true;

                            ViewHint(LI_SetupPswSucess.CurrentText);
                        }
                        else//两次输入密码不一致
                        {
                            mCurNumInputer.ResetDigits();
                            mCurNumInputer.Text_Tile.text = LI_PlzEnterNewPsw.CurrentText;
                            mCurNumInputer.Text_Tile.Commit();
                            pswInputState = 1;
                            ViewHint(LI_EnteredPswNotSame.CurrentText);
                        }
                    }

                }
                else
                    ViewHint(string.Format(LI_EnterNDigitPsw.CurrentText,Defines.PswLength));
            };
    }
    void ViewHint(string erroStr)
    {
        StopCoroutine("_Coro_ViewHint");
        StartCoroutine("_Coro_ViewHint",erroStr);
    }
    IEnumerator _Coro_ViewHint(string ErroStr)
    {
        //Text_Hint.renderer.enabled = false;
        Text_Hint.renderer.enabled = true;
        Text_Hint.text = ErroStr;
        Text_Hint.Commit();
        yield return new WaitForSeconds(3F);

        Text_Hint.renderer.enabled = false;
    }

    void Handle_OptionSelectorChanged(int i)
    {
        mCurSelectIdx = i;
    }
     
}
