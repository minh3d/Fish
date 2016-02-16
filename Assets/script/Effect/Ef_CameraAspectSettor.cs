using UnityEngine;
using System.Collections;

/// <summary>
/// ����Defines.OriginWidthUnit �� Defines.OriginWidthUnit ���������������
/// </summary>
public class Ef_CameraAspectSettor : MonoBehaviour { 
    public bool IsAffectByScreenNum = true;//�Ƿ�����Ļ��Ӱ��
	void Awake () {

        float aspect = (float)Defines.OriginWidthUnit / Defines.OriginHeightUnit;//16 : 9
        if (IsAffectByScreenNum)
            camera.aspect = aspect * GameMain.Singleton.ScreenNumUsing;
        else
            camera.aspect = aspect;
	}
}
