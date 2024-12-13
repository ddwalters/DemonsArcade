using BayatGames.SaveGameFree;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatsManager : MonoBehaviour
{
    private const string SaveIdentifier = "gameSaveIdentifier";
    private PlayerStatsData statsData;
    private PlayerController controller;

    private GameObject canvas;

    [SerializeField] private Image healthFill;
    [SerializeField] private Image staminaFill;
    [SerializeField] private Image manaFill;

    [SerializeField] private GameObject BloodyScreen;
    private Animator BloodyScreenAnim;
    private bool isUsingStamina;
    private bool isRegeneratingStamina;

    private void Awake()
    {
        canvas = GameObject.Find("MainCanvas");

        GameObject healthObject = GameObject.Find("HeartFill");
        healthFill = healthObject.GetComponent<Image>();

        GameObject staminaObject = GameObject.Find("StaminaFill");
        staminaFill = staminaObject.GetComponent<Image>();

        GameObject ManaObject = GameObject.Find("ManaFill");
        manaFill = ManaObject.GetComponent<Image>();

        BloodyScreen = GameObject.Find("DeathScreen");
        BloodyScreenAnim = BloodyScreen.GetComponent<Animator>();

        controller = gameObject.GetComponent<PlayerController>();
    }

    private void Start()
    {
        LoadPlayerStats();
        InitializeStats();
        UpdateAllBars();
    }

    public void GetNewComponents()
    {
        canvas = GameObject.Find("MainCanvas");

        GameObject healthObject = GameObject.Find("HeartFill");
        healthFill = healthObject.GetComponent<Image>();

        GameObject staminaObject = GameObject.Find("StaminaFill");
        staminaFill = staminaObject.GetComponent<Image>();

        GameObject ManaObject = GameObject.Find("ManaFill");
        manaFill = ManaObject.GetComponent<Image>();

        BloodyScreen = GameObject.Find("DeathScreen");
        BloodyScreenAnim = BloodyScreen.GetComponent<Animator>();

        LoadPlayerStats();
        InitializeStats();
        UpdateAllBars();
    }

    private void Update()
    {
        if (isRegeneratingStamina)
            RegenerateStamina();
    }

    private void InitializeStats()
    {
        statsData.currentHealth = statsData.maxHealth;
        statsData.currentStamina = statsData.maxStamina;
        statsData.currentMana = statsData.maxMana;
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
        statsData.maxHealth += statsData.lvl;
        statsData.currentHealth = statsData.maxHealth;
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        healthFill.fillAmount = (float)statsData.currentHealth / statsData.maxHealth;
    }
    #endregion

    #region Stamina
    public void StartUsingStamina() => isUsingStamina = true;

    public void StopUsingStamina()
    {
        isUsingStamina = false;
        isRegeneratingStamina = true;
    }

    public void UseStamina(float staminaAmount)
    {
        if (isUsingStamina && statsData.currentStamina > 0)
        {
            statsData.currentStamina -= staminaAmount;
            UpdateStaminaBar();
        }
    }

    private void RegenerateStamina()
    {
        if (!isUsingStamina && statsData.currentStamina < statsData.maxStamina)
        {
            statsData.currentStamina += 0.25f;
            UpdateStaminaBar();
            if (statsData.currentStamina >= statsData.maxStamina)
                isRegeneratingStamina = false;
        }
    }

    private void IncreaseStaminaStat()
    {
        int increase = Mathf.RoundToInt(statsData.lvl * 1.35f);
        statsData.maxStamina += increase;
        statsData.currentStamina = statsData.maxStamina;
        UpdateStaminaBar();
    }

    private void UpdateStaminaBar()
    {
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

    private void RegenerateMana()
    {
        if (statsData.currentMana < statsData.maxMana)
        {
            statsData.currentMana++;
            UpdateManaBar();
        }
    }

    private void IncreaseManaStat()
    {
        float increase = Mathf.RoundToInt(statsData.lvl * 0.35f);
        statsData.maxMana += increase;
        statsData.currentMana = statsData.maxMana;
        UpdateManaBar();
    }

    private void UpdateManaBar()
    {
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
        int originalValue = stat.Value;
        stat.Value = Mathf.Max(1, stat.Value + amount);
        textObject.text = stat.Value.ToString();
        yield return new WaitForSeconds(duration);
        stat.Value = originalValue;
        textObject.text = stat.Value.ToString();
    }
    #endregion

    #region Save/Load
    public PlayerStatsData GetPlayerStats()
    {
        LoadPlayerStats();
        return statsData;
    }

    public void SavePlayerStats()
    {
        SaveGame.Save(SaveIdentifier, statsData);
    }

    public void LoadPlayerStats()
    {
        statsData = SaveGame.Load<PlayerStatsData>(SaveIdentifier) ?? new PlayerStatsData();
    }
    #endregion

    private void PlayerDeath()
    {
        controller.DisableCameraMovement();
        controller.DisableMovement();
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