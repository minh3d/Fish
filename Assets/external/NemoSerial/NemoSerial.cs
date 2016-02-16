//#define  TestSerial

using System;
using System.IO.Ports; 
using System.Threading;
using UnityEngine;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// �ֻ����ư� �������������,(��Ӧһ����ư�)
/// </summary>
/// <remarks>
/// ע��:
///     1.��UNITY3D��ʹ�ø���,��Ҫ��PlayerSetting��,Api Compatibility Level��Ϊ .NET(��Ϊ.Net Sub�в�����System.IO.Port)
///     2.����UNITY3Dʹ��MONO,���´��ڿ������쳣,��֪������:
///         a.���ܴ������ڿⱾ��Ļص�����:DataReceived,ErrorReceived
///         b.�շ����ݱ�����ͬһ�߳���,���߻���ִ���.
/// </remarks>
/// 
public class NemoSerial : MonoBehaviour
{

    public class Package
    {
        public byte[] data;
        
    }
     
    public delegate void RecivePackage(Package pack);
    public RecivePackage HandleRecivePackage;

    /// <summary>
    /// ���������
    /// </summary>
    /// <param name="byteReaded"></param>
    /// <returns></returns>
    public delegate List<Package> Handle_SetupPackage(byte[] byteReaded);
    public Handle_SetupPackage HandleSetupPackage;
#if TestSerial
    public delegate Package Handle_SetupPackageT(byte[] byteReaded);
    public Handle_SetupPackageT HandleSetupPackageT;
    private Thread mTestSerialThread;//���Է������߳�
#endif

    public string PortName = "COM1";
    public int BaudRate = 9600;
    public int BytesPerPackage = 7;//ÿһ��������(�����ٸ�byte)
    private SerialPort mSerialPort;
 
  
    private Thread mSerialThread;   // �����߳�
    private bool mSerialThreadLoop = true;

   
    private byte[] mReadBuffer; // ��ȡbuf 

    private System.Object mSendPack_ThreadLock;  // ������
    private System.Object mRecivePack_ThreadLock;  // �հ���
 
    private static int READ_BUFFER_SIZE = 1024;

 
    private List<Package> mPackToSendMT;//���߳�(Main Thread)���ݷ�������
    private List<Package> mPackToSendST;//�����߳�(SerialPort Thread)���ݷ�������

    private List<Package> mPackToReciveMT;//���߳�(Main Thread)���ݽ��ն���
    private List<Package> mPackToReciveST;//�����߳�(SerialPort Thread)���ݽ��ն���


    Package mPackAutoSend ; 
    private int mAutoSendTime;
    private int mAutoSendInterval = -1;
    /// <summary>
    /// Unity�ر�ʱ����
    /// </summary>
    void OnDestroy()
    {
        

        if (IsOpen())
        {
            Close();
        }
    }

    /// <summary>
    /// Unity����
    /// </summary>
    void Update()
    {
        MainThreadUpdate();
    }
 
    void Start()
    {
        if (mSerialPort == null)
            mSerialPort = new SerialPort();
        if (mSendPack_ThreadLock == null)
            mSendPack_ThreadLock = new System.Object();
        if (mRecivePack_ThreadLock == null)
            mRecivePack_ThreadLock = new System.Object();

        if (mPackToSendMT == null)
            mPackToSendMT = new List<Package>();
        if (mPackToSendST == null)
            mPackToSendST = new List<Package>();

        if (mPackToReciveMT == null)
            mPackToReciveMT = new List<Package>();
        if (mPackToReciveST == null)
            mPackToReciveST = new List<Package>();
 
    }
  
    /// <summary>
    /// �� portNo���ں�
    /// </summary>
    /// <param name="portNo">���ں�</param>
    /// <returns>�Ƿ�򿪳ɹ�</returns>
    public bool Open()
    {
        if (mSerialPort == null)
            mSerialPort = new SerialPort();

        if (mSerialPort.IsOpen)
            return true;

        mSerialPort.PortName = PortName;
        mSerialPort.BaudRate = BaudRate;
        mSerialPort.Parity = Parity.None;
        mSerialPort.DataBits = 8;
        mSerialPort.StopBits = StopBits.One;
        mSerialPort.ReadTimeout = 2;
        mSerialPort.ReadBufferSize = READ_BUFFER_SIZE;  // ��C#Ĭ��ֵ��һ����

 
        if (mSendPack_ThreadLock == null)
            mSendPack_ThreadLock = new System.Object();
        if (mRecivePack_ThreadLock == null)
            mRecivePack_ThreadLock = new System.Object();

        if (mPackToSendMT == null)
            mPackToSendMT = new List<Package>();
        if (mPackToSendST == null)
            mPackToSendST = new List<Package>();

        if (mPackToReciveMT == null)
            mPackToReciveMT = new List<Package>();
        if (mPackToReciveST == null)
            mPackToReciveST = new List<Package>();
   
#if TestSerial
        mSerialThreadLoop = true;
              ThreadStart ts1 = new ThreadStart(Test_ThreadUpdate);
        mTestSerialThread = new Thread(ts1);
        mTestSerialThread.Start();
#endif
  

        try
        {
    
            mSerialPort.Open();

            mSerialThreadLoop = true;
    
            mReadBuffer = new byte[READ_BUFFER_SIZE];

            ThreadStart ts = new ThreadStart(SerialThreadUpdate);

            mSerialThread = new Thread(ts);
            mSerialThread.Start();
   

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Open Serialport Error."+e);
            return false;
        }
    }
#if TestSerial

