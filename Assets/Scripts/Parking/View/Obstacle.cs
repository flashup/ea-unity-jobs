using System;
using UnityEngine;

namespace Parking.View
{
	public class Obstacle : MonoBehaviour
	{
		private SpriteRenderer _spriteRenderer;
		private Color _color;

		private void Awake()
		{
			_spriteRenderer = GetComponent<SpriteRenderer>();
			_color = _spriteRenderer.color;
		}

		public void Hit(bool value)
		{
			_spriteRenderer.color = value ? Color.red : _color;
		}
	}
}