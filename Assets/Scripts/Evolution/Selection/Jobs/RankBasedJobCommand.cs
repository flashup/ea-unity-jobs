using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Evolution.Selection.Jobs
{
	public class RankBasedJobCommand : ISelectionCommand
	{
		private static readonly int _batchCount = JobSettings.BatchCount;
		private readonly int _parentsAmount;
		private readonly int _populationSize;
		private readonly Random _random;

		private NativeArray<int> _selectedIndices;
		private NativeArray<int> _ranks;

		public RankBasedJobCommand(int populationSize, int parentsAmount, Random random)
		{
			_parentsAmount = parentsAmount;
			_populationSize = populationSize;
			_random = random;

			_selectedIndices = new NativeArray<int>(parentsAmount, Allocator.Persistent);
			_ranks = new NativeArray<int>(populationSize, Allocator.Persistent);
		}

		public void Execute<T>(EvaluatedGenome<T>[] evaluatedGeneration, T[] parents) where T : IGenome<T>
		{
			for (int i = 0; i < _populationSize; i++)
				_ranks[i] = _populationSize - i;

			var totalRankSum = _populationSize * (_populationSize + 1) / 2;

			var r = new Unity.Mathematics.Random((uint)_random.Next(1, 10000));

			var job = new RankBasedJob
			{
				ranks = _ranks,
				totalRankSum = totalRankSum,
				selectedIndices = _selectedIndices,
				r = r,
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
			if (_ranks.IsCreated) _ranks.Dispose();
		}
	}

	[BurstCompile]
	internal struct RankBasedJob : IJobParallelFor
	{
		[ReadOnly] public NativeArray<int> ranks;
		[ReadOnly] public int totalRankSum;
		[ReadOnly] public Unity.Mathematics.Random r;

		[WriteOnly] public NativeArray<int> selectedIndices;

		public void Execute(int index)
		{
			var generationSize = ranks.Length;

			var rand = r.NextDouble() * totalRankSum;
			var cumulativeRank = 0;

			for (int i = 0; i < generationSize; i++)
			{
				var rank = generationSize - i;
				cumulativeRank += rank;
				if (cumulativeRank >= rand)
				{
					selectedIndices[index] = i;
					break;
				}
			}
		}
	}
}