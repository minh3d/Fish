using UnityEngine;
using System.Collections;

/// <summary>
/// ◊÷ÃÂ…¡À∏
/// </summary>
public class Ef_RendererFlash : MonoBehaviour {
    public float IntervalFlash = 1F;
    public Renderer RendererToFlash;
    public bool AutoFlashAtStart = false;
    [System.NonSerialized]
    public bool IsFlashing = false;
    void Start()
    {
        if (AutoFlashAtStart)
            StartFlash();
    }

    public void StartFlash()
    {
        if (IsFlashing)
            return;
        IsFlashing = true;
        StartCoroutine(_Coro_Flash());
    }


    public void StopFlash()
    {
        IsFlashing = false;
        
        StopAllCoroutines();
        RendererToFlash.enabled = true;
    }

    IEnumerator _Coro_Flash()
    {
        if (RendererToFlash == null)
        {
            RendererToFlash = renderer;
            if (RendererToFlash == null)
                RendererToFlash = GetComponentInChildren<Renderer>();
            if(RendererToFlash == null)
                yield break;
        }
        while (true)
        {
            RendererToFlash.enabled = true;
            yield return new WaitForSeconds(IntervalFlash);
            RendererToFlash.enabled = false;
            yield return new WaitForSeconds(IntervalFlash);
        }
    }  
}
