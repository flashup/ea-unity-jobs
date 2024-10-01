using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace TSP.FitnessEvaluation.Jobs
{
	public class TspEvaluateFitnessJobCommand : ITspEvaluateFitnessCommand
	{
		private int _populationSize;
		private int _genomeSize;

		private NativeHashMap<int, int> _distances;
		private NativeArray<int> _populationGenomes;
		private NativeArray<float> _fitnessValues;

		private static readonly int _batchCount = JobSettings.BatchCount;

		public TspEvaluateFitnessJobCommand(Dictionary<int, int> distances, int populationSize, int fullGenomeSize)
		{
			_populationSize = populationSize;
			_genomeSize = fullGenomeSize;

			_distances = new NativeHashMap<int, int>(distances.Count, Allocator.Persistent);

			foreach (var (citiesId, distance) in distances)
				_distances[citiesId] = distance;

			_populationGenomes = new NativeArray<int>(populationSize * fullGenomeSize, Allocator.Persistent);
			_fitnessValues = new NativeArray<float>(populationSize, Allocator.Persistent);
		}

		public NativeArray<float> Execute(Path[] generation)
		{
			for (int i = 0; i < _populationSize; i++)
			{
				var path = generation[i];
				for (int j = 0; j < _genomeSize; j++)
				{
					_populationGenomes[i * _genomeSize + j] = path.cities[j];
				}
			}

			RunJob();

			return _fitnessValues;
		}

		private void RunJob()
		{
			var job = new FitnessRosenbrockJob
			{
				distances = _distances,
				populationGenomes = _populationGenomes,
				genomeSize = _genomeSize,
				fitness = _fitnessValues,
			};
			var handle = job.Schedule(_populationSize, _batchCount);
			handle.Complete();
		}

		public void Dispose()
		{
			if (_fitnessValues.IsCreated) _fitnessValues.Dispose();
			if (_populationGenomes.IsCreated) _populationGenomes.Dispose();
			if (_distances.IsCreated) _distances.Dispose();
		}
	}

	[BurstCompile]
	public struct FitnessRosenbrockJob : IJobParallelFor
	{
		[ReadOnly] public NativeHashMap<int, int> distances;

		[ReadOnly] public NativeArray<int> populationGenomes;
		[ReadOnly] public int genomeSize;

		[WriteOnly] public NativeArray<float> fitness;

		public void Execute(int index)
		{
			var distance = 0;
			var startIndex = index * genomeSize;

			for (int i = 0; i < genomeSize - 1; i++)
			{
				var fromId = populationGenomes[startIndex + i];
				var toId = populationGenomes[startIndex + i + 1];
				distance += distances[fromId * 10_000 + toId];
			}

			fitness[index] = 1000f / (1 + distance);
		}
	}
}