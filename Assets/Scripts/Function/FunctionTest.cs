using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Evolution;
using Function.FitnessEvaluation;
using Function.FitnessEvaluation.Functions;
using Function.FitnessEvaluation.Jobs;
using TMPro;
using UI;
using UI.Controls;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
using Random = System.Random;

// using Random = RandomC;

namespace Function
{
	public class FunctionTest : MonoBehaviour
	{
		[Header("Sequences of tests")] 
		[SerializeField] private TestSequenceParameters _testSequenceParameters;

		[Header("Algorithm")] 
		[SerializeField] private ParameterControlSliderDropdown _fitnessFunctionControl;
		[SerializeField] private ParameterControlSlider _genomeSizeControl;

		[SerializeField] private ParameterControlToggle _jobsFitnessControl;
		[SerializeField] private ParameterControlToggle _jobsSelectionControl;
		[SerializeField] private ParameterControlToggle _jobsCrossoverControl;
		[SerializeField] private ParameterControlToggle _jobsMutationControl;

		[SerializeField] private ParameterControlSlider _threadsControl;
		[SerializeField] private ParameterControlSlider _parallelSortingControl;

		[Header("Chart")] 
		[SerializeField] private FitnessChart _chart;

		[Header("Params")]
		[SerializeField] private EvolutionParameters _evParams;

		[Header("Logs")] 
		[SerializeField] private LogsPanel _logs;

		[Header("Buttons")]
		[SerializeField] private UIButton _hintBtn;
		[SerializeField] private UIButton _startBtn;
		[SerializeField] private UIButton _stopBtn;

		[Header("Hint")] 
		[SerializeField] private GameObject _hintContainer;
		[SerializeField] private TextMeshProUGUI _hintTxt;

		[Header("ComputeShader")]
		[SerializeField] private ComputeShader _computeShaderRastrigin;
		[SerializeField] private ComputeShader _computeShaderLevy;

		//
		
		private bool _useJobsFitness;
		private bool _useJobsSelection;
		private bool _useJobsCrossover;
		private bool _useJobsMutation;

		private bool _useParallelSorting;
		private bool _useThreads;
		
		//

		private bool _isTestActive;

		private SelectionMethod _selectionMethod;
		private int _parentsAmount;
		private int _elitesAmount;

		private int _numGenerations;
		private int _populationSize;
		private int _dimensions;

		private int _crossoverPercent;
		private int _mutationPercent;

		private FitnessFunction _fitnessFunction;
		private float[] _functionBounds;
		
		private (int defaultGenomeSize, int defaultPopulationSize) _sequenceAffectedValues;

		private IEvaluateFitnessCommand _evaluateFitnessCommand;

		private readonly Stopwatch _fitnessTimer = new Stopwatch();
		private readonly List<long> _fitnessTimes = new List<long>();

		private readonly Stopwatch _selectionTimer = new Stopwatch();
		private readonly List<long> _selectionTimes = new List<long>();

		private readonly Stopwatch _crossoverTimer = new Stopwatch();
		private readonly List<long> _crossoverTimes = new List<long>();

		private readonly Stopwatch _mutationTimer = new Stopwatch();
		private readonly List<long> _mutationTimes = new List<long>();

		private int _msPerFrame;

		private readonly Dictionary<FitnessFunction, float[]> _functionBoundsDictionary = new()
		{
			{ FitnessFunction.Rastrigin, new[] { -5.12f, 5.12f } },
			{ FitnessFunction.Rosenbrock, new[] { -5f, 10f } },
			{ FitnessFunction.Ackley, new[] { -32.768f, 32.768f } },
			{ FitnessFunction.Levy, new[] { -10f, 10f } },
			{ FitnessFunction.Schwefel, new[] { -500f, 500f } },
		};

