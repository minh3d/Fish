using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ��ʱֹͣ�ָ�
/// </summary>
/// <remarks>���������Ҫ����ָ�����:
/// 1.�ָ���ʱ�����������Ϸʱ����.
/// 2.�ָ���ʱ����ڹ���(sweep)��,:�ָ�����,��Ϊ�����ӵ���������BUG
/// 4.��ʼ��ʱ����ڹ�����,�ȴ��ӵ���ʧ������->�����ӵ����ж�ʱը�� : ֱ�ӻָ�,ԭ��ͬ��
/// </remarks>
public class FreezeBombRecover : MonoBehaviour {

    void Awake()
    {
        StartCoroutine(_Coro_DelayRecover());
        //if (GameMain.State_ == GameMain.State.Normal)
        //{
        //    StartCoroutine(_Coro_DelayRecover());
        //}
        //else //������ͨ��Ϸ�п�ʼ,���4(����˵��)
        //{
        //    Recover();

        //    Destroy(gameObject);
        //} 
    } 
    //void Handle_PrepareChangeScene()
    //{ 
    //    StopAllCoroutines();
    //    Recover();
    //    Destroy(gameObject);
    //}

    /// <summary>
    /// ����ը���ָ����������ƶ�
    /// </summary>
    /// <returns></returns>
    IEnumerator _Coro_DelayRecover()
    {

        yield return new WaitForSeconds(10F); 

        Recover();
        
        Destroy(gameObject);
    }

    //�ָ�����,������ƶ�
    void Recover()
    {
        //GameMain gm = GameMain.Singleton;
        GameMain.IsMainProcessPause = false;
        if (GameMain.EvtFreezeBombDeactive != null)
            GameMain.EvtFreezeBombDeactive();
        
    }
}
