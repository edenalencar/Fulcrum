using NAudio.Wave;

namespace Fulcrum.Bu
{
    public class ReprodutorTrem : Reprodutor
    {
        public ReprodutorTrem()
        {
            reader = new AudioFileReader(ObterAudio("Assets\\Sounds\\trem.wav"));
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