		private void Start()
		{
			QualitySettings.vSyncCount = 0;
			Application.targetFrameRate = AppSettings.FPS;
			Physics.gravity = Vector3.zero;
			_msPerFrame = (int)(1f / AppSettings.FPS * 1000);

			_hintBtn.OnClick(OnClickHint);
			_startBtn.OnClick(OnClickStart);
			_stopBtn.OnClick(OnClickStop);

			_evParams.Load("Function");
			_evParams.ValueChanged.AddListener(OnValueChanged);

			_fitnessFunctionControl.Dropdown.options = Enum.GetValues(typeof(FitnessFunction))
				.Cast<FitnessFunction>()
				.Select(x => new TMP_Dropdown.OptionData(x.ToString()))
				.ToList();
			_fitnessFunctionControl.DropdownIndex = PlayerPrefs.GetInt(PlayerPrefIds.FunctionType, 0);
			_fitnessFunctionControl.ValueChanged.AddListener((_) => OnValueChanged());
			
			_jobsFitnessControl.Value = PlayerPrefs.GetInt(PlayerPrefIds.UseJobsFitness, 0);
			_jobsSelectionControl.Value = PlayerPrefs.GetInt(PlayerPrefIds.UseJobsSelection, 0);
			_jobsCrossoverControl.Value = PlayerPrefs.GetInt(PlayerPrefIds.UseJobsCrossover, 0);
			_jobsMutationControl.Value = PlayerPrefs.GetInt(PlayerPrefIds.UseJobsMutation, 0);

			_genomeSizeControl.Value = PlayerPrefs.GetInt(PlayerPrefIds.GenomeSize, _genomeSizeControl.defaultValue);
			_genomeSizeControl.ValueChanged.AddListener((_) => OnValueChanged());
			
			// _hintContainer.SetActive(PlayerPrefs.GetInt("FunctionShowHint", 1) == 1);
			_hintContainer.SetActive(false);

			OnValueChanged();

			UpdateUiState();

			_logs.Log($"<b>ProcessorCount: {Environment.ProcessorCount}</b>");
			_logs.Log($"MaxJobThreadCount: {JobsUtility.MaxJobThreadCount}");
			_logs.Log($"JobWorkers: {JobsUtility.JobWorkerCount}/{JobsUtility.JobWorkerMaximumCount}");
			// _logs.Log($"GraphicsDeviceType: {SystemInfo.graphicsDeviceType}");
			// _logs.Log($"GraphicsDeviceName: {SystemInfo.graphicsDeviceName}");
			// _logs.Log($"GraphicsMemorySize: {SystemInfo.graphicsMemorySize} MB");

			// if (SystemInfo.supportsComputeShaders)
			// 	_logs.Log("The graphics card supports Compute Shaders.");
			// else
			// 	_logs.Log("The graphics card doesn't support Compute Shaders.");
			//
			// if (SystemInfo.graphicsShaderLevel >= 50)
			// {
			// 	_logs.Log("The graphics card supports Shader Model 5.0 and higher.");
			// }
			// else
			// {
			// 	_logs.Log("The graphics card doesn't support Shader Model 5.0.");
			// }
			//
			// _logs.Log($"Max threads per group: {SystemInfo.maxComputeWorkGroupSize}");
			//
			// var maxX = SystemInfo.maxComputeWorkGroupSizeX;
			// var maxY = SystemInfo.maxComputeWorkGroupSizeY;
			// var maxZ = SystemInfo.maxComputeWorkGroupSizeZ;
			// _logs.Log($"Max compute work group size: X = {maxX}, Y = {maxY}, Z = {maxZ}");
		}

		private void OnClickStop()
		{
			if (!_isTestActive) return;

			StopAllCoroutines();

			RunsCompleted();
		}

		private void RunsCompleted()
		{
			_isTestActive = false;

			// return values at start test (in case of test sequence)
			var (genomeSize, populationSize) = _sequenceAffectedValues;
			_genomeSizeControl.Value = genomeSize;
			_evParams.PopulationSize = populationSize;

			UpdateUiState();
		}

