using DG.Tweening;
using UnityEngine;

namespace Parking.View
{
	public class Car : MonoBehaviour
	{
		public bool useMomentum;
		public bool showSensorRays;

		public CarSensors sensors;

		[Header("Engine")]
		public float maxSpeed = 3f;
		public float speedSlowing = 0.99f;
		public float speedIncreasing = 0.02f;
		
		[SerializeField] private float _currentSpeed = 0;

		[Header("Wheel")]
		public float maxSteeringSpeed = 20f;
		public float steeringSpeedSlowing = 0.98f;
		public float steeringSpeedIncreasing = 0.1f;
		
		[SerializeField] private float _currentSteeringSpeed = 0;
		
		private int _accelerationInput;
		private int _steeringInput;

		private bool _hitsCounterDisabled;

		public int Hits { get; private set; } = 0;
		public int Path { get; private set; } = 0;

		public double Fitness { get; set; }

		private Rigidbody2D _rb;

		public SpriteRenderer SpriteRenderer { get; private set; }

		private CarGenome _genome;
		public CarGenome Genome => _genome;

		private Color _color = Color.white;
		private Color _disabledColor = new Color32(100, 100, 100, 100);

		public bool Highlighted { get; private set; }

		public Vector3 StartPosition { get; private set; }

		public int ID => _genome?.ID ?? default;

		private void Awake()
		{
			_color = Color.white;

			_rb = GetComponent<Rigidbody2D>();
			_rb.centerOfMass = new Vector2(); // set pivot point as a rotation center

			SpriteRenderer = GetComponent<SpriteRenderer>();
		}

		private void Start()
		{
			sensors.ShowRays(showSensorRays);
		}

		public void SetGenome(CarGenome genome)
		{
			_genome = genome;
			_genome.DecodedGenome ??= LpEncoding.DecodeGenome(_genome.genes);

			gameObject.name = $"Car_{genome.ID}";

			StartPosition = transform.position;
		}

		public void SetInput((int engineSignal, int wheelSignal) input)
		{
			var (engineSignal, wheelSignal) = input;
			
			if (!_accelerationInput.Equals(engineSignal))
				_accelerationInput = engineSignal;

			_steeringInput = wheelSignal;

			if (_accelerationInput != 0)
				Path++;
		}

		public void StartCar()
		{
			stopped = false;
			_rb.simulated = true;
			_rb.WakeUp();
		}

		public void Stop()
		{
			_rb.velocity = Vector2.zero;
			_rb.angularVelocity = 0f;
			_rb.inertia = 0;
			_rb.simulated = false;
			_rb.Sleep();

			_currentSpeed = 0;
			_currentSteeringSpeed = 0;

			SetInput((0, 0));
			
			_color = Color.white;
			Highlighted = false;
			
			SpriteRenderer.sortingOrder = 0;

			stopped = true;
		}

		public bool stopped = true;

		public void FixedUpdateCar()
		{
			if (stopped) return;

			sensors.UpdateSensors();

			if (useMomentum)
			{
				// change speed values
				if (_accelerationInput == 0)
					_currentSpeed *= speedSlowing;
				else
					_currentSpeed = Mathf.Clamp(_currentSpeed + _accelerationInput * speedIncreasing, -maxSpeed, maxSpeed);
			
				if (_steeringInput == 0)
					_currentSteeringSpeed *= steeringSpeedSlowing;
				else
					_currentSteeringSpeed = Mathf.Clamp(_currentSteeringSpeed + _steeringInput * steeringSpeedIncreasing, -maxSteeringSpeed, maxSteeringSpeed);
				
				// apply physic forces
				_rb.MovePosition(_rb.position +
				                 (Vector2)transform.up * _currentSpeed * 0.02f);

				var rotationAmount = _currentSteeringSpeed;
				_rb.MoveRotation(_rb.rotation - rotationAmount * (_currentSpeed / maxSpeed));
			}
			else
			{
				// OLD
				// _rb.velocity = transform.up * _accelerationInput * maxSpeed;
				_rb.MovePosition(_rb.position +
				                 (Vector2)transform.up * _accelerationInput * maxSpeed * 0.02f);
				var rotationAmount = _steeringInput * maxSteeringSpeed;
				_rb.MoveRotation(_rb.rotation - rotationAmount * _accelerationInput);
			}
		}

		private void OnCollisionEnter2D(Collision2D collision)
		{
			if (collision.gameObject.CompareTag("Obstacle"))
			{
				var obstacle = collision.gameObject.GetComponent<Obstacle>();
				if (obstacle != null)
				{
					if (sensors.Visible) obstacle.Hit(true);

					if (!_hitsCounterDisabled) Hits++;

					Hit(true);
				}
			}
		}

		private void OnCollisionExit2D(Collision2D collision)
		{
			if (collision.gameObject.CompareTag("Obstacle"))
			{
				var obstacle = collision.gameObject.GetComponent<Obstacle>();
				if (obstacle != null)
				{
					if (sensors.Visible) obstacle.Hit(false);
					
					Hit(false);
				}
			}
		}
		
		private void Hit(bool value)
		{
			SpriteRenderer.color = value ? Color.red : _color;

			if (!Highlighted) SpriteRenderer.color = _disabledColor;

			// if (Highlighted && Id != 1)
			// {
			// 	Debug.Log($"Car {Id} HIT!");
			// }
		}

		public void Highlight(int index, Color color, bool flick = false)
		{
			Highlighted = true;

			_color = color;

			if (!flick)
				SpriteRenderer.color = _color;
			else
				SpriteRenderer.DOColor(_color, 0.2f)
					.SetLoops(4, LoopType.Yoyo)
					.SetEase(Ease.InOutQuad)
					.SetUpdate(true);

			SpriteRenderer.sortingOrder = index + 100;
			gameObject.name = $"Car_{ID} (BEST)";
		}

		public void DisableHitsCounter()
		{
			_hitsCounterDisabled = true;
		}
	}
}