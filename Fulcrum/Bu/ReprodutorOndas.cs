using NAudio.Wave;

namespace Fulcrum.Bu
{
    public class ReprodutorOndas : Reprodutor
    {
        public ReprodutorOndas()
        {
            audioFile = @"C:\Users\edena\Projetos\Fulcrum\Fulcrum\Assets\Sounds\ondas.wav";
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
        public override void AlterarVolume(double volume) => reader.Volume = (float)volume;

        public override void Parar()
        {
            waveOut.Stop();
        }
    }
}
