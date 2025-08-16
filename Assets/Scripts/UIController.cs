using UnityEngine;
using TMPro;

public class UIController : MonoBehaviour
{
    public TMP_Text scoreText;
    public TMP_Text coinsText;

    void Awake()
    {
        if (scoreText) { scoreText.enableWordWrapping = false; scoreText.overflowMode = TextOverflowModes.Overflow; }
        if (coinsText) { coinsText.enableWordWrapping = false; coinsText.overflowMode = TextOverflowModes.Overflow; }
    }

    void Update()
    {
        if (GameManager.I == null) return;
        // NBSP keeps the number glued to the label and avoids weird wraps
        scoreText.text = $"Score:\u00A0{GameManager.I.Score}";
        coinsText.text = $"Coins:\u00A0{GameManager.I.Coins}";
    }
}
