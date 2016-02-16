using UnityEngine;
using System.Collections;

public class Tester_DatasOverload : MonoBehaviour
{
    private int mDataBackLen = 1;//[����]���󷵻����ݳ���
    private int mDataBackInterval = 10;//[����]���󷵻����ݼ��
    private bool mDataBackOnOff = false;//[����]���ݵ�ǰ�Ƿ��ڷ�����
    private bool mFlashLighting = false;

    IEnumerator _Coro_FlashAllPlayerLight()
    {
        //while (true)
        //{
        //    for (int i = 0; i != Defines.MaxNumPlayer; ++i )
        //    {
        //        GameMain.Singleton.ArcIO.FlashButtomLight(i);
        //    }
        //    yield return 0;
        //}
        yield break;
    }


    void OnGUI()
    {
        GUILayout.BeginHorizontal();
        {
            if (!mDataBackOnOff)
            {
                if (GUILayout.Button("���ش�������[��ʼ]"))
                {
                    //GameMain.Singleton.ArcIO.RequestDataBack((byte)mDataBackLen, (byte)mDataBackInterval, true);
                    mDataBackOnOff = true;
                }
            }
            else
            {
                if (GUILayout.Button("���ش�������[ֹͣ]"))
                {
                    //GameMain.Singleton.ArcIO.RequestDataBack((byte)mDataBackLen, (byte)mDataBackInterval, false);
                    mDataBackOnOff = false;
                }
            }

            GUILayout.Label("����(byte)");
            GUILayout_IntArea(ref mDataBackLen);
            GUILayout.Label("���(ms)");
            GUILayout_IntArea(ref mDataBackInterval);
        }
        GUILayout.EndHorizontal();


        if (mFlashLighting ? GUILayout.Button("Ƶ��������������[�ر�]") : GUILayout.Button("Ƶ��������������[��ʼ]"))
        {
            if (mFlashLighting)
            {
                StopCoroutine("_Coro_FlashAllPlayerLight");
                mFlashLighting = false;
            }
            else
            {
                StartCoroutine("_Coro_FlashAllPlayerLight");
                mFlashLighting = true;
            }
        }
    }

    private bool GUILayout_IntArea(ref int val)
    {
        int oldVal = val;
        string valStr = GUILayout.TextArea(val.ToString(), GUILayout.MinWidth(50F));
        int newVal = int.Parse(valStr);
        if (newVal != oldVal)
        {
            val = newVal;
            return true;
        }
        return false;

    }
}
