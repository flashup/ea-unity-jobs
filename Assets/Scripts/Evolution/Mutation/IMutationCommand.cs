using System;
using Function;

namespace Evolution.Mutation
{
	public interface IMutationCommand<T> : IDisposable
		where T : struct, IComparable
	{
		public void Execute(T[] genomes);
	}
}