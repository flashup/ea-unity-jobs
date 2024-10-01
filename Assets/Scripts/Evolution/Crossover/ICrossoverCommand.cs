using System;
using Unity.Collections;

namespace Evolution.Crossover
{
	public interface ICrossoverCommand<T> : IDisposable
		where T : struct, IComparable
	{
		// public NativeArray<float> Parents { get; }
		// public NativeArray<float> Children { get; }

		public void Execute(T[] parentsGenome, T[] childrenGenome);
	}
}