using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Evolution;
using Helpers;
using TSP.FitnessEvaluation;
using TSP.FitnessEvaluation.Functions;
using TSP.FitnessEvaluation.Jobs;
using UI;
using UI.Controls;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Random = System.Random;

namespace TSP
{
	public class TspTest : MonoBehaviour
	{
		[SerializeField] private ComputeShader _computeShader;

		private bool _useThreads;
		private bool _useJobsFitness;

		[SerializeField] private bool _useJobsCrossover;
		[SerializeField] private bool _useJobsMutation;

		private ComputeBuffer _bufferParent1;
		private ComputeBuffer _bufferParent2;
		private ComputeBuffer _bufferChild;

		[Header("Map")]
		[SerializeField] private RectTransform _mapCitiesContainer;
		[SerializeField] private RectTransform _mapPathsContainer;
		[SerializeField] private CityView _cityPrefab;
		[SerializeField] private Image _pathPrefab;
		[SerializeField] private ParameterControl _mapSeed;
		[SerializeField] private ParameterControl _mapSize;
		[SerializeField] private UIButton _generateBtn;
		[SerializeField] private UIButton _drawPathBtn;
		[SerializeField] private LogsPanel _mapLogs;

		[Header("Chart")]
		[SerializeField] private FitnessChart _chart;

		[Header("Params")]
		[SerializeField] private EvolutionParameters _evParams;
		[SerializeField] private ParameterControlToggle _jobsFitnessControl;

		[Header("UI")]
		[SerializeField] private LogsPanel _logs;
		[SerializeField] private UIButton _startBtn;
		[SerializeField] private UIButton _stopBtn;

		// state

		private readonly List<City> _cities = new();

		private Dictionary<int, int> _distances;

		private CityView _selectedCity;

		private EvaluatedGenome<Path> _bestPath;

		private Coroutine _drawPathCoroutine;

		private bool _isTestActive;

		private SelectionMethod _selectionMethod;
		private int _parentsAmount;
		private int _elitesAmount;

		private int _numGenerations;
		private int _populationSize;
		private int _genomeSize;

		private int _crossoverPercent;
		private int _mutationPercent;

		private int _msPerFrame;
		
		private ITspEvaluateFitnessCommand _evaluateFitnessCommand;

		private readonly Stopwatch _fitnessTimer = new Stopwatch();
		private readonly List<long> _fitnessTimes = new List<long>();

		private readonly Stopwatch _selectionTimer = new Stopwatch();
		private readonly List<long> _selectionTimes = new List<long>();

		private readonly Stopwatch _crossoverTimer = new Stopwatch();
		private readonly List<long> _crossoverTimes = new List<long>();

		private readonly Stopwatch _mutationTimer = new Stopwatch();
		private readonly List<long> _mutationTimes = new List<long>();

		private void Start()
		{
			QualitySettings.vSyncCount = 0;
			Application.targetFrameRate = AppSettings.FPS;
			Physics.gravity = Vector3.zero;
			_msPerFrame = (int)(1f / AppSettings.FPS * 1000);

			_startBtn.OnClick(OnClickStart);
			_stopBtn.OnClick(OnClickStop);
			_generateBtn.OnClick(OnClickGenerateMap);
			_drawPathBtn.OnClick(OnDrawPath);

			_evParams.Load("Tsp");

			_mapSeed.Value = PlayerPrefs.GetInt("TspMapSeed", _mapSeed.defaultValue);
			_mapSize.Value = PlayerPrefs.GetInt("TspNumCities", _mapSize.defaultValue);

			UpdateUiState();

			_logs.Log($"<b>ProcessorCount: {Environment.ProcessorCount}</b>");
			_logs.Log($"MaxJobThreadCount: {JobsUtility.MaxJobThreadCount}");
			_logs.Log($"JobWorkers: {JobsUtility.JobWorkerCount}/{JobsUtility.JobWorkerMaximumCount}");
		}

		private void OnClickStop()
		{
			if (!_isTestActive) return;

			StopAllCoroutines();

			_isTestActive = false;

			UpdateUiState();
		}

		private void OnClickStart()
		{
			if (_isTestActive) return;
			ClearPath();

			StartCoroutine(StartRuns());
		}

