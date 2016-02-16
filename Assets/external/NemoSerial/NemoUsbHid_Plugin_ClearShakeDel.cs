using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NemoUsbHid_Plugin_ClearShakeDel : MonoBehaviour {

    class KeyData //: IEqualityComparer<KeyData>
    {
        public int CtrllerIdx;
        public NemoUsbHid_HardScan.InputCmd InputKey;


        public override bool Equals(object obj)
        {
            if (obj==null||GetType() != obj.GetType())
                return false;

            return (CtrllerIdx == ((KeyData)obj).CtrllerIdx) && (InputKey == ((KeyData)obj).InputKey);
        }
        public override int GetHashCode()
        {
            return (CtrllerIdx + 1) * 100 + (int)InputKey;
        }
        
    }

    class ValData
    {
        public bool KeyStateStart = false;//����״̬(down == true / up == false)
        public bool KeyStateLast = false;//�����µİ���״̬(down == true / up == false)
        public long TimeStartDelayTrigger;//��ʼ��ʱ������ʱ��(��λ:tick)
    }

    public int TimeIgnoreInputMS = 20;//tick 30*10000 (1����=10000tick)
    private Dictionary<KeyData, ValData> mInputKeyTimeDict;//����ʱ���״̬�²ż�����ֵ�

    private KeyData mKeyDataBuff;
    private ValData mValBuff;
    private List<KeyValuePair<KeyData, ValData>> mDeleteKeyDataInUpdateLoop;
    private int mTimeIgnoreInputTick;
    private NemoUsbHid_HardScan mUsbHid;

    // Use this for initialization
    void Awake()
    {
        mInputKeyTimeDict = new Dictionary<KeyData, ValData>();

        mUsbHid = GetComponent<NemoUsbHid_HardScan>();
        mUsbHid.FuncHSThread_AddKeyPress += Func_InInvokeKeyEvent;
        mUsbHid.Evt_HSThread_FrameStart += Handle_HSThread_Update;

        mKeyDataBuff = new KeyData();
        //mValBuff = new ValData();
        mDeleteKeyDataInUpdateLoop = new List<KeyValuePair<KeyData, ValData>>();


        mTimeIgnoreInputTick = TimeIgnoreInputMS * 10000;


        //
        //mKeyDataBuff.CtrllerIdx = 1;
        //mKeyDataBuff.InputKey = NemoUsbHid_HardScan.InputCmd.BtnA;
        //Debug.Log(mInputKeyTimeDict.TryGetValue(mKeyDataBuff, out mValBuff));
        //KeyData kd = new KeyData();
        //kd.CtrllerIdx = 1;
        //kd.InputKey = NemoUsbHid_HardScan.InputCmd.BtnA;
        //ValData vd = new ValData();
        //vd.KeyStateStart = vd.KeyStateLast = true;
        //vd.TimeStartDelayTrigger = System.DateTime.Now.Ticks;
        //mInputKeyTimeDict.Add(kd, vd);


        //mKeyDataBuff.CtrllerIdx = 2;
        //mKeyDataBuff.InputKey = NemoUsbHid_HardScan.InputCmd.BtnA;
        //Debug.Log(mInputKeyTimeDict.TryGetValue(mKeyDataBuff, out mValBuff));
    }


    bool Func_InInvokeKeyEvent(int ctrllerIdx, NemoUsbHid_HardScan.InputCmd key, bool keyState)
    {
        mKeyDataBuff.CtrllerIdx = ctrllerIdx;
        mKeyDataBuff.InputKey = key;

        //Debug.Log("Func_InInvokeKeyEvent keyState = " + keyState);

        if (mInputKeyTimeDict.TryGetValue(mKeyDataBuff, out mValBuff))
        {
            //Debug.Log("mInputKeyTimeDict.TryGetValue;");
            mValBuff.KeyStateLast = keyState;
        }
        else
        {
            KeyData kd = new KeyData();
            kd.CtrllerIdx = ctrllerIdx;
            kd.InputKey = key;

            ValData vd = new ValData();
            vd.KeyStateStart = vd.KeyStateLast = keyState;
            vd.TimeStartDelayTrigger = System.DateTime.Now.Ticks;


            mInputKeyTimeDict.Add(kd, vd);
            //Debug.Log("mInputKeyTimeDict.Add(kd, vd);");
        }

        return false;
    }



    void Handle_HSThread_Update()
    {
        foreach (KeyValuePair<KeyData, ValData> kv in mInputKeyTimeDict)
        {
            if (System.DateTime.Now.Ticks - kv.Value.TimeStartDelayTrigger > mTimeIgnoreInputTick)//������ʱʱ��
                
            {
                if (kv.Value.KeyStateLast == kv.Value.KeyStateStart)//��ʼ�����İ���һ��
                {
                    NemoUsbHid_HardScan.InputPack_Key pack = new NemoUsbHid_HardScan.InputPack_Key(NemoUsbHid_HardScan.InputPackCmd.Key, kv.Key.CtrllerIdx, kv.Key.InputKey, kv.Value.KeyStateLast);
                    mUsbHid.AddInputPackage(pack);
                    //Debug.Log("mUsbHid.AddInputPackage(pack);");
                }
                
                mDeleteKeyDataInUpdateLoop.Add(kv);
            }
        }


        foreach (KeyValuePair<KeyData, ValData> kv in mDeleteKeyDataInUpdateLoop)
        {
            
            mInputKeyTimeDict.Remove(kv.Key);
        }
        mDeleteKeyDataInUpdateLoop.Clear();
    }

}
