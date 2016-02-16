using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class Bullet : MonoBehaviour,IPoolObj {
    
    public static readonly Vector3 ColliderOffsetZ = Vector3.forward * 3000F;

    public float Speed = 1F;
    public int Score = 1;
    public int FishOddsMulti = 1;//�������ټӱ�.(����������X2)
    public tk2dTextMesh Text_CoinNum;
    
    public float RadiusStandardBoom = 0.175F;//��ը���뾶,��׼ֵ: ��ͨ�ӵ�:0.175F, ������:0.35
    public float ScaleCollider = 0.5F;//����ײĬ�ϴ�С,��IsScaleWebΪtrue����Ч,��gamemain�����й�
    public GameObject Prefab_GoAnisprBullet;//�ӵ�sprite prefab
    public StringSet Prefab_SpriteNameSet;//�ӵ�spr���ּ���
    [System.NonSerialized]
    public Player Owner;//����player
    [System.NonSerialized]
    public Rect MoveArea;//�ƶ�����

    
    
    private Fish mTargetFish;//������
    [System.NonSerialized]
    public bool IsLockingFish = false;//�Ƿ��������ӵ�
    private Vector3 mPosSaved;
    private bool mIsDestroyed = false;
    private Transform mTs;
    private GameObject mBulletGO;
 

    private GameObject mPrefab;

    public bool IsBezierWhenLocking = true;
    private bool mBezierMoveLR;//left = false ; right = true;
    private Vector3 mSLineCurPostion;//���ٵ���ǰ��λ��,���ڼ����ƶ��ٷֱ�
    //private Vector3 mSLineDistance;//���ٵ�����

    public GameObject Prefab
    {
        get { return mPrefab; }
        set { mPrefab = value; }

    }
    void Awake()
    {
 
        mTs = transform;
    }

    void CopyDataTo(Bullet tar)
    {
        tar.Speed = Speed;
        tar.Score = Score;
        tar.FishOddsMulti = FishOddsMulti;
        tar.RadiusStandardBoom = RadiusStandardBoom;
        //tar.IsScaleWeb = IsScaleWeb;
        tar.ScaleCollider = ScaleCollider;
        tar.mIsDestroyed = mIsDestroyed;
        tar.MoveArea = MoveArea;
    }

    public void On_Reuse(GameObject prefab)
    {
        gameObject.SetActive(true);
        prefab.GetComponent<Bullet>().CopyDataTo(this);
 
    }

    public void On_Recycle()
    {
        gameObject.SetActive(false);
        //gameObject.active = false;
        //gameObject.SetActiveRecursively(false);

        if (mBulletGO != null)
        {
            mBulletGO.SetActive(false);
            Pool_GameObj.RecycleGO(Prefab_GoAnisprBullet, mBulletGO);

            mBulletGO = null;
        }
        --Owner.GunInst.NumBulletInWorld;
        if (Owner.GunInst.NumBulletInWorld < 0)
            Debug.LogError("�ڳ��ӵ�������Ϊ��" + Owner.GunInst.NumBulletInWorld.ToString());
    }

    public void SelfDestroy()
    {
        if (mIsDestroyed)
            return;

        if (GameMain.EvtBulletDestroy != null)
            GameMain.EvtBulletDestroy(this);

        Pool_GameObj.RecycleGO(null, gameObject);
        mIsDestroyed = true;//Destroy��������ʹ��OnTriggerʧЧ,��ֹ���������ײ,
    }
 
    public void Fire(Player from, Fish tar,Quaternion dir)
    {
        GameMain gm = GameMain.Singleton;
        if (tar != null)
        {
            mTargetFish = tar;
            IsLockingFish = true;
            Vector3 targetDir = tar.transform.position - mTs.position;
            targetDir.z = 0F;
            targetDir.Normalize();
            targetDir = Quaternion.Euler(0F, 0F,Random.Range(-90F,90F)) * targetDir  +from.transform.up;//�ó��ӵ�ƫ��  Random.Range(0,2)==0?-90F:90F
            mTs.rotation =   Quaternion.FromToRotation(Vector3.up, targetDir);
            
        }
        else
        {
            IsLockingFish = false;
            mTs.rotation = dir;
        } 
        mTs.parent = gm.TopLeftTs;

        Text_CoinNum.text = Score.ToString();
        Text_CoinNum.Commit();

        Owner = from;
        ++Owner.GunInst.NumBulletInWorld;

        BackStageSetting bss = gm.BSSetting;

        if (bss.IsBulletCrossWhenScreenNet.Val && gm.IsScreenNet())
            MoveArea = gm.WorldDimension;
        else
            MoveArea = from.AtScreenArea;
 
        if (mBulletGO == null)
        {
            mBulletGO = Pool_GameObj.GetObj(Prefab_GoAnisprBullet);
            tk2dSpriteAnimator aniSprBullet = mBulletGO.GetComponent<tk2dSpriteAnimator>();
            if (aniSprBullet != null)
            {
                //aniSprBullet.clipId = aniSprBullet.GetClipIdByName(Prefab_SpriteNameSet.Texts[from.Idx % Prefab_SpriteNameSet.Texts.Length]);
                //Debug.Log(Prefab_SpriteNameSet.Texts[from.Idx % Prefab_SpriteNameSet.Texts.Length]);
                aniSprBullet.Play(Prefab_SpriteNameSet.Texts[from.Idx % Prefab_SpriteNameSet.Texts.Length]);
                //aniSprBullet.PlayFrom("1",0.1F);
            }

            mBulletGO.SetActive(true);
            mBulletGO.transform.parent = mTs;
            mBulletGO.transform.localPosition = Vector3.zero;
            mBulletGO.transform.localRotation = Quaternion.identity; 
            
        }

    }
 

	void Update () {
        if (mIsDestroyed)
            return;

        mPosSaved = mTs.position;
        
        if (IsLockingFish && mTargetFish != null)
        {
            if (!IsBezierWhenLocking)
            {
                Vector3 toward = mTargetFish.transform.position - mTs.position;
                toward.z = 0F;
                toward.Normalize();
                mTs.rotation = Quaternion.FromToRotation(Vector3.up, toward);
            }
            else
            {
                Vector3 toward = mTargetFish.transform.position - mTs.position;
                toward.z = 0F;

                Quaternion quatToTarget = Quaternion.FromToRotation(Vector3.up, toward);
                float limitDistance = 1F / toward.sqrMagnitude;
                if (limitDistance < 1F)
                    limitDistance = 1F;
                mTs.rotation = Quaternion.Slerp(mTs.rotation, quatToTarget, Time.deltaTime * Speed * 0.00651F * limitDistance);
            }
        }

        //if (!IsLockingFish || mTargetFish == null)
            mTs.position += Speed * Time.deltaTime * mTs.up;
        Vector3 curPos = mTs.position;

        if (curPos.y < MoveArea.yMin || curPos.y > MoveArea.yMax)
        {
            //curPos.y = -curPos.y;
            Vector3 dir = mTs.up;
            dir.y = -dir.y;
            mTs.up = dir;
            mTs.position = mPosSaved;
            Vector3 euler = mTs.localEulerAngles;
            euler.y = 180F;
            mTs.localEulerAngles = euler;
            IsLockingFish = false;
        }
        if (curPos.x < MoveArea.xMin || curPos.x > MoveArea.xMax)
        {
            Vector3 dir = mTs.up;
            dir.x = -dir.x;
            mTs.up = dir;
            mTs.position = mPosSaved;
            Vector3 euler = mTs.localEulerAngles;
            euler.y = 180F;
            mTs.localEulerAngles = euler;
            IsLockingFish = false;
        }



	}
    
    void OnTriggerEnter(Collider other)
    {
        if (mIsDestroyed)
            return;
 

        Fish fishFirst = other.GetComponent<Fish>();//���ӵ����е���
        if (fishFirst == null)
        {
            Debug.LogError("Fish�����ﲻ������null!");
            return;
        }

        if (IsLockingFish && mTargetFish != null && mTargetFish.ID != fishFirst.ID)//��Ŀ��
        {
            return;
        }

        HitProcessor.ProcessHit(this, fishFirst);
    }

}