		private IEnumerator StartRuns()
		{
			_isTestActive = true;

			_bestPath = null;
			
			_numGenerations = _evParams.NumGenerations;
			_populationSize = _evParams.PopulationSize;
			_selectionMethod = _evParams.SelectionMethod;

			_crossoverPercent = _evParams.CrossoverPercent;
			_mutationPercent = _evParams.MutationPercent;

			_parentsAmount = (int)Math.Ceiling(_populationSize * (_evParams.SelectionPercent / 100f));
			_elitesAmount = (int)Math.Ceiling(_populationSize * (_evParams.ElitismPercent / 100f));

			_useJobsFitness = _jobsFitnessControl.BoolValue;

			UpdateUiState();

			var seed = _evParams.Seed != 0 ? _evParams.Seed : new Random().Next(10000);
			var random = new Random(seed);

			_logs.Log($"{DateTime.Now:HH:mm:ss} Start with seed: {seed}");
			
			_chart.Clear();
			_evParams.Save();
			
			_fitnessTimes.Clear();
			_selectionTimes.Clear();
			_crossoverTimes.Clear();
			_mutationTimes.Clear();

			var bufferCount = _genomeSize * _populationSize;

			if (_bufferParent1 == null || !_bufferParent1.IsValid() || _bufferParent1.count != bufferCount)
			{
				_bufferParent1?.Dispose();
				_bufferParent1 = new ComputeBuffer(bufferCount, sizeof(int));
			}
			
			if (_bufferParent2 == null || !_bufferParent2.IsValid() || _bufferParent2.count != bufferCount)
			{
				_bufferParent2?.Dispose();
				_bufferParent2 = new ComputeBuffer(bufferCount, sizeof(int));
			}
			if (_bufferChild == null || !_bufferChild.IsValid() || _bufferChild.count != bufferCount)
			{
				_bufferChild?.Dispose();
				_bufferChild = new ComputeBuffer(bufferCount, sizeof(int));
			}

			var bestPaths = new List<EvaluatedGenome<Path>>();

			var numRuns = _evParams.NumRuns;

			var timesPerRun = new double[numRuns];
			var totalTimeTimer = Stopwatch.StartNew();

			for (int run = 0; run < numRuns; run++)
			{
				_startBtn.UpdateText($"{run + 1}/{numRuns}");

				yield return null;

				EvaluatedGenome<Path> genome = null;

				totalTimeTimer.Restart();

				yield return Run(random, g => { genome = g; });

				var time = totalTimeTimer.ElapsedMilliseconds;
				timesPerRun[run] = time;

				bestPaths.Add(genome);

				_chart.AddBestValue(genome.fitness);
				_chart.AddAvgValue(bestPaths.Average(v => v.fitness));
				_chart.AddTime(time);

				yield return null;
			}

			totalTimeTimer.Stop();

			var avgFitness = bestPaths.Average(v => v.fitness);
			var minFitness = bestPaths.Min(v => v.fitness);
			var maxFitness = bestPaths.Max(v => v.fitness);

			var bestPath = bestPaths.OrderByDescending(v => v.fitness).First();

			_bestPath = bestPath;

			DrawPath(_bestPath.genome.cities);

			var avgTime = timesPerRun.Average();

			if (avgTime > 1000)
				_logs.Log($"Avg run time <u>{(avgTime / 1000f):F3} sec</u>");
			else
				_logs.Log($"Avg run time <u>{avgTime} ms</u>");

			_logs.Log($"AvgFitness: {avgFitness:F5}, Min: {minFitness:F5}, Max: {maxFitness:F5}");
			_logs.Log(
				$"Fit: {bestPath.fitness}, Path: {bestPath.genome.cities.Join(",")}");
			
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
	
			_isTestActive = false;

			UpdateUiState();
		}

		private void UpdateUiState()
		{
			_startBtn.SetEnabled(!_isTestActive && _cities.Count > 0);

			if (!_isTestActive) _startBtn.UpdateText("2. Start");

			_stopBtn.gameObject.SetActive(_isTestActive);

			_drawPathBtn.SetEnabled(!_isTestActive && _bestPath != null);
			_generateBtn.SetEnabled(!_isTestActive);
		}

