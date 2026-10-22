using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatsManager : MonoBehaviour
{
    private PlayerStatsData statsData;

    [SerializeField, Min(0f)] private float staminaRecoveryRate = 1.5f;

    [SerializeField] private Image healthFill;
    [SerializeField] private Image staminaFill;
    [SerializeField] private Image manaFill;

    private Animator BloodyScreenAnim;
    private bool isUsingStamina;
    private int temporaryEffects;
    public bool HasTemporaryEffects => temporaryEffects > 0;

    private void Awake()
    {
        EnsureStatsInitialized();
    }

    private void Start() => GetNewComponents();

    private void EnsureStatsInitialized()
    {
        if (statsData == null)
            statsData = new PlayerStatsData();
    }

    // A new scene changes the view, not the live player session.
    public void GetNewComponents()
    {
        BindHUD(GameObject.Find("HeartFill")?.GetComponent<Image>(),
            GameObject.Find("StaminaFill")?.GetComponent<Image>(),
            GameObject.Find("ManaFill")?.GetComponent<Image>(),
            GameObject.Find("DeathScreen")?.GetComponent<Animator>());
    }

    public void BindHUD(Image health, Image stamina, Image mana, Animator deathScreen)
    {
        healthFill = health;
        staminaFill = stamina;
        manaFill = mana;
        BloodyScreenAnim = deathScreen;
        EnsureStatsInitialized();
        UpdateAllBars();
        if (statsData.currentHealth <= 0f)
            PlayerDeath();
    }

    private void Update()
    {
        RegenerateStamina();
    }

    public void ResetResources()
    {
        statsData.currentHealth = statsData.maxHealth;
        statsData.currentStamina = statsData.maxStamina;
        statsData.currentMana = statsData.maxMana;
        isUsingStamina = false;
        PlayerModeController.ForPlayer(GetComponent<PlayerController>())?.ResetToGameplay();
        UpdateAllBars();
    }

    private void UpdateAllBars()
    {
        UpdateHealthBar();
        UpdateStaminaBar();
        UpdateManaBar();
    }

    #region Core Stats
    private void LevelUp()
    {
        if (statsData.lvl >= 100)
        {
            Debug.Log("Max Level");
            return;
        }

        statsData.lvl++;
        IncreaseStats();
    }

    private void IncreaseStats()
    {
        IncreaseHealthStat();
        IncreaseStaminaStat();
        IncreaseManaStat();
    }

    public void GainEXP(int expGain)
    {
        if (statsData.lvl >= 100)
        {
            Debug.Log("Max Level No Exp Gain");
            return;
        }

        statsData.currentXP += expGain;
        while (statsData.currentXP >= statsData.nextLevelXP && statsData.lvl < 100)
        {
            LevelUp();
            statsData.currentXP -= statsData.nextLevelXP;
            statsData.nextLevelXP *= 3;
        }
    }
    #endregion

    #region Health
    public void DamagePlayer(float damageAmount)
    {
        statsData.currentHealth = Mathf.Max(0, statsData.currentHealth - damageAmount);
        UpdateHealthBar();
        if (statsData.currentHealth == 0)
        {
            PlayerDeath();
        }
    }

    public void HealPlayer(int healAmount)
    {
        statsData.currentHealth = Mathf.Min(statsData.maxHealth, statsData.currentHealth + healAmount);
        UpdateHealthBar();
    }

    private void IncreaseHealthStat()
    {
        statsData.maxHealth += statsData.lvl / 10f;
        statsData.currentHealth = statsData.maxHealth;
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (healthFill != null)
            healthFill.fillAmount = (float)statsData.currentHealth / statsData.maxHealth;
    }
    #endregion

    #region Stamina
    public void SetStaminaUse(bool inUse) => isUsingStamina = inUse;

    public void UseStamina(float staminaAmount)
    {
        if (isUsingStamina && statsData.currentStamina > 0)
        {
            statsData.currentStamina = Mathf.Max(0f, statsData.currentStamina - staminaAmount);
            UpdateStaminaBar();
        }
    }

    private void RegenerateStamina()
    {
        if (!isUsingStamina && statsData.currentStamina < statsData.maxStamina)
        {
            statsData.currentStamina = Mathf.Min(statsData.maxStamina,
                statsData.currentStamina + staminaRecoveryRate * Time.deltaTime);
            UpdateStaminaBar();
        }
    }

    private void IncreaseStaminaStat()
    {
        float increase = Mathf.RoundToInt(statsData.lvl * 1.35f) / 10f;
        statsData.maxStamina += increase;
        statsData.currentStamina = statsData.maxStamina;
        UpdateStaminaBar();
    }

    private void UpdateStaminaBar()
    {
        if (staminaFill != null)
            staminaFill.fillAmount = statsData.currentStamina / statsData.maxStamina;
    }

    public float GetStamina() => statsData.currentStamina;
    public float GetMaxStamina() => statsData.maxStamina;
    #endregion

    #region Mana
    public bool UseMana(float manaAmount)
    {
        if (statsData.currentMana >= manaAmount)
        {
            statsData.currentMana -= manaAmount;
            UpdateManaBar();
            return true;
        }
        return false;
    }

    private void IncreaseManaStat()
    {
        float increase = Mathf.RoundToInt(statsData.lvl * 0.35f) / 20f;
        statsData.maxMana += increase;
        statsData.currentMana = statsData.maxMana;
        UpdateManaBar();
    }

    private void UpdateManaBar()
    {
        if (manaFill != null)
            manaFill.fillAmount = (float)statsData.currentMana / statsData.maxMana;
    }
    #endregion

    #region SP Stats
    public void StatBuff(TextMeshProUGUI textObject, Stat stat, int buffAmount, float buffDuration)
    {
        StartCoroutine(ApplyBuffDebuff(textObject, stat, buffAmount, buffDuration));
    }

    public void StatDebuff(TextMeshProUGUI textObject, Stat stat, int debuffAmount, float debuffDuration)
    {
        StartCoroutine(ApplyBuffDebuff(textObject, stat, -debuffAmount, debuffDuration));
    }

    private IEnumerator ApplyBuffDebuff(TextMeshProUGUI textObject, Stat stat, int amount, float duration)
    {
        temporaryEffects++;
        int originalValue = stat.Value;
        stat.Value = Mathf.Max(1, stat.Value + amount);
        textObject.text = stat.Value.ToString();
        yield return new WaitForSeconds(duration);
        stat.Value = originalValue;
        textObject.text = stat.Value.ToString();
        temporaryEffects--;
    }
    #endregion

    #region Save/Load
    public PlayerStatsData GetPlayerStats()
    {
        EnsureStatsInitialized();
        return statsData;
    }

    #endregion

    public void RestoreStats(PlayerStatsData data)
    {
        StopAllCoroutines();
        temporaryEffects = 0;
        statsData = data ?? new PlayerStatsData();
        MigrateResourceScale();
        isUsingStamina = false;
        UpdateAllBars();
    }

    private void MigrateResourceScale()
    {
        if (statsData.resourceScaleVersion >= 2)
            return;

        statsData.maxHealth /= 10f;
        statsData.currentHealth /= 10f;
        statsData.maxStamina /= 10f;
        statsData.currentStamina /= 10f;
        statsData.maxMana /= 20f;
        statsData.currentMana /= 20f;
        statsData.resourceScaleVersion = 2;
    }

    private void PlayerDeath()
    {
        PlayerModeController.ForPlayer(GetComponent<PlayerController>())?.SetMode(PlayerMode.Dead);
        if (BloodyScreenAnim != null)
            BloodyScreenAnim.SetTrigger("Die");
    }
}

public class Stat
{
    public int Value { get; set; }

    public Stat(int value)
    {
        Value = value;
    }
}
