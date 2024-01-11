using System.Collections.Generic;

namespace Fulcrum.Bu
{
    public sealed class AudioManager
    {
        private static AudioManager instance;
        private Dictionary<string, Reprodutor> audioPlayers = new Dictionary<string, Reprodutor>();

        private AudioManager() { }

        public static AudioManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AudioManager();
                }
                return instance;
            }
        }

        public void AddAudioPlayer(string id, Reprodutor player)
        {
            audioPlayers.Add(id, player);
        }
        public Reprodutor GetReprodutorPorId(string id)
        {
            return audioPlayers[id];
        }

        public void PauseAll()
        {
            foreach (var player in audioPlayers.Values)
            {
                player.Pause();
            }
        }

        public void PlayAll()
        {
            foreach (var player in audioPlayers.Values)
            {
                player.Play();
            }
        }

        public void AlterarVolume(string id, double volume)
        {
            GetReprodutorPorId(id).AlterarVolume(volume);
        }
        public int GetQuantidadeReprodutor => audioPlayers.Count;

        public Dictionary<string, Reprodutor> GetListReprodutores() => audioPlayers;


    }

}
