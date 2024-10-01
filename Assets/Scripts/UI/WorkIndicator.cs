using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class WorkIndicator : MonoBehaviour
	{
		[SerializeField] private Image _indicator;

		private float _maxY;
		private bool _isMovingUp;
		private float _speed = 0.5f;

		private void Start()
		{
			var rect = GetComponent<RectTransform>().rect;
			_maxY = rect.height / 2 - _indicator.rectTransform.rect.height / 2;
		}

		private void Update()
		{
			var pos = _indicator.transform.localPosition;

			pos += Vector3.up * (_isMovingUp ? 1 : -1) * _speed;

			if (_isMovingUp && pos.y > _maxY)
			{
				pos.y = _maxY;
				_isMovingUp = !_isMovingUp;
			}
			else if (!_isMovingUp && pos.y < -_maxY)
			{
				pos.y = -_maxY;
				_isMovingUp = !_isMovingUp;
			}

			_indicator.transform.localPosition = pos;
		}
	}
}