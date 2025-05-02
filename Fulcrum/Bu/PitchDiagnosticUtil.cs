using NAudio.Wave;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Fulcrum.Bu;

/// <summary>
/// Classe para testar e diagnosticar o efeito de Pitch
/// </summary>
public class PitchDiagnosticUtil
{
    /// <summary>
    /// Executa um teste abrangente do efeito de Pitch com diferentes configurações
    /// </summary>
    /// <param name="soundId">Identificador do som a ser testado</param>
    public static async Task TestarEfeitoPitch(string soundId)
    {
        try
        {
            Debug.WriteLine($"[DiagnosticoPitch] Iniciando teste do efeito de Pitch para {soundId}");
            
            // Ativar os efeitos e selecionar o tipo Pitch
            AudioManager.Instance.AtivarEfeitos(soundId, true);
            AudioManager.Instance.DefinirTipoEfeito(soundId, TipoEfeito.Pitch);
            
            // Teste com diferentes valores de pitch
            Debug.WriteLine("[DiagnosticoPitch] === Testando diferentes valores de Pitch ===");
            
            // Testar com pitch normal (1.0)
            AudioManager.Instance.AjustarPitch(soundId, 1.0f);
            Debug.WriteLine("[DiagnosticoPitch] Definido Pitch = 1.0 (normal)");
            await Task.Delay(2000); // Aguardar para ouvir o resultado
            
            // Testar com pitch mais grave (0.75)
            AudioManager.Instance.AjustarPitch(soundId, 0.75f);
            Debug.WriteLine("[DiagnosticoPitch] Definido Pitch = 0.75 (mais grave)");
            await Task.Delay(2000);
            
            // Testar com pitch mais grave (0.5)
            AudioManager.Instance.AjustarPitch(soundId, 0.5f);
            Debug.WriteLine("[DiagnosticoPitch] Definido Pitch = 0.5 (muito grave)");
            await Task.Delay(2000);
            
            // Testar com pitch mais agudo (1.5)
            AudioManager.Instance.AjustarPitch(soundId, 1.5f);
            Debug.WriteLine("[DiagnosticoPitch] Definido Pitch = 1.5 (mais agudo)");
            await Task.Delay(2000);
            
            // Testar com pitch mais agudo (2.0)
            AudioManager.Instance.AjustarPitch(soundId, 2.0f);
            Debug.WriteLine("[DiagnosticoPitch] Definido Pitch = 2.0 (muito agudo)");
            await Task.Delay(2000);
            
            // Voltar para o valor normal
            AudioManager.Instance.AjustarPitch(soundId, 1.0f);
            Debug.WriteLine("[DiagnosticoPitch] Voltando para Pitch = 1.0 (normal)");
            
            // Teste de desativação/ativação do efeito
            Debug.WriteLine("[DiagnosticoPitch] === Testando ativação/desativação do efeito ===");
            
            // Desativar efeito
            AudioManager.Instance.AtivarEfeitos(soundId, false);
            Debug.WriteLine("[DiagnosticoPitch] Efeito desativado");
            await Task.Delay(2000);
            
            // Reativar efeito com um valor diferente
            AudioManager.Instance.AtivarEfeitos(soundId, true);
            AudioManager.Instance.AjustarPitch(soundId, 0.8f);
            Debug.WriteLine("[DiagnosticoPitch] Efeito reativado com Pitch = 0.8");
            await Task.Delay(2000);
            
            // Restaurar configuração original
            AudioManager.Instance.AjustarPitch(soundId, 1.0f);
            Debug.WriteLine("[DiagnosticoPitch] Teste concluído, configuração restaurada");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DiagnosticoPitch] ERRO: {ex.Message}");
            Debug.WriteLine($"[DiagnosticoPitch] StackTrace: {ex.StackTrace}");
        }
    }
    
    /// <summary>
    /// Realiza uma análise do fluxo de buffer no efeito de Pitch
    /// </summary>
    /// <param name="soundId">Identificador do som a ser analisado</param>
    /// <param name="pitchFactor">Fator de pitch a ser testado</param>
    public static void AnalisarBufferPitch(string soundId, float pitchFactor)
    {
        try
        {
            Debug.WriteLine($"[DiagnosticoPitch] Iniciando análise de buffer para Pitch={pitchFactor}");
            
            // Ativar efeitos
            AudioManager.Instance.AtivarEfeitos(soundId, true);
            AudioManager.Instance.DefinirTipoEfeito(soundId, TipoEfeito.Pitch);
            AudioManager.Instance.AjustarPitch(soundId, pitchFactor);
            
            // O resultado da análise será mostrado nos logs de depuração
            // gerados pela classe PitchShiftingSampleProvider
            Debug.WriteLine("[DiagnosticoPitch] Análise em andamento, verificar logs de saída");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DiagnosticoPitch] ERRO na análise: {ex.Message}");
        }
    }
}