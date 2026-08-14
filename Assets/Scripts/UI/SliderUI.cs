using UnityEngine;
using UnityEngine.UI;

public class SliderUI : MonoBehaviour
{
    protected ObservableValue source;

    [SerializeField]
    private Slider slider;

    protected void Start()
    {
        Initialize(source);
    }

    protected virtual void OnDestroy()
    {
        if (source == null) return;
        source.OnValueChanged -= Refresh;
    }

    private void Refresh(float current, float max)
    {
        slider.value = current / max;

        OnRefresh();
    }

    protected virtual void OnRefresh()
    {

    }

    public virtual void Initialize(ObservableValue source)
    {
        if (source == null) return;

        if (this.source != null)
            this.source.OnValueChanged -= Refresh;

        this.source = source;
        source.OnValueChanged += Refresh;

        Refresh(
            source.CurrentValue,
            source.MaxValue
        );
    }
}
