using System;

public static class VolumeMapper
{
    private const float MinDb = -80.0f;
    private const float MaxDb = 20.0f;

    /// <summary>
    /// 将滑块值(0-1)映射到分贝值(-80dB 到 20dB)
    /// </summary>
    public static float SliderToDb(float sliderValue)
    {
        // 边界处理
        if (sliderValue <= 0) return MinDb;
        if (sliderValue >= 1) return MaxDb;

        // 归一化到0-1范围
        float normalized = sliderValue;

        // 对数映射: 使用log10(1 + 9*x) 将0-1映射到0-1
        double dbRange = MaxDb - MinDb;
        double db = MinDb + dbRange * Math.Log10(1 + 9 * normalized);

        return (float)db;
    }

    /// <summary>
    /// 将分贝值(-80dB 到 20dB)映射回滑块值(0-1)
    /// </summary>
    public static float DbToSlider(float dbValue)
    {
        // 边界处理
        if (dbValue <= MinDb) return 0;
        if (dbValue >= MaxDb) return 1;

        // 计算归一化值 (0-1)
        double dbRange = MaxDb - MinDb;
        double normalizedDb = (dbValue - MinDb) / dbRange;

        // 反向对数映射: 从log10(1+9x) 反推 x
        // normalizedDb = log10(1 + 9x)  => 10^normalizedDb = 1 + 9x
        double x = (Math.Pow(10, normalizedDb) - 1) / 9;

        // 转换为滑块值 (0-1)
        return (float)x;
    }
}
