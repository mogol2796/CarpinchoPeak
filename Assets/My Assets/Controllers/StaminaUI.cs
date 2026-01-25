using UnityEngine;
using UnityEngine.UI;

public class StaminaUI : MonoBehaviour
{
    public ClimbManager climbManager;
    public Image fillImage;

    [Header("Optional")]
    public bool hideWhenFull = false;
    public float smooth = 12f;

    private float current;

    void Start()
    {
        current = fillImage ? fillImage.fillAmount : 1f;
    }

    void Update()
    {
        if (!climbManager || !fillImage) return;

        float target = Mathf.Clamp01(climbManager.Stamina01);
        current = Mathf.Lerp(current, target, 1f - Mathf.Exp(-smooth * Time.deltaTime));
        fillImage.fillAmount = current;

        if (hideWhenFull)
            fillImage.transform.parent.gameObject.SetActive(target < 0.999f);
    }
}
