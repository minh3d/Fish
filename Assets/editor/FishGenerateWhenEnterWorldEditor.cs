using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(FishGenerateWhenEnterWorld))]
[CanEditMultipleObjects]
public class FishGenerateWhenEnterWorldEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("ת��ΪFish"))
        {
            ConvertToFish(target as FishGenerateWhenEnterWorld);
        }
    }

    [MenuItem("HappyFishes/Fish/����� -> Fish")]
    static void Menu_ConvertToFish()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            FishGenerateWhenEnterWorld fp = go.GetComponent<FishGenerateWhenEnterWorld>();
            if (fp != null)
                ConvertToFish(fp);
        }
    }

    static void ConvertToFish(FishGenerateWhenEnterWorld fishPot)
    {
        if (AssetDatabase.Contains(fishPot))
            return;

        if (fishPot.Prefab_Fish == null)
        {
            Debug.LogWarning("[����]������Prefab_Fish,ת��ʧ��.", fishPot.gameObject);
            return;
        }
        //ɾ���Ӷ���
        foreach (Transform child in fishPot.transform)
        {
            DestroyImmediate(child.gameObject);
        }
        
        //���������
        Fish f = Instantiate(fishPot.Prefab_Fish) as Fish;
 
        f.gameObject.name = fishPot.gameObject.name;
        f.transform.parent = fishPot.transform.parent;
        f.transform.localPosition = fishPot.transform.localPosition;
        f.transform.localRotation = fishPot.transform.localRotation;
        f.transform.localScale = fishPot.transform.localScale;

        Selection.activeGameObject = f.gameObject;
        //ɾ��Fishpot����
        DestroyImmediate(fishPot.gameObject);

    }
}