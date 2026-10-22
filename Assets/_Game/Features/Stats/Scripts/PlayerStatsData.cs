using System;

[Serializable]
public class PlayerStatsData
{
    public int resourceScaleVersion;
    public InventoryType inventoryType;
    public int lvl;
    public int nextLevelXP;
    public int currentXP;
    public float maxHealth;
    public float currentHealth;
    public float maxStamina;
    public float currentStamina;
    public float maxMana;
    public float currentMana;

    public PlayerStatsData()
    {
        resourceScaleVersion = 2;
        inventoryType = InventoryType.FourByFourFull;

        lvl = 1;
        nextLevelXP = 500;

        maxHealth = 10f;
        maxStamina = 10f;
        maxMana = 5f;
        currentHealth = maxHealth;
        currentStamina = maxStamina;
        currentMana = maxMana;
    }
}
