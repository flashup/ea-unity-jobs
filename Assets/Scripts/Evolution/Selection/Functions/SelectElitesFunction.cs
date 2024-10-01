using System;

namespace Evolution.Selection.Functions
{
	public class SelectElitesFunction : ISelectionCommand
	{
		public void Execute<T>(EvaluatedGenome<T>[] evaluatedGeneration, T[] elites)
			where T : IGenome<T>
		{
			Array.Sort(evaluatedGeneration, EvaluatedGenomeHelper.SortDescending);

			for (int i = 0; i < elites.Length; i++)
				elites[i] = evaluatedGeneration[i].genome;
		}

		public void Dispose()
		{
		}
	}
}