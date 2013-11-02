using System;

namespace IncodeWindow
{
	internal class LowPass
	{
		private readonly float[] input = new float[3];
		private readonly float[] output = new float[3];
		private readonly float a1, a2, a3;
		private readonly float b1, b2;

		private LowPass()
		{
		}

		public LowPass(float sampleRate, float freq, float resonance)
		{
			var c = 1.0f/(float) Math.Tan(3.1415f*freq*sampleRate);

			a1 = 1.0f/(1.0f + resonance*c + c*c);
			a2 = 2.0f*a1;
			a3 = a1;

			b1 = 2.0f*(1.0f - c*c)*a1;
			b2 = (1.0f - resonance*c + c*c)*a1;
		}

		public float Next(float val)
		{
			input[2] = input[1];
			input[1] = input[0];
			input[0] = val;
			output[2] = output[1];
			output[1] = output[0];

			return output[0] = a1*input[0] + a2*input[1] + a3*input[2] - b1*output[1] - b2*output[2];
		}

		public void Set(float x)
		{
			input[0] = input[1] = input[2] = x;
			output[0] = output[1] = output[2] = x;
		}
	}
}
