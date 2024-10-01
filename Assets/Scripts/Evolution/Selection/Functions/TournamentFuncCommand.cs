using System;

namespace Evolution.Selection.Functions
{
	public class TournamentFuncCommand : ISelectionCommand
	{
		private readonly int _parentsAmount;
		private readonly Random _random;

		public TournamentFuncCommand(int parentsAmount, Random random)
		{
			_parentsAmount = parentsAmount;
			_random = random;
		}

		public void Execute<T>(EvaluatedGenome<T>[] evaluatedGeneration, T[] parents)
			where T : IGenome<T>
		{
			var generationSize = evaluatedGeneration.Length;
		
			for (int i = 0; i < _parentsAmount; i++)
			{
				// simple binary tournament (2 participants)
				var sg1 = evaluatedGeneration[_random.Next(generationSize)];
				var sg2 = evaluatedGeneration[_random.Next(generationSize)];

				parents[i] = sg1.fitness > sg2.fitness ? sg1.genome : sg2.genome;

				// N participants
				// var tournamentParticipants = new List<ScoredGenome<T>>();
				//
				// for (int j = 0; j < 4; j++)
				// {
				// 	var participant = evaluatedGeneration[random.Next(generationSize)];
				// 	tournamentParticipants.Add(participant);
				// }
				//
				// var winner = tournamentParticipants
				// 	.OrderByDescending(ind => ind.fitness)
				// 	.First();
				//
				// parents.Add(winner.genome);
			}
		}

		public void Dispose()
		{
		}
	}
}