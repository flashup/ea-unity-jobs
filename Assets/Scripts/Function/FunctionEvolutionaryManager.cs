using System.Linq;
using Evolution;
using Evolution.Crossover.Functions;
using Evolution.Crossover.Jobs;
using Evolution.Crossover;
using Evolution.Mutation;
using Evolution.Mutation.Functions;
using Evolution.Mutation.Jobs;
using Helpers;

namespace Function
{
	public class FunctionEvolutionaryManager : AbstractEvolutionaryManager<Point>
	{
		private readonly float _xMin;
		private readonly float _xMax;
		private readonly float _xRange;
		private readonly int _dimensions;
		
		private ICrossoverCommand<float> _crossoverCommand;
		private IMutationCommand<float> _mutationCommand;

		public FunctionEvolutionaryManager(int randomSeed, int dimensions, float xMin, float xMax) 
			: base(randomSeed)
		{
			Point.ResetCounter();

			_dimensions = dimensions;
			_xMin = xMin;
			_xMax = xMax;
			_xRange = _xMax - _xMin;
		}

		private float RandomCoordinateValue => _xMin + (float)_random.NextDouble() * _xRange;

		public override Point CreateGenome(bool empty = false)
		{
			var coordinates = new float[_dimensions];

			if (!empty)
			{
				for (var i = 0; i < _dimensions; i++)
					coordinates[i] = RandomCoordinateValue;
			}

			return new Point(coordinates);
		}

		public override void InitCrossover(int crossoverPercent, int numParents, int numChildren, bool useJobs)
		{
			_crossoverCommand = useJobs 
				? new OnePointCrossoverJobCommand(crossoverPercent, _random, numParents, numChildren, _dimensions)
				: new OnePointCrossoverFuncCommand<float>(crossoverPercent, _random, numParents, numChildren, _dimensions);
			// _crossoverCommand = useJobs 
			// 	? new RandomCrossoverJobCommand(crossoverPercent, _random, numParents, numChildren, _dimensions)
			// 	: new RandomCrossoverFuncCommand<float>(crossoverPercent, _random, numParents, numChildren, _dimensions);
		}

		public override void InitMutation(int mutationPercent, int numGenomes, bool useJobs)
		{
			_mutationCommand = useJobs
				? new RandomFloatJobCommand(mutationPercent, _random, _xMin, _xMax, numGenomes, _dimensions)
				: new RandomFloatFuncCommand(mutationPercent, _random, _xMin, _xMax);
		}

		/**
		 * 1 Point Crossover
		 */
		public override Point[] PerformCrossover(int requiredChildren, Point[] parents)
		{
			// TODO cache arrays in crossover
			var childrenGenome = new float[requiredChildren * _dimensions];
			var parentsGenome = new float[parents.Length * _dimensions];

			// var childrenGenome = _crossoverCommand.Parents;
			// var parentsGenome = _crossoverCommand.Children;

			for (int i = 0; i < parents.Length; i++)
			{
				for (var j = 0; j < _dimensions; j++)
					parentsGenome[i * _dimensions + j] = parents[i].coordinates[j];
			}

			_crossoverCommand.Execute(parentsGenome, childrenGenome);

			var children = new Point[requiredChildren];
			for (var i = 0; i < requiredChildren; i++)
			{
				var child = CreateGenome(true);

				for (var j = 0; j < _dimensions; j++)
					child.coordinates[j] = childrenGenome[_dimensions * i + j];

				children[i] = child;
			}

			return children;
		}

		public override void PerformMutation(Point[] genomes)
		{
			var allGenomes = new float[genomes.Length * _dimensions];

			for (int i = 0; i < genomes.Length; i++)
			{
				for (var j = 0; j < _dimensions; j++)
					allGenomes[i * _dimensions + j] = genomes[i].coordinates[j];
			}

			_mutationCommand.Execute(allGenomes);

			for (int i = 0; i < genomes.Length; i++)
			{
				for (var j = 0; j < _dimensions; j++)
					genomes[i].coordinates[j] = allGenomes[i * _dimensions + j];
			}
		}

		public override void Dispose()
		{
			base.Dispose();
			
			_crossoverCommand?.Dispose();
			_mutationCommand?.Dispose();
		}
	}

	public struct Point : IGenome<Point>
	{
		public int ID { get; private set; }

		public readonly float[] coordinates;

		private static int _idCounter;

		public Point(float[] coordinates)
		{
			ID = ++_idCounter;
			this.coordinates = coordinates;
		}

		public Point Copy()
		{
			return new Point(coordinates);
		}

		public bool Equals(Point otherGenome)
		{
			return coordinates.SequenceEqual(otherGenome.coordinates);
		}

		public override string ToString()
		{
			return $"({coordinates.Join(", ")})";
		}
		
		public static void ResetCounter()
		{
			_idCounter = 0;
		}
	}
}