		private void OnValueChanged()
		{
			var hint =
				"<b>{FunctionType} Test:</b> Conduct {Runs} tests, each generating a population of {PopulationSize} and evolving it for {NumGenerations} generations." +
				"The best fitness from each run will be recorded, and the average fitness will be calculated after all runs.\n" +
				"<b>Evolution Process:</b>\n" +
				"Each individual's genome contains {GenomeSizeß} coordinates\n" +
				"Elitism: Preserve {Elitism}% of top individuals.\n" +
				"Selection: Top {Selection}% become parents by {SelectionType} algo.\n" +
				"Crossover: {Crossover}% chance for two parents to produce offspring.\n" +
				"Mutation: {Mutation}% chance to randomly alter X or Y in offspring.";

			var parameters = new Dictionary<string, string>
			{
				{ "FunctionType", $"{((FitnessFunction)_fitnessFunctionControl.DropdownIndex).ToString()}" },
				{ "Runs", $"{_evParams.NumRuns}" },
				{ "GenomeSize", $"{_genomeSizeControl.Value}" },
				{ "PopulationSize", $"{_evParams.PopulationSize}" },
				{ "NumGenerations", $"{_evParams.NumGenerations}" },
				{ "Elitism", $"{_evParams.ElitismPercent}" },
				{ "Selection", $"{_evParams.SelectionPercent}" },
				{ "SelectionType", $"{_evParams.SelectionMethod.ToString()}" },
				{ "Crossover", $"{_evParams.CrossoverPercent}" },
				{ "Mutation", $"{_evParams.MutationPercent}" },
			};

			_hintTxt.text = ReplaceNamedParameters(hint, parameters);
		}

		private string ReplaceNamedParameters(string template, Dictionary<string, string> parameters)
		{
			return Regex.Replace(template, @"\{(\w+)\}", match =>
			{
				var key = match.Groups[1].Value;
				return parameters.ContainsKey(key) ? parameters[key] : match.Value;
			});
		}

		private void OnClickHint()
		{
			_hintContainer.SetActive(!_hintContainer.activeSelf);
			PlayerPrefs.SetInt("FunctionShowHint", _hintContainer.activeSelf ? 1 : 0);
			PlayerPrefs.Save();
		}

		private void OnClickStart()
		{
			if (_isTestActive) return;

			_isTestActive = true;

			_sequenceAffectedValues = (_genomeSizeControl.Value, _evParams.PopulationSize);
			_testSequenceParameters.CommitChanges();

			InitParameters();

			StartCoroutine(StartRuns());
		}

		private void InitParameters()
		{
			_fitnessFunction = (FitnessFunction)_fitnessFunctionControl.DropdownIndex;

			_dimensions = _genomeSizeControl.Value;
			_populationSize = _evParams.PopulationSize;
			_crossoverPercent = _evParams.CrossoverPercent;
			_mutationPercent = _evParams.MutationPercent;
			_numGenerations = _evParams.NumGenerations;

			_selectionMethod = _evParams.SelectionMethod;
			_parentsAmount = (int)Math.Ceiling(_populationSize * (_evParams.SelectionPercent / 100f));
			_elitesAmount = (int)Math.Ceiling(_populationSize * (_evParams.ElitismPercent / 100f));

			_functionBounds = _functionBoundsDictionary.ContainsKey(_fitnessFunction) 
				? _functionBoundsDictionary[_fitnessFunction]
				: new[] { -10f, 10f };

			_useThreads = _threadsControl.Value == 1;
			_useParallelSorting = _parallelSortingControl.Value == 1;

			_useJobsFitness = _jobsFitnessControl.BoolValue;
			_useJobsSelection = _jobsSelectionControl.BoolValue;
			_useJobsCrossover = _jobsCrossoverControl.BoolValue;
			_useJobsMutation = _jobsMutationControl.BoolValue;

			PlayerPrefs.SetInt(PlayerPrefIds.FunctionType, _fitnessFunctionControl.DropdownIndex);
			PlayerPrefs.SetInt(PlayerPrefIds.GenomeSize, _genomeSizeControl.Value);
			PlayerPrefs.SetInt(PlayerPrefIds.UseJobsFitness, _jobsFitnessControl.Value);
			PlayerPrefs.SetInt(PlayerPrefIds.UseJobsSelection, _jobsSelectionControl.Value);
			PlayerPrefs.SetInt(PlayerPrefIds.UseJobsCrossover, _jobsCrossoverControl.Value);
			PlayerPrefs.SetInt(PlayerPrefIds.UseJobsMutation, _jobsMutationControl.Value);
			PlayerPrefs.Save();

			_evParams.Save();
		}

