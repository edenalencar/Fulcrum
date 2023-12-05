using NAudio.Wave;
using System;

namespace Fulcrum.Bu
{
    public abstract class Reprodutor
    {
        public WaveOutEvent waveOut = new WaveOutEvent();

        // Defina o local do arquivo de som
        public string audioFile = String.Empty;

        // Crie um novo AudioFileReader
        public AudioFileReader reader;
        public void Play()
        {
            waveOut.Play();
        }
        public void Pause()
        {
            waveOut.Pause();
        }
        public void AlterarVolume(double volume) => reader.Volume = (float)volume;

    }

}
