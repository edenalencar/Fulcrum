using NAudio.Wave;

namespace Fulcrum.Bu
{
    public class ReprodutorVentos : Reprodutor
    {
        public ReprodutorVentos()
        {
            audioFile = @"C:\Users\edena\Projetos\Fulcrum\Fulcrum\Assets\Sounds\ventos.wav";
            reader = new AudioFileReader(audioFile);
            reader.Volume = 0.0f;
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

