using System;

namespace Evolution.Selection.Functions
{
	public class SusFuncCommand : ISelectionCommand
	{
		private readonly int _parentsAmount;
		private readonly Random _random;
		private readonly double[] _pointers;

		public SusFuncCommand(int parentsAmount, Random random)
		{
			_parentsAmount = parentsAmount;
			_random = random;
			_pointers = new double[_parentsAmount];
		}

		public void Execute<T>(EvaluatedGenome<T>[] evaluatedGeneration, T[] parents)
			where T : IGenome<T>
		{
			var populationSize = evaluatedGeneration.Length;

			var totalFitness = 0f;
			for (int i = 0; i < populationSize; i++)
				totalFitness += evaluatedGeneration[i].fitness;

			var numSelections = _parentsAmount;

			var pointersDistance = totalFitness / numSelections;

			var startPoint = _random.NextDouble() * pointersDistance;

			for (int i = 0; i < numSelections; i++)
			{
				_pointers[i] = startPoint + i * pointersDistance;
			}

			double cumulativeSum = 0;
			var currentPointerIndex = 0;

			foreach (var sg in evaluatedGeneration)
			{
				cumulativeSum += sg.fitness;

				while (currentPointerIndex < _pointers.Length && cumulativeSum >= _pointers[currentPointerIndex])
				{
					parents[currentPointerIndex] = sg.genome;
					currentPointerIndex++;
				}
			}
		}

		public void Dispose()
		{
		}
	}
}