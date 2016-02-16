//#define RECORE_INITSTATE //记录初始化状态,用于记录退币器的常态
#define MOBILE_EDITION
using System;
using System.IO.Ports;
using System.Threading;
using UnityEngine;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Collections;

public class NemoUsbHid_HardScan : MonoBehaviour, INemoControlIO
{

 
    //输出包命令
    enum OutputPackCmd
    {
        OutCoin,
        OutTicket,
        FlashLight,
        RequestMCUInfo,//请求下位机信息
        EditMCUInfo,//修改下位机信息
        ReadWriteRequest,//读写请求
    }

    //输入包命令
    public enum InputPackCmd
    {
        Key,
        InsertCoin,
        OutCoin,
        OutTicket,
        LackCoin,
        LackTicket,
        MCUInfo,//下位机信息
        CtrlBoardConnectState,//控制板连接状态改变
        ResultEditMCUInfo,//修改下位机信息结果
        ResultReadWrite,//读写请求返回结果
    }

    /// <summary>
    /// 输入命令,大小与LenInputDataPerCtrller保持一致,并和ofsCtrller_xx对应
    /// </summary>
    public enum InputCmd
    {
        Up = 0,
        Down = 1,
        Left = 2,
        Right = 3,
        BtnA = 4,
        BtnB = 5,
        BtnC = 6,
        BtnD = 7,
        InCoin = 8,
        BtnE = 9,
        OutTicket = 10,
        OutCoin = 11
    }

    public class Package
    {

    }
    class OutputPackage : Package
    {
        public OutputPackage(OutputPackCmd cmd, int ctrllerIdx)
        {
            Cmd = cmd;
            CtrllerIdx = ctrllerIdx;
        }
        public OutputPackCmd Cmd;
        public int CtrllerIdx;//控制器索引
    }

    /// <summary>
    /// 输出包-灯
    /// </summary>
    class OutputPack_Light:OutputPackage
    {
        
        public OutputPack_Light(OutputPackCmd cmd, int ctrllerIdx, int lightIdx, bool isOn):base(cmd,ctrllerIdx)
        {
            LightIdx = lightIdx;
            IsOn = isOn;
        }
        public int LightIdx;
        public bool IsOn;
    }
    /// <summary>
    /// 输出包-出币/票
    /// </summary>
    class OutputPack_OutBounty : OutputPackage
    {
        public OutputPack_OutBounty(OutputPackCmd cmd, int ctrllerIdx, bool enable):base(cmd,ctrllerIdx)
        {
            Enable = enable; 
        } 
        public bool Enable;
    }

    class OutputPack_EditMCUInfo : OutputPackage
    {
        public OutputPack_EditMCUInfo(OutputPackCmd cmd, int ctrllerIdx, int gameIdx, int mainVersion, int subVersion)
            : base(cmd, ctrllerIdx)
        {
            GameIdx = gameIdx;
            MainVersion = mainVersion;
            SubVersion = subVersion;
        } 
        public int GameIdx;
        public int MainVersion;
        public int SubVersion;
    }

    /// <summary>
    /// 读写请求包
    /// </summary>
    class OutputPack_ReadWriteRequest : OutputPackage
    {
        public OutputPack_ReadWriteRequest(OutputPackCmd cmd, bool isWrite, uint address,byte dataLength,byte[] data)
            : base(cmd, 0)
        {
            IsWrite = isWrite;
            Address = address;
            DataLength = dataLength;
            Data = data;
        }
        public bool IsWrite;//0读1写
        public uint Address;//地址
        public byte DataLength;//数据长度,单位:字节
        public byte[] Data;//数据内容

    }

    /// <summary>
    /// 输入包-
    /// </summary>
    public class InputPackage : Package
    {
        public InputPackCmd Cmd;
        public int CtrllerIdx;

        public InputPackage(InputPackCmd cmd, int ctrllerIdx)
        {
            Cmd = cmd;
            CtrllerIdx = ctrllerIdx;
        }


    }

    /// <summary>
    /// 输入包-按键
    /// </summary>
    public class InputPack_Key : InputPackage
    {
        public InputCmd Key_;
        public bool DownOrUp;

        public InputPack_Key(InputPackCmd cmd, int ctrllerIdx,InputCmd key,bool downOrUp):base(cmd,ctrllerIdx)
        {
            Key_ = key;
            DownOrUp = downOrUp;
        }

    }

    class InputPack_MCUInfo : InputPackage
    {
        public bool VerifySucess;//验证是否成功
        public int GameIdx;//游戏序号
        public int VersionMain;
        public int VersionSub;
        public InputPack_MCUInfo(InputPackCmd cmd, int ctrllerIdx, bool verifySucess, int gIdx, int verMain, int verSub)
            : base(cmd, ctrllerIdx)
        {
            VerifySucess = verifySucess;
            GameIdx = gIdx;
            VersionMain = verMain;
            VersionSub = verSub;
        }
    }

    class InputPack_CtrlBoardState : InputPackage
    {
        public int CtrlBoardId;//控制板ID
        public bool ConectOrDisconect;//连接或断开
        public InputPack_CtrlBoardState(InputPackCmd cmd, int ctrlIdx,int ctrlBoardId, bool conectOrDisConect)
            :base(cmd,ctrlIdx)
        {
            CtrlBoardId = ctrlBoardId;
            ConectOrDisconect = conectOrDisConect;
        }
    }

    class InputPack_ResultEditMCUInfo : InputPackage
    { 
        public bool Result;//连接或断开
        public InputPack_ResultEditMCUInfo(InputPackCmd cmd, int ctrlIdx, bool result)
            : base(cmd, ctrlIdx)
        { 
            Result = result;
        }
    }

    /// <summary>
    /// 读写请求返回
    /// </summary>
    class InputPack_ResultReadWrite : InputPackage
    {
        public bool IsWrite;//0读1写
        public uint Address;//地址
        public byte DataLength;//数据长度,单位:字节
        public byte ResultCode;//读写结果
        public byte[] Data;//数据内容

        public InputPack_ResultReadWrite(InputPackCmd cmd, bool isWrite, uint address, byte dataLength, byte resultCode, byte[] data)
            : base(cmd, 0)
        {
            IsWrite = isWrite;
            Address = address;
            DataLength = dataLength;
            ResultCode = resultCode;
            Data = data;
        }
        
    }
    //退奖励延时,标记奖励已出时间,用于判断是否退奖不足(通过超时)
    class OutBountyElapse
    {
        public OutBountyElapse(long time)
        {
            Time = time;
        }
        public long Time;
    }

    NemoCtrlIO_EventHardwareInfo mEvtHardwareInfo;
    NemoCtrlIO_EventKey mEvtKey;
    NemoCtrlIO_EventInsertCoin mEvtInsertCoin;
    NemoCtrlIO_EventController mEvtOutCoinReflect;
    NemoCtrlIO_EventController mEvtOutTicketReflect;
    NemoCtrlIO_EventController mEvtLackCoin;
    NemoCtrlIO_EventController mEvtLackTicket;
    NemoCtrlIO_EventCtrlBoardStateChanged mEvtCtrlBoardStateChanged;
    NemoCtrlIO_EventGeneral mEvtOpened;
    NemoCtrlIO_EventGeneral mEvtClosed;
    NemoCtrlIO_EventResultReadWrite mEvtResultReadWrite;

