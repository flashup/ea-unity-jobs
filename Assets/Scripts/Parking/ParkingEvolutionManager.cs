using Data;
using Evolution;
using Evolution.Crossover;
using Evolution.Crossover.Functions;

namespace Parking
{
	public class ParkingEvolutionManager : AbstractEvolutionaryManager<CarGenome>
	{
		private IEvolutionaryManager<CarGenome> _evolutionManagerImplementation;

		private ICrossoverCommand<byte> _crossoverCommand;
		private float _crossoverRate;
		private float _mutationRate;

		private readonly int _genomeLength;

		public ParkingEvolutionManager(int randomSeed, int genomeLength) : base(randomSeed)
		{
			_genomeLength = genomeLength;

			CarGenome.ResetCounter();
		}

		public override CarGenome CreateGenome(bool empty = false)
		{
			var genes = new Bit[_genomeLength];

			if (!empty)
			{
				for (int i = 0; i < _genomeLength; i++)
					genes[i] = (Bit)(_random.NextDouble() < 0.5f ? 0 : 1);
			}

			return new CarGenome(genes);
		}

		public override void InitCrossover(int crossoverPercent, int numParents, int numChildren, bool useJobs)
		{
			// _crossoverCommand = new RandomCrossoverFuncCommand<byte>(crossoverPercent, _random, numParents, numChildren, _genomeLength);
			_crossoverCommand = new OnePointCrossoverFuncCommand<byte>(crossoverPercent, _random, numParents, numChildren, _genomeLength);
		}

		public override void InitMutation(int mutationPercent, int numChildren, bool useJobs)
		{
			_mutationRate = mutationPercent / 100f;
		}

		/**
		 * Binary random respectful crossover
		 */
		public override CarGenome[] PerformCrossover(int requiredChildren, CarGenome[] parents)
		{
			var parentsGenome = new byte[parents.Length * _genomeLength];
			var childrenGenome = new byte[requiredChildren * _genomeLength];

			for (int i = 0; i < parents.Length; i++)
			{
				for (var j = 0; j < _genomeLength; j++)
					parentsGenome[i * _genomeLength + j] = (byte)parents[i].genes[j];
			}

			_crossoverCommand.Execute(parentsGenome, childrenGenome);

			var children = new CarGenome[requiredChildren];
			for (var i = 0; i < requiredChildren; i++)
			{
				var child = CreateGenome(true);

				for (var j = 0; j < _genomeLength; j++)
					child.genes[j] = childrenGenome[_genomeLength * i + j];

				children[i] = child;
			}

			return children;
		}

		public override void PerformMutation(CarGenome[] genomes)
		{
			foreach (var genome in genomes)
			{
				var genes = genome.genes;

				for (var geneIndex = 0; geneIndex < genes.Length; geneIndex++)
				{
					var gene = genes[geneIndex];
					if (_random.NextDouble() < _mutationRate)
						genes[geneIndex] = (Bit)(gene == 0 ? 1 : 0);
				}
			}
		}
		
		
		public override void Dispose()
		{
			base.Dispose();
			
			_crossoverCommand?.Dispose();
		}
	}

	public class CarGenome : IGenome<CarGenome>
	{
		public int ID { get; }

		public readonly Bit[] genes;

		public DecodedGenome DecodedGenome { get; set; }

		private static int _idCounter;

		public CarGenome(Bit[] genes)
		{
			ID = ++_idCounter;
			this.genes = genes;
		}

		public CarGenome Copy()
		{
			return new CarGenome(genes);
		}

		public bool Equals(CarGenome otherGenome)
		{
			var otherGenes = otherGenome.genes;
			for (int i = 0; i < genes.Length; i++)
			{
				if (genes[i] != otherGenes[i]) return false;
			}

			return true;
		}

		public static void ResetCounter()
		{
			_idCounter = 0;
		}
	}
}