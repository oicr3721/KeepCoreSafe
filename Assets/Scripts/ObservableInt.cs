using System;
using UnityEngine;

[Serializable]
public sealed class ObservableInt
{
    [SerializeField] private int currentValue;
    [SerializeField] private int minValue;
    [SerializeField, Min(1)] private int maxValue = 1;

    public int CurrentValue => currentValue;
    public int MinValue => minValue;
    public int MaxValue => maxValue;
    public event Action<int, int> OnValueChanged;

    public void Initialize(int current, int maximum, int minimum = 0)
    {
        maxValue = Mathf.Max(1, maximum);
        minValue = Mathf.Min(minimum, maxValue);
        currentValue = Mathf.Clamp(current, minValue, maxValue);
        OnValueChanged?.Invoke(currentValue, maxValue);
    }

    public void SetValue(int value)
    {
        int next = Mathf.Clamp(value, minValue, maxValue);
        if (next == currentValue)
            return;

        currentValue = next;
        OnValueChanged?.Invoke(currentValue, maxValue);
    }

    public void AddValue(int value)
    {
        SetValue(currentValue + value);
    }
}
