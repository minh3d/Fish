using UnityEngine;
using System.Collections;

public class FishEx_OddsMulti : MonoBehaviour {
    public int OddsMulti = 1;
	// Use this for initialization
	void Awake () {
        if (OddsMulti <= 0)
            Debug.LogError("oddsMultiֵ�ñ������0");
	}
}
