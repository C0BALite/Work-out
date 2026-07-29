public static class ScoringUtils
{
    // множитель растёт если решили быстро (много времени осталось) и правильно
    public static float ComputeMultiplier(float timeLeftRatio, float correctness, float baseMult = 1f)
        => baseMult * (0.5f + 0.5f * timeLeftRatio) * correctness;
}