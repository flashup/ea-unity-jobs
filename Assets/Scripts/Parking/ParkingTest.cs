using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Data;
using Evolution;
using UnityEngine;
using Helpers;
using UI;
using Parking.View;
using UI.Controls;
using Random = System.Random;

// TODO car sprites

namespace Parking
{
	public class ParkingTest : MonoBehaviour
	{
		[Header("Test")] 
		public bool checkDeterminism;
		public bool useCommonSchema; // TODO useCommonSchema

		[Header("Params")]
		[SerializeField] private ParkingEvolutionParameters _evParams;

		[Header("UI")]
		[SerializeField] private CarLabels _carLabels;
		[SerializeField] private LogsPanel _logs;
		[SerializeField] private UIButton _startBtn;
		[SerializeField] private UIButton _startManualBtn;
		[SerializeField] private UIButton _stopBtn;
		[SerializeField] private ParameterControlSlider _speedControl;

		[SerializeField] private FitnessChart _chart;

		[Header("View")]
		[SerializeField] private SpriteRenderer _spawnPlace;
		[SerializeField] private SpriteRenderer _parkingPlace;
		[SerializeField] private Transform _carsContainer;

		public Car manualCar;

		[Header("Prefabs")]
		public Car carPrefab;

		[Header("State")] 
		[SerializeField] 
		[Range(0, 6)]
		private int _currentTimeScaleIndex = 3;

		private Dictionary<int, Car> _cars = new();

		private int _generationIndex;
		private CarGenome[] _generation;

		private int _currentIteration;
		private int _iterationsPerGeneration;

		private bool _isTestActive;
		private bool _isSimulationWorking;

		private float[] _timeScaleValues = new[] { 0.25f, 0.5f, 1f, 2f, 5f, 10f };

		private Dictionary<int, float[]> _sensors = new();
		private Dictionary<int, Vector3> _carPositions = new();

		private int _bestGenomesMaxItems = 3;
		private List<EvaluatedGenome<CarGenome>> _bestGenomes = new();

		private int[] _commonSchemaSourceIds;
		private List<Bit?> _commonSchema;

		private Queue<Car> _carsCache = new(); // TODO cache

		private Vector3[] _parkingPlaceCorners;
		private Coroutine _testCoroutine;

		private SelectionMethod _selectionMethod;
		private int _parentsAmount;
		private int _elitesAmount;

		private int _numGenerations;
		private int _populationSize;
		private int _genomeSize;

		private int _crossoverPercent;
		private int _mutationPercent;

		private void Start()
		{
			QualitySettings.vSyncCount = 0;
			Application.targetFrameRate = AppSettings.FPS;
			Physics.gravity = Vector3.zero;

			_evParams.Load("Parking");

			_parkingPlaceCorners = GetSpriteCorners(_parkingPlace);

			StopSimulation();

			_commonSchema = null;
			_commonSchemaSourceIds = new int[] { };

			_bestGenomes.Clear();
			_sensors.Clear();
			_carPositions.Clear();

			manualCar.gameObject.SetActive(false);

			_speedControl.SetLabels(_timeScaleValues.Select(v => $"{v}").ToArray());
			_speedControl.ValueChanged.AddListener(OnSimulationSpeedChanged);
			OnSimulationSpeedChanged(_speedControl.defaultValue);

			_startBtn.OnClick(OnClickStart);
			_startManualBtn.OnClick(OnClickManualStart);
			_stopBtn.OnClick(OnClickStop);

			UpdateUiState();
		}

		private void OnClickStart()
		{
			if (_isTestActive) return;

			_testCoroutine = StartCoroutine(StartRuns());
		}

		private void OnClickManualStart()
		{
			if (_isTestActive) return;

			StartCoroutine(StartManualDriving());
		}

		private bool IsTestActive() => _isTestActive;

