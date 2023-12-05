using NAudio.Wave;

namespace Fulcrum.Bu
{
    public class ReprodutorCafeteria : Reprodutor
    {
        public ReprodutorCafeteria()
        {
            audioFile = @"C:\Users\edena\Projetos\Fulcrum\Fulcrum\Assets\Sounds\cafeteria.wav";
            reader = new AudioFileReader(audioFile);
            reader.Volume = 0.0f;
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