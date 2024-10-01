using System;
using Unity.Collections;

namespace TSP.FitnessEvaluation
{
	public interface ITspEvaluateFitnessCommand : IDisposable
	{
		public NativeArray<float> Execute(Path[] generation);
	}
}