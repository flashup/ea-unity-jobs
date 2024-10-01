using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace UI.Controls
{
	public class ParameterControl : MonoBehaviour
	{
		public int defaultValue;
		public int min;
		public int max;

		public string label;
		public bool addPercentValueToLabel;

		[SerializeField] protected TextMeshProUGUI _labelText;
		[SerializeField] protected TMP_InputField _inputField;

		public UnityEvent<int> ValueChanged { get; } = new();

		protected int _value;
		public int Value
		{
			get => _value;
			set
			{
				_value = Mathf.Clamp(value, min, max);
				SetValueToUi();
				
				ValueChanged?.Invoke(_value);
			}
		}

		public void SetValueWithoutNotify(int value)
		{
			_value = value;
			SetValueToUi();
		}

		private void Awake()
		{
			if (defaultValue < min) defaultValue = min;
			else if (defaultValue > max) defaultValue = max;

			_value = defaultValue;
			
			if (label == null)
				label = _labelText.text;
		}

		public virtual void Start()
		{
			_inputField.text = $"{_value}";
			_inputField.characterLimit = max.ToString().Length;
			_inputField.onValueChanged.AddListener(OnInputFieldValueChanged);
			_inputField.onValidateInput = OnValidateInput;
			_inputField.onEndEdit.AddListener(OnInputDeselect);
		}

		protected virtual void SetValueToUi()
		{
			_inputField.SetTextWithoutNotify(_value.ToString());

			if (addPercentValueToLabel)
				_labelText.text = $"{label}: {_value}%";
		}

		private void OnInputDeselect(string rawValue)
		{
			if (string.IsNullOrEmpty(rawValue))
			{
				Value = _value;
				return;
			}

			int.TryParse(rawValue, out var result);

			var value = Mathf.Clamp(result, min, max);

			Value = value;
		}

		protected virtual void OnInputFieldValueChanged(string rawValue)
		{
			if (string.IsNullOrEmpty(rawValue)) return;

			int.TryParse(rawValue, out var result);

			if (result >= min && result <= max)
				Value = result;
		}

		private char OnValidateInput(string text, int charIndex, char addedChar)
		{
			if (char.IsDigit(addedChar))
			{
				return addedChar;
			}
	
			return '\0';
		}

		public virtual void RevertValue()
		{
			Value = defaultValue;
		}
	}
}