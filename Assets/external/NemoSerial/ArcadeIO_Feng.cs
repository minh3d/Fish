using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// �ֻ��������,(���) 
/// </summary>
//[RequireComponent(typeof(NemoUsbHid))] 
public class ArcadeIO_Feng : MonoBehaviour {

    /// <summary>
    /// ����
    /// </summary>
    public enum Key
    {
        Up,
        Down,
        Left,
        Right,
        A,B,C,D,
        E,F,G,H,
        I,J,K,L//Ԥ��
    }

    /// <summary>
    /// ������ģʽ
    /// </summary>
    public enum ButtomLightMode
    {
        AwalyOff= 0,        //�Ƴ���
        AwalyOn,           //�Ƴ���
        Flash,              //����˸
        OffWhenClick,       //�����ɿ��Ƴ��������µƳ���
        OnWhenClick,        //�������µƳ������ɿ��Ƴ���
        OffWhenClickFlash,  //�����ɿ�����˸�����µƳ���
        OnWhenClickFlash,   //�������µ���˸���ɿ��Ƴ���
    }

    /// <summary>
    /// ������ģʽ
    /// </summary>
    public enum ScenicLightMode
    {
        Off,        //ȫ���Ƴ���
        On,         //ȫ���Ƴ���
        FlashA,     //ȫ������˸
        TraceA,     //׷�⣨16ֻ�ƣ�
        FlashB,     //��˫����˸
        TraceB      //���������
    }

    public struct SendParam
    {
        public byte HD;//��λ
        public byte LD;//��λ
    }

    public delegate void GenericEvent(uint count, int playerIdx);

    /// <summary>
    /// �����̨
    /// </summary>
    public delegate void EnterBackground();
    /// <summary>
    /// ����ʱ��
    /// </summary>
    /// <param name="ctrllerId">��̨id</param>
    /// <param name="k">����ö��</param>
    /// <param name="downOrUp">true:����,false:�ɿ�</param>
    public delegate void KeyEvent(int ctrllerId, Key k, bool downOrUp);

    /// <summary>
    /// ��̨����
    /// </summary>
    /// <param name="k"></param>
    /// <param name="downOrUp"></param>
    public delegate void BackGroundKeyEvent(Key k, bool downOrUp);

    /// <summary>
    /// Ӳ����Ӧ�汾��
    /// </summary>
    /// <param name="main">���汾��</param>
    /// <param name="sub">�ΰ汾��</param>
    public delegate void VersionResponEvent(uint main, uint sub);

    /// <summary>
    /// ��֤���
    /// </summary>
    /// <param name="sucess"></param>
    public delegate void VerifyResultEvent(bool sucess);

    /// <summary>
    /// �ı���֤
    /// </summary>
    /// <param name="sucess"></param>
    public delegate void TextVerifyEvent(byte[] sucess);


    /// <summary>
    /// ״̬���
    /// </summary>
    /// <param name="usbEquipOk">usb�豸�Ƿ�����</param>
    /// <param name="arcIOOk">���ư��Ƿ�����</param>
    public delegate void StateCheckEvent(bool usbEquipOk, bool arcIOOk);

    public event GenericEvent HandlerInsertCoin;//Ͷ��ʱ��
    /// <summary>
    /// ��������
    /// </summary>
    public event KeyEvent HandlerKey;
    public event GenericEvent HandlerOutCoinReflect;//���һ�Ӧ
    public event GenericEvent HandlerOutTicketReflect;//��Ʊ��Ӧ
    public event GenericEvent HandlerLackCoin;//���Ҳ���
    public event GenericEvent HandlerLackTicket;//��Ʊ����
    //public event VersionResponEvent HandlerVersionRespon;//�汾�����Ӧ
    //public event VerifyResultEvent HandlerVerifyResult;//��֤������
    //public event EnterBackground HandlerEnterBackground;//�����̨
    public event BackGroundKeyEvent HandlerBackGroundKeyEvent;
    //public event TextVerifyEvent HandlerTextVerifyResult;//�ı���֤���

    
    public event StateCheckEvent HandlerStateCheck;//״̬���


    private NemoUsbHid mSerial;
    
