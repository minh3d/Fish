using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ͬ��ը���������
/// </summary>
public class FishEx_RndPosSTB : MonoBehaviour {

    [System.Serializable]
    public class FishToAni
    {
        public Fish Prefab_fish;
        public GameObject Prefab_Ani;
        public GameObject Prefab_AniDie;
    }
    [System.NonSerialized]
    public FishToAni[] Prefab_FishInstanced;
    public Transform[] TsLocalPos;
    public FishToAni[] FishAniConnect;

    private Fish mFish;
    private Transform mTs;

    //public int mustHaveFishIdx;
	void Awake () {
        if (TsLocalPos.Length == 0)
            Debug.LogError("localPos ����Ϊ��");

        if(FishAniConnect.Length < 3)
            Debug.LogError("FishAniConnect ��С������ڵ���3");

        mFish = GetComponent<Fish>();
        mTs = transform;
        mFish.EvtFishKilled += Handle_FishKilled;
        Prefab_FishInstanced = new FishToAni[TsLocalPos.Length];

        List<FishToAni> tmpRndLst = new List<FishToAni>();
        foreach(FishToAni fta in FishAniConnect)
        {
            tmpRndLst.Add(fta);
        }

        int totalOdds = 0;

        for (int i = 0; i != TsLocalPos.Length; ++i)
        {
            int getIdx = Random.Range(0, tmpRndLst.Count);
            //if (i == 0)
            //    getIdx = mustHaveFishIdx;
            Prefab_FishInstanced[i] = tmpRndLst[getIdx];

            totalOdds += Prefab_FishInstanced[i].Prefab_fish.Odds;
            //�����㶯��
            GameObject goAni = Instantiate(tmpRndLst[getIdx].Prefab_Ani) as GameObject;
            goAni.transform.parent = TsLocalPos[i];
            goAni.transform.localPosition = Vector3.zero;
            goAni.transform.localRotation = Quaternion.identity;


            tmpRndLst.RemoveAt(getIdx);
        }

        mFish.Odds = totalOdds;
        Fish[] stbAffected = new Fish[Prefab_FishInstanced.Length];
        for (int i = 0; i != stbAffected.Length; ++i)
            stbAffected[i] = Prefab_FishInstanced[i].Prefab_fish;

        mFish.Prefab_SameTypeBombAffect = stbAffected;
        

	}


    public void Handle_FishKilled(Player killer, Bullet b, Fish f)
    {
        GameObject goDieAnimation = new GameObject("goSameTypeBombDieAni");
        goDieAnimation.transform.parent = GameMain.Singleton.FishGenerator.transform;
        goDieAnimation.transform.position = new Vector3(mTs.position.x, mTs.position.y, Defines.GlobleDepth_DieFish);
        goDieAnimation.transform.rotation = mTs.rotation;

        for (int i = 0; i != Prefab_FishInstanced.Length; ++i)
        {
            //�����㶯��
            GameObject goAni = Instantiate(Prefab_FishInstanced[i].Prefab_AniDie) as GameObject;
            goAni.transform.parent = goDieAnimation.transform;
            Vector3 pos = new Vector3(TsLocalPos[i].position.x, TsLocalPos[i].position.y, Defines.GlobleDepth_DieFish-0.0001f);
            goAni.transform.position = pos;
            goAni.transform.rotation = TsLocalPos[i].rotation;
        }

        Ef_DestroyDelay fishDestroyDelay = goDieAnimation.AddComponent<Ef_DestroyDelay>();
        fishDestroyDelay.delay = mFish.TimeDieAnimation;

    }
     
}
