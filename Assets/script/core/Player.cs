using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour {
    [System.NonSerialized]
    public int Idx;
    [System.NonSerialized]
    public int CtrllerIdx;
    public Gun GunInst;
    public Gun[] Prefab_Guns;//����ǹ,��ӦGunType

    public tk2dTextMesh Text_Score;
    public CoinStackManager Ef_CoinStack;//������
    public FlyingCoinManager Ef_FlyCoin;
    public ParticleSystem Prefab_ParChangeGun;

    public GameObject[] Prefab_OutBountyHints;//���ҳ�Ʊ��ʾ.0:�˱���  1:��Ʊ�� 2:�˱ҹ��� 3:��Ʊ����
    //public GameObject SliSpr_ScoreBG;//�����屳��
    public tk2dSprite Spr_GunBottom;//��̨����
    [System.NonSerialized]
    public Rect AtScreenArea;//������Ļ����

    [System.NonSerialized]
    public bool CanChangeScore = true;//�Ƿ��Ѻ��

    [System.NonSerialized]
    public bool CanCurrentScoreChange = true;//

    private uint mScoreChangeLowest = 0;//��ͷ���ֵ
    //[System.NonSerialized]
    //public int Score;//ӵ�з���

    private bool[] mLeftRightKeystate;//���Ҽ�״̬,true����,false����

    private bool mIsUpScoreBatch = false;
    private bool mIsDownScoreBatch = false;
    private bool mIsOutingBounty = false;//�����˽�״̬

    private GameObject mGOViewingOutBountyHint;//������ʾ���˽���ʾ
    private static readonly Vector3 LocalOutBountyHint = new Vector3(192F, 26.88F, 0F);
    //private Player_FishLocker mFishLocker;
    
    void Awake()
    { 
        mLeftRightKeystate = new bool[2] { false, false };

        //mFishLocker = GetComponent<Player_FishLocker>();
        //������������
        Rect[] mWorldAreas = new Rect[GameMain.Singleton.ScreenNumUsing];

        for (int i = 0; i != mWorldAreas.Length; ++i)
        {
            mWorldAreas[i].x = GameMain.Singleton.WorldDimension.x + Defines.WorldDimensionUnit.width * i;
            mWorldAreas[i].y = GameMain.Singleton.WorldDimension.y;
            mWorldAreas[i].width = Defines.WorldDimensionUnit.width;
            mWorldAreas[i].height = Defines.WorldDimensionUnit.height;

            if (mWorldAreas[i].Contains(transform.position))
            {

                AtScreenArea = mWorldAreas[i];

                break;
            }
        }
    }

	// Use this for initialization
	void Start () {
      
       // Text_Score.text = GameMain.Singleton.BSSetting.Dat_PlayersScore[Idx].Val.ToString();
       // Text_Score.Commit();

        if (GameMain.Singleton.BSSetting.Dat_PlayersScoreWon[Idx].Val != 0)
        {
            //Ef_CoinStack.OneStack_SetNum(GameMain.Singleton.BSSetting.Dat_PlayersScoreWon[Idx].Val);
            StartCoroutine(_Coro_OutBountyImm());
        }
        
        //��ʼ��Gun
        GunLevelType glt = Gun.GunNeedScoreToLevelType(GameMain.Singleton.BSSetting.Dat_PlayersGunScore[Idx].Val);
        GunType gt = Gun.CombineGunLevelAndPower(glt,GunPowerType.Normal);
        //if (GameMain.Singleton.BSSetting.Dat_PlayersGunScore[Idx].Val >= Defines.ChangeGunNeedScoreTri)
        if (GunInst != null)
            Destroy(GunInst.gameObject);

        GunInst = Instantiate(Prefab_Guns[(int)gt]) as Gun;
        GunInst.GunType_ = gt;

        GunInst.transform.parent = transform;
        GunInst.transform.localPosition = Vector3.zero;
        GunInst.transform.localRotation = Quaternion.identity;

        //if (mFishLocker != null)
        //    mFishLocker.EvtRelock += (Fish f) => { GunInst.LockAt(f); };
        
        if (PlayerPrefs.GetInt("init") <= 0)
        {
            ChangeScore(200);
            PlayerPrefs.SetInt("init", 1);
        }
    }

    /// <summary>
    /// �ı����(������)
    /// </summary>
    /// <param name="change"></param>
    /// <returns>�ı��Ƿ�ɹ�</returns>
    public bool ChangeScore(int change)
    {
        //if (!CanChangeScore)
        //    return false;

        BackStageSetting bss = GameMain.Singleton.BSSetting ;//
        if (bss.Dat_PlayersScore[Idx].Val + change < mScoreChangeLowest)
            return false;

        bss.Dat_PlayersScore[Idx].Val += change;
        if (GameMain.EvtPlayerScoreChanged != null)
            GameMain.EvtPlayerScoreChanged(this, bss.Dat_PlayersScore[Idx].Val, change);

        //������
       // Text_Score.text = bss.Dat_PlayersScore[Idx].Val.ToString();
        //Text_Score.Commit();
        return true;
    }

    /// <summary>
    /// Ӯ�õķ���(���������)
    /// </summary>
    /// <param name="numWon"></param>
    public void WonScore(int numWon)
    {
        GameMain.Singleton.BSSetting.Dat_PlayersScoreWon[Idx].Val += numWon;
        if (GameMain.EvtPlayerWonScoreChanged != null)
            GameMain.EvtPlayerWonScoreChanged(this, GameMain.Singleton.BSSetting.Dat_PlayersScoreWon[Idx].Val);
        //Ef_CoinStack.OneStack_SetNum(GameMain.Singleton.BSSetting.Dat_PlayersScoreWon[Idx].Val);
        StartCoroutine(_Coro_OutBountyImm());
    }

    /// <summary>
    /// ���Ӯ�ñ�,�ۺ�ChangeScore��WonScore
    /// </summary>
    /// <param name="num"></param>
    public void GainScore(int scoreTotalGetted, int fishodd, int bulletScore)
    {
        BackStageSetting bss = GameMain.Singleton.BSSetting;
        if (bss.OutBountyType_.Val == OutBountyType.OutCoinButtom
            || bss.OutBountyType_.Val == OutBountyType.OutTicketButtom)
        {
            ChangeScore(scoreTotalGetted);
            //�Ҷ�.Ч��
            //Ef_CoinStack.RequestViewOneStack(scoreTotalGetted, fishodd, bulletScore, Idx);
        }
        else if (bss.OutBountyType_.Val == OutBountyType.OutCoinImmediate
        || bss.OutBountyType_.Val == OutBountyType.OutTicketImmediate)
            WonScore(scoreTotalGetted);
    }

    public void GainScore(int scoreTotalGetted)
    {
        GainScore(scoreTotalGetted, 2, 10);
    }

    /// <summary>
    /// �����˱�
    /// </summary>
    public void OutBountyButtom()
    {
        StartCoroutine(_Coro_OutBountyButtom());
    }

    /// <summary>
    /// �����˱�-Эͬ
    /// </summary>
    /// <returns></returns>
    public IEnumerator _Coro_OutBountyButtom()
    {
        if(mIsOutingBounty)
            yield break;

        BackStageSetting bss =  GameMain.Singleton.BSSetting;
        INemoControlIO arcIO = GameMain.Singleton.ArcIO;
        int oneBountyNeedScore = 100000000;//������
        if (bss.OutBountyType_.Val == OutBountyType.OutCoinButtom)
            oneBountyNeedScore = bss.InsertCoinScoreRatio.Val;
        else if (bss.OutBountyType_.Val == OutBountyType.OutTicketButtom)
            oneBountyNeedScore = (int)(((float)bss.InsertCoinScoreRatio.Val  ) * bss.CoinTicketRatio_Coin.Val / bss.CoinTicketRatio_Ticket.Val);//Ͷ�ұ���(��/��) * ��Ʊ��(��/Ʊ) = ��һƱ��Ҫ���ٷ�(��/Ʊ)
        //����Ƿ���һ��
        if (bss.Dat_PlayersScore[Idx].Val < oneBountyNeedScore)
            yield break;

        //��ʾ�˽�����ʾ
        if (mGOViewingOutBountyHint == null)
        {
            if (bss.OutBountyType_.Val == OutBountyType.OutCoinButtom)
                mGOViewingOutBountyHint = Instantiate(Prefab_OutBountyHints[0]) as GameObject;
            else if (bss.OutBountyType_.Val == OutBountyType.OutTicketButtom)
                mGOViewingOutBountyHint = Instantiate(Prefab_OutBountyHints[1]) as GameObject;
            mGOViewingOutBountyHint.transform.parent = transform;
            mGOViewingOutBountyHint.transform.localPosition = LocalOutBountyHint;
            mGOViewingOutBountyHint.transform.localRotation = Quaternion.identity;
        }



        mScoreChangeLowest = (uint)oneBountyNeedScore;//���÷����ı�����,��ֹ���ư巵����Ϣʱ���ܿ۳�
        mIsOutingBounty = true;

        if (bss.OutBountyType_.Val == OutBountyType.OutCoinButtom)
            arcIO.OutCoin(1, CtrllerIdx);
        else if (bss.OutBountyType_.Val == OutBountyType.OutTicketButtom)
            arcIO.OutTicket(1, CtrllerIdx);

        bool isBoutyOutted = false;//�Ƿ��ѳ�����
        float timeOut = 10F;//��ʱʱ��
        float elapse = 0F;
        NemoCtrlIO_EventController outBountyreflect = (int cIdx) =>
        {
            if (CtrllerIdx == cIdx)
                isBoutyOutted = true; 
        };
        
		if (bss.OutBountyType_.Val == OutBountyType.OutCoinButtom)
            arcIO.EvtOutCoinReflect += outBountyreflect;
        else if (bss.OutBountyType_.Val == OutBountyType.OutTicketButtom)
            arcIO.EvtOutTicketReflect += outBountyreflect;
        while (!isBoutyOutted)
        {
            if (elapse > timeOut)
            {
                StartCoroutine(_Coro_OutBountyErrorView());//��ʾ����
                mIsOutingBounty = false;
                mScoreChangeLowest = 0;//�ָ��ı��������
                yield break;
            }
            elapse += Time.deltaTime;
            yield return 0;
        }

        //������Ӧ��
		if (bss.OutBountyType_.Val == OutBountyType.OutCoinButtom)
            arcIO.EvtOutCoinReflect -= outBountyreflect;
        else if (bss.OutBountyType_.Val == OutBountyType.OutTicketButtom)
            arcIO.EvtOutTicketReflect -= outBountyreflect;
        mIsOutingBounty = false;
        mScoreChangeLowest = 0;//�ָ��ı��������

        //��ʷ��¼
        if (bss.OutBountyType_.Val == OutBountyType.OutCoinButtom)
        {
            bss.His_CoinOut.Val += 1;
            bss.His_GainCurrent.Val -= 1;
            bss.UpdateGainCurrentAndTotal();
        }
        else if (bss.OutBountyType_.Val == OutBountyType.OutTicketButtom)
        {
            bss.His_TicketOut.Val += 1;

            //������ҹ���
            bss.TicketOutFragment.Val += 1;
            int outCoinNumBase = bss.TicketOutFragment.Val / bss.CoinTicketRatio_Ticket.Val;//���һ���,����CoinTicketRatio_Coin �ó�Ӧ�����ٸ�����
            if (outCoinNumBase > 0)
            {
                bss.His_GainCurrent.Val -= outCoinNumBase * bss.CoinTicketRatio_Coin.Val;
                bss.TicketOutFragment.Val -= bss.CoinTicketRatio_Ticket.Val;
            } 
            bss.UpdateGainCurrentAndTotal();
        }

        ChangeScore(-oneBountyNeedScore);

        //�Ƿ���ٿ۷�,��������� 
        if (bss.Dat_PlayersScore[Idx].Val >= oneBountyNeedScore)
        {
            StartCoroutine(_Coro_OutBountyButtom());
        }
        else//�˳�����
        {
            if (mGOViewingOutBountyHint != null)
            {
                Destroy(mGOViewingOutBountyHint);
                mGOViewingOutBountyHint = null;
            }
        }
    }

    /// <summary>
    /// �����˱� - ������
    /// </summary>
    /// <remarks>�ڰ����˱�ģʽ��,�����ҵĻ���ʾ���Ϻ󲻻�����</remarks>
    /// <returns></returns>
    IEnumerator _Coro_OutBountyErrorView()
    {
        if (mGOViewingOutBountyHint != null)
        {
            Destroy(mGOViewingOutBountyHint);
        }
        BackStageSetting bss = GameMain.Singleton.BSSetting;
        if (bss.OutBountyType_.Val == OutBountyType.OutCoinButtom)
            mGOViewingOutBountyHint = Instantiate(Prefab_OutBountyHints[2]) as GameObject;
        else if (bss.OutBountyType_.Val == OutBountyType.OutTicketButtom)
            mGOViewingOutBountyHint = Instantiate(Prefab_OutBountyHints[3]) as GameObject;
        mGOViewingOutBountyHint.transform.parent = transform;
        mGOViewingOutBountyHint.transform.localPosition = LocalOutBountyHint;
        mGOViewingOutBountyHint.transform.localRotation = Quaternion.identity;

        yield return new WaitForSeconds(5F);

        Destroy(mGOViewingOutBountyHint);
        mGOViewingOutBountyHint = null;
    }

    /// <summary>
    /// ��ʱ�˽�
    /// </summary>
    /// <remarks>Dat_PlayersScoreWon��ֵ,�����˽������</remarks>
    /// <returns></returns>
    public IEnumerator _Coro_OutBountyImm()
    {
        if(mIsOutingBounty)
            yield break;

        BackStageSetting bss = GameMain.Singleton.BSSetting;
        INemoControlIO arcIO= GameMain.Singleton.ArcIO;

        int oneBountyNeedScore = 100000000;//��ֹ����bug������
        if (bss.OutBountyType_.Val == OutBountyType.OutCoinImmediate)
            oneBountyNeedScore = bss.InsertCoinScoreRatio.Val;
        else if (bss.OutBountyType_.Val == OutBountyType.OutTicketImmediate)
            oneBountyNeedScore = (int)(((float)bss.InsertCoinScoreRatio.Val) * bss.CoinTicketRatio_Coin.Val / bss.CoinTicketRatio_Ticket.Val);//Ͷ�ұ���(��/��) * ��Ʊ��(��/Ʊ) = ��һƱ��Ҫ���ٷ�(��/Ʊ)
        if(bss.Dat_PlayersScoreWon[Idx].Val  < oneBountyNeedScore)
            yield break;

        mIsOutingBounty = true;

        //����Ӳ���۱�
        if (bss.OutBountyType_.Val == OutBountyType.OutCoinImmediate)
            arcIO.OutCoin(1, CtrllerIdx);
        else if (bss.OutBountyType_.Val == OutBountyType.OutTicketImmediate)
            arcIO.OutTicket(1, CtrllerIdx);


        bool isBoutyOutted = false;//�Ƿ��ѳ�����
        float timeOut = 10F;//��ʱʱ��
        float elapse = 0F;
        NemoCtrlIO_EventController outBountyreflect = (int cIdx) =>
        {
            if (CtrllerIdx == cIdx)
                isBoutyOutted = true;
        };
		if (bss.OutBountyType_.Val == OutBountyType.OutCoinImmediate)
            arcIO.EvtOutCoinReflect += outBountyreflect;
        else if (bss.OutBountyType_.Val == OutBountyType.OutTicketImmediate)
            arcIO.EvtOutTicketReflect += outBountyreflect;

        
        while (!isBoutyOutted)
        {
            if (elapse > timeOut)// ��ʱ
            {
                //��ʾ�˽�������ʾ
                if (bss.OutBountyType_.Val == OutBountyType.OutCoinImmediate)
                    mGOViewingOutBountyHint = Instantiate(Prefab_OutBountyHints[2]) as GameObject;
                else if (bss.OutBountyType_.Val == OutBountyType.OutTicketImmediate)
                    mGOViewingOutBountyHint = Instantiate(Prefab_OutBountyHints[3]) as GameObject;
                mGOViewingOutBountyHint.transform.parent = transform;
                mGOViewingOutBountyHint.transform.localPosition = LocalOutBountyHint;
                mGOViewingOutBountyHint.transform.localRotation = Quaternion.identity;

                yield return new WaitForSeconds(30F);
                mIsOutingBounty = false;
                StartCoroutine(_Coro_OutBountyImm());
                yield break;
            }
            elapse += Time.deltaTime;
            yield return 0;
        }

        //������Ӧ��
        if (bss.OutBountyType_.Val == OutBountyType.OutCoinImmediate)
            arcIO.EvtOutCoinReflect -= outBountyreflect;
        else if (bss.OutBountyType_.Val == OutBountyType.OutTicketImmediate)
            arcIO.EvtOutTicketReflect -= outBountyreflect;
        


        //��ʷ��¼
        if (bss.OutBountyType_.Val == OutBountyType.OutCoinImmediate)
        {
            bss.His_CoinOut.Val += 1;
            bss.His_GainCurrent.Val -= 1;
            bss.UpdateGainCurrentAndTotal();
        }
        else if (bss.OutBountyType_.Val == OutBountyType.OutTicketImmediate)
        {
            bss.His_TicketOut.Val += 1;

            //������ҹ���
            bss.TicketOutFragment.Val += 1;
            int outCoinNumBase = bss.TicketOutFragment.Val / bss.CoinTicketRatio_Ticket.Val;//���һ���,����CoinTicketRatio_Coin �ó�Ӧ�����ٸ�����
            if (outCoinNumBase > 0)
            {
                bss.His_GainCurrent.Val -= outCoinNumBase * bss.CoinTicketRatio_Coin.Val;
                bss.TicketOutFragment.Val -= bss.CoinTicketRatio_Ticket.Val;
            } 
            bss.UpdateGainCurrentAndTotal();
        }
        if (bss.Dat_PlayersScoreWon[Idx].Val < oneBountyNeedScore)
        {
            Debug.LogError("[�˽�����]�ÿ۷������������Ӯ����.");
            yield break;
        }

        bss.Dat_PlayersScoreWon[Idx].Val = bss.Dat_PlayersScoreWon[Idx].Val - oneBountyNeedScore;
        if (GameMain.EvtPlayerWonScoreChanged != null)
            GameMain.EvtPlayerWonScoreChanged(this, bss.Dat_PlayersScoreWon[Idx].Val);
        //Ef_CoinStack.OneStack_SetNum(bss.Dat_PlayersScoreWon[Idx].Val);
        mIsOutingBounty = false;

        //�Ƿ���ٿ۷�,��������� 
        if (bss.Dat_PlayersScoreWon[Idx].Val >= oneBountyNeedScore)
        {
            StartCoroutine(_Coro_OutBountyImm());
        }

        if (mGOViewingOutBountyHint != null)
        {
            Destroy(mGOViewingOutBountyHint);
            mGOViewingOutBountyHint = null;
        }
    }
   

    /// <summary>
    /// ��ǹ
    /// </summary>
    /// <param name="gt"></param>
    /// <returns></returns>
    public bool ChangeGun(GunType gt)
    {
        Gun g = Instantiate(Prefab_Guns[(int)gt]) as Gun;
        g.GunType_ = gt;

        g.transform.parent = GunInst.transform.parent;
        g.transform.localPosition = GunInst.transform.localPosition;
        g.transform.localRotation = GunInst.transform.localRotation;

        //��λ
        g.TsGun.transform.localPosition = GunInst.TsGun.transform.localPosition;
        g.TsGun.transform.localRotation = GunInst.TsGun.transform.localRotation;


        GunInst.CopyDataTo(g);
        Destroy(GunInst.gameObject);
        GunInst = g;

        //����Ĳ���
        //�ڿ����зֵĻ��ͼ�����
//         if (g.FastChangingCoin)
//         {
//             g.StartChangeNeedCoin();
//         }

        //Ч��
        StartCoroutine(_Coro_Effect_ChangeGun(g));

        //��Ч
        if (g.Snd_Equip != null)
            GameMain.Singleton.SoundMgr.PlayOneShot(g.Snd_Equip);

        if (GameMain.EvtPlayerGunChanged != null)
            GameMain.EvtPlayerGunChanged(this, g);

        return true;
    }


    public bool ChangeGun(GunLevelType lvType, GunPowerType pwrType)
    {
        return ChangeGun(Gun.CombineGunLevelAndPower(lvType, pwrType));
    }
    IEnumerator _Coro_Effect_ChangeGun(Gun newGun)
    {
        ParticleSystem ef_particle = null;
        if (Prefab_ParChangeGun != null)
        {
            ef_particle = Instantiate(Prefab_ParChangeGun) as ParticleSystem;
            ef_particle.transform.parent = newGun.transform;
            ef_particle.transform.localPosition = new Vector3(0F, -18.55325184F, -10F);
            ef_particle.transform.localRotation = Quaternion.identity;
            ef_particle.Play(true);
 
        }
        float time = 0.1F;
        float elapse = 0F;
        Transform tsAniGun = newGun.AniSpr_GunPot.transform;
        //Vector3 oriPos = tsAniGun.localPosition;
        float scale = 1F;
        while (elapse <time)
        {
            if (tsAniGun == null)//ͬʱ������ǹָ���ʹ��ʱʧЧ
                break;
            //tsAniGun.localPosition = oriPos + tsAniGun.localRotation * Vector3.up *(Curve_GunShakeOffset.Evaluate(elapse/time));
            scale  = 1F+ 0.5F*(1F-elapse/time);
            tsAniGun.localScale = new Vector3(scale,scale,scale);

            elapse += Time.deltaTime;
            yield return 0;
        }

        if (tsAniGun != null)
            tsAniGun.localScale = Vector3.one;

        if (ef_particle != null)
        {
            yield return new WaitForSeconds(3F);
            if (ef_particle != null)
                Destroy(ef_particle.gameObject);
        }

    }
    //GunType mCurGunType = GunType.Normal;
    //void OnGUI()
    //{
    //    if (Idx == 0 && GUILayout.Button("change gun"))
    //    {
    //        if (mCurGunType == GunType.Normal)
    //        {
    //            ChangeGun(GunType.Lizi);
    //            mCurGunType = GunType.Lizi;
    //        }
    //        else if (mCurGunType == GunType.Lizi)
    //        {
    //            ChangeGun(GunType.Normal);
    //            mCurGunType = GunType.Normal;
    //        }
    //    }
    //}


    public void ScoreUp(bool isMultiTen)
    {
        BackStageSetting bss = GameMain.Singleton.BSSetting;

        int numCoin = 10 * (isMultiTen ? 10 : 1);
        int score = bss.InsertCoinScoreRatio.Val * numCoin;
        
        //�ж��Ƿ񱬻�
        if (bss.Dat_PlayersScore[Idx].Val + score >= Defines.NumScoreUpMax)
        {
            return;
        }
        ChangeScore(score);
        
        //�Ϸ�
        bss.His_CoinUp.Val += numCoin;
        bss.His_GainCurrent.Val += numCoin;
        bss.UpdateGainCurrentAndTotal();

        //����
        GameMain.Singleton.SoundMgr.PlayOneShot(GameMain.Singleton.SoundMgr.snd_AddScore);
    }

    void ScoreDown(bool isAll)
    {

        BackStageSetting bss = GameMain.Singleton.BSSetting;
        if (isAll)
        {
            int scoreDown = (bss.Dat_PlayersScore[Idx].Val / bss.InsertCoinScoreRatio.Val) * bss.InsertCoinScoreRatio.Val;
            ChangeScore(-scoreDown);

            int numCoin = scoreDown / bss.InsertCoinScoreRatio.Val;

            bss.His_CoinDown.Val += numCoin;
            bss.His_GainCurrent.Val -= numCoin;
            bss.UpdateGainCurrentAndTotal();
        }
        else
        {
            int requireDown = bss.InsertCoinScoreRatio.Val * 10;

            if (requireDown <= bss.Dat_PlayersScore[Idx].Val)
            {
                ChangeScore(-requireDown);

                int numCoin = requireDown / bss.InsertCoinScoreRatio.Val;

                bss.His_CoinDown.Val += numCoin;
                bss.His_GainCurrent.Val -= numCoin;
                bss.UpdateGainCurrentAndTotal();
            }
        } 

        //����
        GameMain.Singleton.SoundMgr.PlayOneShot(GameMain.Singleton.SoundMgr.snd_AddScore);
    }
    /// <summary>
    /// ��������,��GameMain����
    /// </summary>
    /// <param name="p"></param>
    /// <param name="k"></param>
    /// <param name="down"></param>
    public void OnKeyDown(HpyInputKey k, bool down)
    {

        if (down && k == HpyInputKey.Left)
        {
            mLeftRightKeystate[0] = true;
            goto tag_GunRotate;
        }
        else if (!down && k == HpyInputKey.Left)
        {
            mLeftRightKeystate[0] = false;
            goto tag_GunRotate;
        }

        else if (down && k == HpyInputKey.Right)
        {
            mLeftRightKeystate[1] = true;
            goto tag_GunRotate;
        } 
        else if (!down && k == HpyInputKey.Right)
        {
            mLeftRightKeystate[1] = false;
            goto tag_GunRotate;
        }

        //else if (down && k == HpyInputKey.Down)
        //{
        //    if (mFishLocker != null)
        //    {
        //        Fish f = mFishLocker.Lock();

        //        if (f != null)
        //            GunInst.LockAt(f);
        //    }
            
        //}
        //else if (!down && k == HpyInputKey.Down)
        //{
        //    if (mFishLocker != null)
        //    {
        //        mFishLocker.UnLock();
        //        GunInst.UnLock();
        //    }
        //}

        else if (down && k == HpyInputKey.Fire)
        {
            
            GunInst.StartFire();
        }
        else if (!down && k == HpyInputKey.Fire)
        {
            GunInst.StopNormalFire();
        }
        else if (down && k == HpyInputKey.Advance)
        {
            //GunInst.StartChangeNeedCoin();
            StartCoroutine("_Coro_ChangeNeedScore");
        }
        else if (!down && k == HpyInputKey.Advance)
        {
            //GunInst.StopChangeNeedCoin();
            StopCoroutine("_Coro_ChangeNeedScore");
        }
        else if (down && k == HpyInputKey.ScoreUp)
        {
            StartCoroutine("_Coro_ScoreBatchUp");
            mIsUpScoreBatch = false;
        }
        else if (!down && k == HpyInputKey.ScoreUp)
        {
 
            StopCoroutine("_Coro_ScoreBatchUp");
            if (!mIsUpScoreBatch)
            {
                ScoreUp(false);
            }
        }
        else if (down && k == HpyInputKey.ScoreDown)
        {
            StartCoroutine("_Coro_ScoreBatchDown");
            mIsDownScoreBatch = false;
        }
        else if (!down && k == HpyInputKey.ScoreDown)
        {
            StopCoroutine("_Coro_ScoreBatchDown");
            if (!mIsDownScoreBatch)
            {
                ScoreDown(false);
            }
        }

        return;

    tag_GunRotate:
        if (mLeftRightKeystate[0] && mLeftRightKeystate[1])
            GunInst.StopRotate();
        else if (!mLeftRightKeystate[0] && !mLeftRightKeystate[1])
            GunInst.StopRotate();
        else if (mLeftRightKeystate[0])
            GunInst.RotateTo(true);
        else if (mLeftRightKeystate[1])
            GunInst.RotateTo(false);
    }

 
    /// <summary>
    /// �����Ϸ�
    /// </summary>
    /// <returns></returns>
    IEnumerator _Coro_ScoreBatchUp()
    {
        yield return new WaitForSeconds(1F);
        ScoreUp(true);
        mIsUpScoreBatch = true;
    }

    /// <summary>
    /// �����·�
    /// </summary>
    /// <returns></returns>
    IEnumerator _Coro_ScoreBatchDown()
    {
        yield return new WaitForSeconds(1F);
        ScoreDown(true);
        mIsDownScoreBatch = true;
    }

    /// <summary>
    /// �ı�Ѻ��
    /// </summary>
    /// <returns></returns>
    IEnumerator _Coro_ChangeNeedScore()
    {
        if (!CanChangeScore)
            yield break;
        BackStageSetting bss = GameMain.Singleton.BSSetting;
 
        GunLevelType preType = Gun.GunNeedScoreToLevelType(bss.Dat_PlayersGunScore[Idx].Val);
        GunInst.AdvanceNeedScore();
        GunLevelType curType = Gun.GunNeedScoreToLevelType(bss.Dat_PlayersGunScore[Idx].Val);
        if(curType  != preType)
            On_GunLevelTypeChanged(curType);

        yield return new WaitForSeconds(0.2F);


        while (CanChangeScore)
        {
            preType = Gun.GunNeedScoreToLevelType(bss.Dat_PlayersGunScore[Idx].Val);
            GunInst.AdvanceNeedScore();
            curType = Gun.GunNeedScoreToLevelType(bss.Dat_PlayersGunScore[Idx].Val);
            if (curType != preType)
                On_GunLevelTypeChanged(curType);

            yield return new WaitForSeconds(0.1F);
        }
    }


    public void ChangeNeedScore(int newScore)
    {
        BackStageSetting bss = GameMain.Singleton.BSSetting;

        GunLevelType preType = Gun.GunNeedScoreToLevelType(bss.Dat_PlayersGunScore[Idx].Val);
        GunInst.SetNeedScore(newScore);
        GunLevelType curType = Gun.GunNeedScoreToLevelType(bss.Dat_PlayersGunScore[Idx].Val);

        if (curType != preType)
            On_GunLevelTypeChanged(curType);
    }




    /// <summary>
    /// Ѻ�ָı�(������)
    /// </summary>
    /// <param name="curScore"></param>
    void On_GunLevelTypeChanged(GunLevelType curType)
    {
        ChangeGun(curType, GunInst.GetPowerType()); 
    }
}