    private static int mPlayerNum = 6;//�����Ŀ,(��̨��)
    private static int mDataByteNum = 6;//�����ֽ���
    private static int mKeyCountPerPlayer = 16;//һ����ҵİ�����Ŀ

  
    private byte[][] mKeyBuff;//�˸���̨���а���(16��,4��Ԥ��)��״̬
    private BitArray[] mKeyBitBuff;//��KeyBuffͬ��

    private byte[] mVerifyInfo;//��֤��Ϣ

    
 

    void Awake()
    {
           
        mKeyBuff = new byte[mPlayerNum][];//�˸���̨���а���(16��,4��Ԥ��)��״̬
        mKeyBitBuff = new BitArray[mPlayerNum];//��KeyBuffͬ��

        mVerifyInfo = new byte[2];//��֤��Ϣ

        for (int i = 0; i != mPlayerNum; ++i)
        {
            mKeyBuff[i] = new byte[2];
        }


        for (int i = 0; i != mPlayerNum; ++i)
        {
            mKeyBitBuff[i] = new BitArray(mKeyCountPerPlayer, false);
        }

        mPackageSetup_ReadBuf = new byte[512]; 
        mPackageSetup_Readed = new List<byte>();
    }

	void Start () {
        if (mSerial == null)
        {
            mSerial = GetComponent<NemoUsbHid>();
            mSerial.HandleRecivePackage += OnRecivePackage;
            mSerial.HandleSetupPackage += Handle_PackageSetupProcess2;
            //mSerial.HandleSetupPackageT += Handle_PackageSetupProcess;//test
        }

        Open();
	} 

    public bool Open()
    {
        if (mSerial == null)
        {
            mSerial = GetComponent<NemoUsbHid>();
            mSerial.HandleRecivePackage += OnRecivePackage;
        }
 
        
 
        return mSerial.Open();
    }

    public void Close()
    {
        //mSeriasl.Close();
    }

    public class PackageGeneric : NemoUsbHid.Package
    {
        public int ID;
        public int Length;
    }
    public class PackTest1 : PackageGeneric
    {
        public int PlayerId;
        public int OutCointNum;
    }
    enum ReadPackageStage
    {
        NotStart,//δ��ʼ
        Head,//��ͷ
        DataToEnd//��ȡ���ݺͽ����׶�
    };

    byte[] mPackageSetup_ReadBuf;
    int mPackageSetup_RestNum;
    List<byte> mPackageSetup_Readed;//һ�����Ѷ�ȡ�ֽ�

    int mPackSetupingLen;//��װ�а��ĳ���
    //byte mPackSetupingID;//��װ�а���id
    ReadPackageStage mReadPackStage;

    int mPackHeadSize = 5;

    /// <summary>
    ///  �������
    /// </summary>
    /// <param name="datas"></param>
    /// <returns></returns>
    List<NemoUsbHid.Package> Handle_PackageSetupProcess2(byte[] datas)
    {
        List<NemoUsbHid.Package> packsOut = new List<NemoUsbHid.Package>();

        //int readNum = mPackageSetup_RestNum + datas.Length; 
        //System.Array.Copy(datas, 0, mPackageSetup_ReadBuf, mPackageSetup_RestNum, datas.Length);

        //for (int i = 0; i != datas.Length; ++i)
        foreach(byte b in datas)
        { 

            switch (mReadPackStage)
            {
                case ReadPackageStage.NotStart:
                    {
                        if (b == 0xFB)
                        { 
                            mPackageSetup_Readed.Add(b);
                            mReadPackStage = ReadPackageStage.Head; 
                        }
                    }
                    break;
                case ReadPackageStage.Head:
                    {
                        mPackageSetup_Readed.Add(b); 
                        if (mPackageSetup_Readed.Count == 5)
                        { 
                            mPackSetupingLen = 256 * mPackageSetup_Readed[3] + mPackageSetup_Readed[4];
                            mReadPackStage = ReadPackageStage.DataToEnd;

                            if (mPackSetupingLen > 256)
                            {
                                mReadPackStage = ReadPackageStage.NotStart;
                                mPackageSetup_Readed.Clear();
                                Debug.LogWarning("[ArcadeIO_Cheng] PackSetupingLen > 256,pack may wrong!");
                                //return packsOut;
                            }
                        }
                    }
                    break;
                case ReadPackageStage.DataToEnd:
                    {
                        mPackageSetup_Readed.Add(b);
                        if (mPackageSetup_Readed.Count == mPackSetupingLen + 5 + 1)
                        {
                            if (mPackageSetup_Readed[mPackageSetup_Readed.Count - 1]
                                == (mPackageSetup_Readed[1] ^ mPackageSetup_Readed[2]))//��β������-������
                            {
                                mReadPackStage = ReadPackageStage.NotStart;

                                //System.Array.Copy(mPackageSetup_ReadBuf, i + 1, mPackageSetup_ReadBuf, 0, readNum - i - 1);
        
                                //mPackageSetup_RestNum = readNum - i - 1;
                                NemoUsbHid.Package pOut = new NemoUsbHid.Package();
                                pOut.data = mPackageSetup_Readed.ToArray();
                                mPackageSetup_Readed.Clear();

                                packsOut.Add(pOut);
                            }
                            else//�д��������
                            { 
                                mReadPackStage = ReadPackageStage.NotStart;
                                mPackageSetup_Readed.Clear();
                                Debug.LogWarning("[ArcadeIO_Cheng] Wrong package appear in PackageSetup!");
                                //return packsOut;
                            }
                        }
                    }
                    break;

            }
        }
        //Debug.Log("outpack num = " + packsOut.Count.ToString());
        return packsOut;
    }

