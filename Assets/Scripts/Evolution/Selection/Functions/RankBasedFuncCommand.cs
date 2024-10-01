using System;

namespace Evolution.Selection.Functions
{
	public class RankBasedFuncCommand : ISelectionCommand
	{
		private readonly int _parentsAmount;
		private readonly Random _random;

		public RankBasedFuncCommand(int parentsAmount, Random random)
		{
			_parentsAmount = parentsAmount;
			_random = random;
		}

		public void Execute<T>(EvaluatedGenome<T>[] evaluatedGeneration, T[] parents)
			where T : IGenome<T>
		{
			var populationSize = evaluatedGeneration.Length;

			var totalRankSum = (populationSize * (populationSize + 1)) / 2.0;

			for (int i = 0; i < _parentsAmount; i++)
			{
				var rand = _random.NextDouble() * totalRankSum;
				var cumulativeRank = 0;

				for (int j = 0; j < populationSize; j++)
				{
					var rank = populationSize - i;
					cumulativeRank += rank;
					if (cumulativeRank >= rand)
					{
						parents[i] = evaluatedGeneration[i].genome;
						break;
					}
				}
			}
		}

		public void Dispose()
		{
		}
	}
}