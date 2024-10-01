using System;
using TMPro;
using UnityEngine;

namespace Parking.View
{
	public class CarLabels : MonoBehaviour
	{
		[SerializeField] private TextMeshProUGUI _fitnessTxt;

		private Car _carToWatch;

		public Car CarToWatch => _carToWatch;

		private void Start()
		{
			_fitnessTxt.text = "";
		}

		public void WatchCar(Car car)
		{
			_carToWatch = car;

			_fitnessTxt.gameObject.SetActive(_carToWatch != null);
		}

		private void Update()
		{
			if (_carToWatch == null) return;

			_fitnessTxt.text = $"Fit:{_carToWatch.Fitness:F3}, Hits:{_carToWatch.Hits}";

			var carPos = (Vector2)_carToWatch.transform.position;
			_fitnessTxt.transform.position = carPos;
		}
	}
}