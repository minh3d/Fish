using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System.Security.Cryptography;

public class Tester_NemoUsbHidHardScan : MonoBehaviour
{
    
    public NemoUsbHid_HardScan ArcIO;
    public GUISkin Skin;



    public float TextAreaMinWidth = 50.0f;


    public int SerialPort = 4;
    public int boudarate = 9600;
    public int PlayerIdx = 0;
    private List<string> mDebugInfos = new List<string>();
    private float mScrollVal = 0.0f;
    private int mConsoloViewMaxLine = 20;




    //private bool mRealTimeOutBounty = true;

    //private int mOutCoinNum = 1;
    //private int mOutTicketNum = 1;
    private int mButtomLightIdx = 0;//0-5

    //private int mDataBackLen = 1;//[����]���󷵻����ݳ���
    //private int mDataBackInterval = 10;//[����]���󷵻����ݼ��
    //private bool mDataBackOnOff = false;//[����]���ݵ�ǰ�Ƿ��ڷ�����
  
    private bool[] mLighttingTag;

    private uint mReadWrite_address;
    private byte mReadWrite_dataLen = 4;
    private ulong mReadWrite_data;
    private int mNumToWrite = 1;
    private bool mViewReadWrite = false;
    void Start()
    {
        mLighttingTag = new bool[30];
        if (ArcIO == null)
        {
            ArcIO = GetComponent<NemoUsbHid_HardScan>();
            if (ArcIO == null)
                Debug.LogError("ArcadeSinglePlayerTester��ʼ������."); 
        }

        ArcIO.EvtKey += KeyEvent;
        ArcIO.EvtCtrlBoardStateChanged += Handle_CtrlBoardStateChanged;
        ArcIO.EvtOutCoinReflect += OnOutCoin;
        ArcIO.EvtOutTicketReflect += OnOutTicket;
        ArcIO.EvtInsertCoin += OnInsertCoin;
        ArcIO.EvtHardwareInfo += Hnalde_HardwareInfoRespon;
        ArcIO.EvtResultReadWrite += Handle_ResultReadWrite;
        //ArcIO.EvtLackCoin += OnLackCoin;
        //ArcIO.EvtLackTicket += OnLackTicket;
        ArcIO.Open();
        if (ArcIO.IsOpen())
        {
            DebugLog("������");
        }
        else
            DebugLog("δ����");
  
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
    void Handle_ResultReadWrite(bool isWrite, uint adress, byte datalen, byte resultCode,byte[] data)
    {
        //return;
        if (!mViewReadWrite)
            return;

        DebugLog(string.Format("[��д���󷵻�] ����:{0}  ��ַ:{1:d} ���ݳ���:{2:d} �����:{3:d} ����:{4}"
            ,isWrite ? "д" : "��"
            ,adress
            ,datalen
            ,resultCode
            , data==null?"��":NemoSerial.ByteArrayToString(data)));
        //Debug.Log(string.Format("[��д���󷵻�] ����:{0}  ��ַ:{1:d} ���ݳ���:{2:d} �����:{3:d} ����:{4}"
        //    , isWrite ? "д" : "��"
        //    , adress
        //    , datalen
        //    , resultCode
        //    , data == null ? "��" : NemoSerial.ByteArrayToString(data)));
    }
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
        DebugLog("���ư� :"+ctrlBoardid + (state?"������":"δ����"));
    }

    void Hnalde_HardwareInfoRespon(int gameIdx, int mainVer, int subVer, bool verifySucess)
    {
        DebugLog("��֤��Ϣ���� :" + (verifySucess? "��֤�ɹ�":"��֤ʧ��") 
            +"  ������Ϸ:"+gameIdx+"  ���汾��:"+mainVer+"  ���汾��:"+subVer);
    }
    void OnBackGroundKeyEvent(NemoCtrlIO_Key k, bool downOrUp)
    {
        
        DebugLog("��̨����:" + k + (downOrUp ? " ����" : " �ɿ�"));
    }

    void OnInsertCoin(uint coinnum,int playerIdx)
    {
        DebugLog("Ͷ��,���:" + playerIdx);
    }
    void OnOutCoin( int playerIdx)
    {
        DebugLog("���ҷ���,���:" + playerIdx );
    }

    void OnOutTicket( int playerIdx)
    {
        DebugLog("��Ʊ����,���:" + playerIdx);
    }

    void OnLackCoin(int playerIdx)
    {
        DebugLog("ȱ��,���:" + playerIdx );
    }

