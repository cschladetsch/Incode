using System;
using System.Collections.Generic;
using System.Windows.Forms;
using NAudio.Wave;

namespace IncodeWindow {
    internal class Audio {
        private Dictionary<Keys, BufferedWaveProvider> _sounds = new Dictionary<Keys, BufferedWaveProvider>();
        private const float SemiToneFactor = 1.05946309436f;

        public Audio() {
            GenerateSounds();
        }

        void GenerateSounds() {
            Keys[] keys = new[] { Keys.A, Keys.S, Keys.D, Keys.F };

            var frequency = 55.0f; // basw freqnecy of A1
            var seconds = 0.300f;
            var sampleRate = 44 * 1000;
            for (int i = 0; i < keys.Length; i++) {
                _sounds.Add(keys[i], GenerateWaveForm(keys, frequency, seconds, sampleRate, i));
                frequency *= SemiToneFactor;
            }
        }

        private BufferedWaveProvider GenerateWaveForm(Keys[] keys, float frequency, float seconds, int sampleRate, int i) {
            var wave = new BufferedWaveProvider(new WaveFormat(sampleRate, 1));
            wave.BufferLength = (int)(sampleRate / seconds);
            byte[] bytes = GenerateSignWave(frequency, seconds, sampleRate);
            wave.AddSamples(bytes, 0, bytes.Length);
            return wave;
        }

        private byte[] GenerateSignWave(float frequency, float seconds, float sampleRate) {
            int numSamples = (int)(sampleRate * seconds);
            byte[] values = new byte[2 * numSamples];
            double increment = 2.0 * Math.PI * frequency / sampleRate;
            short amplitude = 16384; // 16-bit PCM amplitude for maximum volume
            for (int n = 0; n < numSamples; n++) {
                short sampleValue = (short)(amplitude * Math.Sin(increment * n));
                byte[] sampleBytes = BitConverter.GetBytes(sampleValue);

                values[n * 2] = sampleBytes[0];         // Lower byte (little-endian)
                values[n * 2 + 1] = sampleBytes[1];     // Upper byte (little-endianvalues[n]
            }
            return values;
        }

        public void KeyDown(KeyEventArgs key) {
            if (!_sounds.TryGetValue(key.KeyCode, out var sound)) {
                return;
            }

            var output = new WaveOut();
            output.Init(sound);
            output.Play();
        }
    }
}

