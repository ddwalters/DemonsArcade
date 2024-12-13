public class PlayerStatsData
{
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
        lvl = 1;
        nextLevelXP = 500;

        maxHealth = 100f;
        maxStamina = 100f;
        maxMana = 100f;
    }
}