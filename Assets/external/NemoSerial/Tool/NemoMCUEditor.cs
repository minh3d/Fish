using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System.Security.Cryptography;

public class NemoMCUEditor : MonoBehaviour
{

    public NemoUsbHid_HardScan ArcIO;
    public GUISkin Skin;



    public float TextAreaMinWidth = 50.0f;


    public int SerialPort = 4;
    public int boudarate = 9600;
    
    private List<string> mDebugInfos = new List<string>();
    private float mScrollVal = 0.0f;
    private int mConsoloViewMaxLine = 20;




    //private bool mRealTimeOutBounty = true;

    //private int mOutCoinNum = 1;
    //private int mOutTicketNum = 1;
    //private int mButtomLightIdx = 0;//0-5

    //private int mDataBackLen = 1;//[����]���󷵻����ݳ���
    //private int mDataBackInterval = 10;//[����]���󷵻����ݼ��
    //private bool mDataBackOnOff = false;//[����]���ݵ�ǰ�Ƿ��ڷ�����

    //private bool[] mLighttingTag;

    private int mGameIdx = 0;
    private int mMainVersion = 0;
    private int mSubVersion = 0;
    void Start()
    {
        //mLighttingTag = new bool[30];
        if (ArcIO == null)
        {
            ArcIO = GetComponent<NemoUsbHid_HardScan>();
            ArcIO.Open();
            if (ArcIO == null)
                Debug.LogError("ArcadeSinglePlayerTester��ʼ������.");
        }

        //ArcIO.EvtKey += KeyEvent;
        //ArcIO.EvtCtrlBoardStateChanged += Handle_CtrlBoardStateChanged;
        //ArcIO.EvtOutCoinReflect += OnOutCoin;
        //ArcIO.EvtOutTicketReflect += OnOutTicket;
        //ArcIO.EvtInsertCoin += OnInsertCoin;
        ArcIO.EvtHardwareInfo += Hnalde_HardwareInfoRespon;
        ArcIO.EvtResultEditMCU += Handle_ResultEditMCU;
        
        //ArcIO.EvtLackCoin += OnLackCoin;
        //ArcIO.EvtLackTicket += OnLackTicket;
        DebugLog("������Ϸ˵��:  0:	�������� 1:�������� 2:�������� 3:ҡǮ�� 4:�������� 5:������ 6:������� 7:һ���� 8:һ����2 9:�����ֺ�");

        if (ArcIO.IsOpen())
        {
            ArcIO.RequestHardwareInfo();//����汾��Ϣ    
            DebugLog("������");
        }
        else
            DebugLog("δ����");
 
        //HMACMD5 cryptor = new HMACMD5(System.Text.Encoding.ASCII.GetBytes("FX20120927YIDINGYAOCHANGAEF51FM2"));

        //byte[] challengeAnswer = cryptor.ComputeHash(System.BitConverter.GetBytes(123));
        //byte[] testAry = new byte[]{
        //    0,0,0,0,
        //    0,0,0,0,
        //    0,0,0,0,
        //    0,0,0,7B
        //};
        //byte[] challengeAnswer = cryptor.ComputeHash(System.Text.Encoding.ASCII.GetBytes("123456789A123456"));
        
        //Debug.Log(NemoUsbHid_HardScan.ByteArrayToString(challengeAnswer));
    }

    //IEnumerator _Coro_DebugLog()
    //{
    //    int i = 0;
    //    while (true)
    //    {
    //        DebugLog("TestLog" + (i++));
    //        yield return new WaitForSeconds(0.1F);
    //    }
    //}
    void KeyEvent2(int ctrlIdx, NemoCtrlIO_Key k, bool downOrUp)
    {

    }
    void KeyEvent(int playerIdx, NemoCtrlIO_Key k, bool downOrUp)
    {
        string keyStr = "";
        switch (k)
        {
            case NemoCtrlIO_Key.A:
                keyStr = "Ѻ��";
                break;
            case NemoCtrlIO_Key.B:
                keyStr = "����";
                break;
            case NemoCtrlIO_Key.C:
                keyStr = "�·�";
                break;
            case NemoCtrlIO_Key.D:
                keyStr = "�Ϸ�";
                break;
            case NemoCtrlIO_Key.F:
                keyStr = "����";
                break;
            default:
                keyStr = k.ToString();
                break;

        }
        DebugLog("����:" + keyStr + (downOrUp ? " ����" : " �ɿ�") + "  �����: " + playerIdx + " ����.");
    }

    void Handle_CtrlBoardStateChanged(int ctrlBoardid, bool state)
    {
        DebugLog("���ư� :" + ctrlBoardid + (state ? "������" : "δ����"));
    }