		private IEnumerator Run(Random random, Action<EvaluatedGenome<Path>> callback)
		{
			EvaluatedGenome<Path> bestGenome = null;

			var evolution = new TspEvolutionaryManager(random.Next(), _cities);
			evolution.InitSelection(_selectionMethod, _populationSize, _elitesAmount, _parentsAmount);
			evolution.InitCrossover(_crossoverPercent, _parentsAmount, _populationSize - _elitesAmount, _useJobsCrossover);
			evolution.InitMutation(_mutationPercent, _populationSize - _elitesAmount, _useJobsMutation);

			_evaluateFitnessCommand = _useJobsFitness
				? new TspEvaluateFitnessJobCommand(_distances, _populationSize, _genomeSize)
				: new TspEvaluateFitnessFuncCommand(_distances, _populationSize);

			var evaluatedGeneration = new EvaluatedGenome<Path>[_populationSize];
			var generation = evolution.CreateGeneration(_populationSize);

			for (int i = 0; i < _populationSize; i++)
				generation[i] = evolution.CreateGenome();;

			for (int i = 0; i < _populationSize; i++)
				evaluatedGeneration[i] = new EvaluatedGenome<Path>();
			
			var timer = Stopwatch.StartNew();

			for (int i = 0; i < _numGenerations; i++)
			{
				if (_useThreads)
				{
					var evalComplete = false;
					var generation1 = generation;

					Task.Run(() =>
					{
						EvaluateGeneration(generation1, evaluatedGeneration);

						evalComplete = true;
					});
				
					while (!evalComplete)
						yield return null;
				}
				else
				{
					EvaluateGeneration(generation, evaluatedGeneration);
				}
				
				var localBestGenome = evaluatedGeneration[0];

				if (bestGenome == null || localBestGenome.fitness > bestGenome.fitness)
					bestGenome = localBestGenome;

				generation = CreateNextGeneration(evaluatedGeneration, evolution);
				
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

			callback?.Invoke(bestGenome);
		}

		private void OnClickGenerateMap()
		{
			_selectedCity = null;
			_bestPath = null;

			var seed = _mapSeed.Value != 0 ? _mapSeed.Value : new Random().Next(10000);
			var mapRandom = new Random(seed);

			_mapLogs.Log($"Seed: {seed}");

			foreach (Transform t in _mapCitiesContainer)
				Destroy(t.gameObject);

			ClearPath();

			_cities.Clear();

			var cityViewSize = _cityPrefab.GetComponent<RectTransform>().rect.width;

			var fieldWidth = (_mapCitiesContainer.rect.width - cityViewSize) / 2;
			var fieldHeight = (_mapCitiesContainer.rect.height - cityViewSize) / 2;

			bool IsValidCoords(Vector2 coords, int decreaseSizePercent)
			{
				return _cities.All(c =>
					Vector2.Distance(coords, c.coords) > cityViewSize * (1f - decreaseSizePercent / 100f));
			}

			var numCities = _mapSize.Value;
			for (int i = 0; i < numCities; i++)
			{
				var attempts = 0;

				while (true)
				{
					var coords = new Vector2(
						(float)(-1 + 2 * mapRandom.NextDouble()) * fieldWidth,
						(float)(-1 + 2 * mapRandom.NextDouble()) * fieldHeight
					);

					if (IsValidCoords(coords, attempts) || attempts > 100)
					{
						_cities.Add(new City
						{
							id = i + 1, // (int)('A' + i), // (i % 26)
							coords = coords,
						});
						break;
					}

					attempts++;
				}
			}

			foreach (var city in _cities)
			{
				var cityView = Instantiate(_cityPrefab, _mapCitiesContainer);
				cityView.SetCity(city, OnCityClick);
			}

			_distances = new();
			foreach (var city1 in _cities)
			{
				foreach (var city2 in _cities)
				{
					if (city1.id == city2.id) continue;

					_distances[city1.id * 10_000 + city2.id] = (int)Vector2.Distance(city1.coords, city2.coords);
				}
			}

			_genomeSize = numCities + 1;

			UpdateUiState();

			PlayerPrefs.SetInt("TspMapSeed", _mapSeed.Value);
			PlayerPrefs.SetInt("TspNumCities", numCities);
			PlayerPrefs.Save();
		}

		private void EvaluateGeneration(Path[] generation, EvaluatedGenome<Path>[] evaluatedGeneration)
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

			// Array.Sort(evaluatedGeneration, SortEvaluatedGenomes);
		}
		
		// private int SortEvaluatedGenomes(EvaluatedGenome<Path> a, EvaluatedGenome<Path> b)
		// {
			// return b.fitness.CompareTo(a.fitness);
		// }

		private void OnDrawPath()
		{
			if (_bestPath == null) return;

			DrawPath(_bestPath.genome.cities);
		}

		private void DrawPath(int[] bestCities)
		{
			ClearPath();

			_drawPathCoroutine = StartCoroutine(DrawPathCoroutine(bestCities));
		}

		private IEnumerator DrawPathCoroutine(int[] bestCities)
		{
			// var widthStep = 10f / bestCities.Count;
			for (int i = 0; i < bestCities.Length - 1; i++)
			{
				var fromCity = bestCities[i];
				var toCity = bestCities[i + 1];

				var path = Instantiate(_pathPrefab, _mapPathsContainer);
				var lineTr = path.rectTransform;

				path.color = Color.Lerp(Color.green, Color.red, Mathf.Clamp01(i / (bestCities.Length - 2f)));

				var pointA = _cities.Find(c => c.id == fromCity).coords;
				var pointB = _cities.Find(c => c.id == toCity).coords;

				var midPoint = (pointA + pointB) / 2f;
				lineTr.localPosition = midPoint;

				var direction = pointB - pointA;
				var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
				lineTr.rotation = Quaternion.Euler(0, 0, angle);

				var distance = Vector2.Distance(pointA, pointB);
				lineTr.sizeDelta = new Vector2(distance, lineTr.sizeDelta.y);

				yield return new WaitForSeconds(0.3f);
			}

			_drawPathCoroutine = null;
		}

