using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Random = System.Random;

namespace Evolution.Crossover.Jobs
{
	public class RandomCrossoverJobCommand : ICrossoverCommand<float>
	{
		private readonly int _batchCount = JobSettings.BatchCount;

		private readonly Random _random;
		private readonly int _numParents;
		private readonly int _numChildren;
		private readonly int _genomeLength;
		private readonly float _crossoverRate;

		private NativeArray<float> _parentsGenomes;
		private NativeArray<float> _childrenGenomes;

		public RandomCrossoverJobCommand(int crossoverPercent, Random random, int numParents, int numChildren, int genomeLength)
		{
			_random = random;
			_numParents = numParents;
			_numChildren = numChildren;
			_genomeLength = genomeLength;
			_crossoverRate = crossoverPercent / 100f;

			_parentsGenomes = new NativeArray<float>(numParents * genomeLength, Allocator.Persistent);
			_childrenGenomes = new NativeArray<float>(numChildren * genomeLength, Allocator.Persistent);
			// _crossoverChances = new NativeArray<bool>(numChildren, Allocator.Persistent);
		}

		// public NativeArray<float> Parents => _parentsGenomes;
		// public NativeArray<float> Children => _childrenGenomes;

		public void Execute(float[] parentsGenomes, float[] childrenGenomes)
		{
			for (int i = 0; i < parentsGenomes.Length; i++)
				_parentsGenomes[i] = parentsGenomes[i];

			var r = new Unity.Mathematics.Random((uint)_random.Next(1, 10000));

			var job = new RandomCrossoverJob
			{
				genomeLength = _genomeLength,
				numParents = _numParents,
				crossoverRate = _crossoverRate,
				parentsGenome = _parentsGenomes,
				childrenGenome = _childrenGenomes,
				r = r,
			};

			var handle = job.Schedule(_numChildren, _batchCount);
			handle.Complete();

			for (int i = 0; i < _numChildren; i++)
			{
				for (int j = 0; j < _genomeLength; j++)
				{
					childrenGenomes[i * _genomeLength + j] = _childrenGenomes[i * _genomeLength + j];
				}
			}
		}

		public void Dispose()
		{
			if (_parentsGenomes.IsCreated) _parentsGenomes.Dispose();
			if (_childrenGenomes.IsCreated) _childrenGenomes.Dispose();
		}
	}

	[BurstCompile]
	internal struct RandomCrossoverJob : IJobParallelFor
	{
		[ReadOnly] public int genomeLength;
		[ReadOnly] public int numParents;
		[ReadOnly] public float crossoverRate;

		[ReadOnly] public NativeArray<float> parentsGenome;
		[ReadOnly] public Unity.Mathematics.Random r;

		// TODO WriteOnly disabled, to be allow to write not only in current index, but in next too
		[NativeDisableParallelForRestriction] public NativeArray<float> childrenGenome;

		public void Execute(int index)
		{
			var isCrossover = r.NextFloat() < crossoverRate;

			int parent1Index;
			int parent2Index;

			do
			{
				parent1Index = r.NextInt(numParents);
				parent2Index = r.NextInt(numParents);
			} while (numParents >= 2 && parent1Index == parent2Index);

			var childGenomeOffset = index * genomeLength;
	
			if (isCrossover)
			{
				for (int i = 0; i < genomeLength; i++)
				{
					childrenGenome[childGenomeOffset + i] = r.NextFloat() < 0.5f
						? parentsGenome[parent1Index * genomeLength + i]
						: parentsGenome[parent2Index * genomeLength + i];
				}
			}
			else
			{
				var parentIndex = r.NextFloat() < 0.5f ? parent1Index : parent2Index;
				for (int i = 0; i < genomeLength; i++)
					childrenGenome[childGenomeOffset + i] = parentsGenome[parentIndex * genomeLength + i];
			}
		}
	}
}