    void Hnalde_HardwareInfoRespon(int gameIdx, int mainVer, int subVer, bool verifySucess)
    {
        DebugLog("��֤��Ϣ���� :" + (verifySucess ? "��֤�ɹ�" : "��֤ʧ��")
            + "  ������Ϸ:" + gameIdx + "  ���汾��:" + mainVer + "  ���汾��:" + subVer);
    }
    void OnBackGroundKeyEvent(NemoCtrlIO_Key k, bool downOrUp)
    {

        DebugLog("��̨����:" + k + (downOrUp ? " ����" : " �ɿ�"));
    }

    void OnInsertCoin(uint coinnum, int playerIdx)
    {
        DebugLog("Ͷ��,���:" + playerIdx);
    }
    void OnOutCoin(int playerIdx)
    {
        DebugLog("���ҷ���,���:" + playerIdx);
    }

    void OnOutTicket(int playerIdx)
    {
        DebugLog("��Ʊ����,���:" + playerIdx);
    }

    void OnLackCoin(int playerIdx)
    {
        DebugLog("ȱ��,���:" + playerIdx);
    }

    void OnLackTicket(int playerIdx)
    {
        DebugLog("ȱƱ,���:" + playerIdx);
    }

    void Handle_ResultEditMCU(bool result)
    {
        DebugLog("�޸ļ��ܰ�:" + (result ? "�ɹ�" : "ʧ��"));
    }

    void OnGUI()
    {

        if (Skin != null)
            GUI.skin = Skin;


        GUILayout.BeginHorizontal();
        {
            GUILayout.BeginVertical();//��߿���̨
            {
                GUILayout.BeginVertical("box");
                {

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("������Ϸ:");
                        //GUILayout.
                        GUILayout_IntArea(ref mGameIdx);//player�ı� 
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("���汾��:");
                        //GUILayout.
                        GUILayout_IntArea(ref mMainVersion); 
                    }
                    GUILayout.EndHorizontal();


                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("���汾��:");
                        //GUILayout.
                        GUILayout_IntArea(ref mSubVersion);
                    }
                    GUILayout.EndHorizontal();


                    if (GUILayout.Button("����"))
                    {

                        ArcIO.EditMCUInfo(mGameIdx,mMainVersion,mSubVersion);
                    } 

                    if (GUILayout.Button("������ư���Ϣ"))
                    {

                        ArcIO.RequestHardwareInfo();
                    } 
                     
                }

                GUILayout.EndVertical();

                GUILayout.Space(20.0f);



                if (GUILayout.Button("���LOG"))
                {
                    mDebugInfos.Clear();
                    mScrollVal = 0F;
                }

            }
            GUILayout.EndVertical();//��߿���̨����
        }

        //GUILayout.TextArea(""

        //        , new GUILayoutOption[] { GUILayout.MinWidth(750), GUILayout.MinHeight(390) });
        {
            string viewStr = "";
            int i = 0;
            int j = 0;
            foreach (string info in mDebugInfos)
            {
                if (i >= (int)mScrollVal)
                {
                    viewStr = viewStr + info + "\n";
                    ++j;
                    if (j >= mConsoloViewMaxLine)
                    {
                        break;
                    }
                }
                ++i;
            }
            GUILayout.TextArea(viewStr
                , new GUILayoutOption[] { GUILayout.MinWidth(500), GUILayout.MinHeight(330) });


            if (mDebugInfos.Count > mConsoloViewMaxLine)
            {
                mScrollVal = GUILayout.VerticalScrollbar(mScrollVal, 1f, 0F, (1 + mDebugInfos.Count - mConsoloViewMaxLine)
                , GUILayout.MinHeight(330));
            }
        }

        GUILayout.EndHorizontal();

        //GUILayout.BeginHorizontal();//�ұ�combobox����ʾ

        //GUILayout.EndHorizontal();

        //GUILayout.TextField(mDebugInfos.Count.ToString());
        //GUILayout.TextField(mScrollVal.ToString());
        //GUILayout.EndHorizontal();
    }

    void DebugLog(string str)
    {

        mDebugInfos.Add(str);
        mScrollVal = mDebugInfos.Count - mConsoloViewMaxLine;
    }



    private bool GUILayout_IntArea(ref int val)
    {
        int oldVal = val;
        string valStr = GUILayout.TextArea(val.ToString(), GUILayout.MinWidth(TextAreaMinWidth));
        int newVal = int.Parse(valStr);
        if (newVal != oldVal)
        {
            val = newVal;
            return true;
        }
        return false;

    }
}

