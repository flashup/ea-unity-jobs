using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Evolution.Mutation.Jobs
{
	public class RandomShiftJobCommand : IMutationCommand<int>
	{
		private static readonly int _batchCount = JobSettings.BatchCount;

		private readonly Random _random;

		private readonly float _mutationRate;
		private readonly int _numGenomes;
		private readonly int _genomeLength;

		private readonly int _allGenomesLength;

		private NativeArray<int> _genomesBefore;
		private NativeArray<int> _genomes;
		private NativeArray<bool> _mutationChances;

		public RandomShiftJobCommand(int mutationPercent, Random random, int numGenomes, int genomeLength)
		{
			_mutationRate = mutationPercent / 100f;
			_random = random;
			_numGenomes = numGenomes;
			_genomeLength = genomeLength;

			_allGenomesLength = numGenomes * genomeLength;
			_genomes = new NativeArray<int>(_allGenomesLength, Allocator.Persistent);
			_genomesBefore = new NativeArray<int>(_allGenomesLength, Allocator.Persistent);
			_mutationChances = new NativeArray<bool>(_numGenomes, Allocator.Persistent);
		}

		public void Execute(int[] genomes)
		{
			for (int i = 0; i < _allGenomesLength; i++)
			{
				_genomes[i] = _genomesBefore[i] = genomes[i];
			}

			for (int i = 0; i < _numGenomes; i++)
				_mutationChances[i] = _random.NextDouble() < _mutationRate;

			var job = new RandomSwapMutationJob
			{
				genomesBefore = _genomesBefore,
				mutationChances = _mutationChances,
				genomeLength = _genomeLength,
				genomes = _genomes,
			};
			var handle = job.Schedule(_allGenomesLength, _batchCount);
			handle.Complete();

			for (int i = 0; i < _allGenomesLength; i++)
				genomes[i] = _genomes[i];
		}

		public void Dispose()
		{
			if (_genomesBefore.IsCreated) _genomesBefore.Dispose();
			if (_genomes.IsCreated) _genomes.Dispose();
			if (_mutationChances.IsCreated) _mutationChances.Dispose();
		}
	}

	[BurstCompile]
	internal struct RandomSwapMutationJob : IJobParallelFor
	{
		[ReadOnly] public int genomeLength;
		[ReadOnly] public NativeArray<int> genomesBefore;
		[ReadOnly] public NativeArray<bool> mutationChances;

		[WriteOnly] public NativeArray<int> genomes;

		public void Execute(int index)
		{
			var genomeIndex = (int)((float)index / genomeLength);
			
			if (!mutationChances[genomeIndex]) return;

			var geneIndex = (int)((float)index % genomeLength);

			if (geneIndex == genomeLength - 1)
				genomes[index] = genomesBefore[index + 1 - genomeLength];
			else
				genomes[index] = genomesBefore[index + 1];
		}
	}
}