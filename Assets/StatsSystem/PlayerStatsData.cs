public class PlayerStatsData
{
    public int lvl;
    public int nextLevelXP;
    public int currentXP;
    public int gold;
    public int maxHealth;
    public int currentHealth;
    public int maxStamina;
    public int currentStamina;
    public int maxMana;
    public int currentMana;
    public int baseSP;
    public float spGrowth;
    public int SP;
    public int strength;
    public int agility;
    public int vitality;
    public int endurance;
    public int intelligence;
    public int luck;

    public PlayerStatsData()
    {
        lvl = 1;
        nextLevelXP = 500;

        maxHealth = 100;
        maxStamina = 100;
        maxMana = 100;

        SP = 0;
        baseSP = 0;
        spGrowth = 1.5f;
        strength = 1;
        agility = 1;
        vitality = 1;
        endurance = 1;
        intelligence = 1;
        luck = 1;
    }
}