using System;

namespace Evolution
{
	public interface ISelectionCommand : IDisposable
	{
		public void Execute<T>(EvaluatedGenome<T>[] evaluatedGeneration, T[] parents)
			where T : IGenome<T>;
	}
}