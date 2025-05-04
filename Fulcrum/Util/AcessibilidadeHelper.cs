using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Fulcrum.Util;

/// <summary>
/// Classe auxiliar para funcionalidades de acessibilidade
/// </summary>
public static class AcessibilidadeHelper
{
    /// <summary>
    /// Anuncia uma mensagem para leitores de tela
    /// </summary>
    /// <param name="elemento">Elemento de origem para o anúncio</param>
    /// <param name="mensagem">Mensagem a ser anunciada</param>
    /// <param name="prioridade">Indica se a mensagem tem alta prioridade</param>
    public static void AnunciarParaLeitoresEscreens(FrameworkElement elemento, string mensagem, bool prioridade = false)
    {
        if (elemento == null || string.IsNullOrEmpty(mensagem))
            return;

        AutomationPeer peer = FrameworkElementAutomationPeer.FromElement(elemento);
        if (peer != null)
        {
            peer.RaiseNotificationEvent(
                prioridade ? AutomationNotificationKind.ActionCompleted : AutomationNotificationKind.Other,
                prioridade ? AutomationNotificationProcessing.ImportantAll : AutomationNotificationProcessing.ImportantMostRecent,
                mensagem,
                "AcessibilidadeNotificacao");
        }
    }

    /// <summary>
    /// Configura um elemento para uso eficiente com leitores de tela
    /// </summary>
    /// <param name="elemento">Elemento a ser configurado</param>
    /// <param name="nome">Nome acessível do elemento</param>
    /// <param name="descricao">Descrição acessível do elemento</param>
    /// <param name="tipo">Tipo de landmark para o elemento</param>
    public static void ConfigurarParaLeitoresEscreens(FrameworkElement elemento, string nome, string descricao = "", AutomationLandmarkType tipo = AutomationLandmarkType.None)
    {
        if (elemento == null)
            return;

        // Define o nome acessível
        if (!string.IsNullOrEmpty(nome))
            AutomationProperties.SetName(elemento, nome);

        // Define a descrição de ajuda
        if (!string.IsNullOrEmpty(descricao))
            AutomationProperties.SetHelpText(elemento, descricao);

        // Define o tipo de landmark, se especificado
        if (tipo != AutomationLandmarkType.None)
            AutomationProperties.SetLandmarkType(elemento, tipo);
    }

    /// <summary>
    /// Atualiza dinamicamente o texto de um elemento TextBlock e anuncia a mudança
    /// </summary>
    /// <param name="textBlock">TextBlock a ser atualizado</param>
    /// <param name="novoTexto">Novo texto a ser definido</param>
    /// <param name="anunciar">Indica se a mudança deve ser anunciada</param>
    public static void AtualizarTextoAcessivel(TextBlock textBlock, string novoTexto, bool anunciar = true)
    {
        if (textBlock == null || novoTexto == textBlock.Text)
            return;

        string textoAntigo = textBlock.Text;
        textBlock.Text = novoTexto;

        if (anunciar)
        {
            AnunciarParaLeitoresEscreens(textBlock, novoTexto);
        }
    }

    /// <summary>
    /// Habilita padrões de navegação por teclado em um painel (especialmente útil para grids com controles)
    /// </summary>
    /// <param name="container">Contêiner a ser configurado</param>
    public static void ConfigurarNavegacaoTeclado(Panel container)
    {
        if (container == null)
            return;

        int indice = 0;
        foreach (UIElement elemento in container.Children)
        {
            if (elemento is Control controle)
            {
                controle.TabIndex = indice++;
                controle.KeyboardAcceleratorPlacementMode = KeyboardAcceleratorPlacementMode.Auto;
            }
        }
    }

    /// <summary>
    /// Anuncia o estado atual de um som (ligado/desligado, volume)
    /// </summary>
    /// <param name="elemento">Elemento relacionado ao som</param>
    /// <param name="nomeSom">Nome do som</param>
    /// <param name="volume">Volume atual (0-1)</param>
    /// <param name="ativo">Indica se o som está ativo</param>
    public static void AnunciarEstadoSom(FrameworkElement elemento, string nomeSom, double volume, bool ativo)
    {
        if (elemento == null)
            return;

        string estadoSom = ativo ? "ativado" : "desativado";
        int volumePorcentagem = (int)(volume * 100);

        string mensagem = ativo
            ? $"Som de {nomeSom} {estadoSom} com volume em {volumePorcentagem}%"
            : $"Som de {nomeSom} {estadoSom}";

        AnunciarParaLeitoresEscreens(elemento, mensagem);
    }
}