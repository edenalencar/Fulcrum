using NAudio.Wave;

namespace Fulcrum.Bu
{
    public class ReprodutorPraia : Reprodutor
    {
        public ReprodutorPraia()
        {
            reader = new AudioFileReader(ObterAudio("Assets\\Sounds\\manhã-praia.wav"));
            reader.Volume = 0.0f;
            var fadeOut = new DelayFadeOutSampleProvider(reader);
            fadeOut.BeginFadeOut(10000, 1000);
            waveOut = new WaveOutEvent();
            waveOut.Init(reader);
            waveOut.Play();
            waveOut.PlaybackStopped += (s, e) =>
            {
                reader.Position = 0; // Reinicie o áudio do início
                waveOut.Play(); // Comece a tocar novamente
            };
        }
    }
}
