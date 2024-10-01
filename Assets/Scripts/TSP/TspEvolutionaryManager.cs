using System.Collections.Generic;
using System.Linq;
using Evolution;
using Evolution.Crossover;
using Evolution.Crossover.Functions;
using Evolution.Crossover.Jobs;
using Evolution.Mutation;
using Evolution.Mutation.Functions;
using Evolution.Mutation.Jobs;
using Helpers;

namespace TSP
{
	public class TspEvolutionaryManager : AbstractEvolutionaryManager<Path>
	{
		private readonly int _startCityId;
		private readonly int[] _citiesIds;
		private readonly int _fullRoute;
		private readonly int _dynamicRoute; // = genomeSize

		private ICrossoverCommand<int> _crossoverCommand;
		private IMutationCommand<int> _mutationCommand;

		public TspEvolutionaryManager(int randomSeed, List<City> allCities) : base(randomSeed)
		{
			Path.ResetCounter();

			// cities: 1, 2, 3, 4 => dyn route: 2, 3, 4 => full route: 1, 2, 3, 4, 1

			_startCityId = allCities[0].id;
			_citiesIds = allCities.Skip(1).Select(c => c.id).ToArray();

			_fullRoute = allCities.Count + 1;
			_dynamicRoute = allCities.Count - 1;
		}

		public override Path CreateGenome(bool empty = false)
		{
			var cities = new int[_fullRoute];

			cities[0] = cities[^1] = _startCityId;

			if (!empty)
			{
				Shuffle(_citiesIds);

				for (int i = 0; i < _citiesIds.Length; i++)
				{
					cities[i + 1] = _citiesIds[i];
				}
			}

			return new Path(cities);
		}

		public override void InitCrossover(int crossoverPercent, int numParents, int numChildren, bool useJobs)
		{
			_crossoverCommand = useJobs 
				? new OrderCrossoverJobCommand(crossoverPercent, _random, numParents, numChildren, _dynamicRoute)
				: new OrderCrossoverFuncCommand(crossoverPercent, _random, numParents, numChildren, _dynamicRoute);
		}

		public override void InitMutation(int mutationPercent, int numChildren, bool useJobs)
		{
			_mutationCommand = useJobs
				? new RandomShiftJobCommand(mutationPercent, _random, numChildren, _dynamicRoute)
				: new RandomShiftFuncCommand<int>(mutationPercent, _random, numChildren, _dynamicRoute);
		}

		/**
		 * Partially Mapped Crossover
		 */
		public override Path[] PerformCrossover(int requiredChildren, Path[] parents)
		{
			var parentsGenome = new int[parents.Length * _dynamicRoute];
			var childrenGenome = new int[requiredChildren * _dynamicRoute];

			// var childrenGenome = _crossoverCommand.Parents;
			// var parentsGenome = _crossoverCommand.Children;

			for (int i = 0; i < parents.Length; i++)
			{
				for (var j = 0; j < _dynamicRoute; j++)
					parentsGenome[i * _dynamicRoute + j] = parents[i].cities[j + 1];
			}

			_crossoverCommand.Execute(parentsGenome, childrenGenome);

			var children = new Path[requiredChildren];
			for (var i = 0; i < requiredChildren; i++)
			{
				var child = CreateGenome(true);

				for (var j = 0; j < _dynamicRoute; j++)
					child.cities[j + 1] = childrenGenome[_dynamicRoute * i + j];

				children[i] = child;
			}

			return children;
		}

		public override void PerformMutation(Path[] genomes)
		{
			var allGenomes = new int[genomes.Length * _dynamicRoute];

			for (int i = 0; i < genomes.Length; i++)
			{
				for (var j = 0; j < _dynamicRoute; j++)
					allGenomes[i * _dynamicRoute + j] = genomes[i].cities[j + 1];
			}
			
			_mutationCommand.Execute(allGenomes);
			
			for (int i = 0; i < genomes.Length; i++)
			{
				for (var j = 0; j < _dynamicRoute; j++)
					genomes[i].cities[j + 1] = allGenomes[i * _dynamicRoute + j];
			}
		}

		private void Shuffle<T>(T[] array)
		{
			var count = array.Length;
			var last = count - 1;
			for (var i = 0; i < last; ++i)
			{
				var r = _random.Next(i, count);
				(array[i], array[r]) = (array[r], array[i]);
			}
		}
		
		
		public override void Dispose()
		{
			base.Dispose();
			
			_crossoverCommand?.Dispose();
		}
	}

	public struct Path : IGenome<Path>
	{
		public int ID { get; }

		public readonly int[] cities;

		private static int _idCounter;

		public Path(int[] cities)
		{
			ID = ++_idCounter;
			this.cities = cities;
		}

		public Path Copy()
		{
			return new Path(cities);;
		}

		public bool Equals(Path otherGenome)
		{
			return cities.SequenceEqual(otherGenome.cities);
		}

		public override string ToString()
		{
			return $"Id: {ID}, Path: {cities.Join(",")}";
		}

		public static void ResetCounter()
		{
			_idCounter = 0;
		}
	}
}