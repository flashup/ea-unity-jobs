using System;

namespace Evolution
{
	public interface IEvolutionaryManager<T> where T : IGenome<T>
	{
		T CreateGenome(bool empty = false);

		T[] CreateGeneration(int size);
		void CacheGeneration(T[] generation);

		// @see: https://en.wikipedia.org/wiki/Selection_(genetic_algorithm)
		(T[] elites, T[] parents) PerformSelection(
			SelectionMethod method,
			EvaluatedGenome<T>[] evaluatedGeneration
		);

		// @see: https://en.wikipedia.org/wiki/Crossover_(genetic_algorithm)
		T[] PerformCrossover(int requiredChildren, T[] parents);

		// @see: https://en.wikipedia.org/wiki/Mutation_(genetic_algorithm)
		void PerformMutation(T[] genomes);

		void Dispose();
	}

	public class EvaluatedGenome<T>
		where T : IGenome<T>
	{
		public T genome;
		public float fitness;
	}

	public static class EvaluatedGenomeHelper
	{
		public static int SortDescending<T>(EvaluatedGenome<T> a, EvaluatedGenome<T> b)
			where T : IGenome<T>
		{
			return b.fitness.CompareTo(a.fitness);
		}
	}

	public interface IGenome<T> where T : IGenome<T>
	{
		int ID { get; }

		T Copy();

		bool Equals(T otherGenome);
	}
	
	public enum SelectionMethod
	{
		Truncation,
		Tournament,
		Roulette,
		RankBased,
		Sus,
	}
}