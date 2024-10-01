using UnityEngine;

namespace Parking.View
{
	public class CarSensors : MonoBehaviour
	{
		public float sensorLength = 5f;
		public LayerMask obstacleLayer;

		public BoxCollider2D target;

		public SpriteRenderer rayPrefab;

		private const int NUM_SENSORS = 8;

		public float[] SensorData { get; } = new float[NUM_SENSORS];

		private readonly Vector2[] _directions = new Vector2[NUM_SENSORS];
		private readonly Vector2[] _origins = new Vector2[NUM_SENSORS];

		private readonly SpriteRenderer[] _rays = new SpriteRenderer[NUM_SENSORS];

		private readonly RaycastHit2D[] _hitResults = new RaycastHit2D[1];

		public bool Visible { get; private set; }

		public void ShowRays(bool value)
		{
			Visible = value;

			if (Visible)
				CreateRays();
			else
				enabled = false;
		}

		private void CreateRays()
		{
			for (int i = 0; i < NUM_SENSORS; i++)
			{
				var ray = Instantiate(rayPrefab, transform);
				_rays[i] = ray;
			}
		}

		private void Update()
		{
			if (!Visible) return;

			for (var i = 0; i < SensorData.Length; i++)
			{
				var sensor = SensorData[i];
				DrawRay(
					_origins[i],
					_directions[i] * sensor,
					sensor < 0.4f ? Color.red : (sensor == sensorLength ? Color.green : Color.yellow),
					i
				);

				// Debug.DrawRay(
				// 	_origins[i],
				// 	_directions[i] * sensor,
				// 	sensor < 0.4f ? Color.red : (sensor == sensorLength ? Color.green : Color.yellow)
				// );
			}
		}

		public void UpdateSensors()
		{
			var directions = new (Vector2 direction, int diagSignX, int diagSignY)[]
			{
				(transform.up, 0, 0),
				(transform.up + transform.right, 1, 1),
				(transform.right, 0, 0),
				(transform.right - transform.up, 1, -1),
				(-transform.up, 0, 0),
				(-transform.up - transform.right, -1, -1),
				(-transform.right, 0, 0),
				(-transform.right + transform.up, -1, 1)
			};

			for (var i = 0; i < directions.Length; i++)
			{
				var dir = directions[i];
				CastSensor(dir, i);
			}
		}

		private void CastSensor((Vector2 direction, int diagSignX, int diagSignY) sensorData, int sensorIndex)
		{
			var (direction, _, _) = sensorData;

			var origin = GetRayOrigin(sensorData, sensorIndex);
			var end = origin + direction.normalized * sensorLength;

			_origins[sensorIndex] = origin;
			_directions[sensorIndex] = direction.normalized;

			var hitCount = Physics2D.LinecastNonAlloc(origin, end, _hitResults, obstacleLayer);
			SensorData[sensorIndex] = hitCount > 0 ? _hitResults[0].distance : sensorLength;
		}

		private void DrawRay(Vector2 startPosition, Vector2 direction, Color color, int index)
		{
			var ray = _rays[index];
			ray.color = color;
			var endPosition = startPosition + direction;

			var midpoint = (startPosition + endPosition) / 2f;
			ray.transform.position = midpoint;

			var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
			ray.transform.rotation = Quaternion.Euler(0, 0, angle);

			var distance = direction.magnitude;
			ray.transform.localScale = new Vector3(distance, ray.transform.localScale.y, ray.transform.localScale.z);
		}

		private Vector2 GetRayOrigin((Vector3 direction, int diagSignX, int diagSignY) sensorData, int sensorIndex)
		{
			var (direction, diagSignX, diagSignY) = sensorData;

			var offset = target.offset;
			var halfSize = target.size * 0.5f;

			Vector2 localOffset;

			if (direction == transform.up)
				localOffset = new Vector2(0, halfSize.y);
			else if (direction == -transform.up)
				localOffset = new Vector2(0, -halfSize.y);
			else if (direction == transform.right)
				localOffset = new Vector2(halfSize.x, 0);
			else if (direction == -transform.right)
				localOffset = new Vector2(-halfSize.x, 0);
			else 
				localOffset = new Vector2(halfSize.x * diagSignX, halfSize.y * diagSignY);

			Vector2 rotatedOffset = transform.TransformDirection(localOffset + offset);

			return (Vector2)transform.position + rotatedOffset;
		}
	}
}