using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Random = System.Random;

namespace Evolution.Crossover.Jobs
{
	public class OnePointCrossoverJobCommand : ICrossoverCommand<float>
	{
		private readonly int _batchCount = JobSettings.BatchCount;

		private readonly Random _random;
		private readonly int _numParents;
		private readonly int _numChildren;
		private readonly int _genomeLength;
		private readonly float _crossoverRate;

		private NativeArray<float> _parentsGenomes;
		private NativeArray<float> _childrenGenomes;

		public OnePointCrossoverJobCommand(int crossoverPercent, Random random, int numParents, int numChildren, int genomeLength)
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

			var job = new OnePointCrossoverJob
			{
				genomeLength = _genomeLength,
				numParents = _numParents,
				numChildren = _numChildren,
				crossoverRate = _crossoverRate,
				parentsGenome = _parentsGenomes,
				childrenGenome = _childrenGenomes,
				r = r,
			};

			// 2 children per call will be created
			var handle = job.Schedule(UnityEngine.Mathf.CeilToInt(_numChildren / 2f), _batchCount);
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
	internal struct OnePointCrossoverJob : IJobParallelFor
	{
		[ReadOnly] public int genomeLength;
		[ReadOnly] public int numParents;
		[ReadOnly] public int numChildren;
		[ReadOnly] public float crossoverRate;

		[ReadOnly] public NativeArray<float> parentsGenome;
		[ReadOnly] public Unity.Mathematics.Random r;

		// TODO WriteOnly disabled, to be allow to write not only in current index, but in next too
		[NativeDisableParallelForRestriction] public NativeArray<float> childrenGenome;
		// [WriteOnly] public NativeArray<float> childrenGenomes;

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

			// 2 children per call will be created
			var child1GenomeOffset = (index * 2) * genomeLength;
			var child2GenomeOffset = (index * 2 + 1) < numChildren ? (index * 2 + 1) * genomeLength : -1;
	
			if (isCrossover)
			{
				var crossoverPoint = r.NextInt(1, genomeLength - 1);

				for (int i = 0; i < crossoverPoint; i++)
				{
					childrenGenome[child1GenomeOffset + i] = parentsGenome[parent1Index * genomeLength + i];
					if (child2GenomeOffset != -1)
						childrenGenome[child2GenomeOffset + i] = parentsGenome[parent2Index * genomeLength + i];
				}

				for (int i = crossoverPoint; i < genomeLength; i++)
				{
					childrenGenome[child1GenomeOffset + i] = parentsGenome[parent2Index * genomeLength + i];
					if (child2GenomeOffset != -1)
						childrenGenome[child2GenomeOffset + i] = parentsGenome[parent1Index * genomeLength + i];
				}
			}
			else
			{
				for (int i = 0; i < genomeLength; i++)
				{
					childrenGenome[child1GenomeOffset + i] = parentsGenome[parent1Index * genomeLength + i];
					if (child2GenomeOffset != -1)
						childrenGenome[child2GenomeOffset + i] = parentsGenome[parent2Index * genomeLength + i];
				}
			}
		}
	}
}

