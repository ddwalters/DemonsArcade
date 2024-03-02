using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour
{
    int currentLvl;
    int currentXP;
    int nextLevelXP;
    TextMeshProUGUI lvlText;
    Slider xpSlider;
    TextMeshProUGUI xpText;


    int maxHealth;
    int currentHealth;
    TextMeshProUGUI healthText;
    // hud health icon (should have tag)

    int maxStamina;
    int currentStamina;
    TextMeshProUGUI staminaText;
    // hud stamina slider (should have tag)

    int maxMana;
    int currentMana;
    TextMeshProUGUI manaText;
    // hud mana slider (should have tag)

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
        List<TextMeshProUGUI> uiTexts = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None).ToList();
        List<Slider> uiSliders = FindObjectsByType<Slider>(FindObjectsSortMode.None).ToList();

        // core stats
        lvlText = uiTexts.FirstOrDefault(x => x.CompareTag("LevelText"));
        xpText = uiTexts.FirstOrDefault(x => x.CompareTag("XPText"));
        healthText = uiTexts.FirstOrDefault(x => x.CompareTag("HealthText"));
        staminaText = uiTexts.FirstOrDefault(x => x.CompareTag("StaminaText"));
        manaText = uiTexts.FirstOrDefault(x => x.CompareTag("ManaText"));

        xpSlider = uiSliders.FirstOrDefault(x => x.CompareTag("XPBar"));

        // SP stats
        spText = uiTexts.FirstOrDefault(x => x.CompareTag("SPText"));
        strengthText = uiTexts.FirstOrDefault(x => x.CompareTag("StrengthText"));
        agilityText = uiTexts.FirstOrDefault(x => x.CompareTag("AgilityText"));
        vitalityText = uiTexts.FirstOrDefault(x => x.CompareTag("VitalityText"));
        enduranceText = uiTexts.FirstOrDefault(x => x.CompareTag("EnduranceText"));
        intelligenceText = uiTexts.FirstOrDefault(x => x.CompareTag("IntelligenceText"));
        luckText = uiTexts.FirstOrDefault(x => x.CompareTag("SPText"));
    }

    private void Start()
    {
        lvlText.text = "Lvl: " + currentLvl;
        xpText.text = "Exp: " + currentXP + "/" + nextLevelXP;
        healthText.text = currentHealth + "/" + maxHealth;
        staminaText.text = currentStamina + "/" + maxStamina;
        manaText.text = currentMana + "/" + maxMana;

        // xp slider stuff

        spText.text = "SP: " + SP;
        strengthText.text = strength.ToString();
        agilityText.text = agility.ToString();
        vitalityText.text = vitality.ToString();
        enduranceText.text = endurance.ToString();
        intelligenceText.text = intelligence.ToString();
        luckText.text = luck.ToString();
    }

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

    #region Core Stats
    private void IncreaseHealthStat()
    {
        maxHealth += currentLvl;
        currentHealth += currentLvl;
    }

    private void IncreaseStaminaStat()
    {
        int toIncrease = Mathf.RoundToInt(currentLvl * 1.35f);
        maxStamina += toIncrease;
        currentStamina += toIncrease;
    }

    private void IncreaseManaStat()
    {
        int toIncrease = Mathf.RoundToInt(currentLvl * .35f);
        maxMana += toIncrease;
        currentMana += toIncrease;
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

        // xp bar max = nextLevelXP
        // xp bar fill = currentXP
    }
    #endregion

    #region SP Stats
    private void GainSP()
    {
        SP += Mathf.RoundToInt(baseSP * Mathf.Pow(currentLvl, spGrowth));
    }

    #region Strength
    public void IncreaseStrength()
    {
        strength += 1;
        // strength text update
    }

    public void StrengthBuff(int buffAmount, float buffTimer)
    {
        int tempStrengthHolder = strength;
        strength += buffAmount;
        // strength text update

        while (buffTimer > 0)
        {
            buffTimer -= Time.deltaTime;
        }

        strength = tempStrengthHolder;
        // strength text update
    }

    public void StrengthDebuff(int debuffAmount, float buffTimer)
    {
        int tempStrengthHolder = strength;
        if (strength - debuffAmount < 1)
        {
            strength = 1;
            // strength text update
        }
        else
        {
            strength -= debuffAmount;
            // strength text update
        }

        while (buffTimer > 0)
        {
            buffTimer -= Time.deltaTime;
        }

        strength = tempStrengthHolder;
        // strength text update
    }
    #endregion

    #region Agility
    public void IncreaseAgility()
    {
        agility += 1;
    }

    public void AgilityBuff(int buffAmount, float buffTimer)
    {
        int tempAgilityHolder = agility;
        agility += buffAmount;

        while (buffTimer > 0)
        {
            buffTimer -= Time.deltaTime;
        }

        agility = tempAgilityHolder;
    }

    public void AgilityDebuff(int debuffAmount, float buffTimer)
    {
        int tempAgilityHolder = agility;
        if (agility - debuffAmount < 1)
            agility = 1;
        else
            agility -= debuffAmount;

        while (buffTimer > 0)
        {
            buffTimer -= Time.deltaTime;
        }

        agility = tempAgilityHolder;
    }
    #endregion

    #region Vitality
    public void IncreaseVitality()
    {
        vitality += 1;
    }

    public void VitalityBuff(int buffAmount, float buffTimer)
    {
        int tempVitalityHolder = vitality;
        vitality += buffAmount;

        while (buffTimer > 0)
        {
            buffTimer -= Time.deltaTime;
        }

        vitality = tempVitalityHolder;
    }

    public void VitalityDebuff(int debuffAmount, float buffTimer)
    {
        int tempVitalityHolder = vitality;
        if (vitality - debuffAmount < 1)
            vitality = 1;
        else
            vitality -= debuffAmount;

        while (buffTimer > 0)
        {
            buffTimer -= Time.deltaTime;
        }

        vitality = tempVitalityHolder;
    }
    #endregion

    #region Endurance
    public void IncreaseEndurance()
    {
        endurance += 1;
    }

    public void EnduranceBuff(int buffAmount, float buffTimer)
    {
        int tempEnduranceHolder = endurance;
        endurance += buffAmount;

        while (buffTimer > 0)
        {
            buffTimer -= Time.deltaTime;
        }

        endurance = tempEnduranceHolder;
    }

    public void EnduranceDebuff(int debuffAmount, float buffTimer)
    {
        int tempEnduranceHolder = endurance;
        if (endurance - debuffAmount < 1)
            endurance = 1;
        else
            endurance -= debuffAmount;

        while (buffTimer > 0)
        {
            buffTimer -= Time.deltaTime;
        }

        endurance = tempEnduranceHolder;
    }
    #endregion

    #region Intelligence
    public void IncreaseIntelligence()
    {
        intelligence += 1;
    }

    public void IntelligenceBuff(int buffAmount, float buffTimer)
    {
        int tempIntelligenceHolder = intelligence;
        intelligence += buffAmount;

        while (buffTimer > 0)
        {
            buffTimer -= Time.deltaTime;
        }

        intelligence = tempIntelligenceHolder;
    }

    public void IntelligenceDebuff(int debuffAmount, float buffTimer)
    {
        int tempIntelligenceHolder = intelligence;
        if (intelligence - debuffAmount < 1)
            intelligence = 1;
        else
            intelligence -= debuffAmount;

        while (buffTimer > 0)
        {
            buffTimer -= Time.deltaTime;
        }

        intelligence = tempIntelligenceHolder;
    }
    #endregion

    #region Luck
    public void IncreaseLuck()
    {
        luck += 1;
    }

    public void LuckBuff(int buffAmount, float buffTimer)
    {
        int tempLuckHolder = luck;
        luck += buffAmount;

        while (buffTimer > 0)
        {
            buffTimer -= Time.deltaTime;
        }

        luck = tempLuckHolder;
    }

    public void LuckDebuff(int debuffAmount, float buffTimer)
    {
        int tempLuckHolder = luck;
        if (luck - debuffAmount < 1)
            luck = 1;
        else
            luck -= debuffAmount;

        while (buffTimer > 0)
        {
            buffTimer -= Time.deltaTime;
        }

        luck = tempLuckHolder;
    }
    #endregion
    #endregion
}