    public NemoCtrlIO_EventHardwareInfo EvtHardwareInfo { get { return mEvtHardwareInfo; } set { mEvtHardwareInfo = value; } }
    public NemoCtrlIO_EventKey EvtKey { get { return mEvtKey; } set { mEvtKey = value; } }
    public NemoCtrlIO_EventInsertCoin EvtInsertCoin { get { return mEvtInsertCoin; } set { mEvtInsertCoin = value; } }
    public NemoCtrlIO_EventController EvtOutCoinReflect { get { return mEvtOutCoinReflect; } set { mEvtOutCoinReflect = value; } }
    public NemoCtrlIO_EventController EvtOutTicketReflect { get { return mEvtOutTicketReflect; } set { mEvtOutTicketReflect = value; } }
    public NemoCtrlIO_EventController EvtLackCoin { get { return mEvtLackCoin; } set { mEvtLackCoin = value; } }
    public NemoCtrlIO_EventController EvtLackTicket { get { return mEvtLackTicket; } set { mEvtLackTicket = value; } }
    public NemoCtrlIO_EventCtrlBoardStateChanged EvtCtrlBoardStateChanged { get { return mEvtCtrlBoardStateChanged; } set { mEvtCtrlBoardStateChanged = value; } }
    public NemoCtrlIO_EventGeneral EvtOpened { get { return mEvtOpened; } set { mEvtOpened = value; } }
    public NemoCtrlIO_EventGeneral EvtClosed { get { return mEvtClosed; } set { mEvtClosed = value; } }
    public NemoCtrlIO_EventResultReadWrite EvtResultReadWrite { get { return mEvtResultReadWrite; } set { mEvtResultReadWrite = value; } }

    public delegate void Event_ResultEditMCU(bool result);
    public Event_ResultEditMCU EvtResultEditMCU;

    public delegate bool Func_IsInvokeKeyEvent(int ctrlIdx, InputCmd k, bool keyStateDownOrUp);
    public Func_IsInvokeKeyEvent FuncHSThread_AddKeyPress;
    public delegate void Event_HSThread_FrameStart();
    public Event_HSThread_FrameStart Evt_HSThread_FrameStart;


    public int PID;
    public int VID;
    public int NumControlBoard = 1;//控制板数目,必填,用于判断控制板状态

    
    static int[,] InputCmdTrans;//单双数玩家的按键转换,硬件的特殊设定
    static int[] BackStageInputCmdTrans;//后台小键盘按键转换
        
    static readonly int OfsOutput_Light0 = 0;
    static readonly int OfsOutput_Light1 = 1;
    static readonly int OfsOutput_OutCoin = 2;
    static readonly int OfsOutput_OutTicket = 3;

    static readonly int OfsInput_ControlBoardState = 240;//bit,控制板状态,最多8个bit
    

    static readonly int OfsInput_BsKeyStartBit = 249;//bit,后台按键开始位置,因为空了一个bit,所以不是248 
    static readonly int OfsInput_BsKeyEndBit = 256;//bit,后台按键结束位置

    static readonly int LenInputBuf = 33;//字节
    static readonly int LenOuputBuf = 33;//字节
    static readonly int LenInputDataPerCtrller = 12; //bit,一个控制端口的数据长度
    static readonly int LenOuputDataPerCtrller = 4; //bit,一个控制端口的数据长度

    static readonly int NumCtrller = 20;//个,控制器数目 

    static readonly uint Timeout_Read = 1150;//读取延时 
    static readonly long Timeout_Outbounty = 50000000;//tick,5000*10000 1毫秒有10000tick,因为用到DateTime.Now.Tick,其单位为100

 

    Thread mThreadRecive; 
    bool mThreadLoopRecive = true;

    System.Object mSendPack_ThreadLock;  // 发包锁
    System.Object mRecivePack_ThreadLock;  // 收包锁

    List<OutputPackage> mPackToSendMT;//主线程(Main Thread)数据发出队列
    List<OutputPackage> mPackToSendST;//串口线程(SerialPort Thread)数据发出队列

    List<InputPackage> mPackToReciveMT;//主线程(Main Thread)数据接收队列
    List<InputPackage> mPackToReciveST;//串口线程(SerialPort Thread)数据接收队列

    IntPtr mIOHandler = IntPtr.Zero;
    //int mInputReportLen;
    //int mOutputReportLen;

    Win32Usb.Overlapped mReadOL;
    Win32Usb.Overlapped mWriteOL;
    
    BitArray mInputBuff;//上一帧硬件数据 
    BitArray mOutputBuff;

    BitArray mInputBuffInit;//初始化输入状态
    int mCurChallengeNum;//当前的请求MCUINFO时的ChallengeNum
    
    Dictionary<int, OutBountyElapse> mOuttingCoinTag;
    Dictionary<int, OutBountyElapse> mOuttingTicketTag;

    List<InputPackage> mInputPackageFromPlugin;//来自外部的输入包(主要用于按键的延时触发)

    System.Random mRndObj;
    // 是否打开
    public bool IsOpen()
    {
        return mIOHandler != IntPtr.Zero;
    }

    /// <summary>
    /// Unity关闭时调用
    /// </summary>
    void OnDestroy()
    {
        Close();
    }

    /// <summary>
    /// Unity调用
    /// </summary>
    void Update()
    {
 
        RecivePackages();
    }

    void Start()
    {
#if MOBILE_EDITION
        return;
#endif
        WndMsgHook.Evt_DeviceArrived += Handle_Device_Arrival;
        WndMsgHook.Evt_DeviceRemoved += Handle_Device_RemoveComplete;
 

        InitVals();
        Open();
    }
    void InitVals()
    {
#if MOBILE_EDITION
        return;
#endif

        if (mSendPack_ThreadLock == null)
            mSendPack_ThreadLock = new System.Object();
        if (mRecivePack_ThreadLock == null)
            mRecivePack_ThreadLock = new System.Object();

        if (mPackToSendMT == null)
            mPackToSendMT = new List<OutputPackage>();
        if (mPackToSendST == null)
            mPackToSendST = new List<OutputPackage>();

        if (mPackToReciveMT == null)
            mPackToReciveMT = new List<InputPackage>();
        if (mPackToReciveST == null)
            mPackToReciveST = new List<InputPackage>();
        if (mInputPackageFromPlugin == null)
            mInputPackageFromPlugin = new List<InputPackage>();
 
    }
    /// <summary>
    /// 打开 portNo串口号
    /// </summary>
    /// <param name="portNo">串口号</param>
    /// <returns>是否打开成功</returns>
    public bool Open()
    {
#if MOBILE_EDITION
        return false;
#endif

        if (mIOHandler != IntPtr.Zero)
            return true;
        InitVals();
        try
        {

            string devicePath = FindDevicePath(VID, PID);

            if (devicePath == null || devicePath == "")
            {
                return false;
            }


            mIOHandler = Win32Usb.CreateFile(devicePath, Win32Usb.GENERIC_READ | Win32Usb.GENERIC_WRITE
                        , Win32Usb.FILE_SHARE_READ | Win32Usb.FILE_SHARE_WRITE, IntPtr.Zero, Win32Usb.OPEN_EXISTING, Win32Usb.FILE_FLAG_OVERLAPPED, IntPtr.Zero);

            if (mIOHandler != Win32Usb.InvalidHandleValue)	// if the open worked...
            {
                IntPtr lpData;
                if (Win32Usb.HidD_GetPreparsedData(mIOHandler, out lpData))	// get windows to read the device data into an internal buffer
                {
                    try
                    {
                        Win32Usb.HidCaps oCaps;
                        Win32Usb.HidP_GetCaps(lpData, out oCaps);	// extract the device capabilities from the internal buffer
                        //mInputReportLen = oCaps.InputReportByteLength;	// get the input...
                        //mOutputReportLen = oCaps.OutputReportByteLength;	// ... and output report lengths
                    }
                    catch (Exception)
                    {
                        Debug.LogWarning("[HidUsb]得不到hid报文长度.");
                        return false;
                    }
                    finally
                    {
                        Win32Usb.HidD_FreePreparsedData(ref lpData);	// before we quit the funtion, we must free the internal buffer reserved in GetPreparsedData
                    }
                }
                else	// GetPreparsedData failed? Chuck an exception
                {
                    Debug.LogWarning("[HidUsb]获取不到hid属性.");
                    return false;
                }
            }
            else	// File open failed? Chuck an exception
            {
                mIOHandler = IntPtr.Zero;
                Debug.LogWarning("[HidUsb]打不开hid设备.");
                return false;
            }


            mThreadLoopRecive = true;
 
            mThreadRecive = new Thread(Thread_Recive);
            mThreadRecive.Start();


            if (EvtOpened != null)
                EvtOpened();

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Open Serialport Error." + e);
            return false;
        }
    }

