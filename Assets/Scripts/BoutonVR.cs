using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class BoutonVR : MonoBehaviour
{
    public Transform objetADeplacer;
    private Vector3 startPos;
    void Start()
    {
        startPos = transform.position;

    }
    public void AnimerDescente()
    {
        transform.DOMoveY(startPos.y - 0.0060f, 0.5f)
        .SetEase(Ease.InOutSine)
        .OnComplete(() =>
        {
            transform.DOMoveY(startPos.y + 0.0060f, 0.5f)
                .SetEase(Ease.InOutSine);
        });
    }
}
