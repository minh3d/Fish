using UnityEngine;
using System.Collections;


public class Bs_BoxDecoder : MonoBehaviour {
    public tk2dTextMesh Text_TableNum;//̨��
    public tk2dTextMesh Text_DecodeFeatureNum;//����������

    public Ctrl_InputNum Ctrl_DecodeNum;//��ý�������ؼ�

    public Renderer Rd_ErroInput;//��֤������ʾ��

    void OnEnable()
    {
        //Rd_ErroInput.enabled = false;
        Rd_ErroInput.gameObject.SetActiveRecursively(false);
        //renderer.enabled = true;
    }
 
    public void ViewErroInput()
    {
        StopCoroutine("_Coro_ViewErroInput");
        StartCoroutine("_Coro_ViewErroInput");
    }

    IEnumerator _Coro_ViewErroInput()
    {
        Rd_ErroInput.enabled = true;
        Rd_ErroInput.gameObject.SetActiveRecursively(true);
        //renderer.enabled = false;
        yield return new WaitForSeconds(3F);
        //Rd_ErroInput.enabled = false;
        Rd_ErroInput.gameObject.SetActiveRecursively(false);
        //renderer.enabled = true;
    }
}