		private IEnumerator StartManualDriving()
		{
			_isTestActive = true;

			RemoveCars();

			manualCar.gameObject.SetActive(true);
			manualCar.transform.position = _spawnPlace.transform.position;
			manualCar.Highlight(0, Color.white);
			_carLabels.WatchCar(manualCar);

			UpdateUiState();

			_logs.Log("* Use WASD keys to drive the car *");

			_startBtn.UpdateText("Manual");

			manualCar.StartCar();

			StartSimulation();

			_currentIteration = int.MinValue;

			yield return new WaitWhile(IsTestActive);
			
			manualCar.Stop();

			manualCar.gameObject.SetActive(false);
			_carLabels.WatchCar(null);
		}

		private IEnumerator StartRuns()
		{
			_isTestActive = true;

			_numGenerations = _evParams.NumGenerations;
			_populationSize = _evParams.PopulationSize;
			_selectionMethod = _evParams.SelectionMethod;

			_crossoverPercent = _evParams.CrossoverPercent;
			_mutationPercent = _evParams.MutationPercent;
	
			_parentsAmount = (int)Math.Ceiling(_populationSize * (_evParams.SelectionPercent / 100f));
			_elitesAmount = (int)Math.Ceiling(_populationSize * (_evParams.ElitismPercent / 100f));

			_genomeSize = LpEncoding.GENOME_LENGTH;

			UpdateUiState();

			var seed = _evParams.Seed;
			seed = seed != 0 ? seed : new Random().Next(10000);

			_logs.Log($"{DateTime.Now:HH:mm:ss} Start with seed: {seed}");

			var evolution = new ParkingEvolutionManager(seed, _genomeSize);
			evolution.InitSelection(_selectionMethod, _populationSize, _elitesAmount, _parentsAmount);
			evolution.InitCrossover(_crossoverPercent, _parentsAmount, _populationSize - _elitesAmount, false);
			evolution.InitMutation(_mutationPercent, _populationSize - _elitesAmount, false);

			RemoveCars();

			_bestGenomes.Clear();
			_chart.Clear();
			_evParams.Save();

			_generation = evolution.CreateGeneration(checkDeterminism ? 1 : _populationSize);
			
			for (int i = 0; i < _populationSize; i++)
				_generation[i] = evolution.CreateGenome();

			_iterationsPerGeneration = _evParams.GenLifetime;

			_generationIndex = 0;

			while (_generationIndex < _numGenerations)
			{
				_startBtn.UpdateText($"{_generationIndex}/{_numGenerations}");
				
				_cars = CreateCars(_generation);
				
				// start watching the best = first car in new generation (from elites in prev generation)
				if (_elitesAmount != 0 && _generation.Length > 0)
					_carLabels.WatchCar(_cars[_generation[0].ID]);

				yield return null;

				StartSimulation();

				yield return new WaitUntil(AllIterationsComplete);

				StopSimulation();

				yield return null;

				var evaluatedGeneration = EvaluateGeneration(_cars.Values.ToArray());

				yield return SaveGenerationResult(evaluatedGeneration);

				_generationIndex++;

				if (checkDeterminism)
					_generation = new CarGenome[] { evaluatedGeneration[0].genome };
				else
					_generation = CreateNextGeneration(evaluatedGeneration, evolution);

				RemoveCars();

				yield return null;
			}

			// TODO show the best trained car in cycle
			// _generation[0]

			RunsCompleted();

			UpdateUiState();
		}

		private bool AllIterationsComplete()
		{
			return _currentIteration >= _iterationsPerGeneration;
		}

		private void RunsCompleted()
		{
			_isTestActive = false;

			_isSimulationWorking = false;
		}

		private void StartSimulation()
		{
			_isSimulationWorking = true;

			_currentIteration = 0;

			foreach (var car in _cars.Values)
				car.StartCar();
		}

		private void StopSimulation()
		{
			_isSimulationWorking = false;

			foreach (var car in _cars.Values)
				car.Stop();
		}

		private void OnClickStop()
		{
			if (!_isTestActive) return;

			if (_testCoroutine != null) StopCoroutine(_testCoroutine);

			_testCoroutine = null;

			StopSimulation();

			RunsCompleted();

			UpdateUiState();
		}

		private void UpdateUiState()
		{
			_startBtn.SetEnabled(!_isTestActive);
			if (!_isTestActive) _startBtn.UpdateText("Test");

			_startManualBtn.gameObject.SetActive(!_isTestActive);
			_stopBtn.gameObject.SetActive(_isTestActive);
		}

