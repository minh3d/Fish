using UnityEngine;
using System.Collections;

public class GameLogo : MonoBehaviour {
    public GameStartOneScreen GameStartOneScreen_;
    public tk2dSprite[] Logos;//���ڽ����logo(��ʾ0,spr(1)alphaֵ��0��1, spr(2)��alpha��0��1)
    public AnimationCurve Curve_Alpha;
    public GameObject[] OtherGOs;//������Ҫ���صĶ���
    public bool FlipXWhenIdxScreen2=true;//�ڵڶ�����Ļ��ʱ��תLogo

    void Start()
    {
        if (Logos.Length == 0)
            return;

        if (GameStartOneScreen_ == null)
            GameStartOneScreen_ = GetComponent<GameStartOneScreen>();
        GameStartOneScreen_.EvtGameLogoStart += Handle_GameLogoStart;

        foreach (tk2dSprite spr in Logos)
        {
            spr.renderer.enabled = false;
        }
        foreach (GameObject go in OtherGOs)
        {
            go.SetActive(false);
        }
       
    }
    void Handle_GameLogoStart(int idxScreen, float time)
    {
        StartCoroutine(_Coro_GameLogoStart(idxScreen,time));
    }
    IEnumerator _Coro_GameLogoStart(int idxScreen, float time)
    {
        if(Logos.Length == 0)
            yield break;

        foreach (GameObject go in OtherGOs)
        {
            go.SetActive(true);
        }

        if (FlipXWhenIdxScreen2 && (idxScreen % 2 == 1))
        {
            foreach (tk2dSprite spr in Logos)
            {
                Vector3 scale = spr.transform.localScale;
                scale.x = -scale.x;
                spr.transform.localScale = scale;
            }
        }
        
        for(int i = 1; i != Logos.Length; ++i)
        {
            Color cTmp = Logos[i].color;
            cTmp.a = 0F;
            Logos[i].color = cTmp;
        }
        foreach(tk2dSprite spr in Logos)
        {
            spr.renderer.enabled = true;
        }

        Logos[0].renderer.enabled = true;

        int idxLogoCur = 1;
        float timePerLogo = time/ (Logos.Length - 1);
        while (idxLogoCur < Logos.Length)
        {
            float elapse = 0F;
            Color c = Logos[idxLogoCur].color;
            while(elapse <timePerLogo)
            {
                //c.a = elapse / timePerLogo;
                c.a = Curve_Alpha.Evaluate(elapse / timePerLogo);
                Logos[idxLogoCur].color = c;
                elapse += Time.deltaTime;
                yield return 0;
            }
            ++idxLogoCur;
        }


        Destroy(gameObject);
    }

	 
}
