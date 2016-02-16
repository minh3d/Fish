using UnityEngine;
using System.Collections;

public class TranCreater : MonoBehaviour {
    public GameObject[] Prefab_Creators;
	// Use this for initialization
	void Start () {
        for (int i = 0; i != Prefab_Creators.Length; ++i)
        {
            for (int j = 0; j != Prefab_Creators[i].transform.childCount; ++j)
            {
                Transform tsCreator =  Prefab_Creators[i].transform.GetChild(j);
                tsCreator.localPosition *= 384F;
                PlayerCreator pc = tsCreator.GetComponent<PlayerCreator>();
                if (pc != null)
                {
                    pc.CoinStacksLocalPos *= 384F;
                    pc.ScoreBGLocalPos *= 384F;
                }
            }
        }
	}

    // Update is called once per frame
    void Update()
    {
	
	}
}
