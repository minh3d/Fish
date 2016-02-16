using UnityEngine;
using System.Collections;

public class GunLiziGettor : MonoBehaviour {
    public Transform Preafab_TsLiziKa; 

    public float TimeGunLizi = 20F;
    public AnimationCurve Curve_PopUpY;

    private bool UsingGunLizi = false;
    public void GetLiziKaFrom(Vector3 worldPos)
    {
        if (UsingGunLizi)
            return;
        UsingGunLizi = true;
        StartCoroutine(_Coro_GettingLiziKa(worldPos));
    }

    IEnumerator _Coro_GettingLiziKa(Vector3 worldPos)
    {
        //��ʼ������
        Transform tsLiziKa = Instantiate(Preafab_TsLiziKa) as Transform;
        tsLiziKa.parent = transform;
        worldPos.z = Defines.GlobleDepth_LiziKa;
        tsLiziKa.position = worldPos;
        tsLiziKa.localRotation = Quaternion.identity;
        //tsLiziKa.localPosition = new Vector3(tsLiziKa.localPosition.x, tsLiziKa.localPosition.y, 0F);
        //������Ч
        if(GameMain.Singleton.SoundMgr.snd_GetLiziCard != null)
            GameMain.Singleton.SoundMgr.PlayOneShot( GameMain.Singleton.SoundMgr.snd_GetLiziCard );
        //���𶯻�
        float useTime = 0.5F;
        float elapse = 0F;
        Vector3 oriLocalPos = tsLiziKa.localPosition;
        while (elapse < useTime)
        {
            tsLiziKa.localPosition = new Vector3(oriLocalPos.x, oriLocalPos.y + Curve_PopUpY.Evaluate(elapse / useTime), oriLocalPos.z);
            
            elapse += Time.deltaTime;
            yield return 0;
        }
        tsLiziKa.localPosition = oriLocalPos;
        yield return new WaitForSeconds(0.5F);
        //������̨

        Vector3 flyDirect = -tsLiziKa.localPosition.normalized;
        float flyDistance = tsLiziKa.localPosition.magnitude;
        float flySpeed = 652.8F;
        useTime = flyDistance / flySpeed;
        elapse = 0F;
        
        while ( elapse < useTime)
        {
            tsLiziKa.localPosition += flyDirect * (flySpeed * Time.deltaTime);
            tsLiziKa.localScale = new Vector3(1F - 0.2F * elapse / useTime, 1F - 0.2F * elapse / useTime, 1F);
            elapse += Time.deltaTime;
            yield return 0;
        }

        //��ʧ

        //��ǹ
        Player p = GetComponent<Player>();
        if (p != null)
        {
            p.ChangeGun(Gun.GunNeedScoreToLevelType(GameMain.Singleton.BSSetting.Dat_PlayersGunScore[p.Idx].Val),GunPowerType.Lizi);
        }

        //����

        //�������Ա�
        //Vector3 localPos = new Vector3(0.2566707F, 0.1447247F, -0.05122566F);
        //float rotateOffset = 0.01153028F;
        //tsLiziKa.localScale = Vector3.one;
        //float rotateSpd = 640F;
        //float rotateAngle = 0F;
        //elapse = 0F;
        //while (elapse < TimeGunLizi)
        //{
        //    tsLiziKa.localPosition = localPos + new Vector3(rotateOffset * Mathf.Sin(rotateAngle * Mathf.Deg2Rad), rotateOffset* Mathf.Cos(rotateAngle * Mathf.Deg2Rad), 0F);
        //    rotateAngle += Time.deltaTime * rotateSpd;
        //    elapse += Time.deltaTime;
        //    yield return 0;
        //}

        Destroy(tsLiziKa.gameObject);
        //����ʱ��
        yield return new WaitForSeconds(TimeGunLizi);
        if (p != null)
        {
   
            p.ChangeGun(Gun.GunNeedScoreToLevelType(GameMain.Singleton.BSSetting.Dat_PlayersGunScore[p.Idx].Val), GunPowerType.Normal);
        }
        //Destroy(tsLiziKa.gameObject);
        UsingGunLizi = false;
        
    }

 
}