    //�������
    //1.mPackageSetup_ReadBuf����ɾ��?
    NemoUsbHid.Package Handle_PackageSetupProcess(byte[] datas)
    {
        //Debug.Log("        count:" + datas.Length + "   read:" + NemoUsbHid.ByteArrayToString(datas));
        int readNum = mPackageSetup_RestNum + datas.Length;
        //Debug.Log("datas.Length = " + datas.Length.ToString() + "  mPackageSetup_RestNum = " + mPackageSetup_RestNum);
        System.Array.Copy(datas, 0, mPackageSetup_ReadBuf, mPackageSetup_RestNum, datas.Length);

        //Debug.Log("Handle_PackageSetupProcess readNum = " + readNum + " mPackageSetup_ReadBuf.l = " + mPackageSetup_ReadBuf.Length);
        for (int i = 0; i != readNum; ++i ) 
        {
            byte b = mPackageSetup_ReadBuf[i];

            switch (mReadPackStage)
            {
                case ReadPackageStage.NotStart:
                    {
                        if (b == 0xFB)
                        {
                            mPackageSetup_Readed.Add(b);
                            mReadPackStage = ReadPackageStage.Head;
                            //Debug.Log("NotStart to Head");
                        }
                    }
                    break;
                case ReadPackageStage.Head:
                    {
                        mPackageSetup_Readed.Add(b);
                        //Debug.Log("Head   mPackageSetup_Readed.length = " + mPackageSetup_Readed.Count + "b = " + b);
                        if (mPackageSetup_Readed.Count == 5)
                        {
                            //mPackSetupingID = 256 * mPackageSetup_Readed[1] + mPackageSetup_Readed[2];
                            mPackSetupingLen = 256 * mPackageSetup_Readed[3] + mPackageSetup_Readed[4];
                            mReadPackStage = ReadPackageStage.DataToEnd;
                            //Debug.Log("Head to DataToEnd");
                        }
                    }
                    break;
                case ReadPackageStage.DataToEnd:
                    {
                        mPackageSetup_Readed.Add(b);
                        if (mPackageSetup_Readed.Count == mPackSetupingLen + 5 + 1)
                        {
                            if (mPackageSetup_Readed[mPackageSetup_Readed.Count - 1] 
                                == (mPackageSetup_Readed[1] ^ mPackageSetup_Readed[2]))//��β������-������
                            {
                                mReadPackStage = ReadPackageStage.NotStart;

                                System.Array.Copy(mPackageSetup_ReadBuf, i+1, mPackageSetup_ReadBuf,0, readNum - i -1);
                                //Debug.Log("package setuping:" + NemoUsbHid.ByteArrayToString(mPackageSetup_Readed.ToArray()));

                                //test Start
                                //byte[] testBytes = new byte[readNum - i - 1];
                                //System.Array.Copy(mPackageSetup_ReadBuf, testBytes, readNum - i - 1);
                                //Debug.Log("package setuping:"+NemoUsbHid.ByteArrayToString(testBytes));
                                //test End

                                mPackageSetup_RestNum = readNum - i - 1;
                                NemoUsbHid.Package p = new NemoUsbHid.Package();
                                p.data = mPackageSetup_Readed.ToArray();
                                mPackageSetup_Readed.Clear();

                                //Debug.Log("package : " + NemoUsbHid.ByteArrayToString(p.data));
                                return p;
                            }
                            else//�д��������
                            {
                                //test Start
                                //Debug.Log("package erro msg:" + NemoUsbHid.ByteArrayToString(mPackageSetup_Readed.ToArray()));
                                //test End

                                mReadPackStage = ReadPackageStage.NotStart;
                                mPackageSetup_Readed.Clear();
                                Debug.LogWarning("[ArcadeIO_Cheng] Wrong package appear in PackageSetup!");
                                return null;
                            }
                        }
                    }
                    break;

            } 
        } 
        mPackageSetup_RestNum = 0;
        return null;
    }