		private IEnumerator StartRuns()
		{
			if (_testSequenceParameters.IsSequenceActive)
			{
				_dimensions = _testSequenceParameters.GetValueFor(TestSequenceType.GenomeSize, _dimensions);
				_populationSize = _testSequenceParameters.GetValueFor(TestSequenceType.PopulationSize, _populationSize);

				_genomeSizeControl.Value = _dimensions;
				_evParams.PopulationSize = _populationSize;
			}

			UpdateUiState();

			_chart.Clear();
			_fitnessTimes.Clear();
			_selectionTimes.Clear();
			_crossoverTimes.Clear();
			_mutationTimes.Clear();

			var seed = _evParams.Seed != 0 ? _evParams.Seed : new Random().Next(10000);
			var random = new Random(seed);

			var testLabel = _testSequenceParameters.IsSequenceActive 
				? $"#{_testSequenceParameters.CurrentTest}/{_testSequenceParameters.TotalTests}" 
				: "#1";
			_logs.Log($"{DateTime.Now:HH:mm:ss} Test {testLabel} of {_fitnessFunction.ToString()} (seed: {seed})");

			var bestFitnessValues = new List<float>();
			float bestFitness = 0;

			var numRuns = _evParams.NumRuns;

			var timesPerRun = new double[numRuns];
			var totalTimeTimer = new Stopwatch();

			for (int run = 0; run < numRuns; run++)
			{
				_startBtn.UpdateText($"{run + 1}/{numRuns}");

				yield return null;
				float bestFit = 0;

				totalTimeTimer.Restart();

				yield return Run(random, value => bestFit = value);

				var time = totalTimeTimer.ElapsedMilliseconds;
				timesPerRun[run] = time;

				bestFitnessValues.Add(bestFit);

				if (bestFit > bestFitness)
					bestFitness = bestFit;

				_chart.AddBestValue(bestFit);
				_chart.AddAvgValue(bestFitnessValues.Average());
				_chart.AddTime(time);

				yield return null;
			}

			totalTimeTimer.Stop();

			var avgFitness = bestFitnessValues.Average();
			var minFitness = bestFitnessValues.Min();
			var maxFitness = bestFitnessValues.Max();

			var avgTime = timesPerRun.Average();

			if (avgTime > 1000)
				_logs.Log($"Avg run time <u>{(avgTime / 1000f):F3} sec</u>");
			else
				_logs.Log($"Avg run time <u>{avgTime:F0} ms</u>");

			_logs.Log($"AvgFitness: <u>{avgFitness:F5}</u>, Min: {minFitness:F5}, Max: {maxFitness}");

			var avgFitTime = _fitnessTimes.Average();
			var avgSelTime = _selectionTimes.Average();
			var avgCroTime = _crossoverTimes.Average();
			var avgMutTime = _mutationTimes.Average();

			var total = avgFitTime + avgSelTime + avgCroTime + avgMutTime;

			_logs.Log($"Fit: {(avgFitTime / total * 100):F1}% ({avgFitTime:F0})");
			_logs.Log($"Sel: {(avgSelTime / total * 100):F1}% ({avgSelTime:F0})");
			_logs.Log($"Cro: {(avgCroTime / total * 100):F1}% ({avgCroTime:F0})");
			_logs.Log($"Mut: {(avgMutTime / total * 100):F1}% ({avgMutTime:F0})");
			_logs.Log("---");

			if (_testSequenceParameters.IsSequenceActive)
			{
				_testSequenceParameters.SetTestResults(avgFitTime, avgSelTime, avgCroTime, avgMutTime);
				_testSequenceParameters.IncreaseStep();

				if (!_testSequenceParameters.IsSequenceComplete)
				{
					yield return new WaitForSeconds(0.5f);
					StartCoroutine(StartRuns());
				}
				else
				{
					_testSequenceParameters.Complete();
					RunsCompleted();
				}
			}
			else
			{
				RunsCompleted();
			}
		}

