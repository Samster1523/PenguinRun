using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }

    // ---------- Start / Pause ----------
    [Header("Start")]
    public bool pauseOnStart = true;   // StartScreen will leave this true for the first load

    public void Pause() { Time.timeScale = 0f; }
    public void Unpause() { Time.timeScale = 1f; }

    // ---------- Run state ----------
    public int Lives = 1;
    public int Coins { get; private set; }
    public int Score { get; private set; }

    [Header("Scoring")]
    public float pointsPerSecond = 10f;

    [Header("Global speed")]
    public float baseScrollSpeed = 8f;

    [Header("Optional speed ramp")]
    public bool rampSpeed = false;
    public float maxScrollSpeed = 14f;
    public float rampSeconds = 90f;

    [Header("Revive")]
    public int reviveCost = 10;
    public float reviveInvulnSeconds = 1.2f;

    // internals
    float scoreAccum = 0f;
    float runTime = 0f;
    bool isDead = false;
    bool _invulnerable = false;
    public bool Invulnerable => _invulnerable;

    public float CurrentScrollSpeed
    {
        get
        {
            if (!rampSpeed) return baseScrollSpeed;
            float t = rampSeconds <= 0f ? 1f : Mathf.Clamp01(runTime / rampSeconds);
            t = Mathf.SmoothStep(0f, 1f, t);
            return Mathf.Lerp(baseScrollSpeed, maxScrollSpeed, t);
        }
    }

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (I == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode mode)
    {
        // Fresh run each reload
        Score = 0; scoreAccum = 0f;
        Coins = 0; Lives = 1;
        isDead = false; _invulnerable = false;
        runTime = 0f;

        // IMPORTANT: respect StartScreen
        Time.timeScale = pauseOnStart ? 0f : 1f;
    }

    void Update()
    {
        if (isDead) return;

        scoreAccum += pointsPerSecond * Time.deltaTime;
        Score = (int)scoreAccum;

        runTime += Time.deltaTime;
    }

    // ---- Public API ----
    public void AddCoins(int amount) => Coins += amount;

    public void OnPlayerHit()
    {
        if (isDead || _invulnerable) return;
        Lives--;
        if (Lives <= 0)
        {
            isDead = true;
            Pause();
            var ui = FindObjectOfType<GameOverUI>(true);
            if (ui) ui.Show(Coins >= reviveCost, reviveCost);
        }
    }

    public void TryRevive()
    {
        if (!isDead || Coins < reviveCost) return;
        Coins -= reviveCost;
        isDead = false;
        Unpause();
        StartCoroutine(InvulnRoutine(reviveInvulnSeconds));
    }

    System.Collections.IEnumerator InvulnRoutine(float seconds)
    {
        _invulnerable = true;
        yield return new WaitForSeconds(seconds);
        _invulnerable = false;
    }

    public void RestartScene()
    {
        Unpause();
        var s = SceneManager.GetActiveScene();
        SceneManager.LoadScene(s.buildIndex);
    }

    public void ResetRun(int lives = 1)
    {
        Lives = lives;
        Coins = 0;
        scoreAccum = 0f; Score = 0;
        isDead = false; _invulnerable = false;
        runTime = 0f;
        Time.timeScale = pauseOnStart ? 0f : 1f;
    }
}