    void Test_ThreadUpdate()
    {
        byte[] heartBeatDatas = new byte[] {   0xFB, 0, 0x21, 0, 3, 0, 4, 0, 0x21, 0xFB, 0, 0x21, 0, 3, 0, 4, 1, 0x21
                                                                            ,0xFB, 0, 0x21, 0, 3, 1, 4, 0, 0x21, 0xFB, 0, 0x21, 0, 3, 1, 4, 1, 0x21
                                                                            ,0xFB, 0, 0x21, 0, 3, 2, 4, 0, 0x21, 0xFB, 0, 0x21, 0, 3, 2, 4, 1, 0x21
                                                                            ,0xFB, 0, 0x21, 0, 3, 3, 4, 0, 0x21, 0xFB, 0, 0x21, 0, 3, 3, 4, 1, 0x21
                                                                            ,0xFB, 0, 0x21, 0, 3, 4, 4, 0, 0x21, 0xFB, 0, 0x21, 0, 3, 4, 4, 1, 0x21
                                                                            ,0xFB, 0, 0x21, 0, 3, 5, 4, 0, 0x21, 0xFB, 0, 0x21, 0, 3, 5, 4, 1, 0x21
                };
        //int preSendIdx = 0;
        int curSendIdx = 0;
        System.Random rnd = new System.Random();
        while (mSerialThreadLoop)
        {
            //Debug.Log("package setuping:");
            //for (byte playerI = 0; playerI != Defines.MaxNumPlayer; ++playerI)
            //{
            try
            {

               
                //���հ�
                //int readCount = mSerialPort.Read(mReadBuffer, 0, READ_BUFFER_SIZE);//һ���볬ʱ,������
                //Debug.Log("package setuping:"  );
                if (HandleSetupPackageT != null)
                {
                    int lenSend = rnd.Next(3, 25);
                    //int curSendIdx = preSendIdx
                    byte[] dataToSend = new byte[lenSend];
                    for (int i = 0; i != lenSend; ++i)
                    {
                        dataToSend[i] = heartBeatDatas[(curSendIdx + i) % heartBeatDatas.Length];
                    }
                    curSendIdx += lenSend;

                    Package p = HandleSetupPackageT(dataToSend);

                    if (p != null)
                    {
                        Monitor.Enter(mRecivePack_ThreadLock);
              
                        mPackToReciveST.Add(p);
                        Monitor.Exit(mRecivePack_ThreadLock);
                    }
                    
                }
                //Debug.Log("TestLooping!");
            }
            catch (System.Exception e)
            {
               //Debug.LogError("  Serialport Error."+e);
            }
            Thread.Sleep(10); 
        }
    }
#endif
    // �ر�
    public void Close()
    {
#if TestSerial


        Debug.Log("close");
        mSerialThreadLoop = false;
        mTestSerialThread.Join();
#endif

        if (IsOpen())
        {
            mSerialThreadLoop = false;
            //mSerialThread.Abort();
            mSerialThread.Join();
            //if(!)//�ú���������2��,
            //{
            //    Debug.LogError("Close SerialThread Erro!");
            //}
            mSerialPort.DiscardInBuffer();
            mSerialPort.DiscardOutBuffer();
            mSerialPort.Close();
            mSerialPort.Dispose();
        }
    }

    //private int debugMainThreadLoopNum = 0;
    /// <summary>
    /// ���߳�Update(һ�����ⲿ�̵߳���)
    /// <param name = "delta">��һ֡ʹ��ʱ��</param>
    /// </summary>
    public void MainThreadUpdate(/*float delta*/)
    {
        
       

        if (Monitor.TryEnter(mRecivePack_ThreadLock))
        {
            List<Package> tempList = mPackToReciveMT;
            mPackToReciveMT = mPackToReciveST;
            mPackToReciveST = tempList;
            Monitor.Exit(mRecivePack_ThreadLock);
        }


        foreach(Package p in mPackToReciveMT)
        {
            HandleRecivePackage(p);
        }
        mPackToReciveMT.Clear();
    }