    void OnLackTicket(int playerIdx)
    {
        DebugLog("ȱƱ,���:" + playerIdx);
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
                    GUILayout.BeginHorizontal("box");
                    {
                        GUILayout.Label("��̨��(0-5):");
                        if (GUILayout_IntArea(ref PlayerIdx))//player�ı�
                        {

                        }
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("����"))
                    {
                        ArcIO.OutCoin(PlayerIdx);
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();

                    if (GUILayout.Button("��Ʊ"))
                    {
                        ArcIO.OutTicket(PlayerIdx);
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();

                    if (GUILayout.Button("������ư���Ϣ"))
                    {

                        ArcIO.RequestHardwareInfo();
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("����" + (mLighttingTag[PlayerIdx] ? "(��ing)" : "(��ing_")))
                        {
                            //if (mLighttingTag[PlayerIdx] != null)
                                mLighttingTag[PlayerIdx] = !mLighttingTag[PlayerIdx];

                            ArcIO.FlashLight(PlayerIdx, mButtomLightIdx, mLighttingTag[PlayerIdx]);

                        }
                        GUILayout.Label("��ID(0-1):");
                        GUILayout_IntArea(ref mButtomLightIdx);
                    }

                    GUILayout.EndHorizontal();

                    //��д����
                    GUILayout.BeginVertical("box");
                    {
                        int tagReadWrite = -1;
                        GUILayout.BeginHorizontal();
                        {
                            
                            if (GUILayout.Button("��ȡ"))
                                tagReadWrite = 0;
                            if (GUILayout.Button("д��"))
                                tagReadWrite = 1;
                            GUILayout.Label("��ַ:");
                            GUILayout_UIntArea(ref mReadWrite_address);
                            GUILayout.Label("����:");
                            GUILayout_ByteArea(ref mReadWrite_dataLen);
                            //GUILayout.Label("��ʾ:");
                            mViewReadWrite = GUILayout.Toggle(mViewReadWrite,"��ʾ");

                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label("����д�����:");
                            GUILayout_IntArea(ref mNumToWrite);
                            GUILayout.Label("����:");
                            GUILayout_ULongArea(ref mReadWrite_data);
                        }
                        GUILayout.EndHorizontal();

                        if (tagReadWrite != -1)
                        {
                            bool IsWrite = tagReadWrite == 1 ? true : false;


                            byte[] dataToWrite = null;
                            if (IsWrite)
                            {
                                dataToWrite = new byte[mReadWrite_dataLen];
                                byte[] sourceData = System.BitConverter.GetBytes(mReadWrite_data);
                                int copyLen = sourceData.Length > mReadWrite_dataLen ? mReadWrite_dataLen : sourceData.Length;
                                System.Array.Copy(sourceData, 0, dataToWrite, 0, copyLen);

                                for (int i = 0; i != mNumToWrite; ++i)
                                {
                                    ArcIO.RequestReadWrite(IsWrite, mReadWrite_address, mReadWrite_dataLen, dataToWrite);
                                }
                            }
                            else
                            {
                                ArcIO.RequestReadWrite(IsWrite, mReadWrite_address, mReadWrite_dataLen, dataToWrite);
                            }

                            
                        }
                    }
                    GUILayout.EndVertical();
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

    public void DebugLog(string str)
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

    private bool GUILayout_UIntArea(ref uint val)
    {
        uint oldVal = val;
        string valStr = GUILayout.TextArea(val.ToString(), GUILayout.MinWidth(TextAreaMinWidth));
        uint newVal = uint.Parse(valStr);
        if (newVal != oldVal)
        {
            val = newVal;
            return true;
        }
        return false;

    }

    private bool GUILayout_ByteArea(ref byte val)
    {
        byte oldVal = val;
        string valStr = GUILayout.TextArea(val.ToString(), GUILayout.MinWidth(TextAreaMinWidth));
        byte newVal = byte.Parse(valStr);
        if (newVal != oldVal)
        {
            val = newVal;
            return true;
        }
        return false;
    }

    private bool GUILayout_ULongArea(ref ulong val)
    {
        ulong oldVal = val;
        string valStr = GUILayout.TextArea(val.ToString(), GUILayout.MinWidth(TextAreaMinWidth));
        ulong newVal = ulong.Parse(valStr);
        if (newVal != oldVal)
        {
            val = newVal;
            return true;
        }
        return false;
    }

}

