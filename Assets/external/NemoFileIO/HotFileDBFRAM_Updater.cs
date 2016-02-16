using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// dbFRAM������(���м��ܰ�����ʱ,��DB_GUID��FRAM_GUID��һ��ʱ��������DB���ݵ�FRAM)
/// </summary>
public class HotFileDBFRAM_Updater : MonoBehaviour {
    public interface IUpdatable
    {
        void UpdateToFRAM();
    }

    public static HotFileDBFRAM_Updater Singleton;
    public List<IUpdatable> Updatables;
    private INemoControlIO mMCU;
    
	// Use this for initialization
	public static void Reg (IUpdatable i) 
    {
        if (Singleton == null)
        {
            GameObject go = new GameObject("HotFileDBFRAM_Updater");
            Singleton = go.AddComponent<HotFileDBFRAM_Updater>();

            Singleton.Updatables = new List<IUpdatable>();
            Singleton.mMCU = INemoControlIOSinglton.Get();
            Singleton.mMCU.EvtOpened += Singleton.Handle_MCU_Connect;
        }

        Singleton.Updatables.Add(i);

	}
	
    public void Handle_MCU_Connect()
    {
        //��FRAM���guid
        byte[] guidFRAMByte = null;
        if (mMCU.Read_Block(0, 16, out guidFRAMByte))//��FRAM��ȡguid�ɹ�
        {
            Debug.Log("open guid:" + NemoSerial.ByteArrayToString(StaticValueContainerDBFRAM.GetDBID().ToByteArray()));
            System.Guid FRAMEGUID = new System.Guid(guidFRAMByte);
            if (StaticValueContainerDBFRAM.GetDBID() != System.Guid.Empty
                && FRAMEGUID != StaticValueContainerDBFRAM.GetDBID())//FRAM��db��guid��һ��
            {
                Debug.Log("open guid��һ��:" + NemoSerial.ByteArrayToString(StaticValueContainerDBFRAM.GetDBID().ToByteArray()));
                mMCU.RequestReadWrite(true, 0, 16, StaticValueContainerDBFRAM.GetDBID().ToByteArray());//��dbidд��FRAM
                //��������IUpdatable
                foreach (IUpdatable i in Updatables)
                {
                    i.UpdateToFRAM();
                }
                
            }
        }

        //if(StaticValueContainerDBFRAM.GetDBID() 
    }
}
