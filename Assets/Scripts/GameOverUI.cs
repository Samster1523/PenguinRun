using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    public CanvasGroup group;
    public TMP_Text titleText;
    public TMP_Text costText;
    public Button reviveButton;
    public Button restartButton;

    void Awake() { Hide(); }

    public void Show(bool canRevive, int cost)
    {
        if (group) { group.alpha = 1f; group.interactable = true; group.blocksRaycasts = true; }
        if (titleText) titleText.text = "Game Over";
        if (costText) costText.text = $"Revive ({cost} coins)";
        if (reviveButton) reviveButton.interactable = canRevive;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (group) { group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false; }
        gameObject.SetActive(true);
    }

    public void OnClickRevive() { GameManager.I?.TryRevive(); Hide(); }
    public void OnClickRestart() { Hide(); GameManager.I?.RestartScene(); }
}