    /// <summary>
    /// �����߳�,�����շ�����,
    /// </summary>
    private void SerialThreadUpdate()
    {
        while (mSerialThreadLoop&&IsOpen())
        {
            try
            {
                // ���Ͱ�
                mPackToSendST.Clear();
                List<Package> tempList = mPackToSendST;

                if (Monitor.TryEnter(mSendPack_ThreadLock))
                {
                    mPackToSendST = mPackToSendMT;
                    mPackToSendMT = tempList;
                    Monitor.Exit(mSendPack_ThreadLock);
                }
 

                foreach (Package dtw in mPackToSendST)
                {
                    mSerialPort.Write(dtw.data, 0, dtw.data.Length);
                }

                //�Զ�����
                if (mAutoSendInterval != -1 && Monitor.TryEnter(mSendPack_ThreadLock))
                {
                    //foreach (Package p in mPackToAutoSendST)
                    mAutoSendTime += mSerialPort.ReadTimeout+1;
                    if (mAutoSendTime > mAutoSendInterval)
                    {
                        mSerialPort.Write(mPackAutoSend.data, 0, mPackAutoSend.data.Length);
                        mAutoSendTime = 0;
                    }
                    
                    //mPackToAutoSendMT[i].autoSendTime = 0;
                
                    Monitor.Exit(mSendPack_ThreadLock);
                } 
                
                //Debug.Log("d1," + mReadBuffer.Length+"  mReadBufCount="+mReadBufCount+" READ_BUFFER_SIZE="+READ_BUFFER_SIZE);
            
                //���հ�
                int readCount = mSerialPort.Read(mReadBuffer, 0, READ_BUFFER_SIZE);//һ���볬ʱ,������

                if (HandleSetupPackage != null )
                {

                    byte[] paramDatas = new byte[readCount];
                    Array.Copy(mReadBuffer, paramDatas, readCount);
                    List<Package> packages = HandleSetupPackage(paramDatas);

                    if (packages.Count != 0 )
                    {
                        Monitor.Enter(mRecivePack_ThreadLock);
                        foreach (Package p in packages)
                            mPackToReciveST.Add(p);
                        Monitor.Exit(mRecivePack_ThreadLock);
                    }

                     
                    //���� start:
                    //byte[] paramDatas1 = new byte[] { 0xFB, 0x00, 0x11, 0x00, 0x03 };
                    //byte[] paramDatas2 = new byte[] { 0x01, 0x02, 0x03, 0x11, 0xf2 ,0xff,0x00};
                    //Package p = HandleSetupPackage(paramDatas1);
                    //if (p != null)
                    //{
                    //    Monitor.Enter(mRecivePack_ThreadLock);
                    //    mPackToReciveST.Add(p);
                    //    Monitor.Exit(mRecivePack_ThreadLock);
                    //}
                    //p = HandleSetupPackage(paramDatas2);
                    //if (p != null)
                    //{
                    //    Monitor.Enter(mRecivePack_ThreadLock);
                    //    mPackToReciveST.Add(p);
                    //    Monitor.Exit(mRecivePack_ThreadLock);
                    //}
                    //Thread.Sleep(2000);
                    //����end:


              
                   
                }
                ////���հ�
                //int readCount = mSerialPort.Read(mReadBuffer, mReadBufCount, READ_BUFFER_SIZE - mReadBufCount);//һ���볬ʱ,������
                //mReadBufCount += readCount;
                //if (mReadBufCount >= BytesPerPackage)
                //{
                //    int numPack = mReadBufCount / BytesPerPackage;

                //    for (int i = 0; i != numPack; ++i)
                //    {
                //        Package p;
                //        p.data = new byte[BytesPerPackage];
                //        for (int j = 0; j != BytesPerPackage; ++j)//��ֵ��
                //        {
                //            p.data[j] = mReadBuffer[(i * BytesPerPackage) + j];
                //        }

                //        //Debug.Log("p = " + p.data[0]);
                //        Monitor.Enter(mRecivePack_ThreadLock);
                //        mPackToReciveST.Add(p);
                //        Monitor.Exit(mRecivePack_ThreadLock);
                //    }

                //    mReadBufCount = mReadBufCount - numPack * BytesPerPackage;
                //    //��ʣ���������������ǰ��
                //    if (mReadBufCount > 0)
                //        Array.Copy(mReadBuffer, numPack * BytesPerPackage, mReadBuffer, 0, mReadBufCount);
                //}
            }
            catch (System.TimeoutException)
            {
            }
            catch (System.Exception ex)
            {
                Debug.LogError("SerialThreadUpdate Exception:" + ex.Message);
            }
            finally
            {
                Thread.Sleep(1);
               
            }
            
        }
 
    }

    public static string ByteArrayToString(byte[] byteArray)
    {
        string str = "";
        foreach (byte b in byteArray)
        {
            str += string.Format("{0:x} ", b);
        }
        return str;
    }

    // �Ƿ��
    public bool IsOpen()
    {
        return mSerialPort.IsOpen;
    }



   
    ////////////////////////////////////////////�ڲ�����/////////////////////////////////////////////////////////////////////////////


    // ��������
    public void Send(byte[] data)
    {
        Package dtw = new Package();
        dtw.data = data;
        Monitor.Enter(mSendPack_ThreadLock);
        mPackToSendMT.Add(dtw);
        Monitor.Exit(mSendPack_ThreadLock);

     }

    public void AutotoSend(byte[] data, int interval)
    {
        Package dtw = new Package();
        dtw.data = data;
        mAutoSendInterval = interval;
        Monitor.Enter(mSendPack_ThreadLock);
        mPackAutoSend = dtw;
        Monitor.Exit(mSendPack_ThreadLock);
        
    }
}

