using UnityEngine;
using System.Collections;

public class Test_ViewCurrentGainRatio : MonoBehaviour {
    public Rect windowRect = new Rect(20, 20, 120, 50);
    string[] difficultStrs = { "������", "����", "��", "����", "����" };
    GameMain mGM;
    
	// Use this for initialization
	void Awake() {
        mGM = GameMain.Singleton;
	}
	 
    void OnGUI()
    {
        windowRect = GUILayout.Window(0, windowRect, DoMyWindow, "������Ϣ");
       

    }

    void DoMyWindow(int windowID)
    {
        GUILayout.TextArea(string.Format("������:{0:f4}  ����:{1:f4}  ��:{2:f4}  ����:{3:f4}  ����:{4:f4}"
           , GameOdds.GainRatios[0]
           , GameOdds.GainRatios[1]
           , GameOdds.GainRatios[2]
           , GameOdds.GainRatios[3]
           , GameOdds.GainRatios[4]));

        GUILayout.TextArea("��ǰ�Ѷȣ�" + difficultStrs[(int)GameMain.Singleton.BSSetting.GameDifficult_.Val]);

        GUILayout.TextArea("��ǰ���ʣ�" + GameOdds.GainRatio);
        int totalScore = 0;
        //for(int i = 0; i != Defines.MaxNumPlayer; ++i)
        foreach(Player p in mGM.Players)
        {
            totalScore += mGM.BSSetting.Dat_PlayersScore[p.Idx].Val;
        }
        GUILayout.TextArea("��ǰ�ܷ�:" + totalScore);
        GUI.DragWindow();
    }
}
