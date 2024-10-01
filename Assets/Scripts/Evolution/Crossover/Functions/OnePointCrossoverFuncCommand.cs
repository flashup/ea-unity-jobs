using System;

namespace Evolution.Crossover.Functions
{
	public class OnePointCrossoverFuncCommand<T> : ICrossoverCommand<T>
		where T : struct, IComparable
	{
		private readonly Random _random;
		private readonly int _numParents;
		private readonly int _numChildren;
		private readonly int _genomeLength;
		private readonly float _crossoverRate;

		public OnePointCrossoverFuncCommand(int crossoverPercent, Random random, int numParents, int numChildren, int genomeLength)
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
			// var avgCrossover = false;

			var childrenCounter = 0;

			while (childrenCounter < _numChildren)
			{
				var parent1Index = _random.Next(_numParents);
				var parent2Index = _random.Next(_numParents);

				if (_numParents >= 2 && parent1Index == parent2Index) continue;

				var parent1 = new ArraySegment<T>(parentsGenome, parent1Index * _genomeLength,
					_genomeLength);
				var parent2 = new ArraySegment<T>(parentsGenome, parent2Index * _genomeLength,
					_genomeLength);

				var child1GenomeOffset = childrenCounter * _genomeLength;
				childrenCounter++;
	
				var child2GenomeOffset = childrenCounter < _numChildren ? childrenCounter * _genomeLength : -1;
				if (child2GenomeOffset != -1) childrenCounter++;

				if (_random.NextDouble() < _crossoverRate) 
				{
					var crossoverPoint = _random.Next(1, _genomeLength - 1);

					for (int i = 0; i < crossoverPoint; i++)
					{
						childrenGenome[child1GenomeOffset + i] = parent1[i];
						if (child2GenomeOffset != -1)
							childrenGenome[child2GenomeOffset + i] = parent2[i];
					}

					for (int i = crossoverPoint; i < _genomeLength; i++)
					{
						childrenGenome[child1GenomeOffset + i] = parent2[i];
						if (child2GenomeOffset != -1)
							childrenGenome[child2GenomeOffset + i] = parent1[i];
					}
				}
				else
				{
					for (int i = 0; i < _genomeLength; i++)
					{
						childrenGenome[child1GenomeOffset + i] = parent1[i];
						if (child2GenomeOffset != -1)
							childrenGenome[child2GenomeOffset + i] = parent2[i];
					}
				}
			}
		}

		public void Dispose()
		{
		}
	}
}