    void OnRecivePackage(NemoUsbHid.Package p)
    { 
 
 
        //////////////////////////////////////////////////////////////////////////
        //������
        //Ͷ��
        if (p.data[2] == 0x11)
        {
            int playeridx = p.data[mPackHeadSize];
            if (HandlerInsertCoin != null)
                HandlerInsertCoin(1, playeridx);
        }

        //����������
        if (p.data[2] == 0x21)
        {
            //Debug.Log("p.data.len = " + p.data.Length);
            int playeridx = p.data[mPackHeadSize];
            Key k = (Key)p.data[mPackHeadSize + 1];
            bool isDown = p.data[mPackHeadSize + 2] == 1?true:false;

            if (HandlerKey != null)
                HandlerKey(playeridx, k, isDown);
        }

        //�˱���Ӧ
        if (p.data[2] == 0x51)
        {
            int playeridx = p.data[mPackHeadSize];
            if (HandlerOutCoinReflect != null)
                HandlerOutCoinReflect(1, playeridx);
        }
        //��Ʊ��Ӧ
        if (p.data[2] == 0x61)
        {
            int playeridx = p.data[mPackHeadSize];
            if (HandlerOutTicketReflect != null)
                HandlerOutTicketReflect(1, playeridx);
        }

        //�˱Ҳ���
        if (p.data[2] == 0x32)
        {
            int playeridx = p.data[mPackHeadSize];
            uint num = System.BitConverter.ToUInt32(p.data, mPackHeadSize + 1);
            if (HandlerLackCoin != null)
                HandlerLackCoin(num, playeridx);
        }
        //��Ʊ����
        if (p.data[2] == 0x42)
        {
            int playeridx = p.data[mPackHeadSize];
            uint num = System.BitConverter.ToUInt32(p.data, mPackHeadSize + 1);
            if (HandlerLackTicket != null)
                HandlerLackTicket(num, playeridx);
        }

        //��̨����
        if (p.data[2] == 0xc3)
        { 
            Key k = (Key)p.data[mPackHeadSize  ];
            bool isDown = p.data[mPackHeadSize + 1] == 1?true:false;

            if (HandlerBackGroundKeyEvent != null)
                HandlerBackGroundKeyEvent( k, isDown); 
        }

        //״̬����
        if (p.data[2] == 0x71)
        {
            byte usbState = p.data[mPackHeadSize];
            byte arcState = p.data[mPackHeadSize + 1];
            if (HandlerStateCheck != null)
                HandlerStateCheck(usbState == 1 ? true : false, arcState == 1 ? true : false);
        }
    }


    /// <summary>
    /// ����ת�ߵ�λ
    /// </summary>
    /// <param name="num">����</param>
    /// <param name="hd">��λ</param>
    /// <param name="ld">��λ</param>
    private void _NumberToHDLD(uint num,out byte hd,out byte ld)
    {
        hd = (byte)(num / 256);
        ld = (byte)(num % 256);
    }
    
    /// <summary>
    /// �ߵ�λת����
    /// </summary>
    /// <param name="hd">��λ</param>
    /// <param name="ld">��λ</param>
    /// <returns></returns>
    private uint _HDLDToNumber(byte hd,byte ld)
    {
        return (uint)(hd * 256 + ld);
    }

    /// <summary>
    /// ���̨��idת�ɴ������byte
    /// </summary>
    /// <remarks> ����μ�����ͨѶ </remarks>
    /// <param name="playerIdx"></param>
    /// <returns></returns>
    private byte _PlayerIdxToParamByte(int playerIdx)
    {
        return (byte)Mathf.Pow(2F, (float)playerIdx);
    }

