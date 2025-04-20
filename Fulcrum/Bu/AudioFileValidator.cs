using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;

namespace Fulcrum.Bu;

/// <summary>
/// Utilitário para verificação de arquivos de áudio
/// </summary>
public static class AudioFileValidator
{
    /// <summary>
    /// Verifica se um arquivo de áudio WAV é válido e retorna informações sobre ele
    /// </summary>
    /// <param name="filePath">Caminho completo para o arquivo</param>
    /// <returns>True se o arquivo for válido, false caso contrário</returns>
    public static (bool isValid, string message, WaveFormat format) ValidateWavFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return (false, $"Arquivo não encontrado: {filePath}", null);
        }

        try
        {
            using (var reader = new AudioFileReader(filePath))
            {
                // Verificar formato de áudio
                var format = reader.WaveFormat;
                
                // Verificar tamanho do arquivo
                long expectedFileSize = format.AverageBytesPerSecond * (long)(reader.TotalTime.TotalSeconds) + 44; // 44 bytes para o cabeçalho WAV
                var fileInfo = new FileInfo(filePath);
                
                if (Math.Abs(fileInfo.Length - expectedFileSize) > expectedFileSize * 0.1)
                {
                    return (false, $"Possível corrupção: Tamanho esperado ({expectedFileSize}) difere significativamente do tamanho real ({fileInfo.Length})", format);
                }
                
                // Verificar se há amostras acima do limite
                var buffer = new float[16384]; // 16K amostras
                int totalSamples = 0;
                int extremeValues = 0;
                float maxValue = 0;
                
                int samplesRead;
                while ((samplesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i < samplesRead; i++)
                    {
                        float absValue = Math.Abs(buffer[i]);
                        if (absValue > maxValue) maxValue = absValue;
                        if (absValue > 5.0f) extremeValues++;
                    }
                    totalSamples += samplesRead;
                    
                    // Limita a verificação a ~5 segundos de áudio para arquivos grandes
                    if (totalSamples > 5 * format.SampleRate * format.Channels)
                        break;
                }
                
                string detalhes = $"Formato: {format}, Valor máximo: {maxValue:F6}, Amostras extremas: {extremeValues}/{totalSamples}";
                
                if (extremeValues > 0)
                {
                    double porcentagem = (double)extremeValues / totalSamples * 100;
                    return (porcentagem < 1.0, 
                           $"Arquivo contém {extremeValues} amostras extremas ({porcentagem:F3}%). {detalhes}",
                           format);
                }
                
                return (true, $"Arquivo válido. {detalhes}", format);
            }
        }
        catch (Exception ex)
        {
            return (false, $"Erro ao validar arquivo: {ex.Message}", null);
        }
    }
    
    /// <summary>
    /// Tenta reparar um arquivo WAV corrompido, criando uma cópia corrigida
    /// </summary>
    /// <param name="sourcePath">Caminho do arquivo original</param>
    /// <param name="destinationPath">Caminho para salvar o arquivo corrigido</param>
    /// <returns>True se o reparo foi bem-sucedido</returns>
    public static bool TryRepairWavFile(string sourcePath, string destinationPath)
    {
        if (!File.Exists(sourcePath))
        {
            System.Diagnostics.Debug.WriteLine($"Arquivo de origem não encontrado: {sourcePath}");
            return false;
        }
        
        try
        {
            // Tenta primeiro uma abordagem simples: reescrever o arquivo
            using (var reader = new AudioFileReader(sourcePath))
            {
                WaveFormat format = reader.WaveFormat;
                using (var writer = new WaveFileWriter(destinationPath, format))
                {
                    // Buffer para armazenar amostras
                    var buffer = new float[4096];
                    int samplesRead;
                    
                    // Contador de progresso
                    long totalSamples = 0;
                    long reportedAt = 0;
                    long totalLength = reader.Length;
                    
                    while ((samplesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        // Limita amostras extremas
                        for (int i = 0; i < samplesRead; i++)
                        {
                            // Limita valores extremos
                            if (buffer[i] > 1.0f)
                                buffer[i] = 1.0f;
                            else if (buffer[i] < -1.0f)
                                buffer[i] = -1.0f;
                        }
                        
                        // Escreve as amostras corrigidas
                        writer.WriteSamples(buffer, 0, samplesRead);
                        
                        // Atualiza progresso
                        totalSamples += samplesRead;
                        if (totalSamples - reportedAt > format.SampleRate * format.Channels)
                        {
                            reportedAt = totalSamples;
                            if (totalLength > 0)
                            {
                                double progress = (double)reader.Position / totalLength * 100.0;
                                System.Diagnostics.Debug.WriteLine($"Reparando arquivo: {progress:F1}% concluído");
                            }
                        }
                    }
                }
            }
            
            // Verifica se o arquivo reparado é válido
            var (isValid, message, _) = ValidateWavFile(destinationPath);
            System.Diagnostics.Debug.WriteLine($"Resultado do reparo: {message}");
            
            return isValid;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao reparar arquivo: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Analisa uma pasta inteira de arquivos de áudio e repara os corrompidos
    /// </summary>
    /// <param name="directoryPath">Caminho da pasta a ser analisada</param>
    /// <param name="recursive">Se deve analisar também as subpastas</param>
    /// <returns>Relatório com os resultados da análise</returns>
    public static string ValidateAndRepairDirectory(string directoryPath, bool recursive = false)
    {
        if (!Directory.Exists(directoryPath))
        {
            return $"Diretório não encontrado: {directoryPath}";
        }
        
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var wavFiles = Directory.GetFiles(directoryPath, "*.wav", searchOption);
        
        var report = new List<string>();
        int validFiles = 0;
        int repairedFiles = 0;
        int failedFiles = 0;
        
        report.Add($"Análise de {wavFiles.Length} arquivos WAV em {directoryPath}");
        report.Add(new string('-', 80));
        
        foreach (var file in wavFiles)
        {
            var (isValid, message, _) = ValidateWavFile(file);
            
            if (isValid)
            {
                report.Add($"✅ [{Path.GetFileName(file)}] {message}");
                validFiles++;
            }
            else
            {
                string repairPath = Path.Combine(
                    Path.GetDirectoryName(file),
                    Path.GetFileNameWithoutExtension(file) + "_repaired" + Path.GetExtension(file)
                );
                
                report.Add($"❌ [{Path.GetFileName(file)}] {message}");
                report.Add($"   Tentando reparar para: {Path.GetFileName(repairPath)}");
                
                bool repaired = TryRepairWavFile(file, repairPath);
                if (repaired)
                {
                    report.Add($"   ✅ Reparo bem-sucedido!");
                    repairedFiles++;
                }
                else
                {
                    report.Add($"   ❌ Falha no reparo");
                    failedFiles++;
                }
            }
            
            report.Add(string.Empty);
        }
        
        report.Add(new string('-', 80));
        report.Add($"Resumo: {validFiles} válidos, {repairedFiles} reparados, {failedFiles} com falha");
        
        string fullReport = string.Join(Environment.NewLine, report);
        System.Diagnostics.Debug.WriteLine(fullReport);
        
        return fullReport;
    }
}