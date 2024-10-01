using System;
using UnityEngine;

namespace Parking.View
{
	[RequireComponent(typeof(Car))]
	public class CarManualController : MonoBehaviour
	{
		private Car _car;

		private void Awake()
		{
			_car = GetComponent<Car>();
			_car.DisableHitsCounter();
		}

		private void Update()
		{
			var input = (
				(int)Input.GetAxisRaw("Vertical"),
				(int)Input.GetAxisRaw("Horizontal")
			);
			_car.SetInput(input);
		}
	}
}