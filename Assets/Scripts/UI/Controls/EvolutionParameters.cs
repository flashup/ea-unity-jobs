using System;
using Evolution;
using UnityEngine;
using UnityEngine.Events;

namespace UI.Controls
{
	public class EvolutionParameters : MonoBehaviour
	{
		[SerializeField] private UIButton _revertBtn;

		[SerializeField] private ParameterControl _seed;
		[SerializeField] private ParameterControl _numRuns;
		
		[SerializeField] private ParameterControl _populationSize;
		[SerializeField] private ParameterControl _numGenerations;
		
		[SerializeField] private ParameterControlSliderDropdown _selectionParams;
		[SerializeField] private ParameterControl _elitismPercent;
		[SerializeField] private ParameterControl _crossoverPercent;
		[SerializeField] private ParameterControl _mutationPercent;
		
		private string _keyPrefix;

		public UnityEvent ValueChanged { get; } = new();

		public int Seed => _seed.Value;
		public int NumRuns => _numRuns.Value; 
		public int PopulationSize
		{
			get => _populationSize.Value;
			set => _populationSize.Value = value;
		}

		public int NumGenerations => _numGenerations.Value;
		public int SelectionPercent => _selectionParams.Value;
		public int ElitismPercent => _elitismPercent.Value;
		public SelectionMethod SelectionMethod => (SelectionMethod)_selectionParams.DropdownIndex;
		public int CrossoverPercent => _crossoverPercent.Value;
		public int MutationPercent => _mutationPercent.Value;
		
		public virtual void Start()
		{
			_revertBtn.OnClick(OnRevertBtnClicked);
			
			_seed.ValueChanged.AddListener(OnValueChanged);
			_numRuns.ValueChanged.AddListener(OnValueChanged);
			_populationSize.ValueChanged.AddListener(OnValueChanged);
			_numGenerations.ValueChanged.AddListener(OnValueChanged);
			_selectionParams.ValueChanged.AddListener(OnValueChanged);
			_elitismPercent.ValueChanged.AddListener(OnValueChanged);
			_crossoverPercent.ValueChanged.AddListener(OnValueChanged);
			_mutationPercent.ValueChanged.AddListener(OnValueChanged);
		}

		protected void OnValueChanged(int _)
		{
			ValueChanged?.Invoke();
		}

		public virtual void Load(string keyPrefix)
		{
			_keyPrefix = keyPrefix;
	
			_seed.Value = PlayerPrefs.GetInt(GetKeyName("Seed"), _seed.defaultValue);
			_numRuns.Value = PlayerPrefs.GetInt(GetKeyName("NumRuns"), _numRuns.defaultValue);
			_populationSize.Value = PlayerPrefs.GetInt(GetKeyName("PopulationSize"), _populationSize.defaultValue);
			_numGenerations.Value = PlayerPrefs.GetInt(GetKeyName("NumGenerations"), _numGenerations.defaultValue);
			_selectionParams.Value = PlayerPrefs.GetInt(GetKeyName("SelectionPercent"), _selectionParams.defaultValue);
			_selectionParams.DropdownIndex = PlayerPrefs.GetInt(GetKeyName("SelectionType"), _selectionParams.DropdownIndex);
			_elitismPercent.Value = PlayerPrefs.GetInt(GetKeyName("ElitismPercent"), _elitismPercent.defaultValue);
			_crossoverPercent.Value = PlayerPrefs.GetInt(GetKeyName("CrossoverPercent"), _crossoverPercent.defaultValue);
			_mutationPercent.Value = PlayerPrefs.GetInt(GetKeyName("MutationPercent"), _mutationPercent.defaultValue);
		}

		public virtual void Save()
		{
			PlayerPrefs.SetInt(GetKeyName("Seed"), Seed);
			PlayerPrefs.SetInt(GetKeyName("NumRuns"), NumRuns);
			PlayerPrefs.SetInt(GetKeyName("PopulationSize"), PopulationSize);
			PlayerPrefs.SetInt(GetKeyName("NumGenerations"), NumGenerations);
			PlayerPrefs.SetInt(GetKeyName("SelectionPercent"), SelectionPercent);
			PlayerPrefs.SetInt(GetKeyName("SelectionType"), _selectionParams.DropdownIndex);
			PlayerPrefs.SetInt(GetKeyName("ElitismPercent"), ElitismPercent);
			PlayerPrefs.SetInt(GetKeyName("CrossoverPercent"), CrossoverPercent);
			PlayerPrefs.SetInt(GetKeyName("MutationPercent"), MutationPercent);
		}

		protected string GetKeyName(string key)
		{
			return $"{_keyPrefix}{key}";
		}

		protected virtual void OnRevertBtnClicked()
		{
			_seed.RevertValue();
			_numRuns.RevertValue();
			_populationSize.RevertValue();
			_numGenerations.RevertValue();
			_selectionParams.RevertValue();
			_elitismPercent.RevertValue();
			_crossoverPercent.RevertValue();
			_mutationPercent.RevertValue();
		}
	}
}