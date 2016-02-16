using UnityEngine;
using System.Collections;

public class TranTo384Adapter : MonoBehaviour {
    public GameObject[] PrefabTranObjs;
	// Use this for initialization
	void Start () {
	    for(int i = 0; i != PrefabTranObjs.Length; ++i)
        {
            Swimmer s = PrefabTranObjs[i].GetComponent<Swimmer>();
            if (s != null)
            {
                s.Speed *= 384;
                s.BoundCircleRadius *= 384;

            }
        }
	}

    // Update is called once per frame
    void Update()
    {
	
	}
}
