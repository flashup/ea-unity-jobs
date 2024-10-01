using UnityEngine;
using UnityEngine.UI;

namespace UI.Controls
{
	public class ParameterControlSlider : ParameterControl
	{
		[Header("Slider")]
		public int step;
		
		public string[] labels;
		
		[SerializeField] private Slider _slider;

		public override void Start()
		{
			base.Start();

			if (labels.Length > 0)
			{
				min = 0;
				max = labels.Length - 1;
			}

			_slider.minValue = min;
			_slider.maxValue = max;
			_slider.SetValueWithoutNotify(_value);

			SetValueToUi();

			_slider.onValueChanged.AddListener(OnSliderValueChanged);
		}

		public void SetLabels(string[] labelsValues)
		{
			labels = labelsValues;

			min = 0;
			max = labels.Length - 1;

			_slider.minValue = min;
			_slider.maxValue = max;
			_slider.SetValueWithoutNotify(_value);

			SetValueToUi();
		}

		protected override void SetValueToUi()
		{
			if (labels.Length == 0)
			{
				base.SetValueToUi();
			}
			else
			{
				_inputField.SetTextWithoutNotify(labels[_value]);
			}

			_slider.SetValueWithoutNotify(_value);
		}

		private void OnSliderValueChanged(float rawValue)
		{
			var steppedValue = Mathf.Clamp(Mathf.RoundToInt(rawValue / step) * step, min, max);

			if (steppedValue != Value)
				Value = steppedValue;
			else
				_slider.SetValueWithoutNotify(steppedValue);
		}

		protected override void OnInputFieldValueChanged(string rawValue)
		{
			base.OnInputFieldValueChanged(rawValue);

			_slider.SetValueWithoutNotify(Value);
		}
	}
}