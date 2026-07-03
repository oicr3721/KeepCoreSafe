using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace KeepCoreSafe.UI
{
    public class CountTextUI : MonoBehaviour
    {
        protected ObservableValue source;

        [SerializeField]
        protected TMP_Text tmp;

        protected void Start()
        {
            Initialize(source);
        }

        protected void OnDestroy()
        {
            if (source == null) return;
            source.OnValueChanged -= Refresh;
        }

        protected virtual void Refresh(float current, float max)
        {
            tmp.text = current.ToString();
        }

        public virtual void Initialize(ObservableValue source)
        {
            if (source == null) return;

            tmp.text = source.CurrentValue.ToString();

            source.OnValueChanged += Refresh;
        }
    }
}
