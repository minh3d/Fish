using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class HitProcessor:MonoBehaviour  
{
    //��ø���������ʱ���ӵĽ�������(ֻ���ڼ�����������,�����뽱���ɷ�.��:һ��100����,������������20,��ô�������ʼ���ı���Ϊ120,��������Ϊ100��)
    //public delegate int Func_GetFishOddAddtiveForDieRatio(Player killer, Bullet b, Fish f);
    //public static Func_GetFishOddAddtiveForDieRatio FuncGetFishAddtiveForDieRatio;
    /// <summary>
    /// ���в���
    /// </summary>
    /// <param name="isMiss">ture:��ײ��Ҳ������Ч</param>
    /// <param name="p"></param>
    /// <param name="b"></param>
    /// <param name="f"></param>
    public delegate void Event_HitMiss(bool isMiss, Player p, Bullet b, Fish f);
    public static Event_HitMiss Evt_Hit;//����,(��oddbonusΪ0ʱ����)

    public delegate void Event_HitResult(bool isKillFish, Player p, Bullet b, Fish f);
    public static Event_HitResult Evt_HitResult;//

    public delegate void Event_HitExtension(Bullet b, Fish f);
    public static Event_HitExtension Evt_HitExtension;//�������ײ���ʹ���


    public enum Operator
    {
        LastModule,
        Add
    }
    public class OperatorOddFix
    {
        public OperatorOddFix(Operator o, int v)
        {
            Op = o;
            Val = v;
        }
        public Operator Op = Operator.Add;
        public int Val = 0;
    }
    public delegate OperatorOddFix Func_GetOdd(Player killer, Bullet b, Fish f,Fish fCauser);

    //private static Dictionary<Func_GetFishOddAddtiveForDieRatio, Func_GetFishOddAddtiveForDieRatio> mFuncFishDieRatioAddtiveDict;//���ӵ����������ֵ�
    private static Dictionary<Func_GetOdd, Func_GetOdd> mFuncOddDieRatioDict;//
    private static Dictionary<Func_GetOdd, Func_GetOdd> mFuncOddBonusDict;//

    public static void AddFunc_Odd(Func_GetOdd func_OddDieRatio,Func_GetOdd func_oddBonus)
    {
        if (mFuncOddDieRatioDict == null)
            mFuncOddDieRatioDict = new Dictionary<Func_GetOdd, Func_GetOdd>();
        if (mFuncOddBonusDict == null)
            mFuncOddBonusDict = new Dictionary<Func_GetOdd, Func_GetOdd>();

        if(func_OddDieRatio != null)
            mFuncOddDieRatioDict.Add(func_OddDieRatio, func_OddDieRatio);

        if (func_oddBonus != null)
            mFuncOddBonusDict.Add(func_oddBonus, func_oddBonus);
    }

    public static void RemoveFunc_Odd(Func_GetOdd func_OddDieRatio, Func_GetOdd func_oddBonus)
    {
        if(func_OddDieRatio != null)
            mFuncOddDieRatioDict.Remove(func_OddDieRatio);

        if (func_oddBonus != null)
            mFuncOddBonusDict.Remove(func_oddBonus);
    }

    /// <summary>
    /// �����������ʵ�����
    /// </summary>
    /// <param name="killer"></param>
    /// <param name="b"></param>
    /// <param name="f"></param>
    /// <returns></returns>
    private static int GetFishOddForDieRatio(Player killer, Bullet b, Fish f, Fish fCauser)
    {
        int outVal = f.Odds;
        List<OperatorOddFix> lastModuleLst = new List<OperatorOddFix>();

        foreach (Func_GetOdd func in mFuncOddDieRatioDict.Values)
        {
            OperatorOddFix opOddFix = func(killer, b, f, fCauser);
            if (opOddFix == null)
                continue;

            if (opOddFix.Op == Operator.Add)
                outVal += opOddFix.Val;
            else
                lastModuleLst.Add(opOddFix);
        }

        foreach (OperatorOddFix opOddFix in lastModuleLst)
        {
            outVal *= opOddFix.Val;
        }

        return outVal;
    }

    /// <summary>
    /// ������ʾ������
    /// </summary>
    /// <param name="killer"></param>
    /// <param name="b"></param>
    /// <param name="f"></param>
    /// <returns></returns>
    private static int GetFishOddBonus(Player killer, Bullet b, Fish f, Fish fCauser)
    {
        int outVal = f.Odds;
        List<OperatorOddFix> lastModuleLst = new List<OperatorOddFix>();

        foreach (Func_GetOdd func in mFuncOddBonusDict.Values)
        {
            OperatorOddFix opOddFix = func(killer, b, f, fCauser);
            if (opOddFix == null)
                continue;

            if (opOddFix.Op == Operator.Add)
                outVal += opOddFix.Val;
            else
                lastModuleLst.Add(opOddFix);
        }

        foreach (OperatorOddFix opOddFix in lastModuleLst)
        {
            outVal *= opOddFix.Val;
        }

        return outVal;
    }

    //struct FishAndOddsMulti
    //{
    //    public FishAndOddsMulti(Fish f,int oddMulti)
    //    {
    //        F = f;
    //        OddMulti = oddMulti;

    //    }
    //    public Fish F;
    //    public int OddMulti;
    //}


    public static void ProcessHit(Bullet b , Fish f)
    {

        switch(f.HittableTypeS)
        {
            case "Normal":
                Process_NormalFish(b,f);
                break;
            case "AreaBomb":
                Process_AreaBomb(b,f);
                break;
            case "SameTypeBomb":
                Process_FishTypeBomb2(b,f);
                break;
            case "FreezeBomb":
                Process_FreezeBomb(b, f);
                break;
            case "SameTypeBombEx":
                Process_FishTypeBombEx(b, f);
                break;
            default:
                if (Evt_HitExtension != null)
                    Evt_HitExtension(b, f);
                break;
        }
    }




    static void Process_NormalFish(Bullet b, Fish fishFirst)
    {
        if (!fishFirst.Attackable)
            return;

        Transform bulletTs = b.transform;
        Player bulletOwner = b.Owner;
        bool IsLockFishBullet = b.IsLockingFish;//�Ƿ�������ӵ�


        //������ײ��Χ
        RaycastHit[] results = Physics.SphereCastAll(bulletTs.position - Bullet.ColliderOffsetZ, b.RadiusStandardBoom * 0.6F/** useWebScaleRatio.ScaleCollider //��Ҫ����,��ͬ�ӵ���ͬ��С */, Vector3.forward);


        List<Fish> fishAll = new List<Fish>();
        List<Fish> fishOthers = new List<Fish>();
        List<Fish> fishDieList = new List<Fish>();
        fishAll.Add(fishFirst);
        Fish fishTmp = null;
        for (int i = 0; i != results.Length; ++i)
        {
            fishTmp = results[i].transform.GetComponent<Fish>();

            if (fishTmp != null 
                && fishFirst != fishTmp 
                && fishTmp.Attackable
                && fishTmp.HittableTypeS == "Normal"
                && !fishTmp.HitByBulletOnly )
            {
                fishOthers.Add(fishTmp);
                fishAll.Add(fishTmp);
            }
        }

        List<FishOddsData> dataFishOtherList = new List<FishOddsData>();

        foreach (Fish f in fishOthers)
        { 
            dataFishOtherList.Add(new FishOddsData(f.ID, GetFishOddForDieRatio(bulletOwner, b, f,fishFirst)));//�������ʼ���
            
        }
        FishOddsData dataFishFirst = new FishOddsData(fishFirst.ID, GetFishOddForDieRatio(bulletOwner, b, fishFirst, fishFirst));//�������ʼ���


        List<FishOddsData> dataFishDieLst = GameOdds.OneBulletKillFish(b.Score, dataFishFirst, dataFishOtherList);

        foreach (FishOddsData fishDie in dataFishDieLst)
        {
            foreach (Fish f in fishAll)
            {
                if (fishDie.ID == f.ID)
                {
                    fishDieList.Add(f);
                    break;
                }
            }
        }

        int numKilled = 0;
        int scoreTotalGetted = 0;
        foreach (Fish fDie in fishDieList)
        {
            fDie.OddBonus = GetFishOddBonus(bulletOwner, b, fDie, fishFirst);//��ʾ����
            scoreTotalGetted += fDie.OddBonus * b.Score;

            fDie.Kill(bulletOwner, b, 0.5F * numKilled);//0.5����
            ++numKilled;
        }

        if (Evt_Hit != null)
            Evt_Hit(dataFishFirst.Odds == 0?true:false, bulletOwner, b, fishFirst);

        //BackStageSetting bss = GameMain.Singleton.BSSetting;

        if (scoreTotalGetted != 0)
        {
            bulletOwner.GainScore(scoreTotalGetted);

            if (GameMain.Evt_PlayerGainScoreFromFish != null)
                GameMain.Evt_PlayerGainScoreFromFish(bulletOwner, scoreTotalGetted, fishFirst, b);

            //����ɱ���������¼�
            if (IsLockFishBullet && GameMain.EvtKillLockingFish != null)
                GameMain.EvtKillLockingFish(bulletOwner);

        }

        if (Evt_HitResult != null)
            Evt_HitResult(!fishFirst.Attackable, bulletOwner, b, fishFirst);
    }

    static void Process_AreaBomb(Bullet b, Fish fishFirst)
    {

        if (!fishFirst.Attackable)
            return;

        Transform bulletTs = b.transform;
        Player bulletOwner = b.Owner;
        bool IsLockFishBullet = b.IsLockingFish;
     
 

        //������ײ��Χ
        RaycastHit[] results = Physics.SphereCastAll(bulletTs.position - Bullet.ColliderOffsetZ, fishFirst.AreaBombRadius, Vector3.forward);
  

        //�����Ƿ�ը
        List<Fish> fishAffect = new List<Fish>();
        int oddTotal = 0;//���ڽ����ı���
        int oddTotalForCaclDieratio = 0;//�����������ʵı���

        Fish tmpFish = null;
         
        
        foreach (RaycastHit hitObj in results)
        {
            tmpFish = hitObj.transform.GetComponent<Fish>();
            if (tmpFish != null 
                && tmpFish.Attackable
                && tmpFish.HittableTypeS == "Normal")
            {
                fishAffect.Add(tmpFish);

                oddTotalForCaclDieratio += GetFishOddForDieRatio(bulletOwner, b, tmpFish,fishFirst);//�������ʼ���;
                tmpFish.OddBonus = GetFishOddBonus(bulletOwner, b, tmpFish, fishFirst);
                oddTotal += tmpFish.OddBonus;
            }

            if (oddTotal > fishFirst.AreaBombOddLimit)
                break;
        }

      

        FishOddsData dataBomb = new FishOddsData(0, oddTotalForCaclDieratio);
        List<FishOddsData> lstBombDie = GameOdds.OneBulletKillFish(b.Score, dataBomb, new List<FishOddsData>());

        if (Evt_Hit != null)
            Evt_Hit(oddTotalForCaclDieratio == 0 ? true : false, bulletOwner, b, fishFirst);

        if (lstBombDie.Count == 0)//����ը
        {
            return;
        }

        fishFirst.Kill(bulletOwner, b, 0);
        //ը����ը��ɱ��Χ����
        int scoreTotalGetted = oddTotal * b.Score;

        //�����¼�
        if (GameMain.EvtFishBombKilled != null)
            GameMain.EvtFishBombKilled(bulletOwner, scoreTotalGetted);

        //�ɱ�Ч��(����������)
        foreach (Fish fDie in fishAffect)
        {
            fDie.Kill(bulletOwner, b, 0F);//0.5���� 
        }

        //BackStageSetting bss = GameMain.Singleton.BSSetting;

        if (scoreTotalGetted != 0)
        {
            bulletOwner.GainScore(scoreTotalGetted);

            if (GameMain.Evt_PlayerGainScoreFromFish != null)
                GameMain.Evt_PlayerGainScoreFromFish(bulletOwner, scoreTotalGetted, fishFirst, b);

            //����ɱ���������¼�
            if (IsLockFishBullet && GameMain.EvtKillLockingFish != null)
                GameMain.EvtKillLockingFish(bulletOwner);

        }

        if (Evt_HitResult != null)
            Evt_HitResult(!fishFirst.Attackable, bulletOwner, b, fishFirst);
    }


    static void Process_FishTypeBomb2(Bullet b, Fish fishFirst)
    {
        
        if (!fishFirst.Attackable)
            return;

        //Transform bulletTs = b.transform;
        Player bulletOwner = b.Owner;
        bool IsLockFishBullet = b.IsLockingFish;
            
       
        int numTypeToBomb = fishFirst.Prefab_SameTypeBombAffect.Length;
        Dictionary<int, Fish>[] fishDicts = new Dictionary<int, Fish>[numTypeToBomb];
        for (int i = 0; i != numTypeToBomb; ++i)
            fishDicts[i] = GameMain.Singleton.FishGenerator.FishTypeIndexMap[fishFirst.Prefab_SameTypeBombAffect[i].TypeIndex];


        //�����Ƿ�ը
        List<Fish> fishAffect = new List<Fish>();
        fishAffect.Add(fishFirst);
        

        Fish tmpFish = null;
        //FishEx_OddsMulti sameTypeBombOddsMultiComponent = fishFirst.GetComponent<FishEx_OddsMulti>();
        //int oddMulti = sameTypeBombOddsMultiComponent == null ? 1 : sameTypeBombOddsMultiComponent.OddsMulti;
        //List<Fish> fishNeedOddMulti = new List<Fish>();

        fishFirst.OddBonus = GetFishOddBonus(bulletOwner, b, fishFirst,fishFirst);
        int oddTotal = fishFirst.OddBonus;//fishFirst.Odds * oddMulti;//���ڽ�������ı���
        
        int oddTotalForCaclDieratio = GetFishOddForDieRatio(bulletOwner, b, fishFirst, fishFirst);//fishFirst.Odds * oddMulti + GetFishOddForDieRatio(bulletOwner, b, fishFirst);//�������ʼ���

        foreach (Dictionary<int, Fish> fishDict in fishDicts)
        {
            if (fishDict != null)
            {
                foreach (KeyValuePair<int, Fish> fKV in fishDict)
                {
                    tmpFish = fKV.Value;
                    if (tmpFish.Attackable
                        && tmpFish.HittableTypeS == "Normal")//�Լ���HittableType.SameTypeBomb,����������
                    {
                        fishAffect.Add(tmpFish);

                        tmpFish.OddBonus = GetFishOddBonus(bulletOwner, b, tmpFish, fishFirst);
                        oddTotal += tmpFish.OddBonus;//tmpFish.Odds * oddMulti;//����ͳһ���,��������������һЩЧ��,�ô����ֱ��
                        oddTotalForCaclDieratio += GetFishOddForDieRatio(bulletOwner, b, tmpFish, fishFirst);//tmpFish.Odds * oddMulti + GetFishOddForDieRatio(bulletOwner, b, tmpFish);//�������ʼ���

                        //if (oddMulti > 1)
                        //{
                        //    fishNeedOddMulti.Add(tmpFish);
                        //}
          
                    }
                }
            }
        }


        FishOddsData dataBomb = new FishOddsData(0, oddTotalForCaclDieratio);
        //Debug.Log("oddTotalForCaclDieratio = " + oddTotalForCaclDieratio);
        List<FishOddsData> lstBombDie = GameOdds.OneBulletKillFish(b.Score, dataBomb, new List<FishOddsData>());

        if (Evt_Hit != null)
            Evt_Hit(oddTotalForCaclDieratio == 0? true:false, bulletOwner, b, fishFirst);

        if (lstBombDie.Count == 0)
        {
            return;
        }

        //���㱶����˼��뵽����,��Ч��������ȷ��ʾ
        //foreach (Fish f in fishNeedOddMulti)
        //{
        //    FishEx_OddsMulti odm = f.gameObject.AddComponent<FishEx_OddsMulti>();//���������ϼ������,ȷ����������,(��Ҫ������ʾ)
        //    odm.OddsMulti = oddMulti;
        //}

        //ը����ը
        //�����÷���
        int scoreTotalGetted = oddTotal * b.Score;

        //�ɱ�Ч��(����������)
        foreach (Fish fDie in fishAffect)
        {
            fDie.Kill(bulletOwner, b, 0F);//0.5���� 
        }
        //�����¼�
        if (GameMain.EvtSameTypeBombKiled != null)
            GameMain.EvtSameTypeBombKiled(bulletOwner, scoreTotalGetted);

        //BackStageSetting bss = GameMain.Singleton.BSSetting;

        if (scoreTotalGetted != 0)
        {
            bulletOwner.GainScore(scoreTotalGetted);

            if (GameMain.Evt_PlayerGainScoreFromFish != null)
                GameMain.Evt_PlayerGainScoreFromFish(bulletOwner, scoreTotalGetted,fishFirst,b);

            //����ɱ���������¼�
            if (IsLockFishBullet && GameMain.EvtKillLockingFish != null)
                GameMain.EvtKillLockingFish(bulletOwner);
        }

        if (Evt_HitResult != null)
            Evt_HitResult(!fishFirst.Attackable, bulletOwner, b, fishFirst);
    }

    static void Process_FishTypeBombEx(Bullet b, Fish sameTypeBombEx)
    {
        if (!sameTypeBombEx.Attackable)
            return;

        //Transform bulletTs = b.transform;
        Player bulletOwner = b.Owner;
        bool IsLockFishBullet = b.IsLockingFish;



        //�ҳ���Ļ������ͬ��ը��
        List<Fish> fishMayDie = new List<Fish>();
        fishMayDie.Add(sameTypeBombEx);
        //�����ܵı���
        sameTypeBombEx.OddBonus = GetFishOddBonus(bulletOwner, b, sameTypeBombEx, sameTypeBombEx);
        int oddTotalAll = sameTypeBombEx.OddBonus;//sameTypeBombEx.Odds;

        int oddTotalAllForDieratio = GetFishOddForDieRatio(bulletOwner, b, sameTypeBombEx, sameTypeBombEx);//sameTypeBombEx.Odds + GetFishOddForDieRatio(bulletOwner, b, sameTypeBombEx, sameTypeBombEx);//�������ʼ���

        Dictionary<int, object> mAllAffectFishDict = new Dictionary<int, object>();//��ͬ��ը��ɱ������ֵ�,(��������'�Ѵ���',ÿ�δ���ǰ����ѯ ,�ɷ�ֹ�ظ�ɱͬһ�����������)
 
        //List<FishAndOddsMulti> fishNeedOddMulti = new List<FishAndOddsMulti>();
        //����ͬ��ը��
        foreach (Fish sameTypeBomb in GameMain.Singleton.FishGenerator.SameTypeBombs.Values)
        {
            List<Dictionary<int, Fish>> fishDicts = new List<Dictionary<int, Fish>>();
            foreach (Fish affectFish in sameTypeBomb.Prefab_SameTypeBombAffect)
            {
                if (!mAllAffectFishDict.ContainsKey(affectFish.TypeIndex))
                {
                    fishDicts.Add(GameMain.Singleton.FishGenerator.FishTypeIndexMap[affectFish.TypeIndex]);
                    mAllAffectFishDict.Add(affectFish.TypeIndex, null);
                }
            }
            
            //FishEx_OddsMulti oddsMultiComponent = sameTypeBomb.GetComponent<FishEx_OddsMulti>();
            //int oddTotalSameTypeBombMulti = oddsMultiComponent == null ? 1 : oddsMultiComponent.OddsMulti;
            sameTypeBomb.OddBonus = GetFishOddBonus(bulletOwner, b, sameTypeBomb, sameTypeBombEx);
            int oddTotalSameTypeBomb = sameTypeBomb.OddBonus;//sameTypeBomb.Odds * oddTotalSameTypeBombMulti;
            int oddTotalSameTypeBombForCaclDieratio =  GetFishOddForDieRatio(bulletOwner, b, sameTypeBomb, sameTypeBombEx);//sameTypeBomb.Odds * oddTotalSameTypeBombMulti + GetFishOddForDieRatio(bulletOwner, b, sameTypeBomb, sameTypeBombEx);//�������ʼ���
             
            fishMayDie.Add(sameTypeBomb);
            //����ͬ��ը��Ӱ�쵽����
            foreach (Dictionary<int, Fish> fishDict in fishDicts)
            {
                if (fishDict != null)
                {
                    foreach (Fish f in fishDict.Values)
                    {
                        if (f.Attackable
                            && f.HittableTypeS == "Normal")//�Լ���HittableType.SameTypeBomb,����������
                        {
                            f.OddBonus = GetFishOddBonus(bulletOwner, b, f, sameTypeBomb);
                            oddTotalSameTypeBomb += f.OddBonus; // f.Odds* oddTotalSameTypeBombMulti;
                            oddTotalSameTypeBombForCaclDieratio += GetFishOddForDieRatio(bulletOwner, b, f, sameTypeBomb);// f.Odds * oddTotalSameTypeBombMulti + GetFishOddForDieRatio(bulletOwner, b, f, sameTypeBomb);//�������ʼ���

                            fishMayDie.Add(f);

                            //if (oddTotalSameTypeBombMulti > 1)
                            //{
                            //    fishNeedOddMulti.Add(new FishAndOddsMulti(f, oddTotalSameTypeBombMulti));
                            //}
                            
                        }
                    }
                }
            }
            oddTotalAll += oddTotalSameTypeBomb;
            oddTotalAllForDieratio += oddTotalSameTypeBombForCaclDieratio;
        }

        
        FishOddsData dataBomb = new FishOddsData(0, oddTotalAllForDieratio);
        
        List<FishOddsData> lstBombDie = GameOdds.OneBulletKillFish(b.Score, dataBomb, new List<FishOddsData>());

        if (Evt_Hit != null)
            Evt_Hit(oddTotalAllForDieratio == 0 ? true : false, bulletOwner, b, sameTypeBombEx);

        if (lstBombDie.Count == 0)//����ը
        {
            return;
        }

        ////ȷ����������󽫱��ʸı���뵽Fish��,��Ч����ʾ������ȷ����
        //foreach (FishAndOddsMulti fo in fishNeedOddMulti)
        //{
        //    FishEx_OddsMulti fodm = fo.F.gameObject.AddComponent<FishEx_OddsMulti>();
        //    fodm.OddsMulti = fo.OddMulti;
        //}
 
        int scoreTotalGetted = oddTotalAll * b.Score;

        //�ɱ�Ч��(����������)
        foreach (Fish fDie in fishMayDie)
        {
            fDie.Kill(bulletOwner, b, 0F);//0.5���� 
        }

        if (GameMain.EvtSameTypeBombExKiled != null)
            GameMain.EvtSameTypeBombExKiled(bulletOwner, scoreTotalGetted);

        //BackStageSetting bss = GameMain.Singleton.BSSetting;

        if (scoreTotalGetted != 0)
        {
            bulletOwner.GainScore(scoreTotalGetted);

            if (GameMain.Evt_PlayerGainScoreFromFish != null)
                GameMain.Evt_PlayerGainScoreFromFish(bulletOwner, scoreTotalGetted, sameTypeBombEx, b);

            //����ɱ���������¼�
            if (IsLockFishBullet && GameMain.EvtKillLockingFish != null)
                GameMain.EvtKillLockingFish(bulletOwner);
        }

        if (Evt_HitResult != null)
            Evt_HitResult(!sameTypeBombEx.Attackable, bulletOwner, b, sameTypeBombEx);
    }
    static void Process_FreezeBomb(Bullet b, Fish fishFirst)
    {
        //Debug.Log("Process_FreezeBomb");
        if (!fishFirst.Attackable)
            return;
        //GameMain gm = GameMain.Singleton;
        //Transform bulletTs = b.transform;
        Player bulletOwner = b.Owner;
        bool IsLockFishBullet = b.IsLockingFish;


        //�������ը���Ƿ�ը
        int oddForDieRatio = GetFishOddForDieRatio(bulletOwner, b, fishFirst,fishFirst);
        FishOddsData freezeBomb = new FishOddsData(0, oddForDieRatio);//fishFirst.Odds + GetFishOddForDieRatio(bulletOwner, b, fishFirst,fishFirst));//�������ʼ���
       
        List<FishOddsData> lstBombDie = GameOdds.OneBulletKillFish(b.Score, freezeBomb, new List<FishOddsData>());

        if (Evt_Hit != null)
            Evt_Hit(oddForDieRatio == 0 ? true : false, bulletOwner, b, fishFirst);

        if (lstBombDie.Count == 0)//����ը
        {
            return;
        }

        fishFirst.Kill(bulletOwner, b, 0F);//0.5���� 


        //BackStageSetting bss = gm.BSSetting;
        fishFirst.OddBonus = GetFishOddBonus(bulletOwner, b, fishFirst, fishFirst);
        int scoreTotalGetted = b.Score * fishFirst.OddBonus;

        

        if (scoreTotalGetted != 0)
        {
            bulletOwner.GainScore(scoreTotalGetted);
            if (GameMain.Evt_PlayerGainScoreFromFish != null)
                GameMain.Evt_PlayerGainScoreFromFish(bulletOwner, scoreTotalGetted, fishFirst, b);

            //����ɱ���������¼�
            if (IsLockFishBullet && GameMain.EvtKillLockingFish != null)
                GameMain.EvtKillLockingFish(bulletOwner);
        }


        GameMain.IsMainProcessPause = true;

        if (GameMain.EvtFreezeBombActive != null)
            GameMain.EvtFreezeBombActive();

        GameObject go = new GameObject("Delay_FreezeBombRecover");
        go.AddComponent<FreezeBombRecover>();

        if (Evt_HitResult != null)
            Evt_HitResult(!fishFirst.Attackable, bulletOwner, b, fishFirst);
    }

  
}
