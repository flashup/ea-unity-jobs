using UnityEngine;

namespace UI.Controls
{
	public class ParkingEvolutionParameters : EvolutionParameters
	{
		[SerializeField] private ParameterControl _genLifetime;

		public int GenLifetime => _genLifetime.Value;

		public override void Start()
		{
			base.Start();

			_genLifetime.ValueChanged.AddListener(OnValueChanged);
		}

		public override void Load(string keyPrefix)
		{
			base.Load(keyPrefix);

			_genLifetime.Value = PlayerPrefs.GetInt(GetKeyName("GenLifetime"), _genLifetime.defaultValue);
		}

		public override void Save()
		{
			base.Save();

			PlayerPrefs.SetInt(GetKeyName("GenLifetime"), GenLifetime);
		}

		protected override void OnRevertBtnClicked()
		{
			base.OnRevertBtnClicked();

			_genLifetime.RevertValue();
		}
	}
}