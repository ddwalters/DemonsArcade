using BayatGames.SaveGameFree;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatsManager : MonoBehaviour
{
    string identifier = "gameSaveIdentifier";
    PlayerStatsData statsData;

    [SerializeField] Image healthFill;
    [SerializeField] Image staminaFill;
    [SerializeField] Image manaFill;

    private void Start()
    {
        if (statsData == null)
            LoadPlayerStats();

        statsData.currentHealth = statsData.maxHealth;
        statsData.currentStamina = statsData.maxStamina;
        statsData.currentMana = statsData.maxMana;

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
            //GainSP();
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

        //xpSlider.maxValue = statsData.nextLevelXP;
        //xpSlider.value = statsData.currentXP;
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
        float healthNormalized = (float)statsData.currentHealth / (float)statsData.maxHealth;
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

    public int GetStamina()
    {
        return statsData.currentStamina;
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
        float staminaNormalized = (float)statsData.currentStamina / (float)statsData.maxStamina;
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
        float manaNormalized = (float)statsData.currentMana / (float)statsData.maxMana;
        manaFill.fillAmount = manaNormalized;
    }
    #endregion
    #endregion

    //#region SP Stats
    //private void GainSP()
    //{
    //    statsData.SP += Mathf.RoundToInt(statsData.baseSP * Mathf.Pow(statsData.lvl, statsData.spGrowth));
    //}
    //
    //public void IncreaseStat(TextMeshProUGUI textObject, ref int attribute)
    //{
    //    if (statsData.SP < 1)
    //        return;
    //
    //    statsData.SP -= 1;
    //    attribute += 1;
    //    textObject.text = attribute.ToString();
    //}
    //
    //public void StatBuff(TextMeshProUGUI textObject, ref int attribute, int buffAmount, float buffTimer)
    //{
    //    int temp = attribute;
    //    attribute += buffAmount;
    //    textObject.text = attribute.ToString();
    //
    //    while (buffTimer > 0)
    //    {
    //        buffTimer -= Time.deltaTime;
    //    }
    //
    //    attribute = temp;
    //    textObject.text = attribute.ToString();
    //}
    //
    //public void StatDebuff(TextMeshProUGUI textObject, ref int attribute, int debuffAmount, float debuffTimer)
    //{
    //    int temp = attribute;
    //    if (attribute - debuffAmount < 1)
    //        attribute = 1;
    //    else
    //        attribute -= debuffAmount;
    //
    //    textObject.text = attribute.ToString();
    //
    //    while (debuffTimer > 0)
    //        debuffTimer -= Time.deltaTime;
    //
    //    attribute = temp;
    //    textObject.text = attribute.ToString();
    //}
    //#endregion

    #region gold
    public int GetCurrentGold()
    {
        return statsData.gold;
    }

    public void GainGold(int newGold)
    {
        statsData.gold += newGold;
        //goldText.text = "Gold: " + statsData.gold;
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
        //goldText.text = "Gold: " + statsData.gold;

        return true;
    }
    #endregion

    public PlayerStatsData GetPlayerStats()
    {
        if (statsData == null)
            LoadPlayerStats();

        return statsData;
    }

    public void SavePlayerStats()
    {
        SaveGame.Save(identifier, statsData);
    }

    public void LoadPlayerStats()
    {
        if (statsData != null)
            statsData = SaveGame.Load<PlayerStatsData>(identifier);
        else
            statsData = new PlayerStatsData();
    }
}