		private void FixedUpdate()
		{
			if (!_isTestActive || !_isSimulationWorking) return;

			if (AllIterationsComplete()) return;

			// cars driven by sensors
			foreach (var car in _cars.Values)
			{
				var genomeInput = LpEncoding.SensorsToSignals(car.Genome.DecodedGenome, car.sensors.SensorData);
				// var genomeInput = LpEncoding.SensorsToSignals(car.Genome.genes, car.sensors.SensorData);

				car.SetInput(genomeInput);

				car.FixedUpdateCar();
			}

			if (manualCar.gameObject.activeSelf)
			{
				manualCar.FixedUpdateCar();
			}

			// update this car fitness to show it in label
			if (_carLabels.CarToWatch != null)
			{
				var (_, fitness) = GetScoreAndFitness(_carLabels.CarToWatch);
				_carLabels.CarToWatch.Fitness = fitness;
			}

			if (checkDeterminism)
			{
				var car = _cars.Values.First();
				var sensors = car.sensors.SensorData;
				var tr = car.transform;
				var carPos = new Vector3(tr.position.x, tr.position.y, tr.eulerAngles.z);

				if (_currentIteration == 0)
					Debug.Log($"{_generationIndex + 1}) {carPos} | {sensors.Join()}");

				if (_sensors.ContainsKey(_currentIteration))
				{
					if (!_sensors[_currentIteration].SequenceEqual(sensors))
					{
						Debug.LogWarning($"[{_currentIteration}] Sensors DIFF!");
						Debug.Log(_sensors[_currentIteration].Join(", "));
						Debug.Log(sensors.Join(", "));
					}
				}

				if (_carPositions.ContainsKey(_currentIteration) && _carPositions[_currentIteration] != carPos)
				{
					Debug.LogWarning($"[{_currentIteration}] Position DIFF!");
					Debug.Log(_carPositions[_currentIteration].ToString("F7"));
					Debug.Log(carPos.ToString("F7"));
				}

				_carPositions[_currentIteration] = carPos;
				_sensors[_currentIteration] = sensors.ToArray();
			}

			_currentIteration++;
		}

		// direct relationship
		private double FitnessToEfficiency(double fitness)
		{
			return fitness * 100;
		}

		private (float score, float fitness) GetScoreAndFitness(Car car)
		{
			var distance = GetDistanceToParkingPlace(car);

			var fitness = 1f / (1 + distance);

			// penalties

			// hitting an obstacle
			if (car.Hits > 0) fitness /= (car.Hits + 1);

			// moving in wrong direction
			var startDistanceToParkingSlot = Vector3.Distance(car.StartPosition, _parkingPlace.transform.position);
			var endDistanceToParkingSlot = Vector3.Distance(car.transform.position, _parkingPlace.transform.position);
			if (endDistanceToParkingSlot > startDistanceToParkingSlot)
				fitness /= 2f;

			// less distance = better fitness
			return (distance, fitness);
		}

		private EvaluatedGenome<CarGenome>[] EvaluateGeneration(Car[] generation)
		{
			return generation
				.Select(c =>
				{
					var (score, fitness) = GetScoreAndFitness(c);
					return new EvaluatedGenome<CarGenome>
					{
						genome = c.Genome,
						fitness = fitness,
					};
				})
				.OrderByDescending(g => g.fitness)
				.ToArray();
		}

