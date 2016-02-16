using UnityEngine;
using System.Collections;

public class Module_FishTypeBomb2 : MonoBehaviour {
      
    void Start()
    {
        HitProcessor.AddFunc_Odd(Func_GetFishOddAdditive, Func_GetFishOddAdditive);
    }


    HitProcessor.OperatorOddFix Func_GetFishOddAdditive(Player killer, Bullet b, Fish f, Fish fCauser)
    {
        if (fCauser.HittableTypeS == "SameTypeBomb")//��������ͬ��ը��.����������С��,��ʹû�д�FishEx_OddsMulti,Ҳ��Ҫ�˱�
        {
            FishEx_OddsMulti cpOddMulti = fCauser.GetComponent<FishEx_OddsMulti>();

            if (cpOddMulti == null || cpOddMulti.OddsMulti == 1)
                return null;

            return new HitProcessor.OperatorOddFix(HitProcessor.Operator.LastModule, cpOddMulti.OddsMulti);
        }
        else if (f.HittableTypeS == "SameTypeBomb")//�п��ܳ���causer��SameTypeBombEx,��ΪSameTypeBombEx����SameTypeBomb�Ĵ�����
        {
            FishEx_OddsMulti cpOddMulti = f.GetComponent<FishEx_OddsMulti>();

            if (cpOddMulti == null || cpOddMulti.OddsMulti == 1)
                return null;

            return new HitProcessor.OperatorOddFix(HitProcessor.Operator.LastModule, cpOddMulti.OddsMulti);
        }

        return null;
        
    }
}
