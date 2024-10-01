using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Controls
{
	public class ParameterControlToggle : MonoBehaviour, IPointerClickHandler
	{
		public bool defaultValue;
		
		[SerializeField] private Slider _slider;

		public UnityEvent<bool> ValueChanged { get; } = new();

		public void Awake()
		{
			_value = defaultValue;

			_slider.minValue = 0;
			_slider.maxValue = 1;
			_slider.SetValueWithoutNotify(_value ? 1 : 0);

			SetValueToUi();
		}
		
		private bool _value;

		public bool BoolValue
		{
			get => _value;
			set
			{
				_value = value;
				SetValueToUi();
				
				ValueChanged?.Invoke(_value);
			}
		}
		
		public int Value
		{
			get => _value ? 1 : 0;
			set
			{
				_value = value == 1;
				SetValueToUi();
				
				ValueChanged?.Invoke(_value);
			}
		}
		
		private void SetValueToUi()
		{
			_slider.SetValueWithoutNotify(_value ? 1 : 0);
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			BoolValue = !BoolValue;
		}
	}
}