		private IEnumerator SaveGenerationResult(EvaluatedGenome<CarGenome>[] evaluatedGeneration)
		{
			if (evaluatedGeneration.Length == 0)
				yield break;

			if (evaluatedGeneration.Length == 1)
				Debug.Log($"Fitness: {evaluatedGeneration[0].fitness}");

			var bestLocalGenome = evaluatedGeneration[0];
			var bestLocalCar = _cars[bestLocalGenome.genome.ID];

			if (_elitesAmount != 0 
			    && _bestGenomes.Count > 0
			    && bestLocalGenome.genome.ID == _bestGenomes[^1].genome.ID
				&& !Mathf.Approximately((float)bestLocalGenome.fitness, (float)_bestGenomes[^1].fitness)
			    && bestLocalGenome.fitness < _bestGenomes[^1].fitness
			)
			{
				throw new Exception($"Worse fitness. {bestLocalGenome.genome.ID}: {_bestGenomes[^1].fitness} < {bestLocalGenome.fitness}");
			}

			_chart.AddBestValue(bestLocalGenome.fitness);

			var avgFitness = evaluatedGeneration.Select(c => c.fitness).Average();
			_chart.AddAvgValue(avgFitness);

			var newGenomeAdded = false;

			if (_bestGenomes.Count == 0 || (bestLocalGenome.genome.ID != _bestGenomes[^1].genome.ID && bestLocalGenome.fitness > _bestGenomes[^1].fitness))
			{
				if (!_bestGenomes.Any(g => g.genome.Equals(bestLocalGenome.genome)))
				{
					_bestGenomes.Add(bestLocalGenome);

					if (_bestGenomes.Count > _bestGenomesMaxItems)
						_bestGenomes.RemoveAt(0);

					newGenomeAdded = true;
				}

				// Debug.Log($"Best: {_bestGenomes.Select(b => b.genome.ID).Join(", ")}");
				// Debug.Log($"Best: {_bestGenomes.Select(b => b.fitness).Join(", ")}");

				if (!bestLocalCar.Highlighted)
				{
					_logs.Log(
						$"#{_generationIndex + 1}) New best car. ID: {bestLocalCar.ID} (Fit: {bestLocalGenome.fitness:F4}, Hits: {bestLocalCar.Hits})");

					bestLocalCar.Highlight(_generationIndex + 1, Color.magenta, true);

					yield return new WaitForSecondsRealtime(1f);
				}
			}

			if (useCommonSchema && bestLocalGenome.fitness > 0.4f && newGenomeAdded)
			{
				var needToRecalculateCommonSchema =
					_bestGenomes.Count == _bestGenomesMaxItems
					&& _bestGenomes.All(sg => !_commonSchemaSourceIds.Contains(sg.genome.ID));

				// if (_bestGenomes.Count == _bestGenomesMaxItems)
				// {
				// 	Debug.Log($"GEN: {_bestGenomes.Select(g => g.genome.ID).Join(", ")}");
				// 	Debug.Log($"SAV: {_commonSchemaSourceIds.Join(", ")}");
				// }

				if (needToRecalculateCommonSchema)
				{
					_commonSchema = new List<Bit?>();

					var genome = _bestGenomes[0].genome;
					var genesCount = genome.genes.Length;
					var amountBestGenomes = _bestGenomes.Count;

					Debug.Log($"{_currentIteration}) Detected common schema");
					// Debug.Log(_bestGenomes.Select(g => g.genome.Genes.Join("")).Join("\n"));

					for (int i = 0; i < genesCount; i++)
					{
						var sameGeneAmount = _bestGenomes.Count(g => g.genome.genes[i] == 0);
						if (sameGeneAmount == 0 || sameGeneAmount == amountBestGenomes)
						{
							_commonSchema.Add(genome.genes[i]);
						}
						else
						{
							_commonSchema.Add(null);
						}
					}

					var commonGenes = _commonSchema.Count(gene => gene != null);
					Debug.Log(
						$"Common genes: {commonGenes}/{_commonSchema.Count} ({100f * commonGenes / _commonSchema.Count:F1}%)");
					// Debug.Log(_commonSchema.Select(g => g == null ? "*" : g.ToString()).Join(""));
					Debug.Log("---");

					_commonSchemaSourceIds = _bestGenomes.Select(sg => sg.genome.ID).ToArray();
				}
			}
		}

		private void OnSimulationSpeedChanged(int speedIndex)
		{
			_currentTimeScaleIndex = speedIndex;

			if (_currentTimeScaleIndex < 0)
				_currentTimeScaleIndex = 0;
			else if (_currentTimeScaleIndex > _timeScaleValues.Length - 1)
				_currentTimeScaleIndex = _timeScaleValues.Length - 1;

			Time.timeScale = _timeScaleValues[_currentTimeScaleIndex];
		}