    /// <summary>
    /// ��_PlayerIdxToParamByte�෴,�μ�_PlayerIdxToParamByte
    /// </summary>
    /// <param name="paramByte"></param>
    /// <returns></returns>
    private int _ParamByteToPlayerIdx(byte paramByte)
    {
        return (byte)Mathf.Log((float)paramByte, 2F);
    }
    private void _Send(byte cmd,SendParam param)
    {
        byte[] DataToSend = new byte[mDataByteNum];
        DataToSend[0] = 0Xfa;
        DataToSend[1] = cmd;
        DataToSend[2] = param.HD;
        DataToSend[3] = param.LD;
        DataToSend[4] = (byte)(cmd + param.HD + param.LD);
        DataToSend[5] = (byte)(DataToSend[4] + 1);

        mSerial.Send(DataToSend);
    }

    /// <summary>
    /// ����byte����
    /// </summary>
    /// <remarks>
    /// 1.����Ч������λ(2�ֽ�)
    /// 2.data���ȱ���Ϊ4
    /// </remarks>
    /// <param name="cmd"></param>
    /// <param name="data"></param>
    private void _SendByteData(byte cmd, byte[] data)
    {
        if(data == null)
        {
            return;
        }
 

        byte[] DataToSend = new byte[mDataByteNum];
        DataToSend[0] = 0Xfa;
        DataToSend[1] = cmd;
        //DataToSend[2] = data[0];
        //DataToSend[3] = data[1];
        //DataToSend[4] = data[2];
        //DataToSend[5] = data[3];
        data.CopyTo(DataToSend, 2);
        mSerial.Send(DataToSend);
    }

    /// <summary>
    /// ����
    /// </summary>
    /// <param name="num">����</param>
    /// <param name="PlayerIdx">���id(����̨��,��0��ʼ)</param>
    public void OutCoin(uint num, int playerIdx)
    {
        //Debug.Log("out coin playerIdx=" + playerIdx);
        //byte cmd = (byte)(0x11 + playerIdx);
        //SendParam param;
        //_NumberToHDLD(num, out param.HD, out param.LD);
        //_Send(cmd, param);
        if (mSerial == null)
            return;
        byte[] numBytes = System.BitConverter.GetBytes(num);
        byte[] dataToSend = new byte[]{0xFA, 0x00, 0x11, 0x00, 0x05
                                     , (byte)playerIdx, numBytes[0], numBytes[1], numBytes[2], numBytes[3]
                                     , 0x11};
        
        mSerial.Send(dataToSend);

    }

    /// <summary>
    /// ��Ʊ
    /// </summary>
    /// <param name="num"></param>
    /// <param name="playerIdx"></param>
    public void OutTicket(uint num, int playerIdx)
    {
        //byte cmd = (byte)(0x21 + playerIdx);
        //SendParam param;
        //_NumberToHDLD(num, out param.HD, out param.LD);
        //_Send(cmd, param);
        if (mSerial == null)
            return;
        byte[] numBytes = System.BitConverter.GetBytes(num);
        byte[] dataToSend = new byte[]{0xFA, 0x00, 0x21, 0x00, 0x05
                                     , (byte)playerIdx, numBytes[0], numBytes[1], numBytes[2], numBytes[3]
                                     , 0x21};
 
        mSerial.Send(dataToSend);
    }

    /// <summary>
    /// ���״̬(������)
    /// </summary>
    public void CheckState()
    {
        if (mSerial == null)
            return;
 
        byte[] dataToSend = new byte[]{0xFA, 0x00, 0x71, 0x00, 0x00,   0x71}; 
        mSerial.Send(dataToSend);
    }

    /// <summary>
    /// ��������,(���������ٷ������������)
    /// </summary>
    /// <param name="playerIdx"></param>
    public void FlashButtomLight(int playerIdx)
    {
        if (mSerial == null)
            return;
        byte[] dataToSend = new byte[]{0xFA, 0x00, 0x51, 0x00, 0x01
                                     , (byte)playerIdx 
                                     , 0x51};
        mSerial.Send(dataToSend);
 
    }