    // 关闭
    public void Close()
    {
#if MOBILE_EDITION
        return;
#endif
        if (mIOHandler != IntPtr.Zero)
        {
            {

                Win32Usb.CancelIo(mIOHandler);
                Win32Usb.CloseHandle(mIOHandler);
                mIOHandler = IntPtr.Zero;

               
                mThreadLoopRecive = false;

                if (mThreadRecive != null)
                    mThreadRecive.Join();

                if (EvtClosed != null)
                    EvtClosed();
            }
        }
 

    }

     
    

    void Thread_Recive()
    {
#if MOBILE_EDITION
        return;
#endif
        try
        {
        InputCmdTrans = new int[LenInputDataPerCtrller, 2];

        for (int i = 0; i != LenInputDataPerCtrller; ++i)
            InputCmdTrans[i, 0] = i;

        InputCmdTrans[0,1] = (int)InputCmd.InCoin;
        InputCmdTrans[1,1] = (int)InputCmd.BtnE;
        InputCmdTrans[2,1] = (int)InputCmd.OutTicket;
        InputCmdTrans[3,1] = (int)InputCmd.OutCoin;
        InputCmdTrans[4,1] = (int)InputCmd.Up;
        InputCmdTrans[5,1] = (int)InputCmd.Down;
        InputCmdTrans[6,1] = (int)InputCmd.Left;
        InputCmdTrans[7,1] = (int)InputCmd.Right;
        InputCmdTrans[8,1] = (int)InputCmd.BtnA;
        InputCmdTrans[9,1] = (int)InputCmd.BtnB;
        InputCmdTrans[10,1] = (int)InputCmd.BtnC;
        InputCmdTrans[11,1] = (int)InputCmd.BtnD;
        //取消,左,右,小游戏,确定,下
        BackStageInputCmdTrans = new int[12];
        BackStageInputCmdTrans[0] = (int)InputCmd.Up;
        BackStageInputCmdTrans[1] = (int)InputCmd.BtnB;
        BackStageInputCmdTrans[2] = (int)InputCmd.Left;
        BackStageInputCmdTrans[3] = (int)InputCmd.Right;
        BackStageInputCmdTrans[4] = (int)InputCmd.BtnC;
        BackStageInputCmdTrans[5] = (int)InputCmd.BtnA;
        BackStageInputCmdTrans[6] = (int)InputCmd.Down;
 
        if (mOutputBuff == null)
            mOutputBuff = new BitArray((LenOuputBuf - 1) * 8 );//输入bitarray 减少一字节.以便.复制的时候向右移位
 

        BitArray tmpBA = null;//当前InputBitArray
        byte[] tmpOutputBuff = new byte[LenOuputBuf];
        byte[] tmpInputBuff = new byte[LenInputBuf];
        byte[] tmpPreInputBuff = new byte[LenInputBuf]; 
        List<InputPackage> tmpRecivePack = new List<InputPackage>();


        if (mOuttingCoinTag == null)
            mOuttingCoinTag = new Dictionary<int, OutBountyElapse>();
        if (mOuttingTicketTag == null)
            mOuttingTicketTag = new Dictionary<int, OutBountyElapse>();
        List<int> tmpListI = new List<int>();
      
        int offsetRead = 8;//读缓冲向右移8 bit
        uint numWrite = 0;
        uint readCount = 0;
        mRndObj = new System.Random();
        //读取一次状态
        mWriteOL.Clear();
        Array.Clear(tmpOutputBuff, 0, tmpOutputBuff.Length);
        
        Win32Usb.WriteFile(mIOHandler, tmpOutputBuff, (uint)tmpOutputBuff.Length, ref numWrite, ref mWriteOL);

        if (Win32Usb.WaitForSingleObjectEx(mIOHandler, Timeout_Read, true) == 0)//读取
        {
            Win32Usb.ReadFile(mIOHandler, tmpInputBuff, (uint)tmpInputBuff.Length, ref readCount, ref mReadOL);

            //接收包
            if (Win32Usb.WaitForSingleObjectEx(mIOHandler, Timeout_Read, true) == 0)
            {
                mInputBuff = new BitArray(tmpInputBuff);

#if RECORE_INITSTATE
                mInputBuffInit = new BitArray(tmpInputBuff);
#endif
                //初始化控制板状态
                for (int i = OfsInput_ControlBoardState + offsetRead; i != OfsInput_ControlBoardState + offsetRead + NumControlBoard; ++i)
                {
                    tmpRecivePack.Add(new InputPack_CtrlBoardState(InputPackCmd.CtrlBoardConnectState
                        , 0, i - OfsInput_ControlBoardState - offsetRead, mInputBuff[i]));//控制板状态
                }
                
                _FlushInputPackToMainThread(tmpRecivePack);
            }
            Thread.Sleep(100);
        }

#region 测试程序
        /*
        bool enableOutCoin = false;
        
        while (mThreadLoopRecive && mIOHandler != IntPtr.Zero)
        {
            mWriteOL.Clear();
            Array.Clear(tmpOutputBuff, 0, tmpOutputBuff.Length);


            mOutputBuff[0 * LenOuputDataPerCtrller + OfsOutput_OutCoin] = enableOutCoin;
            enableOutCoin = !enableOutCoin;
            mOutputBuff.CopyTo(tmpOutputBuff, 1);

            Win32Usb.WriteFile(mIOHandler, tmpOutputBuff, (uint)tmpOutputBuff.Length, ref numWrite, ref mWriteOL);

            if (Win32Usb.WaitForSingleObjectEx(mIOHandler, Timeout_Read, true) == 0)//发送完成
            {
                Thread.Sleep(1);
                
                Win32Usb.ReadFile(mIOHandler, tmpInputBuff, (uint)tmpInputBuff.Length, ref readCount, ref mReadOL);

                //接收包
                if (Win32Usb.WaitForSingleObjectEx(mIOHandler, Timeout_Read, true) == 0)//接收完成
                {
                    mInputBuff = new BitArray(tmpInputBuff);

                    //Thread.Sleep(1);

                    //初始化控制板状态
                    //for (int i = OfsInput_ControlBoardState + offsetRead; i != OfsInput_ControlBoardState + offsetRead + NumControlBoard; ++i)
                    //{
                    //    tmpRecivePack.Add(new InputPack_CtrlBoardState(InputPackCmd.CtrlBoardConnectState
                    //        , 0, i - OfsInput_ControlBoardState - offsetRead, mInputBuff[i]));//控制板状态
                    //}

                    //_FlushInputPackToMainThread(tmpRecivePack);
                }
                

            }
        }
        */
#endregion


        while (mThreadLoopRecive && mIOHandler != IntPtr.Zero)
        {
            if (Evt_HSThread_FrameStart != null)
                Evt_HSThread_FrameStart();

            

            if (Monitor.TryEnter(mSendPack_ThreadLock))
            {
                List<OutputPackage> tempList = mPackToSendST;

                mPackToSendST = mPackToSendMT;
                mPackToSendMT = tempList;
                Monitor.Exit(mSendPack_ThreadLock);
            } 
            
            foreach (OutputPackage pack in mPackToSendST)
            {
                   
                switch (pack.Cmd)
                {
                    case OutputPackCmd.OutCoin:
                        mOutputBuff[pack.CtrllerIdx * LenOuputDataPerCtrller + OfsOutput_OutCoin] = ((OutputPack_OutBounty)pack).Enable;

                        if (!mOuttingCoinTag.ContainsKey(pack.CtrllerIdx))
                            mOuttingCoinTag.Add(pack.CtrllerIdx, new OutBountyElapse(DateTime.Now.Ticks));
                        else
                            Debug.LogWarning("重复出币调用");
                        break;
                    case OutputPackCmd.OutTicket:
                        mOutputBuff[pack.CtrllerIdx * LenOuputDataPerCtrller + OfsOutput_OutTicket] = ((OutputPack_OutBounty)pack).Enable;
                        if (!mOuttingTicketTag.ContainsKey(pack.CtrllerIdx))
                            mOuttingTicketTag.Add(pack.CtrllerIdx, new OutBountyElapse(DateTime.Now.Ticks));
                        else
                            Debug.LogWarning("重复出票调用");
                        break;
                    case OutputPackCmd.FlashLight:
                        { 
                            OutputPack_Light lightPack = (OutputPack_Light)pack; 
                            if (lightPack.LightIdx == 0)
                                mOutputBuff[pack.CtrllerIdx * LenOuputDataPerCtrller + OfsOutput_Light0] = lightPack.IsOn;
                            else
                                mOutputBuff[pack.CtrllerIdx * LenOuputDataPerCtrller + OfsOutput_Light1] = lightPack.IsOn;

                        }
                        break;
                    case OutputPackCmd.RequestMCUInfo:
                        {

                            tmpOutputBuff[1] = tmpOutputBuff[tmpOutputBuff.Length - 1] = 1;

                            System.Random randObj = new System.Random();
                            int challengeNum = randObj.Next(int.MinValue, int.MaxValue); //发出要求MCU验证的数字
                            System.Array.Copy(System.BitConverter.GetBytes(challengeNum), 0, tmpOutputBuff, 2, 4);

                            mWriteOL.Clear();
                            Win32Usb.WriteFile(mIOHandler, tmpOutputBuff, (uint)tmpOutputBuff.Length, ref numWrite, ref mWriteOL);
         
                            if (Win32Usb.WaitForSingleObjectEx(mIOHandler, Timeout_Read, true) == 0)//读取
                            {
                                Win32Usb.ReadFile(mIOHandler, tmpInputBuff, (uint)tmpInputBuff.Length, ref readCount, ref mReadOL);
                                bool verfySucess = true;
                                if (Win32Usb.WaitForSingleObjectEx(mIOHandler, Timeout_Read,true) == 0)
                                {
                                            
                                    if (tmpInputBuff[1] == tmpInputBuff[tmpInputBuff.Length - 1] && tmpInputBuff[1] == 1)
                                    {
                                        HMACMD5 cryptor = new HMACMD5(System.Text.Encoding.ASCII.GetBytes("yidingyaochang"));
                                        byte[] challengeAnswer = cryptor.ComputeHash(System.BitConverter.GetBytes(challengeNum));
                                        for (int i = 0; i != challengeAnswer.Length; ++i)
                                        {
                                            if (challengeAnswer[i] != tmpInputBuff[7 + i])
                                            { 
                                                verfySucess = false;
                                                break;
                                            }
                                        }
                                                
                                               
                                    }
                                    else//验证失败
                                    {
                                        verfySucess = false;
                                    }
                                }
                                else//验证失败
                                {
                                    verfySucess = false;
                                }

                                if (verfySucess)
                                {
                                    tmpRecivePack.Add(new InputPack_MCUInfo(InputPackCmd.MCUInfo, 0, true,
                                        tmpInputBuff[2],//游戏序号
                                        System.BitConverter.ToUInt16(tmpInputBuff, 3),//主版本号
                                        System.BitConverter.ToUInt16(tmpInputBuff, 5)//副版本号
                                        ));
                                }
                                else
                                {
                                    tmpRecivePack.Add(new InputPack_MCUInfo(InputPackCmd.MCUInfo, 0, false, -1, 0, 0));
                                }
                                _FlushInputPackToMainThread(tmpRecivePack);
                            } 
                        }
                        break;
                    case OutputPackCmd.EditMCUInfo:
                        {
                            OutputPack_EditMCUInfo op = (OutputPack_EditMCUInfo)pack;

                            bool result = _UsbThread_ChangeMCUInfo(op.GameIdx, op.MainVersion, op.SubVersion);

                            tmpRecivePack.Add(new InputPack_ResultEditMCUInfo(InputPackCmd.ResultEditMCUInfo, 0, result));
                            _FlushInputPackToMainThread(tmpRecivePack);
                        }
                        break;

                    case OutputPackCmd.ReadWriteRequest://有读写请求的,直接进行读取处理
                        {
                            OutputPack_ReadWriteRequest p = (OutputPack_ReadWriteRequest)pack;
                            //包标识
                            tmpOutputBuff[1] = 0xf2;
                            //读写标记
                            tmpOutputBuff[2] = (byte)(p.IsWrite ?  1 : 0);
                            //地址
                            Array.Copy(System.BitConverter.GetBytes(p.Address), 0, tmpOutputBuff, 3, 4);
                            //数据长度
                            tmpOutputBuff[7] = p.DataLength;
                            //数据
                            if(p.Data != null)
                                Array.Copy(p.Data, 0, tmpOutputBuff, 8, p.DataLength);
                            //随机数填充
                            Byte[] tmpByte = new byte[5];
                            mRndObj.NextBytes(tmpByte);
                            Array.Copy(tmpByte, 0, tmpOutputBuff, 24, 5);
                            //校验码
                            Array.Clear(tmpByte,0,5);
                            for (int i = 0; i != 4; ++i)
                            {
                                for (int j = 0; j != 7; ++j)
                                {
                                    tmpByte[i] ^= tmpOutputBuff[j * 4 + i + 1];
                                }
                            }
                            Array.Copy(tmpByte, 0, tmpOutputBuff, 29, 4);
                            //Debug.Log(string.Format("adress:{0:d} len:{1:d}  ", p.Address, p.DataLength));
                            //Debug.Log("w");
                            //发出消息
                            mWriteOL.Clear();

                            Win32Usb.WriteFile(mIOHandler, tmpOutputBuff, (uint)tmpOutputBuff.Length, ref numWrite, ref mWriteOL);
                                

                            //读取消息
                            if (Win32Usb.WaitForSingleObjectEx(mIOHandler, Timeout_Read, true) == 0)//读取
                            {
                                Win32Usb.ReadFile(mIOHandler, tmpInputBuff, (uint)tmpInputBuff.Length, ref readCount, ref mReadOL);
                                if (Win32Usb.WaitForSingleObjectEx(mIOHandler, Timeout_Read, true) == 0)
                                {
                                    //验证包是否正常
                                    if(tmpInputBuff[1] != 0xF2)
                                        goto BREAK_THIS_SWITCH;
                                    //校验码是否正确
                                    Byte[] tmpByte4 = new byte[4];
                                    for (int i = 0; i != 4; ++i)
                                    {
                                        for (int j = 0; j != 7; ++j)
                                        {
                                            tmpByte4[i] ^= tmpInputBuff[j * 4 + i + 1];
                                        }
                                            
                                    }
                                    for(int i = 0; i != 4; ++i)
                                    {
                                        if(tmpByte4[i] != tmpInputBuff[29+i])
                                            goto BREAK_THIS_SWITCH;
                                    }

                                    //Array.Copy(tmpInputBuff, 3, tmpByte4, 0, 4);
                                    uint adress = System.BitConverter.ToUInt32(tmpInputBuff,3);
                                    byte dataLen = tmpInputBuff[7];
                                    byte[] dataOut = null;
                                    if(tmpInputBuff[2] == 0)//读取标记
                                    {
                                        dataOut = new byte[dataLen];
                                        Array.Copy(tmpInputBuff,9,dataOut,0,dataLen);
                                    }
                                    //开始组包
                                    tmpRecivePack.Add(new InputPack_ResultReadWrite(InputPackCmd.ResultReadWrite
                                        , tmpInputBuff[2] == 0 ? false : true,//读写标记
                                        adress,//地址
                                        dataLen,//包长度
                                        tmpInputBuff[8],//读写结果
                                        dataOut//包
                                        ));
                                    //Debug.Log(string.Format("adress:{0:d}  datalen{1:d}  resultcode:{2:d}  ",adress,dataLen,tmpInputBuff[8]));
                                    _FlushInputPackToMainThread(tmpRecivePack);
                                    
                                }
                            }
                        }
                        BREAK_THIS_SWITCH: 
                        break;

                }

            }

            mPackToSendST.Clear();
            
            //发出数据
            mOutputBuff.CopyTo(tmpOutputBuff, 1);
            mWriteOL.Clear();
            Win32Usb.WriteFile(mIOHandler, tmpOutputBuff, (uint)tmpOutputBuff.Length, ref numWrite, ref mWriteOL);
            //Thread.Sleep(5);
            if (Win32Usb.WaitForSingleObjectEx(mIOHandler, Timeout_Read, true) != 0)//读超时
            {
                
                goto TAG_INPUT_PROCESS;//读超时,则跳过缺币检测
            }
            
            //缺币检测(提高扫描速度)
            tmpListI.Clear();
            foreach (KeyValuePair<int, OutBountyElapse> kvp in mOuttingCoinTag)
            {
                if (DateTime.Now.Ticks - kvp.Value.Time > Timeout_Outbounty)//超时放入删除列表
                    tmpListI.Add(kvp.Key);
            }

            foreach (int ctrlIdx in tmpListI)
            {
                tmpRecivePack.Add(new InputPackage(InputPackCmd.LackCoin, ctrlIdx));//发送包
                mOutputBuff[ctrlIdx * LenOuputDataPerCtrller + OfsOutput_OutCoin] = false;//关闭出币口
                mOuttingCoinTag.Remove(ctrlIdx);
            }

            //缺票处理
            tmpListI.Clear();
            foreach (KeyValuePair<int, OutBountyElapse> kvp in mOuttingTicketTag)
            {
                if (DateTime.Now.Ticks - kvp.Value.Time > Timeout_Outbounty)//超时放入删除列表
                    tmpListI.Add(kvp.Key);
            }

            foreach (int ctrlIdx in tmpListI)
            {
                tmpRecivePack.Add(new InputPackage(InputPackCmd.LackTicket, ctrlIdx));
                mOutputBuff[ctrlIdx * LenOuputDataPerCtrller + OfsOutput_OutTicket] = false;//关闭出票口
                mOuttingTicketTag.Remove(ctrlIdx);
            }
 

        TAG_INPUT_PROCESS:
            //接收数据
            mReadOL.Clear(); 
            Win32Usb.ReadFile(mIOHandler, tmpInputBuff, (uint)tmpInputBuff.Length, ref readCount, ref mReadOL);
            //接收包 
            if (Win32Usb.WaitForSingleObjectEx(mIOHandler, Timeout_Read, true) == 0)
            {
                //检测有变化
                #region 检查接受内容是否有变化,没有变化则跳过(这里先对字节循环,而不是比特,提高效率)

                int idxValid = 0;//验证到的索引
                for(idxValid = 0; idxValid != tmpPreInputBuff.Length;++idxValid)
                {
                    if(tmpPreInputBuff[idxValid] != tmpInputBuff[idxValid])
                    {
                        break;
                    }
                }
                if (idxValid == tmpPreInputBuff.Length)//前一帧包与当前包一致
                {
                    //goto TAG_BREAK_INPUT;
                }
                else
                {
                    tmpInputBuff.CopyTo(tmpPreInputBuff, 0);
                }
                #endregion

                tmpBA = new BitArray(tmpInputBuff);
                
                int validDataEnd = LenInputDataPerCtrller * NumCtrller + offsetRead;
                for (int i = offsetRead; i != validDataEnd; ++i)
                {
                    if (tmpBA[i] != mInputBuff[i])
                    {
                        int ctrlIdx = (i - offsetRead) / LenInputDataPerCtrller;
                        InputCmd dataIdx = (InputCmd)InputCmdTrans[((i - offsetRead) % LenInputDataPerCtrller), ctrlIdx % 2];
                        switch (dataIdx)
                        {
                            case InputCmd.Up:
                            case InputCmd.Down:
                            case InputCmd.Left:
                            case InputCmd.Right:
                            case InputCmd.BtnA:
                            case InputCmd.BtnB:
                            case InputCmd.BtnC:
                            case InputCmd.BtnD:
                            case InputCmd.BtnE:
                                {
                                    if (FuncHSThread_AddKeyPress == null)
                                    {
                                        InputPack_Key pack = new InputPack_Key(InputPackCmd.Key, ctrlIdx, dataIdx, tmpBA[i]);
                                        tmpRecivePack.Add(pack);
                                    }
                                    else
                                    {
                                        if (FuncHSThread_AddKeyPress(ctrlIdx, dataIdx , tmpBA[i]))
                                        {
                                            InputPack_Key pack = new InputPack_Key(InputPackCmd.Key, ctrlIdx, dataIdx, tmpBA[i]);
                                            tmpRecivePack.Add(pack);
                                        }
                                    }
                                    
                                    //InputPack_Key pack = new InputPack_Key(InputPackCmd.Key, ctrlIdx, dataIdx, tmpBA[i]);
                                    //tmpRecivePack.Add(pack);
                                }
                                break;
                            case InputCmd.InCoin:
                                {
                                    if (tmpBA[i])
                                    {
                                        InputPackage pack = new InputPackage(InputPackCmd.InsertCoin, ctrlIdx);
                                        tmpRecivePack.Add(pack);
                                    }
                                }
                                break;
                            case InputCmd.OutCoin:
                                {
#if RECORE_INITSTATE
                                    if (tmpBA[i] == mInputBuffInit[i])
#else
                                    if (tmpBA[i])
#endif
                                    {
                                        InputPackage pack = new InputPackage(InputPackCmd.OutCoin, ctrlIdx);
                                        tmpRecivePack.Add(pack);
                                        //重置出币 
                                        mOutputBuff[pack.CtrllerIdx * LenOuputDataPerCtrller + OfsOutput_OutCoin] = false;
                                        mOuttingCoinTag.Remove(pack.CtrllerIdx);
                                    }
                                }
                                break;
                            case InputCmd.OutTicket:
                                {
#if RECORE_INITSTATE
                                    if (tmpBA[i] == mInputBuffInit[i])
#else
                                    if (tmpBA[i])
#endif
                                    {

                                        InputPackage pack = new InputPackage(InputPackCmd.OutTicket, ctrlIdx);
                                        tmpRecivePack.Add(pack);
                                        //重置出票
                                        mOutputBuff[pack.CtrllerIdx * LenOuputDataPerCtrller + OfsOutput_OutTicket] = false;
                                        mOuttingTicketTag.Remove(pack.CtrllerIdx);
                                    }
                                }
                                break;
                        }//switch


                    }//if (tmpBA[i] != mInputBuff[i])判断是否改变了

                }//for (int i = 0; i != validData; ++i)每个有效数据循环,
                //控制板状态
               
                for (int i = OfsInput_ControlBoardState + offsetRead; i != OfsInput_ControlBoardState + offsetRead + NumControlBoard; ++i)
                {
                    if (tmpBA[i] != mInputBuff[i])
                    {
                        tmpRecivePack.Add(new InputPack_CtrlBoardState(InputPackCmd.CtrlBoardConnectState
                        , 0, i - OfsInput_ControlBoardState - offsetRead, tmpBA[i]));//控制板状态
                    }
                    
                }
 
                //Debug.Log("pBA[257]=" + tmpBA[257]);
                //后台按键判断 //上,取消,左,右,小游戏,确定,下
                for (int i = OfsInput_BsKeyStartBit + offsetRead; i != OfsInput_BsKeyEndBit + offsetRead; ++i)
                {
                    if (tmpBA[i] != mInputBuff[i])
                    {
                        InputPack_Key pack = new InputPack_Key(InputPackCmd.Key, 21, (InputCmd)BackStageInputCmdTrans[i - OfsInput_BsKeyStartBit - offsetRead], tmpBA[i]);
                        tmpRecivePack.Add(pack);
                    }
                }

                mInputBuff = tmpBA;
                foreach (InputPackage p in mInputPackageFromPlugin)
                {
                    tmpRecivePack.Add(p);
                }
                mInputPackageFromPlugin.Clear();
                _FlushInputPackToMainThread(tmpRecivePack);
            //TAG_BREAK_INPUT: ;

            }//if (Win32Usb.WaitForSingleObjectEx(mIOHandler, Timeout_Read, true) == 0)//等待读取超时
             
        }//while //线程while循环   
        }//try
        catch (System.TimeoutException)
        {
            Debug.LogError("time out");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("SerialThreadUpdate Exception:" + ex.Message);
        }
        finally
        {
            Debug.Log("thread exit");
        }
    }
 