		private void ClearPath()
		{
			if (_drawPathCoroutine != null) StopCoroutine(_drawPathCoroutine);

			foreach (Transform t in _mapPathsContainer)
				Destroy(t.gameObject);
		}

		private Path[] CreateNextGeneration(
			EvaluatedGenome<Path>[] evaluatedGeneration,
			TspEvolutionaryManager evolution)
		{
			// var rand = new Random();

			var newGeneration = evolution.CreateGeneration(evaluatedGeneration.Length);

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
			// var children = _useComputeShader 
				// ? PerformCrossoverShader(childrenCount, parents, _crossoverPercent, rand)
				// : evolution.PerformCrossover(childrenCount, parents);
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

		private Path[] PerformCrossoverShader(int requiredChildren, Path[] parents, int crossoverPercent, Random rand)
		{
			var numParents = parents.Length;

			if (numParents == 0)
				throw new Exception("No parents passed to perform crossover");

			var crossoverRate = crossoverPercent / 100f;

			if (numParents < 2)
			{
				Debug.LogWarning("Can't perform crossover because number of parents are less than 2");
				crossoverRate = 0;
			}

			var children = new List<Path>();

			while (children.Count < requiredChildren)
			{
				var parent1 = parents[rand.Next(numParents)];
				var parent2 = parents[rand.Next(numParents)];

				if (numParents >= 2 && parent1.ID == parent2.ID) continue;

				Path child;

				if (rand.NextDouble() < crossoverRate)
				{
					var arrayA = parents[0].cities.ToArray();
					var arrayB = parents[1].cities.ToArray();

					var childCities = new int[arrayA.Length];

					var kernelHandle = _computeShader.FindKernel("TspCrossover");

					_bufferParent1.SetData(arrayA);
					_bufferParent2.SetData(arrayB);

					_computeShader.SetInt("reqChildren", requiredChildren);
					_computeShader.SetBuffer(kernelHandle, "parent1", _bufferParent1);
					_computeShader.SetBuffer(kernelHandle, "parent2", _bufferParent2);
					_computeShader.SetBuffer(kernelHandle, "child", _bufferChild);

					_computeShader.Dispatch(kernelHandle, arrayA.Length, 1, 1);

					_bufferChild.GetData(childCities);

					child = new Path(childCities);
				}
				else
				{
					child = rand.NextDouble() < 0.5f ? parent1.Copy() : parent2.Copy();
				}

				children.Add(child);
			}
			
			return children.ToArray();
		}

		private List<Path> PerformCrossoverShader2(int requiredChildren, List<Path> parents, int crossoverPercent)
		{
			var kernelIndex = _computeShader.FindKernel("TspCrossover");

			var numCities = _cities.Count;

			_bufferParent1 = new ComputeBuffer(requiredChildren, numCities * sizeof(int));
			_bufferParent2 = new ComputeBuffer(requiredChildren, numCities * sizeof(int));
			
			var children = new int[requiredChildren];
			
			_computeShader.GetKernelThreadGroupSizes(kernelIndex, out var threadGroupSize,out _, out _);
			
			_computeShader.SetInt("numCities", _cities.Count);

			var threadGroups = (int)(requiredChildren / threadGroupSize);
			
			Debug.Log($"threadGroupSize: {threadGroupSize}, threadGroups: {threadGroups}");

			_computeShader.Dispatch(kernelIndex, threadGroups, 1, 1);
			
			_bufferChild.GetData(children);

			return null; // children.Select(c => new Path { Cities = c}).ToList();
		}

		private void OnCityClick(CityView view)
		{
			if (_selectedCity == null)
			{
				_selectedCity = view;
				return;
			}

			var distance = _distances[_selectedCity.ID * 1000 + view.ID];

			_mapLogs.Log($"Distance ({_selectedCity.ID} -> {view.ID}): {distance}");

			_selectedCity = null;
		}

		private void OnDestroy()
		{
			_bufferParent1?.Dispose();
			_bufferParent2?.Dispose();
			_bufferChild?.Dispose();
		}
	}

	public class City
	{
		public int id;
		public Vector2 coords;
	}
}