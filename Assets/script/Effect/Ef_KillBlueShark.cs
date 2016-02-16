using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class Ef_KillBlueShark : MonoBehaviour {
    public GameObject GO_EfBackground;
    public float Duration = 3F;
    public float LocalHeight = 130F; 
    public Fish[] NormalFishPrefabView;//��ͨ����ʾ����
    public AudioClip Snd_GetPrize;

    private bool[] IsFishBombViewing;
    private Dictionary<int, Object> mViewFishPrefabCache;

    private static float mShareOffsetZ = 0F;//zƫ��,���ڽ���ص�����

    //public float SameTypeBombCoolDown = 0.5F;//��0.5��֮���ٴγ���ͬ��ը������������ʾ������
    //private float mLastTimePreSameTypeBombViewPad;//�ϴ���ʾͬ��ը���������ʱ��
    void Awake()
    {
        mViewFishPrefabCache = new Dictionary<int, Object>();
        foreach (Fish prefabFish in NormalFishPrefabView)
        {
            mViewFishPrefabCache.Add(prefabFish.TypeIndex, null);
        }
    }
	// Use this for initialization
	void Start () {
        GameMain.EvtFishKilled += Handle_FishKilled;
        GameMain.EvtFishBombKilled += Handle_FishBomb;
        GameMain.EvtSameTypeBombKiled += Handle_SameTypeBomb;
        IsFishBombViewing = new bool[Defines.MaxNumPlayer];
	}

    void Handle_FishBomb(Player k, int totalGet)
    {
        StartCoroutine(_Coro_EffectProcessing(totalGet, k, 1.5F, true));
    }

    void Handle_SameTypeBomb(Player k, int totalGet)
    {
        StartCoroutine(_Coro_EffectProcessing(totalGet, k, 1.5F, false));
    }

    void Handle_FishKilled(Player killer, Bullet b, Fish f)
    {
        if (!mViewFishPrefabCache.ContainsKey(f.TypeIndex))
        {
            return;
        }

        StartCoroutine(_Coro_EffectProcessing(b.Score * f.Odds * b.FishOddsMulti, killer, f.TimeDieAnimation, false));
    }
 
    IEnumerator _Coro_EffectProcessing(int num,Player killer,float delay,bool isAreaFishBomb)
    {
        if (isAreaFishBomb)
            IsFishBombViewing[killer.Idx] = true;
        else
            if (IsFishBombViewing[killer.Idx])
                yield break;

        //������������֮���ٲ���
        yield return new WaitForSeconds(delay);
        //����
        GameMain.Singleton.SoundMgr.PlayOneShot(Snd_GetPrize);

        //����������
        GameObject goEffect = Instantiate(GO_EfBackground) as GameObject;
        tk2dSprite aniSpr = goEffect.GetComponentInChildren<tk2dSprite>();
        Transform tsEffect = goEffect.transform;
        tk2dTextMesh text = goEffect.GetComponentInChildren<tk2dTextMesh>();

        text.text = num.ToString();
        text.Commit();

        //��ʼ��λ����
        Vector3 originLocalPos = new Vector3(0, -192F, 0F);
        Vector3 targetLocalPos = new Vector3(0, LocalHeight, 0F);
        
        tsEffect.parent = killer.transform;
        tsEffect.localPosition = originLocalPos;
        tsEffect.localRotation = Quaternion.identity;

        //ҡ������
        iTween.RotateAdd(text.gameObject, iTween.Hash("z", 8F, "time", 0.27F, "looptype", iTween.LoopType.pingPong, "easetype", iTween.EaseType.easeInOutSine));


        //�����ƶ�
        mShareOffsetZ -= 0.5F;
        if (mShareOffsetZ < -5F)
            mShareOffsetZ = 0F;

        float elapse = 0F;
        float useTime = 0.2F;
        Vector3 posTmp;
        while (elapse < useTime)
        {
            tsEffect.localPosition = originLocalPos + (targetLocalPos - originLocalPos) * (elapse / useTime);

            posTmp = tsEffect.position;
            posTmp.z = Defines.GlobleDepth_LiziKa + mShareOffsetZ;
            tsEffect.position = posTmp;

            elapse += Time.deltaTime;
            yield return 0;
        }
        tsEffect.localPosition = targetLocalPos;

        posTmp = tsEffect.position;
        posTmp.z = Defines.GlobleDepth_LiziKa + mShareOffsetZ;
        tsEffect.position = posTmp;

        yield return new WaitForSeconds(Duration);

        //����
        elapse = 0F;
        useTime = 0.2F;
        while (elapse < useTime)
        {
            aniSpr.color = new Color(1F, 1F, 1F, 1F- elapse / useTime);
            text.color = new Color(1F, 1F, 1F,1F- elapse / useTime);
            text.Commit();
            elapse += Time.deltaTime;
            yield return 0;
        }


        Destroy(goEffect.gameObject);
        if (isAreaFishBomb)
            IsFishBombViewing[killer.Idx] = false;
    }

    //int curPlayerIdx = 0;
    //void OnGUI()
    //{
    //    if (GUILayout.Button("popup"))
    //    {
    //        StartCoroutine(_Coro_EffectProcessing(1234,GameMain.Singleton.Players[curPlayerIdx]));
    //        curPlayerIdx = (curPlayerIdx + 1) % 6;
    //    }
    //}
}
