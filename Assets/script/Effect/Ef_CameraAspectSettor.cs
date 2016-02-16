using UnityEngine;
using System.Collections;

/// <summary>
/// 根据Defines.OriginWidthUnit 和 Defines.OriginWidthUnit 来调整摄像机长宽
/// </summary>
public class Ef_CameraAspectSettor : MonoBehaviour { 
    public bool IsAffectByScreenNum = true;//是否受屏幕数影响
	void Awake () {

        float aspect = (float)Defines.OriginWidthUnit / Defines.OriginHeightUnit;//16 : 9
        if (IsAffectByScreenNum)
            camera.aspect = aspect * GameMain.Singleton.ScreenNumUsing;
        else
            camera.aspect = aspect;
	}
}
