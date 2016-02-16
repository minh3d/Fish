using UnityEngine;
using System.Collections;
using System.Collections.Generic;
/// <summary>
/// ��Ⱥƽ����Ļ
/// </summary>
/// <remarks>
///  ����:
///    1.�ƶ�������transform.right
///    2.ԭ����Ҫ����Ⱥ���,��transform.position����
/// </remarks>
public class ScenePrelude_Transition : ScenePrelude
{
    [System.Serializable]
    public class EmitData
    {
        public Transform TsShoalOfFish;
        public FishGenerateWhenEnterWorld LastFishEnterWorld;
        public Transform[] TsPosStart;//��ʼλ��(ά������Ļ����)//todo:����ʹ��gamemain.worldDimemsion�����
    }
    public EmitData[] EmitDatas;

    //public Transform[] TsShoalOfFish;
    public float Speed = 1F;
    //public FishGenerateWhenEnterWorld[] LastFishEnterWorld;//�������������(������TsShoalOfFishһһ��Ӧ)
    public bool IsDepthAdvanceAuto = true;//�Զ���ȵ���
    private bool mIsEnded = false;

    public override void Go() 
    {
        StartCoroutine(_Coro_Transiting());
        StartCoroutine(_Coro_WaitNullFish());
    }

    public IEnumerator _Coro_Transiting()
    {
        //���ÿ�ʼλ��
        for (int i = 0; i != EmitDatas.Length; ++i)
        {
            EmitDatas[i].TsShoalOfFish.localPosition = EmitDatas[i].TsPosStart[GameMain.Singleton.ScreenNumUsing - 1].localPosition;
        }

        if (IsDepthAdvanceAuto)
        {
            for (int i = 0; i != EmitDatas.Length; ++i)
            {
                float depth = EmitDatas[i].TsShoalOfFish.GetChild(0).localPosition.z;
                Vector3 posTmp;
                foreach (Transform t in EmitDatas[i].TsShoalOfFish)
                {
                    posTmp = t.localPosition;
                    posTmp.z = depth;
                    depth -= Defines.OffsetAdv_FishGlobleDepth;
                    t.localPosition = posTmp;
                    
                }

            }
        }
        //��ʼ�ƶ�
        Transform ts;
        while (true)
        {
            int nullNum = 0;
            for (int i = 0; i != EmitDatas.Length; ++i)
            {
                if (EmitDatas[i].TsShoalOfFish == null)
                {
                    ++nullNum;
                    continue;
                }

                ts = EmitDatas[i].TsShoalOfFish;
                ts.position += ts.right * Speed * Time.deltaTime;
                if ((ts.right.x > 0F && ts.position.x > GameMain.Singleton.WorldDimension.xMax)//���󲢴ﵽ�����Ļ��
                    || (ts.right.x <= 0F && ts.position.x < GameMain.Singleton.WorldDimension.x))//�����ƶ����ﵽ�ұ���Ļ��
                {
                    List<Fish> fishToClear = new List<Fish>();

                    foreach (Transform tChild in EmitDatas[i].TsShoalOfFish )
                    {
                        Fish f = tChild.GetComponent<Fish>();
                        if (f != null && f.Attackable)
                        {
                            fishToClear.Add(f);
                        }
                    }
                    foreach (Fish f in fishToClear)
                    {
                        f.Clear();
 
                    }

                    Destroy(EmitDatas[i].TsShoalOfFish.gameObject);
                    EmitDatas[i].TsShoalOfFish = null;
                }
            }



            if (nullNum == EmitDatas.Length)
            {
                EndPrelude();
            }

            yield return 0;
        }

    }

    
    public IEnumerator _Coro_WaitNullFish()
    {
        int numLastFishEnterWorld = 0;
        foreach (EmitData ed in EmitDatas)
        {
            ed.LastFishEnterWorld.EvtFishGenerated += (Fish f) =>
            {
                ++numLastFishEnterWorld;
                //Debug.Log("lastFishGenerated");
            };
        }

        while (numLastFishEnterWorld != EmitDatas.Length)
        {
            yield return 0;
        }
        //Debug.Log("waitZeroFish");

        while (GameMain.Singleton.NumFishAlive != 0)
        {
            yield return 0;
        }

        EndPrelude();
    }

    void EndPrelude()
    {
        if (!mIsEnded)
        {
            mIsEnded = true;
            if (Evt_PreludeEnd != null)
                Evt_PreludeEnd();
            GameMain.Singleton.FishGenerator.KillAllImmediate();//��Destroy(gameObject);֮ǰɾ�������ڳ���,��ֹ©ɾ
            Destroy(gameObject);
        }
    }
}
