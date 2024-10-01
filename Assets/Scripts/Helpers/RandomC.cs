public class RandomC
{
	private int _size;
	private int _counter;
	private double[] _cache;

	public RandomC()
	{
		_size = 10;
		_cache = new double[_size];
		
		var rand = new System.Random();

		for (int i = 0; i < _size; i++)
		{
			_cache[i] = rand.NextDouble();
		}
	}

	public RandomC(int seed)
	{
		_size = 1000;
		_cache = new double[_size];
		
		var rand = new System.Random(seed);

		for (int i = 0; i < _size; i++)
		{
			_cache[i] = rand.NextDouble();
		}
	}

	public double NextDouble()
	{
		return _cache[_counter++ % _size];
	}

	public virtual int Next(int maxValue)
	{
		return Next(0, maxValue);
	}

	public virtual int Next()
	{
		return Next(0, int.MaxValue);
	}

	public virtual int Next(int minValue, int maxValue)
	{
		return minValue + (int)(NextDouble() * (maxValue - minValue));
	}
}