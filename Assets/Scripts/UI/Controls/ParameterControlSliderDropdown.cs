using TMPro;
using UnityEngine;

namespace UI.Controls
{
	public class ParameterControlSliderDropdown : ParameterControlSlider
	{
		[SerializeField] private TMP_Dropdown _dropdown;

		public override void Start()
		{
			base.Start();
			
			_dropdown.onValueChanged.AddListener(OnDdValueChanged);
		}

		private void OnDdValueChanged(int _)
		{
			ValueChanged?.Invoke(_value);
		}
		
		public int DropdownIndex
		{
			get => _dropdown.value;
			set => _dropdown.value = value;
		}

		public TMP_Dropdown Dropdown => _dropdown;

		public override void RevertValue()
		{
			base.RevertValue();

			_dropdown.value = 0;
		}
	}
}