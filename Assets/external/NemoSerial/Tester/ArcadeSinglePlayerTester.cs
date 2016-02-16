using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ArcadeSinglePlayerTester : MonoBehaviour
{
    
    public ArcadeIO_Feng ArcIO;
    public GUISkin Skin;


    public float TextAreaMinWidth = 50.0f;


    public int SerialPort = 4;
    public int boudarate = 9600;
    public int PlayerIdx = 0;
    private List<string> mDebugInfos = new List<string>();
    private float mScrollVal = 0.0f;
    private int mConsoloViewMaxLine = 20;
    void Start()
    {

        if(ArcIO == null)
        {
            Debug.LogError("ArcadeSinglePlayerTester��ʼ������.");
            return;
        }

        ArcIO.HandlerKey += KeyEvent;
        ArcIO.HandlerOutCoinReflect += OnOutCoin;
        ArcIO.HandlerOutTicketReflect += OnOutTicket;

        ArcIO.HandlerLackCoin += OnLackCoin;
        ArcIO.HandlerLackTicket += OnLackTicket;

        //ArcIO.HandlerVersionRespon += OnVersionResponEvent;
        //ArcIO.HandlerVerifyResult += OnVerifyResultEvent;
        //ArcIO.HandlerTextVerifyResult += OnVerifyTextResultEvent;
        ArcIO.HandlerBackGroundKeyEvent += OnBackGroundKeyEvent;
        
        DebugLog("�Զ��򿪴���.");
        //StartCoroutine(_Coro_DebugLog());
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

    void KeyEvent(int playerIdx, ArcadeIO_Feng.Key k, bool downOrUp)
    {
        DebugLog("����:" + k + (downOrUp ? " ����" : " �ɿ�") + "  �����: " + playerIdx + " ����.");
    }
    void OnBackGroundKeyEvent(ArcadeIO_Feng.Key k, bool downOrUp)
    {
        DebugLog("��̨����:" + k + (downOrUp ? " ����" : " �ɿ�") );
    }

    void OnOutCoin(uint count, int playerIdx)
    {
        DebugLog("���ҷ���,���:" + playerIdx + " ��:" + count);
    }

    void OnOutTicket(uint count, int playerIdx)
    {
        DebugLog("��Ʊ����,���:" + playerIdx + " ��:" + count);
    }

    void OnLackCoin(uint count, int playerIdx)
    {
        DebugLog("ȱ��,���:" + playerIdx + " ����:" + count);
    }

    void OnLackTicket(uint count, int playerIdx)
    {
        DebugLog("ȱƱ,���:" + playerIdx + " ����:" + count);
    }

    void OnVersionResponEvent(uint main, uint sub)
    {
        //mHardwareVersion = main.ToString() + "." + sub.ToString();
        DebugLog("Ӳ����Ӧ�汾����:" + main + "." + sub);
    }

    void OnVerifyResultEvent(bool sucess)
    {
        DebugLog("������֤���:" + sucess);
    }

    void OnVerifyTextResultEvent(byte[] data)
    {
       
        string viewstr = "";
        foreach (byte d in data)
        {
            viewstr += string.Format("{0:x}", (int)(d));
        }
        DebugLog("OnVerifyTextResultEvent:" + viewstr);
    }
    void OnGUI()
    {

        if (Skin != null)
            GUI.skin = Skin;


        GUILayout.BeginHorizontal();


        GUILayout.BeginVertical();//��߿���̨
        {
            GUILayout.BeginHorizontal("box");
            GUILayout.Label("����(1-n):");

            GUILayout_IntArea(ref SerialPort);
            GUILayout_IntArea(ref boudarate);
            if (GUILayout.Button("��"))
            {
                NemoSerial ns = GetComponent<NemoSerial>();
                if (ns != null)
                {
                    ns.PortName = "COM" + SerialPort;
                    ns.BaudRate = boudarate;
                }
                else
                    DebugLog("�Ҳ���NemoSerial���,�򿪴���ʧ��.");
                
                //int
            
                if (ArcIO.Open())
                    DebugLog("�򿪴��ڳɹ�");
                else
                    DebugLog("�򿪴���ʧ��");
            

            }


            if (GUILayout.Button("�ر�"))
            {
                ArcIO.Close();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(20.0f);

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
                GUILayout_IntArea(ref mOutCoinNum);
                if (GUILayout.Button("����"))
                {
                    ArcIO.OutCoin((uint)mOutCoinNum, PlayerIdx);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout_IntArea(ref mOutTicketNum);
                if (GUILayout.Button("��Ʊ"))
                {
                    ArcIO.OutTicket((uint)mOutTicketNum, PlayerIdx);
                }
                GUILayout.EndHorizontal();

                bool oldRTOBToggle = mRealTimeOutBounty;
                mRealTimeOutBounty = GUILayout.Toggle(mRealTimeOutBounty, "ʵʱ�˱�(Ʊ)");
                if (oldRTOBToggle != mRealTimeOutBounty)
                {
                    ArcIO.SetRealTimeOutBounty(mRealTimeOutBounty);
                }


                if (GUILayout.Button("����"))
                {
                    ArcIO.ResponCoin(PlayerIdx);
                }

                if (GUILayout.Button("��Ʊ"))
                {
                    ArcIO.ResponTicket(PlayerIdx);
                }

                GUILayout.BeginHorizontal();
                {
                     if (GUILayout.Button("����"))
                    {
                        ArcIO.FlashButtomLight(mButtomLightIdx);
                    }
                    GUILayout.Label("���ID(0-5):");
                    GUILayout_IntArea(ref mButtomLightIdx);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    if (!mDataBackOnOff)
                    {
                        if (GUILayout.Button("���ش�������[��ʼ]"))
                        {
                            ArcIO.RequestDataBack((byte)mDataBackLen,(byte)mDataBackInterval,true);
                            mDataBackOnOff = true;
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("���ش�������[ֹͣ]"))
                        {
                            ArcIO.RequestDataBack((byte)mDataBackLen, (byte)mDataBackInterval, false);
                            mDataBackOnOff = false;
                        }
                    }
                   
                    GUILayout.Label("����(byte)");
                    GUILayout_IntArea(ref mDataBackLen);
                    GUILayout.Label("���(ms)");
                    GUILayout_IntArea(ref mDataBackInterval);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            GUILayout.Space(20.0f);
            GUILayout.BeginVertical("box");
            {

                //if (GUILayout.Button("�汾��" + mHardwareVersion))
                //{
                //    ArcIO.RequestHardwareVersion();
                //}


                GUILayout.BeginHorizontal();
                GUILayout.Label("��Χ�ܵ�ģʽ(0-4):");
                int scenicLightMode = (int)mScenicLightMode;
                if (GUILayout_IntArea(ref scenicLightMode))
                {
                    mScenicLightMode = (ArcadeIO_Feng.ScenicLightMode)scenicLightMode;
                }

                if (GUILayout.Button("����"))
                {
                    ArcIO.SetScenicLight(mScenicLightMode);
                }
                GUILayout.EndHorizontal();

            }
            GUILayout.EndVertical();


           mSendVisibleMsg = GUILayout.TextArea(mSendVisibleMsg, GUILayout.MinWidth(TextAreaMinWidth));
           if (GUILayout.Button("��֤����"))
           {
               byte[] ba = System.Text.Encoding.Default.GetBytes(mSendVisibleMsg);
               string viewPlainTextBytes = "";
               foreach (byte b in ba)
               {
                   viewPlainTextBytes += string.Format("  {0:x}",b);
               }
               DebugLog("��������(hex):" + viewPlainTextBytes);
               ArcIO.SendCheckPlainText(System.Text.Encoding.Default.GetBytes(mSendVisibleMsg));
           }

           if (GUILayout.Button("���LOG"))
           {
               mDebugInfos.Clear();
               mScrollVal = 0F;
           }

        }
        GUILayout.EndVertical();//��߿���̨����

        //GUILayout.BeginVertical("box");
        GUILayout.TextArea(""

                , new GUILayoutOption[] { GUILayout.MinWidth(750), GUILayout.MinHeight(390) });
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();//�ұ�combobox����ʾ
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


            if(mDebugInfos.Count > mConsoloViewMaxLine)
            {
                mScrollVal = GUILayout.VerticalScrollbar(mScrollVal, 1f, 0F, (1 + mDebugInfos.Count - mConsoloViewMaxLine)
                ,GUILayout.MinHeight(330));
            }
        }    
        GUILayout.EndHorizontal();

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
        int newVal = int.Parse(valStr) ;
        if(newVal != oldVal)
        {
            val = newVal;
            return true;
        }
        return false;

    }

    private bool mRealTimeOutBounty = true;
 
    private int mOutCoinNum = 1;
    private int mOutTicketNum = 1;
    private int mButtomLightIdx = 0;//0-5

    private int mDataBackLen = 1;//[����]���󷵻����ݳ���
    private int mDataBackInterval = 10;//[����]���󷵻����ݼ��
    private bool mDataBackOnOff = false;//[����]���ݵ�ǰ�Ƿ��ڷ�����

    private ArcadeIO_Feng.ButtomLightMode mButtomLightMode;//0-6

    //private string mHardwareVersion = "";
    private ArcadeIO_Feng.ScenicLightMode mScenicLightMode;//0-4

    private string mSendVisibleMsg = "";
}

