using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ExplosionParams
{
    public static float radius = 10.0F;
    public static float power = 50F;

    public static float minRadius = 0f;
    public static float maxRadius = 15f;

    public static float minPower = 0f;
    public static float maxPower = 70f;

    public static float minVerticalOffset = -10;
    public static float maxVerticalOffset = 10;
    public static float verticalOffset = 0;
}

public static class SimpleForceParams
{
    public static float radius = 10.0F;

    public static float minRadius = 0f;
    public static float maxRadius = 15f;

    public static float minPower = 0f;
    public static float maxPower = 100f;

    public static float minVerticalOffset = -10;
    public static float maxVerticalOffset = 10;
    public static float verticalOffset = 0;

    public static float powerX = 10f;
    public static float powerY = 0;
    public static float powerZ = 0f;

}

public static class BlackHoleParams
{
    public static float radius = 40.0F;
    public static float power = 20F;

    public static float minRadius = 0f;
    public static float maxRadius = 70f;

    public static float minPower = 0f;
    public static float maxPower = 100f;

    public static float minVerticalOffset = -50;
    public static float maxVerticalOffset = 50;
    public static float verticalOffset = 20;

    public static float minModifier = 0f;
    public static float modifier = 2f;
    public static float maxModifier = 6f;
}