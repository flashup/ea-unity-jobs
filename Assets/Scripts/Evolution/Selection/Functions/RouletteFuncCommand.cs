using System;

namespace Evolution.Selection.Functions
{
	public class RouletteFuncCommand : ISelectionCommand
	{
		private readonly int _parentsAmount;
		private readonly Random _random;

		public RouletteFuncCommand(int parentsAmount, Random random)
		{
			_parentsAmount = parentsAmount;
			_random = random;
		}

		public void Execute<T>(EvaluatedGenome<T>[] evaluatedGeneration, T[] parents)
			where T : IGenome<T>
		{
			var populationSize = evaluatedGeneration.Length;

			var totalFitness = 0f;

			for (int i = 0; i < populationSize; i++)
				totalFitness += evaluatedGeneration[i].fitness;

			for (int i = 0; i < _parentsAmount; i++)
			{
				var rand = _random.NextDouble() * totalFitness;
				double cumulativeSum = 0;

				foreach (var sg in evaluatedGeneration)
				{
					cumulativeSum += sg.fitness;
					if (cumulativeSum >= rand)
					{
						parents[i] = sg.genome;
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