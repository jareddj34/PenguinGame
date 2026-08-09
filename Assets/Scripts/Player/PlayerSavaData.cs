public static class PlayerSaveData
{
    public static bool hasData;
    public static bool hasSword;
    public static bool hasShield;
    public static bool hasDashUpgrade;
    public static int snowballCount;
    public static bool gotSnowballs;
    public static int maxHeartContainers;

    public static void SaveFrom(PlayerAttack attack, PlayerShield shield, PlayerThrow snowball, PlayerHealth health, PlayerMovement movement)
    {
        hasSword = attack.hasSword;
        hasShield = shield.hasShield;
        snowballCount = snowball.snowballCount;
        gotSnowballs = snowball.gotSnowballs;
        hasDashUpgrade = movement.gotDashUpgrade;
        maxHeartContainers = health.maxHeartContainers;
        hasData = true;
    }

    public static void Clear() {
        hasData = false;
    }

}
