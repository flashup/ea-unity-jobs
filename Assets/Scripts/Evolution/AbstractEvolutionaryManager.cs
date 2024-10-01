using System;
using System.Collections.Generic;
using Evolution.Selection.Functions;
using Evolution.Selection.Jobs;

// using Random = RandomC;

namespace Evolution
{
	public abstract class AbstractEvolutionaryManager<T> : IEvolutionaryManager<T>
		where T : IGenome<T>
	{
		protected readonly Random _random;
		protected readonly Queue<T[]> _generationCache = new();

		private SelectionMethod _selectionMethod;
		private bool _useJobsForSelection;

		private T[] _elites;
		private T[] _parents;

		private ISelectionCommand _selectElitesCommand;
		private ISelectionCommand _selectParentsCommand;

		protected AbstractEvolutionaryManager(int seed)
		{
			_random = new Random(seed);
		}

		public abstract T CreateGenome(bool empty = false);

		public virtual T[] CreateGeneration(int size)
		{
			return _generationCache.Count > 0 ? _generationCache.Dequeue() : new T[size];
		}

		public void CacheGeneration(T[] generation)
		{
			_generationCache.Enqueue(generation);
		}

		public void InitSelection(
			SelectionMethod selectionMethod, 
			int populationSize, 
			int elitesAmount,
			int parentsAmount, 
			bool useJobs = false)
		{
			_selectionMethod = selectionMethod;
			_useJobsForSelection = useJobs;

			_elites = new T[elitesAmount];
			_parents = new T[parentsAmount];

			_selectElitesCommand = new SelectElitesFunction();

			if (_useJobsForSelection)
			{
				_selectParentsCommand = _selectionMethod switch
				{
					SelectionMethod.Truncation => new TruncationJobCommand(parentsAmount),
					SelectionMethod.Tournament => new TournamentJobCommand(populationSize, parentsAmount, _random),
					SelectionMethod.Roulette => new RouletteJobCommand(populationSize, parentsAmount, _random),
					SelectionMethod.RankBased => new RankBasedJobCommand(populationSize, parentsAmount, _random),
					SelectionMethod.Sus => new SusJobCommand(populationSize, parentsAmount, _random),
					_ => throw new ArgumentOutOfRangeException()
				};
			}
			else
			{
				_selectParentsCommand = _selectionMethod switch
				{
					SelectionMethod.Truncation => new TruncationFuncCommand(parentsAmount),
					SelectionMethod.Tournament => new TournamentFuncCommand(parentsAmount, _random),
					SelectionMethod.Roulette => new RouletteFuncCommand(parentsAmount, _random),
					SelectionMethod.RankBased => new RankBasedFuncCommand(parentsAmount, _random),
					SelectionMethod.Sus => new SusFuncCommand(parentsAmount, _random),
					_ => throw new ArgumentOutOfRangeException()
				};
			}
		}

		public virtual (T[] elites, T[] parents) PerformSelection(
			SelectionMethod method,
			EvaluatedGenome<T>[] evaluatedGeneration
		)
		{
			_selectElitesCommand.Execute(evaluatedGeneration, _elites);
			_selectParentsCommand.Execute(evaluatedGeneration, _parents);

			return (_elites, _parents);
		}

		public abstract void InitCrossover(int crossoverPercent, int numParents, int numChildren, bool useJobs);
		public abstract void InitMutation(int mutationPercent, int numChildren, bool useJobs);

		public abstract T[] PerformCrossover(int requiredChildren, T[] parents);

		public abstract void PerformMutation(T[] genomes);

		public virtual void Dispose()
		{
			_selectElitesCommand?.Dispose();
			_selectParentsCommand?.Dispose();
		}
	}
}