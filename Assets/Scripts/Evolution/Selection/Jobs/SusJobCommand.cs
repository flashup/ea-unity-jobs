using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Evolution.Selection.Jobs
{
	public class SusJobCommand : ISelectionCommand
	{
		private static readonly int _batchCount = JobSettings.BatchCount;

		private readonly int _populationSize;
		private readonly int _parentsAmount;
		private readonly Random _random;

		private NativeArray<int> _selectedIndices;
		private NativeArray<float> _fitness;

		public SusJobCommand(int populationSize, int parentsAmount, Random random)
		{
			_populationSize = populationSize;
			_parentsAmount = parentsAmount;
			_random = random;

			_selectedIndices = new NativeArray<int>(parentsAmount, Allocator.Persistent);
			_fitness = new NativeArray<float>(populationSize, Allocator.Persistent);
		}

		public void Execute<T>(EvaluatedGenome<T>[] evaluatedGeneration, T[] parents) where T : IGenome<T>
		{
			var totalFitness = 0f;
			for (int i = 0; i < _populationSize; i++)
			{
				var f = evaluatedGeneration[i].fitness;
				totalFitness += f;
				_fitness[i] = f;
			}
			
			var numSelections = _parentsAmount;

			var pointersDistance = totalFitness / numSelections;

			var job = new SusJob
			{
				fitness = _fitness,
				startPoint = (float)_random.NextDouble() * pointersDistance,
				pointersDistance = pointersDistance,
				selectedIndices = _selectedIndices,
			};
			var handle = job.Schedule(numSelections, _batchCount);
			handle.Complete();

			for (int i = 0; i < _parentsAmount; i++)
			{
				var index = _selectedIndices[i];
				parents[i] = evaluatedGeneration[index].genome;
			}
		}
		
		public void Dispose()
		{
			if (_selectedIndices.IsCreated) _selectedIndices.Dispose();
			if (_fitness.IsCreated) _fitness.Dispose();
		}
	}

	[BurstCompile]
	internal struct SusJob : IJobParallelFor
	{
		[ReadOnly] public NativeArray<float> fitness;
		[ReadOnly] public float pointersDistance;
		[ReadOnly] public float startPoint;

		[WriteOnly] public NativeArray<int> selectedIndices;

		public void Execute(int index)
		{
			double cumulativeSum = 0;
			var numFitness = fitness.Length;
			var borderPoint = startPoint + index * pointersDistance;

			for (int i = 0; i < numFitness; i++)
			{
				cumulativeSum += fitness[i];

				if (cumulativeSum >= borderPoint)
				{
					selectedIndices[index] = i;
					break;
				}
			}
		}
	}
}