    void _FlushInputPackToMainThread(List<InputPackage> packLst)
    {
#if MOBILE_EDITION
        return;
#endif

        if (packLst.Count != 0)
        {
            Monitor.Enter(mRecivePack_ThreadLock);
            foreach (InputPackage p in packLst)
                mPackToReciveST.Add(p);
            Monitor.Exit(mRecivePack_ThreadLock);

            packLst.Clear();
        }
    }
    public static string ByteArrayToString(byte[] byteArray)
    {

        string str = "";
        foreach (byte b in byteArray)
        {
            str += string.Format("{0:x2} ", b);
        }
        return str;
    }



    void Handle_Device_Arrival()
    {
#if MOBILE_EDITION
        return;
#endif
        Debug.Log("Handle_Device_Arrival");
        if (FindDevicePath(VID, PID) != "")
        {
            Open();
        }
    }

    void Handle_Device_RemoveComplete()
    {
#if MOBILE_EDITION
        return;
#endif
        Debug.Log("Msg_Device_RemoveComplete");
        if (FindDevicePath(VID, PID) == "")
        {
            Close();
        }
    }

    ////////////////////////////////////////////内部函数/////////////////////////////////////////////////////////////////////////////

    public void OutCoin(uint num, int ctrlIdx)
    {
#if MOBILE_EDITION
        return;
#endif
        OutCoin(ctrlIdx);
    }

