using System;

namespace Evolution.Mutation.Functions
{
	public class RandomShiftFuncCommand<T> : IMutationCommand<T>
		where T : struct, IComparable
	{
		private readonly Random _random;
		private readonly float _mutationRate;
		private readonly int _numGenomes;
		private readonly int _genomeLength;

		public RandomShiftFuncCommand(int mutationPercent, Random random, int numGenomes, int genomeLength)
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
				if (!(_random.NextDouble() < _mutationRate)) continue;

				var startIndex = i * _genomeLength;

				// we should save the value, because it will be replaced on the 1 step of loop below
				var startElement = genomes[startIndex];

				for (var j = 0; j < _genomeLength; j++)
				{
					var geneIndex = startIndex + j;

					if (j == _genomeLength - 1)
						genomes[geneIndex] = startElement;
					else
						genomes[geneIndex] = genomes[geneIndex + 1];
				}
			}
		}

		public void Dispose()
		{
		}
	}
}