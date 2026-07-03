using System;
using UnityEngine;

[System.Serializable]
public class ObservableValue
{
    public event Action<float, float> OnValueChanged;

    [SerializeField]
    private float currentValue;

    [SerializeField]
    private float maxValue = 100f;

    public float CurrentValue => currentValue;
    public float MaxValue => maxValue;

    public void SetValue(float value)
    {
        currentValue = Mathf.Clamp(value, 0f, maxValue);

        if (currentValue < 0.0001f)
            currentValue = 0f;

        OnValueChanged?.Invoke(
            currentValue,
            maxValue
        );
    }

    public void AddValue(float value)
    {
        SetValue(currentValue + value);
    }

    public void SubtractValue(float value)
    {
        SetValue(currentValue - value);
    }

}