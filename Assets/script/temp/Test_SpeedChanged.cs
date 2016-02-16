using UnityEngine;
using System.Collections;

public class Test_SpeedChanged : MonoBehaviour {

    void OnGUI()
    {
        string strTimeScale = GUILayout.TextField(Time.timeScale.ToString());
        float timeScale = 1F;
        if (float.TryParse(strTimeScale, out timeScale))
        {
            if (timeScale != Time.timeScale)
                Time.timeScale = timeScale;
        }
    }

}
