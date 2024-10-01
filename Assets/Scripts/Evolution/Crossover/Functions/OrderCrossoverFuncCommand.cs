using System;
using System.Linq;

namespace Evolution.Crossover.Functions
{
	public class OrderCrossoverFuncCommand : ICrossoverCommand<int>
	{
		private readonly Random _random;
		private readonly int _numParents;
		private readonly int _numChildren;
		private readonly int _genomeLength;
		private readonly float _crossoverRate;
		private readonly bool _fixedEdgePositions;

		public OrderCrossoverFuncCommand(
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

		public void Execute(int[] parentsGenome, int[] childrenGenome)
		{
			var childrenCounter = 0;

			while (childrenCounter < _numChildren)
			{
				var parentIndex1 = _random.Next(_numParents);
				var parentIndex2 = _random.Next(_numParents);

				if (_numParents >= 2 && parentIndex1 == parentIndex2) continue;

				var parent1 = new ArraySegment<int>(parentsGenome, parentIndex1 * _genomeLength,
					_genomeLength);
				var parent2 = new ArraySegment<int>(parentsGenome, parentIndex2 * _genomeLength,
					_genomeLength);

				var child = new ArraySegment<int>(childrenGenome, childrenCounter * _genomeLength,
					_genomeLength);

				for (var i = 0; i < _genomeLength; i++)
				{
					child[i] = default;
				}

				if (_random.NextDouble() < _crossoverRate)
				{
					var crossoverPoint = _random.Next(1, _genomeLength - 1);

					for (int i = 0; i < crossoverPoint; i++)
					{
						child[i] = parent1[i];
					}

					for (int i = crossoverPoint; i < _genomeLength; i++)
					{
						for (int j = 0; j < _genomeLength; j++)
						{
							var index = (i + j) % _genomeLength;
							var city = parent2[index];
							if (!child.Contains(city))
							{
								child[i] = city;
								break;
							}
						}
					}
				}
				else
				{
					var par = _random.NextDouble() < 0.5f ? parent1 : parent2;
					for (int i = 0; i < _genomeLength; i++)
						child[i] = par[i];
				}

				childrenCounter++;
			}
		}

		public void Dispose()
		{
		}
	}
}