using System;
using System.Collections.Generic;
using System.Linq;
using Function;
using Helpers;
using TMPro;
using UnityEngine;

namespace UI.Controls
{
	public class TestSequenceParameters : MonoBehaviour
	{
		[SerializeField] private ParameterControlToggle _genomeSizeSequenceControl;
		[SerializeField] protected TMP_InputField _genomeSizeSequenceInputField;

		[SerializeField] private ParameterControlToggle _populationSizeSequenceControl;
		[SerializeField] protected TMP_InputField _populationSizeSequenceInputField;

		[SerializeField] private ParameterControlToggle _copyResultsToBufferControl;
		[SerializeField] private ParameterControlSliderDropdown _paramTypeToCopyControl;

		private bool _testSequenceForGenomeSize;
		private readonly int[] _genomeSizeSequence = { 2, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50 };

		private bool _testSequenceForPopulationSize;
		private readonly int[] _populationSizeSequence = { 50, 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000 };

		public bool IsSequenceActive { get; private set; }

		public bool IsSequenceComplete => !IsSequenceActive || _currentSequenceIndex >= _currentSequenceValues.Length;

		private readonly List<string> _resultsToCopyToBuffer = new();

		private int _currentSequenceIndex;
		private int[] _currentSequenceValues;
		private bool _needToCopyResults;
		private EAParameter _paramToCopy;

		public TestSequenceType Type { get; private set; }
		
		
		public int CurrentTest => _currentSequenceIndex + 1;
		public int TotalTests => _currentSequenceValues.Length;

		private void Start()
		{
			_genomeSizeSequenceInputField.text = _genomeSizeSequence.Join(",");
			_populationSizeSequenceInputField.text = _populationSizeSequence.Join(",");

			_genomeSizeSequenceControl.ValueChanged.AddListener(OnGenomeSizeTestChanged);
			_populationSizeSequenceControl.ValueChanged.AddListener(OnPopulationSizeTestChanged);

			_copyResultsToBufferControl.ValueChanged.AddListener(OnCopyChanged);

			_paramTypeToCopyControl.Dropdown.options = Enum.GetValues(typeof(EAParameter))
				.Cast<EAParameter>()
				.Select(x => new TMP_Dropdown.OptionData(x.ToString()))
				.ToList();

			OnGenomeSizeTestChanged();
			OnPopulationSizeTestChanged();
			OnCopyChanged();
		}

		private void OnCopyChanged(bool _ = false)
		{
			_paramTypeToCopyControl.gameObject.SetActive(_copyResultsToBufferControl.BoolValue);
		}

		private void OnGenomeSizeTestChanged(bool _ = false)
		{
			var genomeSelected = _genomeSizeSequenceControl.BoolValue;
			_genomeSizeSequenceInputField.gameObject.SetActive(genomeSelected);

			if (genomeSelected && _populationSizeSequenceControl.BoolValue)
			{
				_populationSizeSequenceControl.BoolValue = false;
			}
		}

		private void OnPopulationSizeTestChanged(bool _ = false)
		{
			var populationSelected = _populationSizeSequenceControl.BoolValue;
			_populationSizeSequenceInputField.gameObject.SetActive(_populationSizeSequenceControl.BoolValue);

			if (populationSelected && _genomeSizeSequenceControl.BoolValue)
			{
				_genomeSizeSequenceControl.BoolValue = false;
			}
		}

		public void CommitChanges()
		{
			_currentSequenceIndex = 0;
			_resultsToCopyToBuffer.Clear();
			_needToCopyResults = _copyResultsToBufferControl.BoolValue;
			_paramToCopy = (EAParameter)_paramTypeToCopyControl.DropdownIndex;

			IsSequenceActive = _genomeSizeSequenceControl.BoolValue || _populationSizeSequenceControl.BoolValue;

			Type = _genomeSizeSequenceControl.BoolValue
				? TestSequenceType.GenomeSize
				: TestSequenceType.PopulationSize;

			_currentSequenceValues = Type == TestSequenceType.GenomeSize
				? ParseValues(_genomeSizeSequenceInputField.text)
				: ParseValues(_populationSizeSequenceInputField.text);
		}

		private static int[] ParseValues(string input)
		{
			return input.Split(",").Select(int.Parse).ToArray();
		}

		public void SetTestResults(double fitnessTime, double selectionTime, double crossoverTime, double mutationTime)
		{
			if (!_needToCopyResults) return;

			var result = _paramToCopy switch
			{
				EAParameter.Fitness => fitnessTime,
				EAParameter.Selection => selectionTime,
				EAParameter.Crossover => crossoverTime,
				EAParameter.Mutation => mutationTime,
				_ => throw new ArgumentOutOfRangeException()
			};

			_resultsToCopyToBuffer.Add($"{result:F0}");
		}

		public void IncreaseStep()
		{
			_currentSequenceIndex++;
		}

		public void Complete()
		{
			GUIUtility.systemCopyBuffer = _resultsToCopyToBuffer.Join("\n");
		}

		public int GetValueFor(TestSequenceType forType, int defaultValue)
		{
			return (IsSequenceActive && forType == Type) ? GetCurrentValue() : defaultValue;
		}

		public int GetCurrentValue()
		{
			return _currentSequenceValues[_currentSequenceIndex];
		}
	}

	public enum TestSequenceType
	{
		GenomeSize,
		PopulationSize,
	}
}