    /// <summary>
    /// 出币,,有BUG,不币返回就调用会出现少退现象
    /// </summary>
    public void OutCoin(int ctrlIdx)
    {
#if MOBILE_EDITION
        return;
#endif

        if (!IsOpen())
            return;
        OutputPack_OutBounty pack = new OutputPack_OutBounty(OutputPackCmd.OutCoin, ctrlIdx,true);

        Monitor.Enter(mSendPack_ThreadLock);
        mPackToSendMT.Add(pack);
        Monitor.Exit(mSendPack_ThreadLock); 
    }

    public void OutTicket(uint num, int ctrlIdx)
    {
#if MOBILE_EDITION
        return;
#endif
        OutTicket(ctrlIdx);
    }



    public void OutTicket(int ctrlIdx)
    {
#if MOBILE_EDITION
        return;
#endif
        if (!IsOpen())
            return;
        OutputPack_OutBounty pack = new OutputPack_OutBounty(OutputPackCmd.OutTicket, ctrlIdx, true);

        Monitor.Enter(mSendPack_ThreadLock);
        mPackToSendMT.Add(pack);
        Monitor.Exit(mSendPack_ThreadLock); 
    } 

    public void FlashLight(int ctrlIdx,int lightidx,bool isOn)
    {
#if MOBILE_EDITION
        return;
#endif
        if (!IsOpen())
            return;
        OutputPack_Light pack = new OutputPack_Light(OutputPackCmd.FlashLight, ctrlIdx,lightidx,isOn);

        Monitor.Enter(mSendPack_ThreadLock);
        mPackToSendMT.Add(pack);
        Monitor.Exit(mSendPack_ThreadLock); 
    }