		private void RemoveCars()
		{
			_carLabels.WatchCar(null);

			foreach (var oldCar in _cars.Values)
			{
				// oldCar.Stop();
				// oldCar.gameObject.SetActive(false);
				// _carsCache.Enqueue(oldCar);

				// oldCar.GetComponent<Rigidbody2D>().simulated = false;
				Destroy(oldCar.gameObject);
			}

			_cars.Clear();
		}

		private Dictionary<int, Car> CreateCars(CarGenome[] generation)
		{
			var newCars = new Dictionary<int, Car>();

			Vector3? pivotOffset = null;
			var spawnPlacePos = _spawnPlace.transform.position;
			var spawnPlaceRotation = _spawnPlace.transform.eulerAngles.z;

			for (int i = 0; i < generation.Length; i++)
			{
				var genome = generation[i];
				var car = _carsCache.Count > 0
					? _carsCache.Dequeue()
					: Instantiate(carPrefab, _carsContainer.transform, false);

				car.gameObject.SetActive(true);
				car.transform.position = Vector3.zero;
				car.transform.eulerAngles = Vector3.zero;

				var carSprite = car.GetComponent<SpriteRenderer>().sprite;

				if (pivotOffset == null)
				{
					pivotOffset = new Vector2(
						carSprite.bounds.size.x * (0.5f - carSprite.pivot.x / carSprite.rect.width),
						carSprite.bounds.size.y * (0.5f - carSprite.pivot.y / carSprite.rect.height)
					);
				}

				car.transform.position = spawnPlacePos - pivotOffset.Value;
				car.transform.RotateAround(spawnPlacePos, Vector3.forward, spawnPlaceRotation);

				car.SetGenome(genome);

				if (_bestGenomes.Count > 0 && genome.ID == _bestGenomes[^1].genome.ID)
				{
					car.showSensorRays = true;
					car.Highlight(_generationIndex, Color.green);
				}

				newCars.Add(car.ID, car);
			}

			return newCars;
		}

		private float GetDistanceToParkingPlace(Car car)
		{
			var carCorners = GetSpriteCorners(car.SpriteRenderer);

			var distances = new float[carCorners.Length];
			for (int i = 0; i < distances.Length; i++)
			{
				distances[i] = Vector3.Distance(carCorners[i], _parkingPlaceCorners[i]);
			}

			return distances.Average();
		}

		private static Vector3[] GetSpriteCorners(SpriteRenderer renderer)
		{
			var topRight = renderer.transform.TransformPoint(renderer.sprite.bounds.max);
			var topLeft =
				renderer.transform.TransformPoint(new Vector3(renderer.sprite.bounds.max.x,
					renderer.sprite.bounds.min.y, 0));
			var botLeft = renderer.transform.TransformPoint(renderer.sprite.bounds.min);
			var botRight =
				renderer.transform.TransformPoint(new Vector3(renderer.sprite.bounds.min.x,
					renderer.sprite.bounds.max.y, 0));
			return new[] { topRight, topLeft, botLeft, botRight };
		}

		private CarGenome[] CreateNextGeneration(
			EvaluatedGenome<CarGenome>[] evaluatedGeneration,
			ParkingEvolutionManager evolution)
		{
			var newGeneration = evolution.CreateGeneration(_populationSize);

			var (elites, parents) = evolution.PerformSelection(
				_selectionMethod,
				evaluatedGeneration
			);

			var elitesCount = elites?.Length ?? 0;
			if (elites != null)
			{
				for (int i = 0; i < elitesCount; i++)
				{
					newGeneration[i] = elites[i];
				}
			}

			var childrenCount = _populationSize - elitesCount;
			var children = evolution.PerformCrossover(childrenCount, parents);

			for (int i = 0; i < childrenCount; i++)
			{
				newGeneration[elitesCount + i] = children[i];
			}

			if (useCommonSchema && _commonSchema != null)
			{
				foreach (var genome in children)
				{
					var genes = genome.genes;
					for (int i = 0; i < genes.Length; i++)
					{
						var commonGene = _commonSchema[i];

						if (commonGene == null) continue;

						if (genes[i] != commonGene) genes[i] = commonGene.Value;
					}
				}
			}

			evolution.PerformMutation(children);

			return newGeneration;
		}
	}
}