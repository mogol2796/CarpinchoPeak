using UnityEngine;
using TMPro;

public class HUDStatsTMP : MonoBehaviour
{
    public PlayerManager player;

    public TMP_Text energyTxt;
    public TMP_Text hungerTxt;
    public TMP_Text coldTxt;
    public TMP_Text healthTxt;

    [Header("Refresh")]
    public float refreshHz = 10f;

    float _t;

    void Reset()
    {
        player = FindFirstObjectByType<PlayerManager>();
    }

    void Update()
    {
        if (!player) return;

        _t += Time.deltaTime;
        float step = 1f / Mathf.Max(1f, refreshHz);
        if (_t < step) return;
        _t = 0f;

        int e = Mathf.RoundToInt(player.EnergyPercent01 * 100f);
        int h = Mathf.RoundToInt(player.health);
        int hu = Mathf.RoundToInt(player.hunger);
        int c = Mathf.RoundToInt(player.cold);

        if (energyTxt) energyTxt.text = $"{e}%";
        if (healthTxt) healthTxt.text = $"{h}%";
        if (hungerTxt) hungerTxt.text = $"{hu}%";
        if (coldTxt) coldTxt.text = $"{c}%";
    }
}