    static string GetDevicePath(IntPtr hInfoSet, ref Win32Usb.DeviceInterfaceData oInterface)
    {
#if MOBILE_EDITION
        return "";
#endif
        uint nRequiredSize = 0;
        // Get the device interface details
        if (!Win32Usb.SetupDiGetDeviceInterfaceDetail(hInfoSet, ref oInterface, IntPtr.Zero, 0, ref nRequiredSize, IntPtr.Zero))
        {
            Win32Usb.DeviceInterfaceDetailData oDetail = new Win32Usb.DeviceInterfaceDetailData();
            oDetail.Size = 5;	// hardcoded to 5! Sorry, but this works and trying more future proof versions by setting the size to the struct sizeof failed miserably. If you manage to sort it, mail me! Thx
            if (Win32Usb.SetupDiGetDeviceInterfaceDetail(hInfoSet, ref oInterface, ref oDetail, nRequiredSize, ref nRequiredSize, IntPtr.Zero))
            {
                return oDetail.DevicePath;
            }
        }
        return null;
    }

    public static string FindDevicePath(int nVid, int nPid)
    {
#if MOBILE_EDITION
        return "";
#endif
        //string strPath = string.Empty;
        string strSearch = string.Format("vid_{0:x4}&pid_{1:x4}", nVid, nPid); // first, build the path search string
        Guid gHid = Win32Usb.HIDGuid;
        //HidD_GetHidGuid(out gHid);	// next, get the GUID from Windows that it uses to represent the HID USB interface
        IntPtr hInfoSet = Win32Usb.SetupDiGetClassDevs(ref gHid, null, IntPtr.Zero, (uint)(Win32Usb.DIGCF_DEVICEINTERFACE | Win32Usb.DIGCF_PRESENT));	// this gets a list of all HID devices currently connected to the computer (InfoSet)
        try
        {
            Win32Usb.DeviceInterfaceData oInterface = new Win32Usb.DeviceInterfaceData();	// build up a device interface data block
            oInterface.Size = Marshal.SizeOf(oInterface);
            // Now iterate through the InfoSet memory block assigned within Windows in the call to SetupDiGetClassDevs
            // to get device details for each device connected
            int nIndex = 0;
            while (Win32Usb.SetupDiEnumDeviceInterfaces(hInfoSet, 0, ref gHid, (uint)nIndex, ref oInterface))	// this gets the device interface information for a device at index 'nIndex' in the memory block
            {

                string strDevicePath = GetDevicePath(hInfoSet, ref oInterface);	// get the device path (see helper method 'GetDevicePath')

                if (strDevicePath.IndexOf(strSearch) >= 0)	// do a string search, if we find the VID/PID string then we found our device!
                {
                    return strDevicePath;
                }
                nIndex++;	// if we get here, we didn't find our device. So move on to the next one.
            }
        }
        catch (Exception ex)
        {

            Debug.LogWarning(string.Format("[警告]打开usbhid设备错误 :{0}WinError:{1:X8}", ex.ToString(), Marshal.GetLastWin32Error()));
            //Console.WriteLine(ex.ToString());
        }
        finally
        {
            // Before we go, we have to free up the InfoSet memory reserved by SetupDiGetClassDevs
            Win32Usb.SetupDiDestroyDeviceInfoList(hInfoSet);
        }
        return "";	// oops, didn't find our device
    }
    
