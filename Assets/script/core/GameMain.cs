    #define ENABLE_MCU_VERIFY//��ʱ��ֹmcu��֤
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameMain : MonoBehaviour {

    public enum State
    {
        Idle,//����״̬,��Ϸδ��ʼ
        Normal,//��ͨ����
        Sweeping,//����,(һ��û�ӵ�)
        Preluding,//�������� 
        BackStage,//��̨
        BeforeSweeping_WaitBulletClear//ɨ��ǰ�ȴ��ӵ���ʧ
    }
    //�¼�
    public delegate void Event_InputInsertCoin(int playerControll, int n);
    /// <summary>
    /// ���� - ����
    /// </summary>
    /// <param name="p"></param>
    /// <param name="key"></param>
    public delegate void Event_InputKey(int control, HpyInputKey key,bool down);
    public static  Event_InputKey EvtInputKey;
    public static Event_Generic EvtStopGenerateFish;//ֹͣ����
    public static Event_Generic EvtMainProcess_StartGame;//��ʼ��Ϸ
    public static Event_Generic EvtMainProcess_PrepareChangeScene;//׼������.���˹���
    public static Event_Generic EvtMainProcess_FinishChangeScene;//�������.���˹���
    public static Event_Generic EvtMainProcess_FinishPrelude;//�������
    public static Event_Generic EvtMainProcess_FirstEnterScene;//��һ�ν��볡��


    public delegate void Event_FishKilled(Player killer,Bullet b,Fish f);
    public static Event_FishKilled EvtFishKilled;//�������¼�

    public delegate void Event_FishBombKilled(Player killer, int scoreGetted);
    public static Event_FishBombKilled EvtFishBombKilled;//��ը����ը,(ע��:�ȴ���.������������)
 
    public static Event_FishBombKilled EvtSameTypeBombKiled;//ͬ��ը����ը
    public static Event_FishBombKilled EvtSameTypeBombExKiled;//����ͬ��ը����ը

    public static Event_Generic EvtFreezeBombActive;
    public static Event_Generic EvtFreezeBombDeactive;

    public delegate void Event_FishClear(Fish f);
    public static Event_FishClear EvtFishClear;
    public static Event_FishClear EvtFishInstance;
    public delegate void Event_LeaderInstance(Swimmer s);
    public static Event_LeaderInstance EvtLeaderInstance;//��ӳ�ʼ��

    public delegate void Event_PlayerGainScoreFromFish(Player p,int score,Fish firstFish,Bullet b);//��Ҵ������÷���
    public static Event_PlayerGainScoreFromFish Evt_PlayerGainScoreFromFish;

    public delegate void Event_PlayerScoreChanged(Player p, int ScoreNew, int scoreChange);
    public static Event_PlayerScoreChanged EvtPlayerScoreChanged;

    public delegate void Event_PlayerGunChanged(Player p, Gun newGun);
    public static Event_PlayerGunChanged EvtPlayerGunChanged;

    public delegate void Event_PlayerWonScoreChanged(Player p, int scoreNew);
    public static Event_PlayerWonScoreChanged EvtPlayerWonScoreChanged;//���Ӯ�÷�ֵ�ı�('Ӯ�÷�ֵ'���ڼ�ʱ�˱�)

    public delegate void Event_PlayerGunFired(Player p,Gun gun, int useScore);
    public static Event_PlayerGunFired EvtPlayerGunFired;//

    public delegate void Event_BulletDestroy(Bullet b);
    public static Event_BulletDestroy EvtBulletDestroy;

    public delegate void Event_SoundVolumeChanged(float curentVol);
    public static Event_SoundVolumeChanged EvtSoundVolumeChanged;

    public delegate void Event_KillLockingFish(Player killer);
    public static Event_KillLockingFish EvtKillLockingFish;//ɱ��������������

    //public delegate void Event_FreezeAllFish();
    //public static Event_Generic EvtFreezeAllFishBegin;
    //public static Event_Generic EvtFreezeAllFishEnd;


    public delegate void Event_BackGroundClearAllData_Before();
    public static Event_BackGroundClearAllData_Before EvtBGClearAllData_Before;//��̨�����������

    public delegate void Event_BackGroundChangeArenaType(ArenaType oldType,ArenaType newType);
    public static Event_BackGroundChangeArenaType EvtBGChangeArenaType;//��̨�ı䳡������

    public delegate void Event_LanguageChanged(Language l);
    public static Event_LanguageChanged EvtLanguageChange;

    //����
    public static int GameIdx
    {
        get { return Singleton.mGameIdx; }
        set
        {
            if (Singleton.mGameIdx != value)
            {
                
                Singleton.mGameIdx = value;

#if ENABLE_MCU_VERIFY
                Singleton.ArcIO.RequestHardwareInfo();//����GameIdx�ı�,��������������֤Ӳ��
#endif
                
            }
        }
    }
    public int mGameIdx = 2;//��������
    public static int MainVersion = 211;
    public static int SubVersion = 200;
    

    public Transform TopLeftTs;
    public BackStageSetting BSSetting
    {
        get
        {
            if (mBSSetting == null)
            {
                mBSSetting = GetComponent<BackStageSetting>();
                mBSSetting.TryNewPersistentDatas();
            }
            return mBSSetting;
        }
    }
    private BackStageSetting mBSSetting;

    public PopupDigitFishDie PopupDigitFishdie;//���������� 
    public Ef_FishDieEffectAdditive Ef_FishDieEffectAdd;//��ը����ըЧ��
    public FishGenerator FishGenerator;//��Ⱥ����
    public SceneBGManager SceneBGMgr;//�����л�����
    public ScenePreludeManager ScenePreludeMgr;//�����������
    public BackstageMain Prefab_Backstage;//��̨
    //public GameStart GameStart_;//��Ϸ��ʼ����
    public SoundManager SoundMgr;//����

    public Player Prefab_Player;//���Prefab
    public GameObject[] Prefab_PlayerCreatorSet;//��Ҵ����߼��ϣ�������PlayerCreator�ĸ�����
    [System.NonSerialized]
    public Player[] Players;
    [System.NonSerialized]
    public Rect WorldDimension; 

    [System.NonSerialized]
    public int NumFishAlive = 0;//������Ŀ

    //public WebScoreScaleRatio[] WebScoreSizeRatio;

    public float TimeNormalScene = 240F;//��ͨ��������ʱ�� 
    [System.NonSerialized]
    public int ScreenNumUsing = 1;//ʹ�õ�����Ļ��

    [System.NonSerialized]
    public bool IsRightControlBoard = true;//�Ƿ�ͨ����֤�Ŀ��ư�
//#if UNITY_EDITOR
	public bool OneKillDie = false;//

//#endif
    public float GameSpeed = 1F;//��Ϸ�ٶ�
    //public float 
    [System.NonSerialized]
    public INemoControlIO ArcIO;//��������
    
    //�������
    public GameObject Prefab_GOPrepareEnterBS;//��������
    public GameDataViewer Prefab_GameDataViewer;//����,��Ϸ����С���
    public GameObject Prefab_SprGameErrorBG;//��Ϸ�����ʾ����
    public tk2dTextMesh Prefab_TextGameError;//��Ϸ�����ʾ��Ϣ

    public LanguageItem LI_USBNotAvailable;//���ػ�:usb�豸������
    public LanguageItem[] LI_BoardNotAvailable;//���ػ�:���ư�ͨ�Ź���(���к�0:����,���к�1:˫��)
    public LanguageItem LI_RunTimeout;//���ػ�:����ʱ�䵽,    ����ʱ 21

    public int[] GunStylesScore;//���ݸ÷����ı�ǹ����,��:= new int[]{49,999,10000}����: 1-49,������;50~999,������; 1000~10000�����Ĺ���
    //public List<Fish> FishInScene;//�ڳ����ϵ���
    public bool IsAutoStart = false;
    //��̬����
    public static float WorldWidth = 3.555555555555556F;
    public static float WorldHeight = 2F;

    public static bool IsEditorShutdown = false;//�����Ƿ��Ѿ��ر�(������Editorģʽ��,����ǳ����Ѿ��ر�,�򲻴�������)
    public static State State_;//GameMain״̬
    
    //˽�б���
    private static GameMain mSingleton;
    private int mCurrentControlPlayerId = 0;
    private bool mIsEnteringBackstage = false;//�Ƿ����ڽ����̨
    private GameDataViewer mCurGameDataViewer;

    private bool mDisableAllPlayerKey = false;
    private bool mDisableBackGroundKey = false;

    private static float mLocalY_usbError = 49.5F;//usb�豸������ y��λ
    private static float mLocalY_remainTimeZero = 0F;//ʣ��ʱ��Ϊ0,y��λ
    private static float mLocalY_ioBoardError = -49.5F;//���ư�ͨ�Ź���,y��λ

    private GameObject mGoUSBErroHint;//Usb������ʾ
    private GameObject mGoIoBoardErroHint;//���ư�������ʾ

    private int[] mPControllerIDToPlayerID;//CtrlIDתPlayerID (����������PControllerID,������PlayerID)

    private bool[] mControlBoardState;//���ư�����״̬,�� Handle_CtrlBoardStateChanged ����true:����,false:�Ͽ�

    public static bool IsMainProcessPause = false;//��������ͣ
    //����
    public static GameMain Singleton
    {
        get
        {
            if (mSingleton == null)
            {
                mSingleton = GameObject.FindObjectOfType(typeof(GameMain)) as GameMain; 
                if (mSingleton!=null && mSingleton.ArcIO == null)
                {
                    mSingleton.ArcIO = mSingleton.GetComponent("INemoControlIO") as INemoControlIO;

                    INemoControlIO arcio = mSingleton.ArcIO;
                    //ArcIO = GetComponent("INemoControlIO") as INemoControlIO;
                    if (arcio != null)
                    {
                        arcio.EvtKey += mSingleton.Handle_ArcKeyEvent;
                        //arcio.HandlerBackGroundKeyEvent += Handle_ArcBackStageKeyEvent;
                        arcio.EvtInsertCoin += mSingleton.Hanlde_ArcInsertCoin;
                        //NemoUsbHid usbHid = arcio.GetComponent<NemoUsbHid>();
                        arcio.EvtOpened += mSingleton.Handle_UsbHid_Opened;
                        arcio.EvtClosed += mSingleton.Handle_UsbHid_Closed;
                        arcio.EvtHardwareInfo += mSingleton.Handle_HardwareInfo;
                        arcio.EvtCtrlBoardStateChanged += mSingleton.Handle_CtrlBoardStateChanged;

#if !UNITY_EDITOR//�ڱ༭��ģʽĬ�ϲ�����Ӳ��
#if ENABLE_MCU_VERIFY
                        mSingleton.IsRightControlBoard = false;
#endif
#endif
                        arcio.Open();

                    }

                    
                }
            }
            return mSingleton;
        }
    }

    /// <summary>
    /// �Ƿ�����Ļ
    /// </summary>
    /// <param name="glt"></param>
    /// <returns></returns>
    public bool IsScreenNet()
    {
        return BSSetting.GunLayoutType_.Val >= GunLayoutType.L_L2S;
    }

    /// <summary>
    /// �Ƿ����һ����Ļ
    /// </summary>
    /// <returns></returns>
    public bool IsMoreThanOneScreen( )
    {
        return BSSetting.GunLayoutType_.Val >= GunLayoutType.S_L6S && BSSetting.GunLayoutType_.Val <= GunLayoutType.L_W16S;
    }

    /// <summary>
    /// �Ƿ񵥷ֻ�
    /// </summary>
    /// <returns></returns>
    public bool IsSingleContorlBoard()
    {
        switch(BSSetting.GunLayoutType_.Val)
        {
            case GunLayoutType.L1:
            case GunLayoutType.L2:
            case GunLayoutType.L3:
            case GunLayoutType.L4:
            case GunLayoutType.W4:
            case GunLayoutType.W6:
            case GunLayoutType.W8:
            case GunLayoutType.W10:
            case GunLayoutType.S_L6D:
            case GunLayoutType.S_L8D:
            case GunLayoutType.S_W8D:
            case GunLayoutType.L_L2D:
            case GunLayoutType.L_L4D:
            case GunLayoutType.L_L6D:
            case GunLayoutType.L_L8D:
            case GunLayoutType.L_W8D:
            case GunLayoutType.L_W10D: 
                return true;
            
        }
        return false;

    }
    /// <summary>
    /// �Ƿ��ں�̨��
    /// </summary>
    public bool IsInBackstage
    {
        get
        {
            return mIsEnteringBackstage;//���ø�����,��Ϊ�����̨��Ҳ����ı�Ϊtrue
        }
    }

    /// <summary>
    /// ͨ��script oderһ�����ͷ��ʼ��
    /// </summary>
    void Awake()
    {
        mSingleton = Singleton;//��ʼ������
        //Singleton.ArcIO.Open();

        EvtInputKey += Handle_InputKey;


        State_ = State.Idle;

        //ʹ�õ���Ļ����
        ScreenNumUsing = IsMoreThanOneScreen() ? 2 : 1;

        //��ʼ�� 2D���緶Χ
        WorldDimension.x = Defines.WorldDimensionUnit.x - Defines.WorldDimensionUnit.width * 0.5F * (ScreenNumUsing - 1);
        WorldDimension.y = Defines.WorldDimensionUnit.y;
        WorldDimension.width = Defines.WorldDimensionUnit.width * ScreenNumUsing;
        WorldDimension.height = Defines.WorldDimensionUnit.height;


        //����Ÿ�ֵ
        for (int i = 0; i != FishGenerator.Prefab_FishAll.Length; ++i)
        {
            FishGenerator.Prefab_FishAll[i].TypeIndex = i;
        }


        mPControllerIDToPlayerID = new int[Defines.MaxNumPlayer];
        //��ʼ������
        Defines.NumPlayer = Defines.GunLayoutTypeToNumPlay[(int)BSSetting.GunLayoutType_.Val];

        
       


 
        //���÷ֱ���
#if UNITY_EDITOR
        UnityEditor.PlayerSettings.defaultScreenWidth = Defines.ScreenWidthUnit * ScreenNumUsing;
        UnityEditor.PlayerSettings.defaultScreenHeight = Defines.ScreenHeightUnit ;
#else
        Screen.SetResolution(Defines.ScreenWidthUnit * ScreenNumUsing,Defines.ScreenHeightUnit,false);
#endif
        //��ʾUSB������Ϣ
#if !UNITY_EDITOR
        if(!ArcIO.IsOpen())
            ViewUsbHidErroHint();
#endif

        //�����Ƿ���֡
        //Application.targetFrameRate = 60;

        mControlBoardState = new bool[ScreenNumUsing];
        for (int i = 0; i != ScreenNumUsing; ++i)
            mControlBoardState[i] = true;

        Time.timeScale = GameSpeed;


    }

    void Start()
    {
        if (BSSetting.CodePrintCurrentAction.Val)//������ȫ��״̬,�۲���ҷ����Ƿ�ı�
        {
            Event_PlayerScoreChanged evtScoreChanged = null;
            evtScoreChanged = (Player p, int ScoreNew, int scoreChange) =>
            {
                BSSetting.CodePrintCurrentAction.Val = false;//��ԭ����һ��

                EvtPlayerScoreChanged -= evtScoreChanged;

            };
            EvtPlayerScoreChanged += evtScoreChanged;
        }


        mDisableAllPlayerKey = true;
        mDisableBackGroundKey = true;
        //if (GameStart_ != null)
        //{
        //    GameStart_.EvtGameInitFinish += Handle_GameInitFinish;
        //}
        //else
        //{
        //    Handle_GameInitFinish();
        //}
        if (IsAutoStart)
            StartCoroutine(_Coro_DelayStartGame());
    }

    /// <summary>
    /// ���������ű�������Start����StartGame�¼�,����Ӧ
    /// </summary>
    /// <returns></returns>
    IEnumerator _Coro_DelayStartGame()
    {
        yield return 0;
        StartGame();
    }
    //void OnDestroy()
    //{
    //    ArcIO.Close();
    //}

    /// <summary>
    /// ��ʼ��Ϸ
    /// </summary>
    public void StartGame()
    {
        if (State_ != State.Idle)
            return;
        State_ = State.Normal;


        //��ʼ���������
        Transform prefabPlayerCreatorTs = Prefab_PlayerCreatorSet[(int)BSSetting.GunLayoutType_.Val].transform;
        GameObject playersParent = new GameObject("Players");
        playersParent.transform.parent = transform;
        playersParent.transform.localPosition = Vector3.zero;
        Players = new Player[Defines.NumPlayer];

        foreach (Transform t in prefabPlayerCreatorTs)
        {

            PlayerCreator pc = t.GetComponent<PlayerCreator>();
            Player pNew = pc.CreatePlayer();
            pNew.transform.parent = playersParent.transform;

            Players[pNew.Idx] = pNew;
            mPControllerIDToPlayerID[pNew.CtrllerIdx] = pNew.Idx;
        }

        //�����а���
        mDisableAllPlayerKey = false;
        mDisableBackGroundKey = false;
        //��ʱ���
        //StartCoroutine(_Coro_CheckRemainRunTime());

        //StartCoroutine(_Coro_DeleteNullFishInScene());

        //�Ѷȵ���
        if (BSSetting.Dat_RemoteDiffucltFactor.Val != 0)
        {
            GameOdds.DifficultFactor = (float)(BSSetting.Dat_RemoteDiffucltFactor.Val + 1);
        }
        
        GameOdds.GainRatio = GameOdds.GainRatios[(int)(BSSetting.GameDifficult_.Val)];
        GameOdds.GainRatioConditFactor = GameOdds.GainRatioConditFactors[(int)(BSSetting.ArenaType_.Val)];
        //ScenePrelude pl = ScenePreludeMgr.DoPrelude();

        StartCoroutine("_Coro_MainProcess");
        
        Pool_GameObj.Init();
 
        if (BSSetting.IsNeedPrintCodeAtGameStart.Val)
        {
            EnterPrintCode();
        }

        //SceneBGMgr.NewBG();

        if (EvtMainProcess_StartGame != null)
            EvtMainProcess_StartGame();
    }
  
    void Handle_ArcKeyEvent(int ctrllerID, NemoCtrlIO_Key k, bool downOrUp)
    {
        if (ctrllerID == 21)//��̨����
        {
            Handle_ArcBackStageKeyEvent(k, downOrUp);
            return;
        }
        if (!mDisableAllPlayerKey)
        {
            switch (k)
            {
                case NemoCtrlIO_Key.Up:
                    EvtInputKey(mPControllerIDToPlayerID[ctrllerID], HpyInputKey.Up, downOrUp);
                    break;
                case NemoCtrlIO_Key.Down:
                    EvtInputKey(mPControllerIDToPlayerID[ctrllerID], HpyInputKey.Down, downOrUp);
                    break;
                case NemoCtrlIO_Key.Left:
                    EvtInputKey(mPControllerIDToPlayerID[ctrllerID], HpyInputKey.Left, downOrUp);
                    break;
                case NemoCtrlIO_Key.Right:
                    EvtInputKey(mPControllerIDToPlayerID[ctrllerID], HpyInputKey.Right, downOrUp);
                    break;
                case NemoCtrlIO_Key.B:
                    EvtInputKey(mPControllerIDToPlayerID[ctrllerID], HpyInputKey.Fire, downOrUp);
                    break;
                case NemoCtrlIO_Key.A:
                    EvtInputKey(mPControllerIDToPlayerID[ctrllerID], HpyInputKey.Advance, downOrUp);
                    break;
                case NemoCtrlIO_Key.D:
                    EvtInputKey(mPControllerIDToPlayerID[ctrllerID], HpyInputKey.ScoreUp, downOrUp);
                    break;
                case NemoCtrlIO_Key.C:
                    EvtInputKey(mPControllerIDToPlayerID[ctrllerID], HpyInputKey.ScoreDown, downOrUp);
                    break;
                case NemoCtrlIO_Key.F:
                    EvtInputKey(mPControllerIDToPlayerID[ctrllerID], HpyInputKey.OutBounty, downOrUp);
                    break;
            }
        
        }
   
        
    }
    void Handle_ArcBackStageKeyEvent(NemoCtrlIO_Key k, bool downOrUp)
    {
        if (!mDisableBackGroundKey)
        {
            switch (k)
            {
                case NemoCtrlIO_Key.Up:
                    EvtInputKey(0, HpyInputKey.BS_Up, downOrUp);
                    break;
                case NemoCtrlIO_Key.Down:
                    EvtInputKey(0, HpyInputKey.BS_Down, downOrUp);
                    break;
                case NemoCtrlIO_Key.Left:
                    EvtInputKey(0, HpyInputKey.BS_Left, downOrUp);
                    break;
                case NemoCtrlIO_Key.Right:
                    EvtInputKey(0, HpyInputKey.BS_Right, downOrUp);
                    break;
                case NemoCtrlIO_Key.A:
                    EvtInputKey(0, HpyInputKey.BS_Confirm, downOrUp);
                    break;
                case NemoCtrlIO_Key.B:
                    EvtInputKey(0, HpyInputKey.BS_Cancel, downOrUp);
                    break;
                case NemoCtrlIO_Key.C:
                    EvtInputKey(0, HpyInputKey.BS_GameLite, downOrUp);
                    break;
            }
        }
    
        
    }

    void Hanlde_ArcInsertCoin(uint count, int ctrllerID)
    { 
        if (!mIsEnteringBackstage)
        {
            int num = (int)count;

            Players[mPControllerIDToPlayerID[ctrllerID]].ChangeScore(num * BSSetting.InsertCoinScoreRatio.Val);

            BSSetting.His_CoinInsert.Val += num;
            BSSetting.His_GainCurrent.Val += num;
            BSSetting.UpdateGainCurrentAndTotal();
        }
    }

    IEnumerator _Coro_MainProcess()
    {
		if (EvtMainProcess_FirstEnterScene != null)
            EvtMainProcess_FirstEnterScene();

        float tmpTime = 0F;
        while (true)
        {
            State_ = State.Normal;
            SoundMgr.PlayNewBgm();

            FishGenerator.StartFishGenerate(); 
            //yield return new WaitForSeconds(TimeNormalScene);

            tmpTime = Time.time;
            while (IsMainProcessPause || Time.time - tmpTime < TimeNormalScene)
            {
                yield return 0;
            }
 
            State_ = State.BeforeSweeping_WaitBulletClear;
            //*׼������
            if (EvtMainProcess_PrepareChangeScene != null)
                EvtMainProcess_PrepareChangeScene();

            //ֹͣ��ҹ���
            foreach (Player p in Players)
            {
                p.GunInst.Fireable = false;
                p.CanChangeScore = false;
            } 
            //�ȴ����������ӵ���ʧ
            Player[] tmpPlayers = new Player[Players.Length];
            for (int i = 0; i != Players.Length; ++i)
                tmpPlayers[i] = Players[i];
            while (true)
            {
                float numPlayerHaventBullet = 0;
                for (int i = 0; i != tmpPlayers.Length; ++i)
                {
                    if (tmpPlayers[i] != null && tmpPlayers[i].GunInst.NumBulletInWorld == 0)
                    {
                        tmpPlayers[i] = null;
                    }
                    if (tmpPlayers[i] == null)
                        ++numPlayerHaventBullet;
                }
                if (numPlayerHaventBullet == tmpPlayers.Length)
                    break;
                yield return 0;
            }
            State_ = State.Sweeping;
            //ֹͣ����
            FishGenerator.StopFishGenerate();
            SoundMgr.StopBgm();

            //��ʼ���Ź���,����
            SceneBGMgr.Sweep();
            yield return new WaitForSeconds(SceneBGMgr.UseTime+0.3F);
            State_ = State.Preluding;

            SoundMgr.PlayNewBgm();

            if (EvtMainProcess_FinishChangeScene != null)
                EvtMainProcess_FinishChangeScene();
            //�ָ���ҹ���
            foreach (Player p in Players)
            {
                p.GunInst.Fireable = true;
                p.CanChangeScore = true;
            }
            

            //��������

            ScenePrelude pl = ScenePreludeMgr.DoPrelude();
            //string preludeName = pl.gameObject.name;
            //Debug.Log("sceneprelude start = " + preludeName+ "   fish num = "+NumFishAlive);
            bool waitPrelude = true;
            pl.Evt_PreludeEnd += () => { waitPrelude = false; };
            while (waitPrelude)
            {
                yield return new WaitForSeconds(0.1F);
            }
            //Debug.Log("sceneprelude end = " + preludeName + "   fish num = " + NumFishAlive);
 
            //loop:��һ����������
            if (EvtMainProcess_FinishPrelude != null)
                EvtMainProcess_FinishPrelude();
        }
    }

    //�����Ϸʣ��ʱ��
    IEnumerator _Coro_CheckRemainRunTime()
    {
        yield return 0;
        while (true)
        {
            if (BSSetting.GetRemainRuntime() <= 0)
            {
                //��ʾ��ʱ��Ϣ
                for (int i = 0; i != ScreenNumUsing; ++i)
                {
                    GameObject goErrBG = Instantiate(Prefab_SprGameErrorBG) as GameObject;
                    goErrBG.transform.parent = transform;

                    goErrBG.transform.localPosition = new Vector3(WorldDimension.x + (0.5F + i) * Defines.WorldDimensionUnit.width, mLocalY_remainTimeZero, 0.03F);
                    tk2dTextMesh textErr = Instantiate(Prefab_TextGameError) as tk2dTextMesh;
                    textErr.transform.parent = goErrBG.transform;
                    textErr.transform.localPosition = new Vector3(0F, 0F, -0.001F);
                    textErr.text = LI_RunTimeout.CurrentText;// "����ʱ�䵽,    ����ʱ 21";
                    textErr.Commit();
                }
                

                while(true)
                {
                    foreach (Player p in Players)
                        p.GunInst.Fireable = false;

                    yield return 0;
                }
            }

            yield return new WaitForSeconds(30F);
        }
    }

    //�����ư�,USB�豸�Ƿ����
    //IEnumerator _Coro_CheckHardwareState()
    //{
    //    bool hardWareRespon = false;
    //    bool isUsbOk = true;
    //    bool isArcOk= true;
    //    ArcIO.HandlerStateCheck += (bool usbOk, bool arcOk) =>
    //    {
    //        isUsbOk = usbOk;
    //        isArcOk = arcOk;
    //        hardWareRespon = true;
    //    };

    //    while (true)
    //    {
    //        ArcIO.CheckState();//���ͼ������
    //        yield return new WaitForSeconds(1F);
    //        if (hardWareRespon)
    //        {
    //            if (!isArcOk)
    //            {
    //                tk2dSprite sprErrBG = Instantiate(Prefab_SprGameErrorBG) as tk2dSprite;
    //                sprErrBG.transform.parent = transform;
    //                sprErrBG.transform.localPosition = new Vector3(0, mLocalY_ioBoardError, 0.03F);
    //                tk2dTextMesh textErr = Instantiate(Prefab_TextGameError) as tk2dTextMesh;
    //                textErr.transform.parent = sprErrBG.transform;
    //                textErr.transform.localPosition = new Vector3(0F, 0F, -0.001F);
    //                textErr.text = "���ư�ͨ�Ź���";
    //                textErr.Commit();
    //            }
    //        }
    //    }
    //}

    //IEnumerator _Coro_DeleteNullFishInScene()
    //{
    //    while (true)
    //    {
    //        yield return new WaitForSeconds(20);
    //        List<Fish> fishToDelete = new List<Fish>();
    //        foreach (Fish f in FishInScene)
    //        {
    //            if (f == null)
    //                fishToDelete.Add(f);
    //        }
    //        foreach (Fish f in fishToDelete)
    //        {
    //            FishInScene.Remove(f);
                
    //        }
    //    }
    //}

    //IEnumerator _Coro_ClearPool()
    //{
    //    List<Pool_GameObj> clearQueue = new List<Pool_GameObj>();
    //    while (true)
    //    {
    //        foreach (KeyValuePair<int, Pool_GameObj> kvp in Pool_GameObj.msPoolsDict)
    //        {
    //            clearQueue.Add(kvp.Value);
    //        }

    //        foreach (Pool_GameObj obj in clearQueue)
    //        {
    //            obj.GC_Lite();
    //        }
    //        clearQueue.Clear();
    //        yield return new WaitForSeconds(Defines.Interval_PoolGC);
    //    }
    //}

    bool mIsAutoFiring = true;
    void Update()
    {
        if (mDisableAllPlayerKey)
            goto TAG_BackStageKeyProcess;
        //ģ�ⰴ��
        if (  Input.GetKeyDown(KeyCode.P))//�������һ�𿪻�
        {
            if (EvtInputKey != null)
                for (int i = 0; i != Defines.NumPlayer; ++i )
                    EvtInputKey(i, HpyInputKey.Fire, mIsAutoFiring);
            mIsAutoFiring = !mIsAutoFiring;
        }
        if (Input.GetKeyDown(KeyCode.J))
        {
            if (EvtInputKey != null)
                EvtInputKey(mCurrentControlPlayerId, HpyInputKey.Fire,true);
        }
        else if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.K))//�������һ���ǿ
        {
            if (EvtInputKey != null)
                for(int i = 0; i != Defines.NumPlayer; ++i)
                    EvtInputKey(i, HpyInputKey.Advance, true);
        }
        else if (Input.GetKeyDown(KeyCode.K))
        {
            if (EvtInputKey != null)
                EvtInputKey(mCurrentControlPlayerId, HpyInputKey.Advance, true);
        }
        else if (Input.GetKeyDown(KeyCode.L))
        {
            if (EvtInputKey != null)
                EvtInputKey(mCurrentControlPlayerId, HpyInputKey.OutBounty, true);
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            if (EvtInputKey != null)
                EvtInputKey(mCurrentControlPlayerId, HpyInputKey.Up, true);
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            if (EvtInputKey != null)
                EvtInputKey(mCurrentControlPlayerId, HpyInputKey.Down, true);
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            if (EvtInputKey != null)
                EvtInputKey(mCurrentControlPlayerId, HpyInputKey.Left, true);
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            if (EvtInputKey != null)
                EvtInputKey(mCurrentControlPlayerId, HpyInputKey.Right, true);
        }
        else if (Input.GetKeyDown(KeyCode.O))
        {
            mCurrentControlPlayerId = (mCurrentControlPlayerId + 1) % Defines.NumPlayer;
        }
        else if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.LeftBracket))//�������һ���·�
        {
            if (EvtInputKey != null)
            {
                for (int i = 0; i != Defines.NumPlayer; ++i)
                    EvtInputKey(i, HpyInputKey.ScoreDown, true);
            }
        }
        else if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Equals))//�������һ���Ϸ� *100
        {
            if (EvtInputKey != null)
            {
                for (int i = 0; i != Defines.NumPlayer; ++i)
                {
                    for (int j = 0; j != 10; ++j)
                        Players[i].ScoreUp(true);
                }
            }
        }
        else if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.RightBracket))//�������һ���Ϸ�
        {
            if (EvtInputKey != null)
            {
                for (int i = 0; i != Defines.NumPlayer; ++i)
                    EvtInputKey(i, HpyInputKey.ScoreUp, true);
            }
        }
        else if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            if (EvtInputKey != null)
                EvtInputKey(mCurrentControlPlayerId, HpyInputKey.ScoreDown, true);
        }
        else if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            if (EvtInputKey != null)
                EvtInputKey(mCurrentControlPlayerId, HpyInputKey.ScoreUp, true);
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            if (EvtInputKey != null)
                EvtInputKey(mCurrentControlPlayerId, HpyInputKey.OutBounty, true);
        }
         
   

        ////////////////////////////////keyup/////////////////////////////////
        if (Input.GetKeyUp(KeyCode.J))
        {
            if (EvtInputKey != null)
                EvtInputKey(mCurrentControlPlayerId, HpyInputKey.Fire, false);
        }
        else if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyUp(KeyCode.K))
        {
            if (EvtInputKey != null)
                for (int i = 0; i != Defines.NumPlayer; ++i )
                    EvtInputKey(i, HpyInputKey.Advance, false);
        }
        else if (Input.GetKeyUp(KeyCode.K))
        {
            if (EvtInputKey != null)
                EvtInputKey(mCurrentControlPlayerId, HpyInputKey.Advance, false);
        }
        else if (Input.GetKeyUp(KeyCode.W))
        {
            if (EvtInputKey != null)
                EvtInputKey(mCurrentControlPlayerId, HpyInputKey.Up, false);
        }
        else if (Input.GetKeyUp(KeyCode.S))
        {
            if (EvtInputKey != null)
                EvtInputKey(mCurrentControlPlayerId, HpyInputKey.Down, false);
        }
        else if (Input.GetKeyUp(KeyCode.A))
        {
            if (EvtInputKey != null)
                EvtInputKey(mCurrentControlPlayerId, HpyInputKey.Left, false);
        }
        else if (Input.GetKeyUp(KeyCode.D))
        {
            if (EvtInputKey != null)
                EvtInputKey(mCurrentControlPlayerId, HpyInputKey.Right, false);
        }
        else if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyUp(KeyCode.LeftBracket))
        {
            if (EvtInputKey != null)
            {
                for (int i = 0; i != Defines.NumPlayer; ++i)
                    EvtInputKey(i, HpyInputKey.ScoreDown, false);
            }
        }
        else if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyUp(KeyCode.RightBracket))
        {
            if (EvtInputKey != null)
            {
                for (int i = 0; i != Defines.NumPlayer; ++i)
                    EvtInputKey(i, HpyInputKey.ScoreUp, false);
            }
        }
        else if (Input.GetKeyUp(KeyCode.LeftBracket))
        {
            if (EvtInputKey != null)
                EvtInputKey(mCurrentControlPlayerId, HpyInputKey.ScoreDown, false);
        }
        else if (Input.GetKeyUp(KeyCode.RightBracket))
        {
            if (EvtInputKey != null)
                EvtInputKey(mCurrentControlPlayerId, HpyInputKey.ScoreUp, false);
        }
        else if (Input.GetKeyUp(KeyCode.E))
        {
            if (EvtInputKey != null)
                EvtInputKey(mCurrentControlPlayerId, HpyInputKey.OutBounty, false);
        }

        TAG_BackStageKeyProcess:
        //��̨
        if (!mDisableBackGroundKey)
        {
            if (Input.GetKeyDown(KeyCode.N))
            {
                if (EvtInputKey != null)
                    EvtInputKey(mCurrentControlPlayerId, HpyInputKey.BS_Confirm, true);
            }
            else if (Input.GetKeyDown(KeyCode.M))
            {
                if (EvtInputKey != null)
                    EvtInputKey(mCurrentControlPlayerId, HpyInputKey.BS_Cancel, true);
            }
            else if (Input.GetKeyDown(KeyCode.F))
            {
                if (EvtInputKey != null)
                    EvtInputKey(mCurrentControlPlayerId, HpyInputKey.BS_Up, true);
            }
            else if (Input.GetKeyDown(KeyCode.V))
            {
                if (EvtInputKey != null)
                    EvtInputKey(mCurrentControlPlayerId, HpyInputKey.BS_Down, true);
            }
            else if (Input.GetKeyDown(KeyCode.C))
            {
                if (EvtInputKey != null)
                    EvtInputKey(mCurrentControlPlayerId, HpyInputKey.BS_Left, true);
            }
            else if (Input.GetKeyDown(KeyCode.B))
            {
                if (EvtInputKey != null)
                    EvtInputKey(mCurrentControlPlayerId, HpyInputKey.BS_Right, true);
            }

            //��̨keyup 
            if (Input.GetKeyUp(KeyCode.N))
            {
                if (EvtInputKey != null)
                    EvtInputKey(mCurrentControlPlayerId, HpyInputKey.BS_Confirm, false);
            }
            else if (Input.GetKeyUp(KeyCode.M))
            {
                if (EvtInputKey != null)
                    EvtInputKey(mCurrentControlPlayerId, HpyInputKey.BS_Cancel, false);
            }
            else if (Input.GetKeyUp(KeyCode.F))
            {
                if (EvtInputKey != null)
                    EvtInputKey(mCurrentControlPlayerId, HpyInputKey.BS_Up, false);
            }
            else if (Input.GetKeyUp(KeyCode.V))
            {
                if (EvtInputKey != null)
                    EvtInputKey(mCurrentControlPlayerId, HpyInputKey.BS_Down, false);
            }
            else if (Input.GetKeyUp(KeyCode.C))
            {
                if (EvtInputKey != null)
                    EvtInputKey(mCurrentControlPlayerId, HpyInputKey.BS_Left, false);
            }
            else if (Input.GetKeyUp(KeyCode.B))
            {
                if (EvtInputKey != null)
                    EvtInputKey(mCurrentControlPlayerId, HpyInputKey.BS_Right, false);
            }
        }
        
    }

    void Handle_InputKey(int p, HpyInputKey k, bool down)
    {
        Players[p].OnKeyDown(k, down);

        if (down && k == HpyInputKey.BS_Confirm)
        {
            StartCoroutine("_Coro_PrepareEnterBackStage");
        }
        else if (!down && k == HpyInputKey.BS_Confirm)
        {
            StopCoroutine("_Coro_PrepareEnterBackStage");
        }

        if (down && k == HpyInputKey.OutBounty)
        {
            Players[p].OutBountyButtom();
        }

        if (down && (k == HpyInputKey.BS_Up || k == HpyInputKey.BS_Down))
        {
            if (!mIsEnteringBackstage)
            {
                if (mCurGameDataViewer == null)
                {
                    mCurGameDataViewer = Instantiate(Prefab_GameDataViewer) as GameDataViewer;
                    mCurGameDataViewer.IsOnlyViewNumber = k == HpyInputKey.BS_Up ? false : true;
                    mCurGameDataViewer.transform.position = new Vector3(WorldDimension.xMin, WorldDimension.yMax, Defines.GlobleDepth_GameDataViewer);
                    if (ScreenNumUsing > 1)
                    {
                        for (int i = 0; i != ScreenNumUsing - 1; ++i)
                        {
                            GameDataViewer gdv = Instantiate(Prefab_GameDataViewer) as GameDataViewer;
                            gdv.transform.parent = mCurGameDataViewer.transform;
                            gdv.transform.position = new Vector3(WorldDimension.xMin + Defines.WorldDimensionUnit.width * (i + 1)
                                , WorldDimension.yMax, Defines.GlobleDepth_GameDataViewer);
                        }
                    }
                }
                else
                {
                    Destroy(mCurGameDataViewer.gameObject);
                    mCurGameDataViewer = null;
                }
            }

        }
    }

    public void EnterBackstage()
    {
        if (!mIsEnteringBackstage)
        {
            mIsEnteringBackstage = true;
            StartCoroutine(_Coro_EnterBackstage());
        }
    }
    void EnterPrintCode()
    {
 
        mIsEnteringBackstage = true;
        mDisableAllPlayerKey = true;
        //ֹͣ��ҹ���
        foreach (Player p in Players)
        {
            p.GunInst.Fireable = false;
        }

        //ֹͣ����
        FishGenerator.StopFishGenerate();

        BackstageMain bsMain = Instantiate(Prefab_Backstage) as BackstageMain;
        bsMain.EnterPrintCode();
        State_ = State.BackStage;
    }

    IEnumerator _Coro_PrepareEnterBackStage()
    {
        yield return new WaitForSeconds(1F);
        EnterBackstage();
    }

    //void OnGUI()
    //{
    //    if (GUILayout.Button("fish"))
    //    {
    //        int nullFishNum = 0;
    //        foreach (Fish fNull in FishInScene)
    //        {
    //            if (fNull == null)
    //                ++nullFishNum;
    //        }
    //        Debug.Log("nullFishNum = " + nullFishNum);
    //    }
    //}
    IEnumerator _Coro_EnterBackstage()
    {
        

        //ֹͣ��ҹ���
        foreach (Player p in Players)
        {
            p.GunInst.Fireable = false;
        }

       

        //��������
        GameObject[] goPrepareEnterBSs = new GameObject[ScreenNumUsing];
        for(int i = 0; i != ScreenNumUsing; ++i)
        {
            goPrepareEnterBSs[i] = Instantiate(Prefab_GOPrepareEnterBS) as GameObject;
            goPrepareEnterBSs[i].transform.position = new Vector3(WorldDimension.x + WorldDimension.width * 0.5F * i, WorldDimension.y+WorldDimension.height, Defines.GlobleDepth_PrepareInBG);

        }
        
        yield return new WaitForSeconds(3F);

        //�ȴ��ӵ�����
        Player[] tmpPlayers = new Player[Players.Length];
        for (int i = 0; i != Players.Length; ++i)
            tmpPlayers[i] = Players[i];
        while (true)
        {
            float numPlayerHaventBullet = 0;
            for (int i = 0; i != tmpPlayers.Length; ++i)
            {
                if (tmpPlayers[i] != null && tmpPlayers[i].GunInst.NumBulletInWorld == 0)
                {
                    tmpPlayers[i] = null;
                }
                if (tmpPlayers[i] == null)
                    ++numPlayerHaventBullet;
            }
            if (numPlayerHaventBullet == tmpPlayers.Length)
                break;
            yield return 0;
        }

	//todo ������ĳЩ������ ��ֹ�����bug,������
        StopCoroutine("_Coro_MainProcess");
	
        //ֹͣ����
        FishGenerator.StopFishGenerate();

        Instantiate(Prefab_Backstage);
        foreach (GameObject go in goPrepareEnterBSs)
        {
            Destroy(go);
        }
        if (mCurGameDataViewer != null)
        {
            Destroy(mCurGameDataViewer.gameObject);
            mCurGameDataViewer = null;
        }

        mDisableAllPlayerKey = true;
        State_ = State.BackStage;
    }


    void Handle_UsbHid_Opened()
    {
        if (mGoUSBErroHint != null)
        {
            Destroy(mGoUSBErroHint);
            mGoUSBErroHint = null;
        }

#if ENABLE_MCU_VERIFY
        //����Ӳ����Ϣ
        ArcIO.RequestHardwareInfo();
#endif
    }

    void Handle_UsbHid_Closed()
    {
        if (mGoUSBErroHint != null)
        {
            Destroy(mGoUSBErroHint);
            mGoUSBErroHint = null;
        }
        //[bug],��Ϊ��OnDestroy����,Unity���ܻ����Some objects were not cleaned up when closing the scene������ʾ
        if(!IsEditorShutdown)
            ViewUsbHidErroHint();
    }

    void Handle_HardwareInfo(int gameIdx, int mainVersion, int subVersion, bool isVerifySucess)
    {
        StartCoroutine(_Delay_Handle_HardwareInfo(gameIdx, mainVersion, subVersion, isVerifySucess));
    }

    IEnumerator _Delay_Handle_HardwareInfo(int gameIdx, int mainVersion, int subVersion, bool isVerifySucess)
    {
        yield return new WaitForSeconds(1F);
        HideUsbHidErroHint();
        if (GameIdx == -1)//ֻ��֤���ܰ����
        {
            if (isVerifySucess)
            {
                IsRightControlBoard = true;
            }
            else
            {
                ViewUsbHidErroHint();
                IsRightControlBoard = false;
            }
            yield break;
        }
        else//���汾��
        {
            if (isVerifySucess && GameIdx == gameIdx)
            {
                IsRightControlBoard = true;
            }
            else
            {
                ViewUsbHidErroHint();
                IsRightControlBoard = false;
            }
        }
    }

    /// <summary>
    /// ��Ҫ��ʱ������ư�״̬,��Ϊbss�ı���δ��ʼ��
    /// </summary>
    /// <param name="controlBoardId"></param>
    /// <param name="connectState"></param>
    /// <returns></returns>
    IEnumerator _Delay_Handle_CtrlBoardStateChanged(int controlBoardId, bool connectState)
    {
        yield return new WaitForSeconds(1F);
        string[] strViewStringAry = new string[ScreenNumUsing];
        if (IsSingleContorlBoard())//���ֻ�ģʽ
        {

            if (controlBoardId == 0)//���ֻ���ģʽʱ,ֻ�е�һ��ֻ���״̬�仯��������Ӧ
            {
                //Debug.Log("connect state = " + connectState);
                if (connectState)//������
                {
                    if (mGoIoBoardErroHint != null)
                    {
                        Destroy(mGoIoBoardErroHint);
                        mGoIoBoardErroHint = null;
                    }
                }
                else//û����
                {
                    if (mGoIoBoardErroHint == null)
                    {
                        for (int i = 0; i != ScreenNumUsing; ++i)
                        {
                            strViewStringAry[i] = string.Format(LI_BoardNotAvailable[ScreenNumUsing - 1].CurrentText, 1);

                            if (mGoIoBoardErroHint == null)
                            {
                                mGoIoBoardErroHint = _CreateErroHintBar(new Vector3(WorldDimension.x + (0.5F + i) * Defines.WorldDimensionUnit.width, mLocalY_ioBoardError, 0.03F), strViewStringAry[i]);
                            }
                            else
                            {
                                GameObject goHintSubGo = _CreateErroHintBar(new Vector3(WorldDimension.x + (0.5F + i) * Defines.WorldDimensionUnit.width, mLocalY_ioBoardError, 0.03F), strViewStringAry[i]);
                                goHintSubGo.transform.parent = mGoIoBoardErroHint.transform;
                            }
                        }
                    }
                    
                }
            }
        }
        else//˫�ֻ�ģʽ
        {
            if (mGoIoBoardErroHint != null)
            {
                Destroy(mGoIoBoardErroHint);
                mGoIoBoardErroHint = null;
            }

            for (int i = 0; i != ScreenNumUsing; ++i)
            {
                strViewStringAry[i] = string.Format(LI_BoardNotAvailable[ScreenNumUsing - 1].CurrentText, i + 1);// "�ֻ���" + (i + 1) + "ͨ�Ź���";
            }

 
            mControlBoardState[controlBoardId] = connectState;//��������״̬

            for (int i = 0; i != mControlBoardState.Length; ++i)
            {
                if(!mControlBoardState[i])//������״̬����ʾ
                {
                    if (mGoIoBoardErroHint == null)
                        mGoIoBoardErroHint = _CreateErroHintBar(new Vector3(WorldDimension.x + (0.5F + i) * Defines.WorldDimensionUnit.width, mLocalY_ioBoardError, 0.03F), strViewStringAry[i]);
                    else
                    {
                        GameObject goHintSubGo = _CreateErroHintBar(new Vector3(WorldDimension.x + (0.5F + i) * Defines.WorldDimensionUnit.width, mLocalY_ioBoardError, 0.03F), strViewStringAry[i]);
                        goHintSubGo.transform.parent = mGoIoBoardErroHint.transform;
                    }
                }
                
            }

        }
             
    }
    //����״̬�ı�
    void Handle_CtrlBoardStateChanged(int controlBoardId,bool connectState)
    {
        StartCoroutine(_Delay_Handle_CtrlBoardStateChanged(controlBoardId, connectState));
    }

    void ViewUsbHidErroHint()
    {
        //�ֻ��豸�ϲ���ʾ�Ҳ���USB�豸����
        return;
        for (int i = 0; i != ScreenNumUsing; ++i)
        {
            if (mGoUSBErroHint == null)
            {
                 
                mGoUSBErroHint = _CreateErroHintBar(new Vector3(WorldDimension.x + (0.5F + i) * Defines.WorldDimensionUnit.width, mLocalY_usbError, 0.03F), LI_USBNotAvailable.CurrentText);
            }
            else
            {
                GameObject goHintSubGo = _CreateErroHintBar(new Vector3(WorldDimension.x + (0.5F + i) * Defines.WorldDimensionUnit.width, mLocalY_usbError, 0.03F), LI_USBNotAvailable.CurrentText);
                goHintSubGo.transform.parent = mGoUSBErroHint.transform;
            }
        }

        ////mGoUSBErroHint = _CreateErroHintBar(new Vector3())
        //tk2dSprite sprErrBG = Instantiate(Prefab_SprGameErrorBG) as tk2dSprite;
        //mGoUSBErroHint = sprErrBG.gameObject;
        //sprErrBG.transform.parent = transform;
        //sprErrBG.transform.localPosition = new Vector3(0, mLocalY_usbError, 0.03F);
        //tk2dTextMesh textErr = Instantiate(Prefab_TextGameError) as tk2dTextMesh;
        //textErr.transform.parent = sprErrBG.transform;
        //textErr.transform.localPosition = new Vector3(0F, 0F, -0.001F);
        //textErr.text = "USB�豸������";
        //textErr.Commit();
    }

    void HideUsbHidErroHint()
    {
        if (mGoUSBErroHint != null)
        {
            Destroy(mGoUSBErroHint);
            mGoUSBErroHint = null;
        }
    }

    GameObject _CreateErroHintBar(Vector3 worldPos, string text)
    {
        GameObject goErrBG = Instantiate(Prefab_SprGameErrorBG) as GameObject;

        goErrBG.transform.parent = transform;
        goErrBG.transform.localPosition = worldPos;
        tk2dTextMesh textErr = Instantiate(Prefab_TextGameError) as tk2dTextMesh;
        textErr.transform.parent = goErrBG.transform;
        textErr.transform.localPosition = new Vector3(0F, 0F, -0.001F);
        textErr.text = text;
        textErr.Commit();
        return goErrBG;
    }
    void OnApplicationQuit()
    {
        IsEditorShutdown = true;
    }
}
