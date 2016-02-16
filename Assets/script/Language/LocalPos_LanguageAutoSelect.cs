using UnityEngine;
using System.Collections;

public class LocalPos_LanguageAutoSelect : MonoBehaviour {

    public Vector3[] LocalPosition;
    
    // Use this for initialization
    void OnEnable()
    {


        if (LocalPosition == null   || LocalPosition.Length == 0)
        {
            Debug.LogError("LocalPos_LanguageAutoSelect���������Աδ��ֵ����.");
            Destroy(this);
            return;
        }
        GameMain.EvtLanguageChange += Handle_LanguageChanged;
        transform.localPosition = LocalPosition[(int)GameMain.Singleton.BSSetting.LaguageUsing.Val];
    }

    void OnDisable()
    {
        GameMain.EvtLanguageChange -= Handle_LanguageChanged;
    }
    void Handle_LanguageChanged(Language l)
    {
        transform.localPosition = LocalPosition[(int)GameMain.Singleton.BSSetting.LaguageUsing.Val];
    }
}
