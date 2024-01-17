using NAudio.Wave;
using System;

namespace Fulcrum.Bu
{
    public abstract class Reprodutor
    {
        public WaveOutEvent waveOut;

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

        public string ObterAudio(string urlaudio)
        {
            var localizacaoDoExecutavel = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var diretorioDoExecutavel = System.IO.Path.GetDirectoryName(localizacaoDoExecutavel);
            var caminhoDoAudio = System.IO.Path.Combine(diretorioDoExecutavel, @urlaudio);
            return caminhoDoAudio;
        }

    }

}
