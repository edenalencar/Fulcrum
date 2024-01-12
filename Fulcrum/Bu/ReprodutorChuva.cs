using NAudio.Wave;

namespace Fulcrum.Bu
{
    public class ReprodutorChuva : Reprodutor
    {
        public ReprodutorChuva()
        {
            audioFile = @"C:\Users\edena\Projetos\Fulcrum\Fulcrum\Assets\Sounds\chuva forte.wav";
            reader = new AudioFileReader(audioFile);
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
