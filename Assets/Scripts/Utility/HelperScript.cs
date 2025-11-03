using Unity.Mathematics;
using UnityEngine;

public class HelperScript
{
    public static float Deviate(float center, float deviation)
    {
        return UnityEngine.Random.Range(center - deviation, center + deviation);
    }

    public static float RemapFloat(float value, float2 InMinMax, float2 OutMinMax)
    {
        return OutMinMax.x + (value - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
    }

    public static float OneMinus(float value)
    {
        return 1 - value;
    }

    public static float Reciprocal(float value)
    {
        return 1 / value;
    }

    public static float Square(float x)
    {
        return x * x;
    }

    public static float EaseOut(float x)
    {
        x = OneMinus(x);
        x = Square(x);
        x = OneMinus(x);
        return x;
    }

    public static float EaseIn(float x)
    {
        x = Square(x);
        return x;
    }

    public static float EaseInOut(float x)
    {
        if(x < 0.5f)
        {
            return 0.5f * Square( x * 2 ); // Combine scaling and squaring in one step
        } else
        {
            float inverseX = 1 - (x - 0.5f) * 2;
            return 0.5f * (1 - Square( inverseX )) + 0.5f;
        }
    }


    public static float Saturate(float x)
    {
        return Mathf.Max(0, Mathf.Min(1, x));
    }
}