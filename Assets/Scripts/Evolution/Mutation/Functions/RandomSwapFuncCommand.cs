using System;

namespace Evolution.Mutation.Functions
{
	public class RandomSwapFuncCommand<T> : IMutationCommand<T>
		where T : struct, IComparable
	{
		private readonly Random _random;
		private readonly float _mutationRate;
		private readonly int _numGenomes;
		private readonly int _genomeLength;

		public RandomSwapFuncCommand(int mutationPercent, Random random, int numGenomes, int genomeLength)
		{
			_mutationRate = mutationPercent / 100f;
			_random = random;
			_numGenomes = numGenomes;
			_genomeLength = genomeLength;
		}

		public void Execute(T[] genomes)
		{
			for (int i = 0; i < _numGenomes; i++)
			{
				var startIndex = i * _genomeLength;

				for (var j = 0; j < _genomeLength - 1; j++)
				{
					var geneIndex = startIndex + j;
					
					if (_random.NextDouble() < _mutationRate)
					{
						// swap with next
						var curGene = genomes[geneIndex];
						var nextGene = genomes[geneIndex + 1];
						genomes[geneIndex] = nextGene;
						genomes[geneIndex + 1] = curGene;

						// if (curGene.CompareTo(1) == 0 || nextGene.CompareTo(1) == 0)
							// throw new Exception("1");
					}
				}
			}
		}

		public void Dispose()
		{
		}
	}
}