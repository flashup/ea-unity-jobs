using System;

namespace Evolution.Selection.Functions
{
	public class TruncationFuncCommand : ISelectionCommand
	{
		private readonly int _parentsAmount;

		public TruncationFuncCommand(int parentsAmount)
		{
			_parentsAmount = parentsAmount;
		}

		public void Execute<T>(EvaluatedGenome<T>[] evaluatedGeneration, T[] parents)
			where T : IGenome<T>
		{
			Array.Sort(evaluatedGeneration, EvaluatedGenomeHelper.SortDescending);
	
			for (int i = 0; i < _parentsAmount; i++)
				parents[i] = evaluatedGeneration[i].genome;
		}

		public void Dispose()
		{
		}
	}
}