		private IEnumerator Run(Random random, Action<float> callback)
		{
			var numChildren = _populationSize - _elitesAmount;

			var evolution = new FunctionEvolutionaryManager(random.Next(), _dimensions, _functionBounds[0], _functionBounds[1]);
			evolution.InitSelection(_selectionMethod, _populationSize, _elitesAmount, _parentsAmount, _useJobsSelection);
			evolution.InitCrossover(_crossoverPercent, _parentsAmount, numChildren, _useJobsCrossover);
			evolution.InitMutation(_mutationPercent, _populationSize - _elitesAmount, _useJobsMutation);

			_evaluateFitnessCommand = _useJobsFitness
				? new EvaluateFitnessJobCommand(_fitnessFunction, _populationSize, _dimensions)
				: new EvaluateFitnessFuncCommand(_fitnessFunction, _populationSize);

			// _evaluateFitnessCommand = _selectedFitnessComputation switch
			// {
			// 	FitnessComputation.Function =>
			// 		new EvaluateFitnessFuncCommand(_fitnessFunction, _populationSize),
			// 	FitnessComputation.Job => 
			// 		new EvaluateFitnessJobCommand(_fitnessFunction, _populationSize, _dimensions),
			// 	FitnessComputation.Shader => 
			// 		new EvaluateFitnessShaderCommand(_fitnessFunction, _populationSize,
			// 		_dimensions, GetComputeShader(_fitnessFunction)),
			// 	_ => throw new ArgumentOutOfRangeException()
			// };

			var generation = evolution.CreateGeneration(_populationSize);
			for (int i = 0; i < _populationSize; i++)
				generation[i] = evolution.CreateGenome();

			var evaluatedGeneration = new EvaluatedGenome<Point>[_populationSize];
			for (int i = 0; i < _populationSize; i++)
				evaluatedGeneration[i] = new EvaluatedGenome<Point>();

			var maxFitness = float.MinValue;

			var timer = Stopwatch.StartNew();

			for (int i = 0; i < _numGenerations; i++)
			{
				var generation1 = generation;

				// TODO remove threads
				if (_useThreads)
				{
					var t = Task.Run(() =>
					{
						EvaluateGeneration(generation1, evaluatedGeneration);
					});

					while (!t.IsCompleted)
						yield return null;
				}
				else
				{
					EvaluateGeneration(generation1, evaluatedGeneration);
				}

				for (int j = 0; j < _populationSize; j++)
				{
					var f = evaluatedGeneration[j].fitness;
					if (f > maxFitness) maxFitness = f;
				}

				var oldGeneration = generation;
				if (_useThreads)
				{
					var t = Task.Run(() => { generation = CreateNextGeneration(evaluatedGeneration, evolution); });

					while (!t.IsCompleted)
						yield return null;
				}
				else
				{
					generation = CreateNextGeneration(evaluatedGeneration, evolution);
				}

				evolution.CacheGeneration(oldGeneration);

				// to not block main thread
				// execute as many runs as we have time per 1 frame
				// then go to next frame
				if (timer.ElapsedMilliseconds >= _msPerFrame)
				{
					yield return null;
					timer.Restart();
				}
			}

			timer.Stop();

			_evaluateFitnessCommand?.Dispose();
			evolution.Dispose();

			callback?.Invoke(maxFitness);
		}

