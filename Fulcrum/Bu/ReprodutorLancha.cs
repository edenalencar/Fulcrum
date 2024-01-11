using NAudio.Wave;

namespace Fulcrum.Bu
{
    public class ReprodutorLancha : Reprodutor
    {
        public ReprodutorLancha()
        {
            audioFile = @"C:\Users\edena\Projetos\Fulcrum\Fulcrum\Assets\Sounds\lancha.mp3";
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
