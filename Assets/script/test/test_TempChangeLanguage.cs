using UnityEngine;
using System.Collections;

public class test_TempChangeLanguage : MonoBehaviour {
   


    void OnGUI()
    {
        if (GUILayout.Button("��ǰ����" + GameMain.Singleton.BSSetting.LaguageUsing.Val))
        {
            if (GameMain.Singleton.BSSetting.LaguageUsing.Val == Language.Cn)
            {
                GameMain.Singleton.BSSetting.LaguageUsing.Val = Language.En;
                
            }
            else if (GameMain.Singleton.BSSetting.LaguageUsing.Val == Language.En)
            {
                GameMain.Singleton.BSSetting.LaguageUsing.Val = Language.Cn;
            }
            if (GameMain.EvtLanguageChange != null)
                GameMain.EvtLanguageChange(GameMain.Singleton.BSSetting.LaguageUsing.Val);
        }
    }
	  
}
