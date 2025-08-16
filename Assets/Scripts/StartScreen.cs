using UnityEngine;
using UnityEngine.UI;

public class StartScreen : MonoBehaviour
{
    public CanvasGroup group;   // optional but recommended
    public Button startButton;  // drag StartButton here

    void Awake()
    {
        if (startButton) startButton.onClick.AddListener(StartGame);

        bool shouldPause = (GameManager.I == null) || GameManager.I.pauseOnStart;
        if (shouldPause)
        {
            Show();
            if (GameManager.I != null) GameManager.I.Pause();
            else Time.timeScale = 0f; // fallback
        }
        else
        {
            HideImmediate();
        }
    }

    void OnDestroy()
    {
        if (startButton) startButton.onClick.RemoveListener(StartGame);
    }

    public void StartGame()
    {
        if (GameManager.I != null)
        {
            GameManager.I.pauseOnStart = false; // don’t pause on reloads
            GameManager.I.Unpause();
        }
        else Time.timeScale = 1f;

        Hide();
    }

    // ---- helpers ----
    void Show()
    {
        gameObject.SetActive(true);
        if (group) { group.alpha = 1f; group.interactable = true; group.blocksRaycasts = true; }
    }
    void Hide()
    {
        if (group) { group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false; }
        else gameObject.SetActive(false);
    }
    void HideImmediate()
    {
        if (group) { group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false; }
        else gameObject.SetActive(false);
    }
}