    public void FlashButtomLight(int i)
    {

    }

    /// <summary>
    /// 请求发送大量数据
    /// </summary>
    /// <param name="dataLen"></param>
    /// <param name="sendInterval"></param>
    /// <param name="onOff"></param>
    public void RequestDataBack(byte dataLen, byte sendInterval, bool isOn)
    {
    }

    /// <summary>
    /// 请求MCU信息
    /// </summary>
    public void RequestHardwareInfo()
    {
#if MOBILE_EDITION
        return;
#endif
        if (!IsOpen())
            return;
        OutputPackage pack = new OutputPackage(OutputPackCmd.RequestMCUInfo,0);

        Monitor.Enter(mSendPack_ThreadLock);
        mPackToSendMT.Add(pack);
        Monitor.Exit(mSendPack_ThreadLock); 
    }

    /// <summary>
    /// 请求读写
    /// </summary>
    /// <param name="IsWrite"></param>
    /// <param name="address"></param>
    /// <param name="dataLen"></param>
    /// <param name="data"></param>
    public void RequestReadWrite(bool isWrite, uint address, byte dataLen, byte[] data)
    {
#if MOBILE_EDITION
        return;
#endif
        if (!IsOpen())
            return;

        OutputPack_ReadWriteRequest pack = new OutputPack_ReadWriteRequest(OutputPackCmd.ReadWriteRequest, isWrite, address,dataLen,data);

        Monitor.Enter(mSendPack_ThreadLock);
        mPackToSendMT.Add(pack);
        Monitor.Exit(mSendPack_ThreadLock); 
    }

