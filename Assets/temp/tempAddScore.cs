using UnityEngine;
using System.Collections;

public class tempAddScore : MonoBehaviour {

	// Use this for initialization
	void Start () {
        GameMain.EvtMainProcess_StartGame += Handle_GameStart;
	}

    void Handle_GameStart()
    {
        MobileInterface.ChangePlayerScore(1000);
    }
	 
}
