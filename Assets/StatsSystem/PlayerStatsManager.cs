using BayatGames.SaveGameFree;
using BayatGames.SaveGameFree.Examples;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour
{
    string identifier = "gameSaveIdentifier";
    PlayerStatsData statsData;

    Slider xpSlider;
    Image healthFill;
    Image staminaFill;
    Image manaFill;
    TextMeshProUGUI lvlText;
    TextMeshProUGUI xpText;
    TextMeshProUGUI goldText;
    TextMeshProUGUI healthText;
    TextMeshProUGUI staminaText;
    TextMeshProUGUI manaText;
    TextMeshProUGUI spText;
    TextMeshProUGUI strengthText;
    TextMeshProUGUI agilityText;
    TextMeshProUGUI vitalityText;
    TextMeshProUGUI enduranceText;
    TextMeshProUGUI intelligenceText;
    TextMeshProUGUI luckText;

    private void Awake()
    {
        lvlText = GameObject.Find("PlayerLevelText").GetComponent<TextMeshProUGUI>();
        xpText = GameObject.Find("EXPText").GetComponent<TextMeshProUGUI>();
        goldText = GameObject.Find("PlayerGoldText").GetComponent<TextMeshProUGUI>();
        healthText = GameObject.Find("PlayerHealthText").GetComponent<TextMeshProUGUI>();
        staminaText = GameObject.Find("PlayerStaminaText").GetComponent<TextMeshProUGUI>();
        manaText = GameObject.Find("PlayerManaText").GetComponent<TextMeshProUGUI>();
        spText = GameObject.Find("PlayerSPText").GetComponent<TextMeshProUGUI>();
        strengthText = GameObject.Find("PlayerStrengthText").GetComponent<TextMeshProUGUI>();
        agilityText = GameObject.Find("PlayerAgilityText").GetComponent<TextMeshProUGUI>();
        vitalityText = GameObject.Find("PlayerVitalityText").GetComponent<TextMeshProUGUI>();
        enduranceText = GameObject.Find("PlayerEnduranceText").GetComponent<TextMeshProUGUI>();
        intelligenceText = GameObject.Find("PlayerIntelligenceText").GetComponent<TextMeshProUGUI>();
        luckText = GameObject.Find("PlayerLuckText").GetComponent<TextMeshProUGUI>();

        healthFill = GameObject.Find("HeartFill").GetComponent<Image>();
        staminaFill = GameObject.Find("StaminaFill").GetComponent<Image>();
        manaFill = GameObject.Find("ManaFill").GetComponent<Image>();

        xpSlider = GameObject.Find("XPBarSlider").GetComponent<Slider>();
    }

    private void Start()
    {
        LoadPlayerStats();

        UpdateHealthBar();
        UpdateStaminaBar();
        UpdateManaBar();

        StartCoroutine(RegenerateStamina());
        StartCoroutine(RegenerateMana());
    }

    private void Update()
    {
        RegenerateStamina();
        RegenerateMana();
    }

    #region Core Stats
    private void LevelUp()
    {
        if (statsData.lvl < 100)
        {
            Debug.Log("Max Level");
            return;
        }

        if (statsData.lvl + 1 < 100)
        {
            statsData.lvl += 1;
            IncreaseHealthStat();
            IncreaseStaminaStat();
            IncreaseManaStat();
            GainSP();
        }
    }

    public void GainEXP(int expGain)
    {
        if (statsData.lvl < 100)
            Debug.Log("Max Level No Exp Gain");

        statsData.currentXP += expGain;

        while (statsData.currentXP >= statsData.nextLevelXP && statsData.lvl < 100)
        {
            LevelUp();
            statsData.currentXP -= statsData.nextLevelXP;
            statsData.nextLevelXP *= 3;
        }

        xpSlider.maxValue = statsData.nextLevelXP;
        xpSlider.value = statsData.currentXP;
    }

    #region Health
    public void DamagePlayer(int damageAmount)
    {
        statsData.currentHealth -= damageAmount;

        if (statsData.currentHealth <= 0)
        {
            statsData.currentHealth = 0;

            UpdateHealthBar();
            // player is dead now 
            // do stuff for the dead thingy
        }

        UpdateHealthBar();
    }

    public void HealPlayer(int healAmount)
    {
        if (statsData.currentHealth >= statsData.maxHealth)
            return;

        statsData.currentHealth += healAmount;
        if (statsData.currentHealth > statsData.maxHealth)
            statsData.currentHealth = statsData.maxHealth;

        UpdateHealthBar();
    }

    private void IncreaseHealthStat()
    {
        statsData.maxHealth += statsData.lvl;
        statsData.currentHealth += statsData.lvl;

        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        float healthNormalized = Mathf.Clamp01(statsData.currentHealth / statsData.maxHealth);
        healthFill.fillAmount = healthNormalized;
    }
    #endregion

    #region Stamina
    // true or false for if there is stamina available/ if the action should happen
    public bool UseStamina(int staminaAmount)
    {
        int temp = statsData.currentStamina;

        statsData.maxStamina -= staminaAmount;
        if (statsData.currentStamina < 0)
        {
            statsData.currentStamina = temp;
            return false;
        }

        return true;
    }

    // this time should be based on players endurance stat
    private IEnumerator RegenerateStamina()
    {
        while (statsData.currentStamina < statsData.maxStamina)
        {
            yield return new WaitForSeconds(0.5f);
            statsData.currentStamina++;

            UpdateStaminaBar();
        }
    }

    private void IncreaseStaminaStat()
    {
        int toIncrease = Mathf.RoundToInt(statsData.lvl * 1.35f);
        statsData.maxStamina += toIncrease;
        statsData.currentStamina += toIncrease;

        UpdateStaminaBar();
    }

    private void UpdateStaminaBar()
    {
        float staminaNormalized = Mathf.Clamp01(statsData.currentStamina / statsData.maxStamina);
        staminaFill.fillAmount = staminaNormalized;
    }
    #endregion

    #region Mana
    // true or false for if there is stamina available/ if the action should happen
    public bool UseMana(int manaAmount)
    {
        int temp = statsData.currentMana;

        statsData.currentMana -= manaAmount;
        if (statsData.currentMana < 0)
        {
            statsData.currentMana = temp;
            return false;
        }

        return true;
    }

    private IEnumerator RegenerateMana()
    {
        while (statsData.currentMana < statsData.maxMana)
        {
            // this time should be based on players int stat
            yield return new WaitForSeconds(0.25f);
            statsData.currentMana++;

            UpdateStaminaBar();
        }
    }

    private void IncreaseManaStat()
    {
        int toIncrease = Mathf.RoundToInt(statsData.lvl * .35f);
        statsData.maxMana += toIncrease;
        statsData.currentMana += toIncrease;

        UpdateHealthBar();
    }

    private void UpdateManaBar()
    {
        float manaNormalized = Mathf.Clamp01(statsData.currentMana / statsData.maxMana);
        manaFill.fillAmount = manaNormalized;
    }
    #endregion
    #endregion

    #region SP Stats
    private void GainSP()
    {
        statsData.SP += Mathf.RoundToInt(statsData.baseSP * Mathf.Pow(statsData.lvl, statsData.spGrowth));
    }

    public void IncreaseStat(TextMeshProUGUI textObject, ref int attribute)
    {
        if (statsData.SP < 1)
            return;

        statsData.SP -= 1;
        attribute += 1;
        textObject.text = attribute.ToString();
    }

    public void StatBuff(TextMeshProUGUI textObject, ref int attribute, int buffAmount, float buffTimer)
    {
        int temp = attribute;
        attribute += buffAmount;
        textObject.text = attribute.ToString();

        while (buffTimer > 0)
        {
            buffTimer -= Time.deltaTime;
        }

        attribute = temp;
        textObject.text = attribute.ToString();
    }

    public void StatDebuff(TextMeshProUGUI textObject, ref int attribute, int debuffAmount, float debuffTimer)
    {
        int temp = attribute;
        if (attribute - debuffAmount < 1)
            attribute = 1;
        else
            attribute -= debuffAmount;

        textObject.text = attribute.ToString();

        while (debuffTimer > 0)
            debuffTimer -= Time.deltaTime;

        attribute = temp;
        textObject.text = attribute.ToString();
    }
    #endregion

    #region gold
    public int GetCurrentGold()
    {
        return statsData.gold;
    }

    public void GainGold(int newGold)
    {
        statsData.gold += newGold;
        goldText.text = "Gold: " + statsData.gold;
    }

    /// <summary>
    /// Removes gold from the user if plausable.
    /// </summary>
    /// <param name="removeGoldAmount"></param>
    /// <returns>True if gold has been removed, false if player lacked funds.</returns>
    public bool RemoveGold(int removeGoldAmount)
    {
        int temp = statsData.gold;

        temp -= removeGoldAmount;
        if (temp < 0)
            return false;

        statsData.gold = temp;
        goldText.text = "Gold: " + statsData.gold;

        return true;
    }
    #endregion

    public void SavePlayerStats()
    {
        SaveGame.Save(identifier, statsData);
    }

    public void LoadPlayerStats()
    {
        if (statsData != null)
        {
            statsData = SaveGame.Load<PlayerStatsData>(identifier);
            Debug.Log("Loaded");
        }
        else
        {
            Debug.Log("Saved Data is null");
            statsData = new PlayerStatsData();

            statsData.currentXP = 0;
            lvlText.text = "Lvl: " + statsData.lvl;
            xpText.text = "Exp: " + statsData.currentXP + "/" + statsData.nextLevelXP;
            healthText.text = statsData.currentHealth + "/" + statsData.maxHealth;
            staminaText.text = statsData.currentStamina + "/" + statsData.maxStamina;
            manaText.text = statsData.currentMana + "/" + statsData.maxMana;

            xpSlider.maxValue = statsData.nextLevelXP;
            xpSlider.value = statsData.currentXP;

            spText.text = "SP: " + statsData.SP;
            strengthText.text = statsData.strength.ToString();
            agilityText.text = statsData.agility.ToString();
            vitalityText.text = statsData.vitality.ToString();
            enduranceText.text = statsData.endurance.ToString();
            intelligenceText.text = statsData.intelligence.ToString();
            luckText.text = statsData.luck.ToString();
        }
    }
}