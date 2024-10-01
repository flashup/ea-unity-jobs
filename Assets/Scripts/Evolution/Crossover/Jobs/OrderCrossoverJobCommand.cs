using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Evolution.Crossover.Jobs
{
	public class OrderCrossoverJobCommand : ICrossoverCommand<int>
	{
		private readonly int _batchCount = JobSettings.BatchCount;

		private readonly Random _random;
		private readonly int _numParents;
		private readonly int _numChildren;
		private readonly int _genomeLength;
		private readonly float _crossoverRate;
		private readonly bool _fixedEdgePositions;

		private NativeArray<int> _parentsGenomes;
		private NativeArray<int> _childrenGenomes;
		private NativeArray<bool> _crossoverChances;

		public OrderCrossoverJobCommand(
			int crossoverPercent,
			Random random,
			int numParents,
			int numChildren,
			int genomeLength)
		{
			_random = random;
			_numParents = numParents;
			_numChildren = numChildren;
			_genomeLength = genomeLength;
			_crossoverRate = crossoverPercent / 100f;

			if (_numParents < 2 && crossoverPercent > 0)
				throw new Exception("Not enough parents to perform crossover");

			_parentsGenomes = new NativeArray<int>(numParents * genomeLength, Allocator.Persistent);
			_childrenGenomes = new NativeArray<int>(numChildren * genomeLength, Allocator.Persistent);
			_crossoverChances = new NativeArray<bool>(numChildren, Allocator.Persistent);
		}

		public void Execute(int[] parentsGenome, int[] childrenGenome)
		{
			for (int i = 0; i < parentsGenome.Length; i++)
				_parentsGenomes[i] = parentsGenome[i];
			
			for (int i = 0; i < _numChildren; i++)
				_crossoverChances[i] = _random.NextDouble() < _crossoverRate;

			var r = new Unity.Mathematics.Random((uint)_random.Next(1, 10000));

			var job = new OrderCrossoverJob
			{
				genomeLength = _genomeLength,
				numParents = _numParents,
				parentsGenomes = _parentsGenomes,
				crossoverChances = _crossoverChances,
				childrenGenomes = _childrenGenomes,
				r = r,
			};

			var handle = job.Schedule(_numChildren, _batchCount);
			handle.Complete();
			
			for (int i = 0; i < _numChildren; i++)
			{
				for (int j = 0; j < _genomeLength; j++)
				{
					childrenGenome[i * _genomeLength + j] = _childrenGenomes[i * _genomeLength + j];
				}
			}
		}

		public void Dispose()
		{
			if (_parentsGenomes.IsCreated) _parentsGenomes.Dispose();
			if (_childrenGenomes.IsCreated) _childrenGenomes.Dispose();
			if (_crossoverChances.IsCreated) _crossoverChances.Dispose();
		}
	}

	[BurstCompile]
	internal struct OrderCrossoverJob : IJobParallelFor
	{
		[ReadOnly] public int genomeLength;
		[ReadOnly] public int numParents;

		[ReadOnly] public NativeArray<int> parentsGenomes;
		[ReadOnly] public NativeArray<bool> crossoverChances;
		[ReadOnly] public Unity.Mathematics.Random r;

		[NativeDisableParallelForRestriction] public NativeArray<int> childrenGenomes;

		public void Execute(int index)
		{
			var isCrossover = crossoverChances[index];

			int parent1Index;
			int parent2Index;

			do
			{
				parent1Index = r.NextInt(numParents);
				parent2Index = r.NextInt(numParents);
			} while (numParents >= 2 && parent1Index == parent2Index);

			var childGenomeOffset = index * genomeLength;

			for (int i = 0; i < genomeLength; i++)
				childrenGenomes[childGenomeOffset + i] = 0;

			if (isCrossover)
			{
				var crossoverPoint = r.NextInt(1, genomeLength - 1);

				for (int i = 0; i < crossoverPoint; i++)
					childrenGenomes[childGenomeOffset + i] = parentsGenomes[parent1Index * genomeLength + i];

				for (int i = crossoverPoint; i < genomeLength; i++)
				{
					for (int j = 0; j < genomeLength; j++)
					{
						var geneIndex = (i + j) % genomeLength;
						var city = parentsGenomes[parent2Index * genomeLength + geneIndex];

						var isCityInChild = false;
						for (int k = 0; k < i; k++)
						{
							if (childrenGenomes[childGenomeOffset + k] == city)
							{
								isCityInChild = true;
								break;
							}
						}

						if (!isCityInChild)
						{
							childrenGenomes[childGenomeOffset + i] = city;
							break;
						}
					}
				}
			}
			else
			{
				var parIndex = r.NextBool() ? parent1Index : parent2Index;
				for (int i = 0; i < genomeLength; i++)
					childrenGenomes[childGenomeOffset + i] = parentsGenomes[parIndex * genomeLength + i];
			}
		}
	}
}