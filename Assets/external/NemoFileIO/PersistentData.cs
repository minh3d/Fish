#define ENABLE_MASK
//#if UNITY_EDITOR1//�༭ģʽ�´�д���ݿ�
    #define ENABLE_WRITE_DB//��д�����ݿ�(ֻд��FARM,���Ч��)
//#endif
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

/// <summary>
/// �־����ݷ�װ
/// </summary>
/// <remarks>��Ҫ�����ǵ�һ�ζ�ȡʱ��ȡӲ��,֮��Ͷ�ȡ�ڴ�,������ÿ�ζ���ȡӲ��</remarks>
/// <typeparam name="ValType">���ֵ����</typeparam>
/// <typeparam name="StoreType">�洢������</typeparam>
/// <remark>
/// ע��:
///     1.��֤�ļ�����������һ���߳��н���,
/// </remark>
public class PersistentData<ValType, StoreType>  
//where ValType : StoreType ,IEnumerable ,IEnumerator
{
    private HotFileDatabaseIO<StoreType> mDataInFile;
    private bool mHaveReaded = false;//�Ѿ���Ӳ�̶�ȡ��
    private ValType mVal;
//#if ENABLE_WRITE_DB
    private bool mIsRequestingWrite = false;
//#endif
    private string mName;
    public PersistentData(string name)
    {

        string path = Directory.GetCurrentDirectory() + "/DataFiles/HotRecord/";
        mDataInFile = new HotFileDatabaseIO<StoreType>(name, path);
        mName = name;
        mVal = Mask(Val);
    }
    public ValType Mask(ValType val)
    {
#if ENABLE_MASK
        if (typeof(StoreType) == typeof(int))
        {
            return (ValType)(System.Object)((int)(System.Object)(val) ^ 0x7129A9AD);
        }
#endif
        return val;
    }

    
    public ValType Val
    {
        get
        {
            if (!mHaveReaded)//��һ����Ҫ���ļ�
            {
                //mVal = (ValType)(System.Object)mDataInFile.Read();
                mVal = (ValType)(System.Object)mDataInFile.Read();
                
                mHaveReaded = true;
            }
            //Debug.Log(" get Val = " + mVal + "   maskValue =" + Mask(mVal) + "  type = " + typeof(ValType) + "  mName=" + mName);
            return Mask(mVal);//UnMask
        }

        set
        {
            //mVal = value;
            //return;
            if ((System.Object)mVal != (System.Object)Mask(value))
            {
                //Debug.LogWarning(" set Val = " + value + "   maskValue =" + Mask(value) + "  type = " + typeof(ValType) + "  mName=" + mName);
                mVal = Mask(value);
                //mVal = value;
                //mDataInFile.WriteToFRAM((StoreType)(System.Object)mVal);//��Ϊ����д��
        
            //#if ENABLE_WRITE_DB 
                if (!mIsRequestingWrite)
                {
                    mIsRequestingWrite = true;
                    PersistentDataCoroWriter.CommitWriteRequest(Handle_WritePermission);
                }
            //#endif
                
            }

        }
    }

    //�õ������
    void Handle_WritePermission()
    {
        //return;
#if ENABLE_WRITE_DB
        mDataInFile.FlushWrite((StoreType)(System.Object)mVal);
#endif
        //mDataInFile.WriteToFRAM((StoreType)(System.Object)mVal);
        mIsRequestingWrite = false;
    }


    /// <summary>
    /// ������ֵ������д��Ӳ��
    /// </summary>
    /// <param name="val"></param>
    public void SetImmdiately(ValType val)
    {
        //mVal = val;
        //return;
        if ((System.Object)mVal != (System.Object)Mask(val))
        {
            mVal = Mask(val);
            //mVal = val;
            //mDataInFile.WriteToFRAM((StoreType)(System.Object)mVal);//��Ϊ����д��
#if ENABLE_WRITE_DB 
            mDataInFile.FlushWrite((StoreType)(System.Object)mVal);
#endif
        }
    }
}