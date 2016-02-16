using UnityEngine;
using System.Collections;

public class ChangeFishGenerator: MonoBehaviour
{

    // Use this for initialization
    public float WaitTime = 5F;
    public int FishQuantity = 10;

    private FishGenerator MyFishGenerator;
    private Swimmer MySwimmer;

    public Fish MyFish;
    private float iSp;
    private bool isChangGenerator = false;

    private int mMaxFishAtWorldOrigin;//ԭ�����������
    void Start( )
    {

        MyFishGenerator = GameMain.Singleton.FishGenerator;

        GameMain.EvtFishInstance += Handle_Change;
        GameMain.EvtFishClear += Handle_Resumme;
        //Debug.Log( "inStart");  


    }
    void Handle_Change( Fish f )
    {
        if(f.TypeIndex == MyFish.TypeIndex&&GameMain.State_ == GameMain.State.Normal&&!isChangGenerator)
        {
            MySwimmer = f.GetComponent<Swimmer>( );
            //Debug.Log( "inchang" );
            iSp = MySwimmer.Speed;

            StartCoroutine( "Waite" );

        }
    }

    void Handle_Resumme( Fish f )
    {
        if(f.TypeIndex == MyFish.TypeIndex&& isChangGenerator)
        {
            //Debug.Log( "inresumme" );
            // float iSp = MySwimmer.Speed;
            MyFishGenerator.MaxFishAtWorld = mMaxFishAtWorldOrigin;//�ָ�ԭ���������Ŀ
            isChangGenerator = false; 
        }
    }

    IEnumerator Waite( )
    {
        MySwimmer.Speed = 0F;
        mMaxFishAtWorldOrigin = MyFishGenerator.MaxFishAtWorld;//��¼ԭ���������������
        MyFishGenerator.MaxFishAtWorld = FishQuantity * GameMain.Singleton.ScreenNumUsing;
        isChangGenerator = true;
        yield return new WaitForSeconds( WaitTime );
        MySwimmer.Speed = iSp;
    }
}

