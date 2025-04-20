namespace Fulcrum.Util;

/// <summary>
/// Classe de constantes utilizadas no aplicativo
/// </summary>
public static class Constantes
{
    /// <summary>
    /// Constantes para os IDs dos sons
    /// </summary>
    public static class Sons
    {
        public const string Chuva = "chuva";
        public const string Fogueira = "fogueira";
        public const string Ondas = "ondas";
        public const string Passaros = "passaros";
        public const string Praia = "praia";
        public const string Trem = "trem";
        public const string Ventos = "ventos";
        public const string Cafeteria = "cafeteria";
        public const string Lancha = "lancha";
    }

    /// <summary>
    /// Constantes relacionadas ao tema da aplicação
    /// </summary>
    public static class Tema
    {
        /// <summary>
        /// Chave para armazenar o tema selecionado nas configurações
        /// </summary>
        public const string TemaAppSelecionado = "TemaAplicativoSelecionado";

        /// <summary>
        /// Tema claro
        /// </summary>
        public const string Light = "Light";

        /// <summary>
        /// Tema escuro
        /// </summary>
        public const string Dark = "Dark";

        /// <summary>
        /// Tema padrão do sistema
        /// </summary>
        public const string Default = "Default";

        public const string Claro = "Claro";
        public const string Escuro = "Escuro";
        public const string UsarTemaPadra = "Usar configuração do sistema";
    }

    // Configuração geral
    public static class Config
    {
        public const string Sobre = "Sobre";
        public const string Configuracoes = "Configurações";
    }

    /// <summary>
    /// Constantes para perfis
    /// </summary>
    public static class Perfis
    {
        /// <summary>
        /// Chave para o perfil ativo nas configurações
        /// </summary>
        public const string PerfilAtivo = "PerfilAtivoNome";
    }

    // Valores calculados dinamicamente
    public static string Direitos => $"© {DateTime.Now.Year} Éden Alencar. Todos os direitos reservados.";
}