    /// <summary>
    /// ���õ�
    /// </summary>
    /// <param name="playerIdx"></param>
    /// <param name="lightIdx"></param>
    /// <param name="mode"></param>
    public void SetButtomLight(int playerIdx, int lightIdx, ButtomLightMode mode)
    {

        SendParam param;
        param.HD = (byte)Mathf.Pow(2.0f, (float)playerIdx);
        param.LD = (byte)( (byte)mode  << 4);
        param.LD += (byte)lightIdx;

        _Send(0xAA, param);
    }

    public void SetScenicLight(ScenicLightMode mode)
    {
        SendParam param;
        param.HD = (byte)mode;
        param.LD = 0;
        _Send(0xcc, param);
    }

    ///// <summary>
    ///// Ҫ��Ӳ���巵�ذ汾��
    ///// </summary>
    //public void RequestHardwareVersion()
    //{
    //    _Send(0x88, new SendParam());
    //}

    /// <summary>
    /// ������֤Ӳ���Ƿ�����
    /// </summary>
    public void RequestVerifyHardware()
    {
        mVerifyInfo[0] = (byte)Random.Range(0, 255);
        mVerifyInfo[1] = (byte)Random.Range(0, 255);
        SendParam param;
        param.HD = mVerifyInfo[0];
        param.LD = mVerifyInfo[1];
        _Send(0xc4, param);
    }

    /// <summary>
    /// ����ʵʱ����ģʽ
    /// </summary>
    /// <remarks> Ĭ��ʵʱ����ģʽΪ��</remarks>
    /// <param name="realtime">true:ʵʱ(ÿ��һ�ҷ���һ��),false:��ʵʱ(ȫ�������ٷ�����Ϣ)</param>
    public void SetRealTimeOutBounty(bool realtime)
    {
        SendParam param;
        param.HD = 0;
        param.LD = (byte)(realtime ? 1 : 0);
        _Send(0xbb, param);
    }
    /// <summary>
    /// ��ð���״̬
    /// </summary>
    /// <param name="playerid"></param>
    /// <param name="k"></param>
    /// <returns></returns>
    public bool GetKeyState(int playerid,Key k)
    {
        return mKeyBitBuff[playerid][(int)k];
    }

    /// <summary>
    /// ����
    /// </summary>
    /// <param name="playerIdx"></param>
    public void ResponCoin(int playerIdx)
    {
        //SendParam param;
        //param.HD = 0;
        //param.LD = 0;

        //byte cmd = (byte)(0x31 + playerIdx);
 
        //_Send(cmd, param);
 
        byte[] dataToSend = new byte[]{0xFA, 0x00, 0x31, 0x00, 0x01
                                     , (byte)playerIdx
                                     , 0x31};
        mSerial.Send(dataToSend);
    }

    /// <summary>
    /// ��Ʊ
    /// </summary>
    /// <param name="playerIdx"></param>
    public void ResponTicket(int playerIdx)
    {
        //SendParam param;
        //param.HD = 0;
        //param.LD = 0;
        //byte cmd = (byte)(0x41 + playerIdx);
        //_Send(cmd, param);

        byte[] dataToSend = new byte[]{0xFA, 0x00, 0x41, 0x00, 0x01
                                     , (byte)playerIdx
                                     , 0x41};
        mSerial.Send(dataToSend);
    }

    /// <summary>
    /// ������֤��Ϣ
    /// </summary>
    /// <param name="txt"></param>
    public void SendCheckPlainText(byte[] bs)
    {
        if (bs.Length > 4 || bs.Length == 0)
        {
            Debug.LogError("SendCheckPlainText �����Ϲ���!");
            return;
        }
        _SendByteData(0xC1, bs);
    }

    /// <summary>
    /// ���Ӳ�������ȱ��ȱ������
    /// </summary>
    public void ClearLackBountyData()
    {
        SendParam param;
        param.HD = 0;
        param.LD = 0;
        byte cmd = (byte)(0xCD);
        _Send(cmd, param);
    }

    /// <summary>
    /// �����ʹ�������
    /// </summary>
    /// <param name="dataLen"></param>
    /// <param name="sendInterval"></param>
    /// <param name="onOff"></param>
    public void RequestDataBack(byte dataLen,byte sendInterval,bool isOn)
    {
        byte[] dataToSend = new byte[]{0xFA, 0x00, 0x72, 0x00, 0x03
                                     ,dataLen,sendInterval,(byte)(isOn?1:0)
                                     , 0x72};
        mSerial.Send(dataToSend);
    }
}
