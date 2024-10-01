using System;

namespace Evolution.Crossover.Functions
{
	public class RandomCrossoverFuncCommand<T> : ICrossoverCommand<T>
		where T : struct, IComparable
	{
		private readonly Random _random;
		private readonly int _numParents;
		private readonly int _numChildren;
		private readonly int _genomeLength;
		private readonly float _crossoverRate;
		private readonly bool _fixedEdgePositions;

		public RandomCrossoverFuncCommand(
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
		}

		public void Execute(T[] parentsGenome, T[] childrenGenome)
		{
			var childrenCounter = 0;

			while (childrenCounter < _numChildren)
			{
				var parentIndex1 = _random.Next(_numParents);
				var parentIndex2 = _random.Next(_numParents);

				if (_numParents >= 2 && parentIndex1 == parentIndex2) continue;

				var parent1 = new ArraySegment<T>(parentsGenome, parentIndex1 * _genomeLength,
					_genomeLength);
				var parent2 = new ArraySegment<T>(parentsGenome, parentIndex2 * _genomeLength,
					_genomeLength);

				var childGenomeOffset = childrenCounter * _genomeLength;
				if (_random.NextDouble() < _crossoverRate)
				{
					for (int i = 0; i < _genomeLength; i++)
						childrenGenome[childGenomeOffset + i] = _random.NextDouble() < 0.5f 
							? parent1[i] 
							: parent2[i];
				}
				else
				{
					var par = _random.NextDouble() < 0.5f ? parent1 : parent2;
					for (int i = 0; i < _genomeLength; i++)
						childrenGenome[childGenomeOffset + i] = par[i];
				}

				childrenCounter++;
			}
		}

		public void Dispose()
		{
		}
	}
}