		private ComputeShader GetComputeShader(FitnessFunction fitnessFunction)
		{
			return fitnessFunction switch
			{
				FitnessFunction.Rastrigin => _computeShaderRastrigin,
				FitnessFunction.Rosenbrock or FitnessFunction.Ackley or FitnessFunction.Schwefel or FitnessFunction.Levy
					=> _computeShaderLevy,
				_ => throw new ArgumentOutOfRangeException(nameof(fitnessFunction), fitnessFunction, null)
			};
		}

		private void EvaluateGeneration(Point[] generation, EvaluatedGenome<Point>[] evaluatedGeneration)
		{
			_fitnessTimer.Restart();
			var fitnessValues = _evaluateFitnessCommand.Execute(generation);
			_fitnessTimer.Stop();
			_fitnessTimes.Add(_fitnessTimer.ElapsedTicks);

			for (int i = 0; i < generation.Length; i++)
			{
				var evaluatedGenome = evaluatedGeneration[i];
				evaluatedGenome.genome = generation[i];
				evaluatedGenome.fitness = fitnessValues[i];
			}
		}

		private Point[] CreateNextGeneration(
			EvaluatedGenome<Point>[] evaluatedGeneration,
			FunctionEvolutionaryManager evolution)
		{
			var newGeneration = evolution.CreateGeneration(_populationSize);

			_selectionTimer.Restart();
			var (elites, parents) = evolution.PerformSelection(
				_selectionMethod,
				evaluatedGeneration
			);
			_selectionTimer.Stop();
			_selectionTimes.Add(_selectionTimer.ElapsedTicks);

			var elitesCount = elites?.Length ?? 0;
			if (elites != null)
			{
				for (int i = 0; i < elitesCount; i++)
				{
					newGeneration[i] = elites[i];
				}
			}

			var childrenCount = _populationSize - elitesCount;

			_crossoverTimer.Restart();
			var children = evolution.PerformCrossover(childrenCount, parents);
			_crossoverTimer.Stop();
			_crossoverTimes.Add(_crossoverTimer.ElapsedTicks);

			for (int i = 0; i < childrenCount; i++)
			{
				newGeneration[elitesCount + i] = children[i];
			}

			_mutationTimer.Restart();
			evolution.PerformMutation(children);
			_mutationTimer.Stop();
			_mutationTimes.Add(_mutationTimer.ElapsedTicks);

			return newGeneration;
		}

		private void UpdateUiState()
		{
			_startBtn.SetEnabled(!_isTestActive);

			if (!_isTestActive) _startBtn.UpdateText("Start");

			_stopBtn.gameObject.SetActive(_isTestActive);
		}

		private void OnDestroy()
		{
			_evaluateFitnessCommand?.Dispose();
		}
	}

	public enum FitnessFunction
	{
		Rastrigin,
		Rosenbrock,
		Ackley,
		Levy,
		Schwefel,
		Comp1,
		CompLogN,
		CompN,
		CompNLogN,
		CompN2,
	}

	public enum FitnessComputation
	{
		Function,
		Job,
		Shader,
	}

	public enum EAParameter
	{
		Fitness,
		Selection,
		Crossover,
		Mutation,
	}

	internal static class PlayerPrefIds
	{
		public const string FunctionType = "FunctionFuncType";
		public const string UseJobsFitness = "FunctionUseJobsFitness";
		public const string UseJobsSelection = "FunctionUseJobsSelection";
		public const string UseJobsCrossover = "FunctionUseJobsCrossover";
		public const string UseJobsMutation = "FunctionUseJobsMutation";
		public const string GenomeSize = "FunctionGenomeSize";
	}
}