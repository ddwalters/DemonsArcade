public class PlayerStatsData
{
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
        inventoryType = InventoryType.FourByFourFull;

        lvl = 1;
        nextLevelXP = 500;

        maxHealth = 100f;
        maxStamina = 100f;
        maxMana = 100f;
    }
}