    /// <summary>
    /// 阻塞读(主线程调用)
    /// </summary>
    /// <param name="address"></param>
    /// <param name="dataLen"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public bool Read_Block(uint address, byte dataLen, out byte[] outVal)
    {
#if MOBILE_EDITION
        outVal = null;
        return false;
#endif
        if (!IsOpen())
        {
            outVal = null;
            return false;
        }

        RequestReadWrite(false, address, dataLen, null);//请求读取信息
        float startTime = Time.realtimeSinceStartup;
        bool isReaded = false;//是否已经读取到信息
        byte[] dataReaded = null;
        
        NemoCtrlIO_EventResultReadWrite delegateResultRead = (bool isW, uint ads, byte dl, byte rc, byte[] d) =>
        {
            //Debug.Log(string.Format("delegateResultRead IsWrite:{0} adress:{1:d} resultCode:{2:d}", IsWrite, ads, resultcode));
            if (!isW && ads == address && rc == 1)
            {
                isReaded = true;
                dataReaded = new byte[dl];
                System.Array.Copy(d, dataReaded, dl);
            }
        };

        EvtResultReadWrite += delegateResultRead;//订阅
        while (!isReaded)
        {
            if (Time.realtimeSinceStartup - startTime > 5F)//时间限制
            {
                Debug.Log("[NemoUsbHid_HardScan]阻塞读超时");
                break;
            }
            RecivePackages();
        }
        EvtResultReadWrite -= delegateResultRead;//退订

        //从mcu读取成功 
        if (isReaded)
            outVal = dataReaded;
        else
            outVal = null;

        return isReaded;

    }
    /// <summary>
    /// 有外部加入的输入Package
    /// </summary>
    /// <param name="p"></param>
    public void AddInputPackage(InputPackage p)
    {
#if MOBILE_EDITION
        return;
#endif
        if (!IsOpen())
            return;
        mInputPackageFromPlugin.Add(p);
    }
    /// <summary>
    /// 改变控制板信息
    /// </summary>
    /// <param name="gameIdx"></param>
    /// <param name="mainVersion"></param>
    /// <param name="subVersion"></param>
    /// <returns></returns>
    bool _UsbThread_ChangeMCUInfo(int gameIdx,int mainVersion,int subVersion)
    {
#if MOBILE_EDITION
        return false;
#endif
        byte[] tmpOutputBuff = new byte[LenOuputBuf];
        byte[] tmpInputBuff = new byte[LenInputBuf];
        //List<InputPackage> tmpRecivePack = new List<InputPackage>();
        byte[] plainDatasFromMCU = new byte[16];//来之MCU的明文
        uint numWrite = 0;
        uint readCount = 0;
        //发出修改MCU请求
        tmpOutputBuff[1] = tmpOutputBuff[tmpOutputBuff.Length - 1] = 0xF1;
        tmpOutputBuff[2] = 1; 
        mWriteOL.Clear();
        Win32Usb.WriteFile(mIOHandler, tmpOutputBuff, (uint)tmpOutputBuff.Length, ref numWrite, ref mWriteOL);
 
        if (Win32Usb.WaitForSingleObjectEx(mIOHandler, Timeout_Read, true) != 0)//返回超时
            return false;
        //MCU返回挑战信息
        mReadOL.Clear();
        Win32Usb.ReadFile(mIOHandler, tmpInputBuff, (uint)tmpInputBuff.Length, ref readCount, ref mReadOL);

        //bool verfySucess = true;
        if (Win32Usb.WaitForSingleObjectEx(mIOHandler, Timeout_Read, true) != 0)//读取超时
            return false;
        if (tmpInputBuff[1] != tmpInputBuff[tmpInputBuff.Length - 1] || tmpInputBuff[1] != 0xF1)//返回验证
            return false;

        //回应MCU的挑战信息,并设置控制板信息
        Array.Copy(tmpInputBuff,2,plainDatasFromMCU,0,plainDatasFromMCU.Length);
        HMACMD5 cryptor = new HMACMD5(System.Text.Encoding.ASCII.GetBytes("FX20120927YIDINGYAOCHANGAEF51FM2"));
        byte[] challengeAnswer = cryptor.ComputeHash(plainDatasFromMCU);
        Array.Clear(tmpOutputBuff, 0, tmpOutputBuff.Length);
        tmpOutputBuff[1] = tmpOutputBuff[tmpOutputBuff.Length - 1] = 0xF1;
        tmpOutputBuff[2] = 2;
        tmpOutputBuff[3] = (byte)gameIdx;//游戏序号
        Array.Copy(System.BitConverter.GetBytes((ushort)mainVersion),0,tmpOutputBuff,4,2);//设置版本号
        Array.Copy(System.BitConverter.GetBytes((ushort)subVersion),0,tmpOutputBuff,6,2);//设置版本号
        Array.Copy(challengeAnswer, 0, tmpOutputBuff, 8, challengeAnswer.Length);
        mWriteOL.Clear();
        Win32Usb.WriteFile(mIOHandler, tmpOutputBuff, (uint)tmpOutputBuff.Length, ref numWrite, ref mWriteOL);

        if (Win32Usb.WaitForSingleObjectEx(mIOHandler, Timeout_Read, true) != 0)//写超时
            return false;

        //等待设置是否成功
        mReadOL.Clear();
        Win32Usb.ReadFile(mIOHandler, tmpInputBuff, (uint)tmpInputBuff.Length, ref readCount, ref mReadOL);
        if (Win32Usb.WaitForSingleObjectEx(mIOHandler, Timeout_Read, true) != 0)//读取超时
            return false;
        if (tmpInputBuff[1] != tmpInputBuff[tmpInputBuff.Length - 1]
            || tmpInputBuff[1] != 0xF1
            || tmpInputBuff[2] != 0x55
            || tmpInputBuff[3] != 0xAA)//返回修改结果
            return false;
        return true;
        
    }


    public void EditMCUInfo(int gameIdx, int mainVersion, int subVersion)
    {
#if MOBILE_EDITION
        return;
#endif
        if (!IsOpen())
            return;

        OutputPack_EditMCUInfo pack = new OutputPack_EditMCUInfo(OutputPackCmd.EditMCUInfo, 0,gameIdx,mainVersion,subVersion);

        Monitor.Enter(mSendPack_ThreadLock);
        mPackToSendMT.Add(pack);
        Monitor.Exit(mSendPack_ThreadLock); 

    }
    public void RecivePackages()
    {
#if MOBILE_EDITION
        return;
#endif
        if (!IsOpen())
            return;

        if (Monitor.TryEnter(mRecivePack_ThreadLock))
        {
            List<InputPackage> tempList = mPackToReciveMT;
            mPackToReciveMT = mPackToReciveST;
            mPackToReciveST = tempList;
            Monitor.Exit(mRecivePack_ThreadLock);
        }
        foreach (InputPackage p in mPackToReciveMT)
        {
            switch (p.Cmd)
            {
                case InputPackCmd.Key:
                    
                    if (mEvtKey != null)
                    {
                        InputPack_Key packKey = (InputPack_Key)p;
                        mEvtKey(p.CtrllerIdx, (NemoCtrlIO_Key)packKey.Key_, packKey.DownOrUp);
                    }
                    break;
                case InputPackCmd.OutCoin:
                    if (mEvtOutCoinReflect != null)
                        mEvtOutCoinReflect(p.CtrllerIdx);
                    break;
                case InputPackCmd.OutTicket:
                    if (mEvtOutTicketReflect != null)
                        mEvtOutTicketReflect(p.CtrllerIdx);
                    break;
                case InputPackCmd.LackCoin:
                    if (mEvtLackCoin != null)
                        mEvtLackCoin(p.CtrllerIdx);
                    break;
                case InputPackCmd.LackTicket:
                    if (mEvtLackTicket != null)
                        mEvtLackTicket(p.CtrllerIdx);
                    break;
                case InputPackCmd.InsertCoin:
                    if (mEvtInsertCoin != null)
                    {
                        mEvtInsertCoin(1, p.CtrllerIdx);
                    }
                    break;

                case InputPackCmd.MCUInfo:
                    if (EvtHardwareInfo != null)
                    {
                        InputPack_MCUInfo pMCUInfo = (InputPack_MCUInfo)p;
                        EvtHardwareInfo(pMCUInfo.GameIdx, pMCUInfo.VersionMain, pMCUInfo.VersionSub, pMCUInfo.VerifySucess);
                    }
                    break;
                case InputPackCmd.CtrlBoardConnectState:
                    if (EvtCtrlBoardStateChanged != null)
                    {
                        InputPack_CtrlBoardState pCtrlBoardPack = (InputPack_CtrlBoardState)p;
                        EvtCtrlBoardStateChanged(pCtrlBoardPack.CtrlBoardId, pCtrlBoardPack.ConectOrDisconect);
                    }
                    break;
                case InputPackCmd.ResultEditMCUInfo:
                    if (EvtResultEditMCU != null)
                    {
                        InputPack_ResultEditMCUInfo op = (InputPack_ResultEditMCUInfo)p;
                        EvtResultEditMCU(op.Result);
                    }
                    break;
                case InputPackCmd.ResultReadWrite:
                    if (EvtResultReadWrite != null)
                    {
                        InputPack_ResultReadWrite op = (InputPack_ResultReadWrite)p;
                        //Debug.Log("EvtResultReadWrite.count = " + EvtResultReadWrite.GetInvocationList().Length);
                        EvtResultReadWrite(op.IsWrite, op.Address, op.DataLength, op.ResultCode, op.Data);
                    }
                    break;

            }
        }
        mPackToReciveMT.Clear();
    }
}

