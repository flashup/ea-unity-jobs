using System;
using Unity.Collections;

namespace Function.FitnessEvaluation
{
	public interface IEvaluateFitnessCommand : IDisposable
	{
		public NativeArray<float> Execute(Point[] generation);
	}
}