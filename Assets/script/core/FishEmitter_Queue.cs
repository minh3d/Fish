using UnityEngine;
using System.Collections;

/// <summary>
/// ���������
/// </summary>
/// <remarks>�Զ�ɾ��</remarks>
public class FishEmitter_Queue : MonoBehaviour {

    public Fish Prefab_Fish;
    public float Distance = 0.2F;//���ɾ���
    public int NumMax = 3;//������ɸ���
    public int NumMin = 2;//�������ɸ���

    public float Fish_Speed = 0.5F;
    public float Fish_RotateSpd = 90F;//�Ƕ�/�� 

    //Ai
    public float Fish_RotateAngleRndRange = 30F;
    public float Fish_RotateInterval = 5F;//ת����
    public float Fish_RotateIntervalRndRange = 1F;//ת�������Χ
    void Awake()
    {
        GameMain.EvtStopGenerateFish += Handle_StopGenerateFish;
    }
    void OnDestroy()
    {
        GameMain.EvtStopGenerateFish -= Handle_StopGenerateFish;
    }

    void Handle_StopGenerateFish()
    {
        StopCoroutine("_Coro_Generate");
        Destroy(gameObject);
    }

    public void Generate()
    {
        StartCoroutine("_Coro_Generate");
    }
    
    IEnumerator _Coro_Generate()
    {
        int num = Random.Range(NumMin, NumMax + 1);
        int generatedNum = 0;
        
        yield return 0;//Ϊȡ��transform.localPosition
		
        //����leaderFish
        GameObject goLeader = new GameObject("���_"+Prefab_Fish.name); 
        Swimmer fishLeader = goLeader.AddComponent<Swimmer>();
        goLeader.AddComponent<DestroyWhenSwimmerOut>();

        if (GameMain.EvtLeaderInstance != null)
            GameMain.EvtLeaderInstance(fishLeader);
 

        //fishLeader.Attackable = false;
        Prefab_Fish.swimmer.CopyDataTo(fishLeader);
        fishLeader.SetLiveDimension(Defines.ClearFishRadius);//swim to fix
        fishLeader.RotateSpd = Fish_RotateSpd;
        fishLeader.Speed = Fish_Speed;

        FishAI_FreeSwimSingle fishAILeader = goLeader.AddComponent<FishAI_FreeSwimSingle>();
        Prefab_Fish.GetComponent<FishAI_FreeSwimSingle>().CopyDataTo(fishAILeader);
        fishAILeader.enabled = false;
        fishAILeader.RotateAngleRndRange = Fish_RotateAngleRndRange;
        fishAILeader.RotateInterval = Fish_RotateInterval;
        fishAILeader.RotateIntervalRndRange = Fish_RotateIntervalRndRange;

        fishLeader.transform.parent =  GameMain.Singleton.FishGenerator.transform;
        Vector3 localPos = transform.localPosition;
        localPos.z = Defines.GMDepth_Fish + GameMain.Singleton.FishGenerator.ApplyFishDepth();
        fishLeader.transform.localPosition = localPos;
        fishLeader.transform.localRotation = transform.localRotation;
        fishLeader.Go();

        Fish preFish = null;
        float distanceToLeader = 0F;
        while (generatedNum < num)
        {
            yield return new WaitForSeconds((Distance + fishLeader.BoundCircleRadius * 2F) / fishLeader.Speed);
			if (fishLeader == null)
                break;
            Fish f = Instantiate(Prefab_Fish) as Fish;
            Swimmer s = f.swimmer;
            f.name = Prefab_Fish.name;

            s.RotateSpd = Fish_RotateSpd;
            s.Speed = Fish_Speed;
            //��������
            if (preFish != null)
            {
                //f.AniSprite.clipTime = preFish.AniSprite.clipTime;
                //f.AniSprite.Play(preFish.AniSprite.ClipTimeSeconds);
                f.AniSprite.PlayFrom(f.AniSprite.DefaultClip, preFish.AniSprite.ClipTimeSeconds);
            }

            //ɾ����������ai
            f.ClearAI();
            FishAI_Follow aiFollow = f.gameObject.AddComponent<FishAI_Follow>();
            aiFollow.SetTarget(fishLeader);
            distanceToLeader += (Distance + fishLeader.BoundCircleRadius * 2F);
            aiFollow.DistanceToLeader = distanceToLeader; 
			  
            //��λ����
            f.transform.parent = GameMain.Singleton.FishGenerator.transform;
            localPos = transform.localPosition;
            localPos.z = Defines.GMDepth_Fish + GameMain.Singleton.FishGenerator.ApplyFishDepth();
            f.transform.localPosition = localPos;
            f.transform.localRotation = transform.localRotation;
            s.Go();
 
            ++generatedNum;

            preFish = f;
        }

        yield return 0;
        if(fishAILeader != null)
        fishAILeader.enabled = true;
        Destroy(gameObject);
    }


}
