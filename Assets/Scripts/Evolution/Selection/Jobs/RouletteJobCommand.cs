using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Evolution.Selection.Jobs
{
	public class RouletteJobCommand : ISelectionCommand
	{
		private static readonly int _batchCount = JobSettings.BatchCount;

		private readonly int _populationSize;
		private readonly int _parentsAmount;
		private readonly Random _random;

		private NativeArray<int> _selectedIndices;
		private NativeArray<float> _fitness;

		public RouletteJobCommand(int populationSize, int parentsAmount, Random random)
		{
			_populationSize = populationSize;
			_parentsAmount = parentsAmount;
			_random = random;

			_selectedIndices = new NativeArray<int>(parentsAmount, Allocator.Persistent);
			_fitness = new NativeArray<float>(populationSize, Allocator.Persistent);
		}

		public void Execute<T>(EvaluatedGenome<T>[] evaluatedGeneration, T[] parents) where T : IGenome<T>
		{
			for (int i = 0; i < _populationSize; i++)
				_fitness[i] = evaluatedGeneration[i].fitness;

			var r = new Unity.Mathematics.Random((uint)_random.Next(1, 10000));

			var job = new RouletteJob
			{
				r = r,
				fitness = _fitness,
				selectedIndices = _selectedIndices,
			};
			var handle = job.Schedule(_parentsAmount, _batchCount);
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
	internal struct RouletteJob : IJobParallelFor
	{
		[ReadOnly] public NativeArray<float> fitness;
		[ReadOnly] public Unity.Mathematics.Random r;

		[WriteOnly] public NativeArray<int> selectedIndices;

		public void Execute(int index)
		{
			var generationSize = fitness.Length;

			var totalFitness = 0f;
			for (int i = 0; i < generationSize; i++)
				totalFitness += fitness[i];

			var rand = r.NextFloat() * totalFitness;

			var cumulativeFitness = 0f;
			for (int i = 0; i < fitness.Length; i++)
			{
				cumulativeFitness += fitness[i];
				if (cumulativeFitness >= rand)
				{
					selectedIndices[index] = i;
					break;
				}
			}
		}
	}
}