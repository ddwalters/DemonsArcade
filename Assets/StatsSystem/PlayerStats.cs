using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour
{
    bool playerIsInitialized;

    int currentLvl;
    int currentXP;
    int nextLevelXP;
    TextMeshProUGUI lvlText;
    Slider xpSlider;
    TextMeshProUGUI xpText;

    int goldAmount;
    TextMeshProUGUI goldText;

    int maxHealth;
    int currentHealth;
    TextMeshProUGUI healthText;
    Image healthFill;

    int maxStamina;
    int currentStamina;
    TextMeshProUGUI staminaText;
    Image staminaFill;

    int maxMana;
    int currentMana;
    TextMeshProUGUI manaText;
    Image manaFill;

    int baseSP = 1;
    float spGrowth = 1.5f;
    int SP;
    TextMeshProUGUI spText;

    int strength;
    TextMeshProUGUI strengthText;

    int agility;
    TextMeshProUGUI agilityText;

    int vitality;
    TextMeshProUGUI vitalityText;

    int endurance;
    TextMeshProUGUI enduranceText;

    int intelligence;
    TextMeshProUGUI intelligenceText;

    int luck;
    TextMeshProUGUI luckText;

    private void Awake()
    {
        // I do this so I dont have to serialize every field, find a better way to get the strings
        // getting object by name would be a good fix and prevent making a list of every object
        List<TextMeshProUGUI> uiTexts = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None).ToList();
        List<Slider> uiSliders = FindObjectsByType<Slider>(FindObjectsSortMode.None).ToList();
        List<Image> uiImages = FindObjectsByType<Image>(FindObjectsSortMode.None).ToList();

        // hud 
        healthFill = uiImages.FirstOrDefault(x => x.CompareTag("HeartFill"));
        staminaFill = uiImages.FirstOrDefault(x => x.CompareTag("StaminaFill"));
        manaFill = uiImages.FirstOrDefault(x => x.CompareTag("ManaFill"));

        // core stats
        lvlText = uiTexts.FirstOrDefault(x => x.CompareTag("LevelText"));
        xpText = uiTexts.FirstOrDefault(x => x.CompareTag("XPText"));
        healthText = uiTexts.FirstOrDefault(x => x.CompareTag("HealthText"));
        staminaText = uiTexts.FirstOrDefault(x => x.CompareTag("StaminaText"));
        manaText = uiTexts.FirstOrDefault(x => x.CompareTag("ManaText"));
        goldText = uiTexts.FirstOrDefault(x => x.CompareTag("GoldText"));

        xpSlider = uiSliders.FirstOrDefault(x => x.CompareTag("XPBar"));

        // SP stats
        spText = uiTexts.FirstOrDefault(x => x.CompareTag("SPText"));
        strengthText = uiTexts.FirstOrDefault(x => x.CompareTag("StrengthText"));
        agilityText = uiTexts.FirstOrDefault(x => x.CompareTag("AgilityText"));
        vitalityText = uiTexts.FirstOrDefault(x => x.CompareTag("VitalityText"));
        enduranceText = uiTexts.FirstOrDefault(x => x.CompareTag("EnduranceText"));
        intelligenceText = uiTexts.FirstOrDefault(x => x.CompareTag("IntelligenceText"));
        luckText = uiTexts.FirstOrDefault(x => x.CompareTag("LuckText"));
    }

    private void Start()
    {
        if (!playerIsInitialized)
            InitializePlayer();

        SetupStatsPage();

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
        if (currentLvl < 100)
        {
            Debug.Log("Max Level");
            return;
        }

        if (currentLvl + 1 < 100)
        {
            currentLvl += 1;
            IncreaseHealthStat();
            IncreaseStaminaStat();
            IncreaseManaStat();
            GainSP();
        }
    }

    public void GainEXP(int expGain)
    {
        if (currentLvl < 100)
            Debug.Log("Max Level No Exp Gain");

        currentXP += expGain;

        while (currentXP >= nextLevelXP && currentLvl < 100)
        {
            LevelUp();
            currentXP -= nextLevelXP;
            nextLevelXP *= 3;
        }

        xpSlider.maxValue = nextLevelXP;
        xpSlider.value = currentXP;
    }

    #region Health
    public void DamagePlayer(int damageAmount)
    {
        currentHealth -= damageAmount;

        if (currentHealth <= 0)
        {
            currentHealth = 0;

            UpdateHealthBar();
            // player is dead now 
            // do stuff for the dead thingy
        }

        UpdateHealthBar();
    }

    public void HealPlayer(int healAmount)
    {
        if (currentHealth >= maxHealth)
            return;

        currentHealth += healAmount;
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;

        UpdateHealthBar();
    }

    private void IncreaseHealthStat()
    {
        maxHealth += currentLvl;
        currentHealth += currentLvl;

        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        float healthNormalized = Mathf.Clamp01(currentHealth / maxHealth);
        healthFill.fillAmount = healthNormalized;
    }
    #endregion

    #region Stamina
    // true or false for if there is stamina available/ if the action should happen
    public bool UseStamina(int staminaAmount)
    {
        int temp = currentStamina;

        currentStamina -= staminaAmount;
        if (currentStamina < 0)
        {
            currentStamina = temp;
            return false;
        }

        return true;
    }

    // this time should be based on players endurance stat
    private IEnumerator RegenerateStamina()
    {
        while (currentStamina < maxStamina)
        {
            yield return new WaitForSeconds(0.5f);
            currentStamina++;

            UpdateStaminaBar();
        }
    }

    private void IncreaseStaminaStat()
    {
        int toIncrease = Mathf.RoundToInt(currentLvl * 1.35f);
        maxStamina += toIncrease;
        currentStamina += toIncrease;

        UpdateStaminaBar();
    }

    private void UpdateStaminaBar()
    {
        float staminaNormalized = Mathf.Clamp01(currentHealth / maxHealth);
        staminaFill.fillAmount = staminaNormalized;
    }
    #endregion

    #region Mana
    // true or false for if there is stamina available/ if the action should happen
    public bool UseMana(int manaAmount)
    {
        int temp = currentMana;

        currentMana -= manaAmount;
        if (currentMana < 0)
        {
            currentMana = temp;
            return false;
        }

        return true;
    }

    private IEnumerator RegenerateMana()
    {
        while (currentStamina < maxStamina)
        {
            // this time should be based on players int stat
            yield return new WaitForSeconds(0.25f);
            currentStamina++;

            UpdateStaminaBar();
        }
    }

    private void IncreaseManaStat()
    {
        int toIncrease = Mathf.RoundToInt(currentLvl * .35f);
        maxMana += toIncrease;
        currentMana += toIncrease;

        UpdateHealthBar();
    }

    private void UpdateManaBar()
    {
        float manaNormalized = Mathf.Clamp01(currentHealth / maxHealth);
        manaFill.fillAmount = manaNormalized;
    }
    #endregion
    #endregion

    #region SP Stats
    private void GainSP()
    {
        SP += Mathf.RoundToInt(baseSP * Mathf.Pow(currentLvl, spGrowth));
    }

    public void IncreaseStat(TextMeshProUGUI textObject, ref int attribute)
    {
        if (SP < 1)
            return;

        SP -= 1;
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
        return goldAmount;
    }

    public void GainGold(int newGold)
    {
        goldAmount += newGold;
        goldText.text = "Gold: " + goldAmount;
    }


    /// <summary>
    /// Removes gold from the user if plausable.
    /// </summary>
    /// <param name="removeGoldAmount"></param>
    /// <returns>True if gold has been removed, false if player lacked funds.</returns>
    public bool RemoveGold(int removeGoldAmount)
    {
        int temp = goldAmount;

        temp -= removeGoldAmount;
        if (temp < 0)
            return false;

        goldAmount = temp;
        goldText.text = "Gold: " + goldAmount;

        return true;
    }
    #endregion

    private void InitializePlayer()
    {
        currentLvl = 1;

        maxHealth = 15;
        currentHealth = 15;
        maxStamina = 20;
        currentStamina = 20;
        maxMana = 10;
        currentMana = 10;

        SP = 0;
        strength = 1;
        agility = 1;
        vitality = 1;
        endurance = 1;
        intelligence = 1;
        luck = 1;

        playerIsInitialized = true;
    }

    private void SetupStatsPage()
    {
        lvlText.text = "Lvl: " + currentLvl;
        xpText.text = "Exp: " + currentXP + "/" + nextLevelXP;
        healthText.text = currentHealth + "/" + maxHealth;
        staminaText.text = currentStamina + "/" + maxStamina;
        manaText.text = currentMana + "/" + maxMana;

        xpSlider.maxValue = nextLevelXP;
        xpSlider.value = currentXP;

        spText.text = "SP: " + SP;
        strengthText.text = strength.ToString();
        agilityText.text = agility.ToString();
        vitalityText.text = vitality.ToString();
        enduranceText.text = endurance.ToString();
        intelligenceText.text = intelligence.ToString();
        luckText.text = luck.ToString();
    }
}