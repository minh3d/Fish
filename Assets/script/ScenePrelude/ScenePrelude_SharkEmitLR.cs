using UnityEngine;
using System.Collections;

public class ScenePrelude_SharkEmitLR : ScenePrelude
{
    public Fish Prefab_LaJiao;
    public Fish[] Prefab_Sharks;

    public float IntervalGenerateSharkMin = 0.5F;//���������� ����
    public float IntervalGenerateSharkMax = 1.5F;//���������� ���
    public float AngleGenerateShark = 20F;//��������Ƕ�
    public float SpeedShark = 0.2F;//�����ƶ��ٶ�
    public float TimeEmitShark = 10F;//��������ʱ��

    public float EmitLaJiaoDelay = 1F;//���������ӳ�
    public float IntervalEmitLaJiao = 1F;//���������ӳ�
    public float RoadToLeftRightSpace = 0.1F;//�����������Ҽ��
    public float SpeedLaJiao = 0.3F;//�����ƶ��ٶ�
    public float TimeLimit = 54F;//ʱ������

    //private float mMaxSharkRadius = 0F;
    private bool mIsEnded = false;
    private float mDepthOffset;
    public override void Go()
    {
        StartCoroutine(_Coro_Process());
        StartCoroutine(_Coro_TimeCountdown());
    }

    void EndPrelude()
    {
        if (!mIsEnded)
        {
            mIsEnded = true;
            if (Evt_PreludeEnd != null)
                Evt_PreludeEnd();
            GameMain.Singleton.FishGenerator.KillAllImmediate();
            Destroy(gameObject);
        }
    }

    IEnumerator _Coro_TimeCountdown()
    {
        yield return new WaitForSeconds(TimeLimit);
        EndPrelude();
    }

    IEnumerator _Coro_Process()
    {
        yield return 0;
        StartCoroutine("_Coro_LaJiaoGenerating");
//         //����������뾶
//         foreach (Fish f in Prefab_Sharks)
//         {
//             if (f.swimmer.BoundCircleRadius > mMaxSharkRadius)
//                 mMaxSharkRadius = f.swimmer.BoundCircleRadius;
//         }

        //������ұ�
        bool isLeft = Random.Range(0, 2) == 0;
        

        float elapse = 0F;
        int emitSharkIdx = 0;
        while (elapse < TimeEmitShark)
        {

           
            Fish shark = Instantiate(Prefab_Sharks[emitSharkIdx]) as Fish;
            Vector3 generatorPot = new Vector3(isLeft ? GameMain.Singleton.WorldDimension.x - shark.swimmer.BoundCircleRadius
                     : GameMain.Singleton.WorldDimension.xMax + shark.swimmer.BoundCircleRadius
                 , 0F, 0F);

            emitSharkIdx = (emitSharkIdx + 1) % Prefab_Sharks.Length;
            shark.ClearAI();
            shark.swimmer.Speed = SpeedShark;

            shark.transform.parent = transform;
            shark.transform.rotation =  Quaternion.AngleAxis((isLeft?0F:180F)+Random.Range(-AngleGenerateShark,AngleGenerateShark),Vector3.forward);
            generatorPot.z = Defines.GlobleDepth_FishBase + mDepthOffset;
            mDepthOffset -= Defines.OffsetAdv_FishGlobleDepth;
            shark.transform.localPosition = generatorPot;
            
            shark.swimmer.Go();

            float delta = Random.Range(IntervalGenerateSharkMin, IntervalGenerateSharkMax);
            elapse += delta;
            yield return new WaitForSeconds(delta);
        }

        float swimNeedTime = GameMain.Singleton.WorldDimension.width / SpeedShark;//todo ��׼ȷ
        yield return new WaitForSeconds(swimNeedTime);

        StopCoroutine("_Coro_LaJiaoGenerating");
        StartCoroutine(_Coro_WaitNullFish());
        ////�ȴ�����,����ʱ������
        //while (GameMain.Singleton.NumFishAlive != 0)
        //{
        //    yield return 0;
        //}

        //EndPrelude();
    }
    
    IEnumerator _Coro_LaJiaoGenerating()
    {
        yield return new WaitForSeconds(EmitLaJiaoDelay);
        Vector2 leftRightLimit = new Vector2(GameMain.Singleton.WorldDimension.x + RoadToLeftRightSpace, GameMain.Singleton.WorldDimension.xMax - RoadToLeftRightSpace);
  
        while (true)
        {
            Fish[] fish = new Fish[2] { Instantiate(Prefab_LaJiao) as Fish, Instantiate(Prefab_LaJiao) as Fish };

            for (int i = 0; i != 2; ++i)
            { 
                fish[i].ClearAI();
                fish[i].swimmer.Speed = SpeedLaJiao;
                fish[i].transform.parent = transform;
                fish[i].transform.position = new Vector3(Random.Range(leftRightLimit.x, leftRightLimit.y)
                    , i == 0 ? (GameMain.Singleton.WorldDimension.yMax + fish[i].swimmer.BoundCircleRadius) : (GameMain.Singleton.WorldDimension.y - fish[i].swimmer.BoundCircleRadius), Defines.GlobleDepth_FishBase + mDepthOffset);
                mDepthOffset -= Defines.OffsetAdv_FishGlobleDepth;
                fish[i].transform.right = Vector3.up * (i == 0 ? -1F : 1F);//����
                fish[i].swimmer.Go();
 
 
            }

            yield return new WaitForSeconds(IntervalEmitLaJiao);
        }
    }

    public IEnumerator _Coro_WaitNullFish()
    {
        while (GameMain.Singleton.NumFishAlive != 0)
        {
            yield return 0;
        }

        EndPrelude();
    }
}
