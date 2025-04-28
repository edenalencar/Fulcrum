# Fulcrum - Documento de Requisitos Técnicos (TRD)

**Versão:** 1.0  
**Data:** 28/04/2025  
**Autor:** Equipe de Desenvolvimento Fulcrum

## 1. Introdução

### 1.1 Propósito do Documento

Este documento técnico detalha os requisitos de engenharia, arquitetura, tecnologias e implementação do aplicativo Fulcrum, um reprodutor de sons ambientes para Windows 10/11.

### 1.2 Escopo

Este documento abrange todos os aspectos técnicos relacionados ao desenvolvimento, teste e implantação do Fulcrum, incluindo:
- Arquitetura do sistema
- Tecnologias e frameworks utilizados
- Requisitos de hardware e software
- Considerações de desempenho e segurança
- Estratégias de teste e qualidade

### 1.3 Documentos Relacionados

- [Documento de Requisitos do Produto (PRD)](./PRD.md)
- [Documento de Requisitos Funcionais (FRD)](./FRD.md)
- [Especificação de API interna](./api-spec.md)

## 2. Visão Geral da Arquitetura

### 2.1 Arquitetura do Sistema

O Fulcrum segue uma arquitetura baseada no padrão MVVM (Model-View-ViewModel) com clara separação entre a lógica de negócios e a interface do usuário:

```
Fulcrum/
├── Bu/                  # Lógica de negócios (Business)
│   ├── Audio/           # Processamento e gerenciamento de áudio
│   ├── Models/          # Modelos de dados
│   └── Services/        # Serviços de aplicativo
├── View/                # Interface do usuário
│   ├── Controls/        # Controles personalizados
│   ├── Pages/           # Páginas da aplicação
│   └── Themes/          # Recursos de temas
└── Assets/              # Recursos estáticos
```

### 2.2 Diagrama de Componentes

O sistema é composto pelos seguintes componentes principais:

1. **AudioManager (Singleton)**
   - Gerencia a reprodução de todos os sons ambientes
   - Controla volumes e estados de reprodução individuais
   - Implementa carregamento e gerenciamento de recursos de áudio

2. **EffectsManager**
   - Gerencia efeitos de áudio (reverb, pitch, eco, flanger)
   - Implementa processamento de sinal digital para efeitos sonoros
   - Aplica configurações de equalização

3. **VisualizationEngine**
   - Renderiza visualizações de ondas sonoras para cada som
   - Processa amostras de áudio em tempo real
   - Gera representações visuais dos dados de áudio

4. **ProfileManager**
   - Gerencia perfis de usuário para combinações de sons
   - Persiste configurações entre sessões
   - Implementa serialização/deserialização de perfis

5. **SleepTimerService**
   - Gerencia o temporizador de sono
   - Implementa notificações e callbacks para eventos do temporizador

## 3. Tecnologias e Frameworks

### 3.1 Plataforma e Framework

- **Sistema Operacional:** Windows 10/11
- **Framework de UI:** Windows App SDK (WinUI 3)
- **Runtime:** .NET 8.0
- **Linguagem:** C#

### 3.2 Bibliotecas Principais

| Biblioteca | Versão | Propósito |
|------------|--------|-----------|
| NAudio | 2.2.1 | Processamento e reprodução de áudio |
| NAudio.WinMM | 2.2.1 | APIs de áudio do Windows |
| CommunityToolkit.WinUI | 7.1.2 | Componentes e controles para WinUI 3 |
| Microsoft.WindowsAppSDK | 1.4.0 | Framework base WinUI 3 |
| Microsoft.Extensions.DependencyInjection | 8.0.0 | Injeção de dependência |
| System.Text.Json | 8.0.0 | Serialização/deserialização JSON |

### 3.3 Ferramentas de Desenvolvimento

- **IDE:** Visual Studio 2022
- **Controle de Versão:** Git
- **CI/CD:** GitHub Actions
- **Empacotamento:** MSIX
- **Distribuição:** Microsoft Store

## 4. Requisitos de Implementação

### 4.1 Reprodução de Áudio

#### 4.1.1 Requerimentos de Processamento de Áudio

- **Formato de áudio:** WAV (PCM 16-bit, 44.1kHz) e MP3 (320kbps)
- **Mixagem:** Suporte para no mínimo 10 fontes de áudio simultâneas
- **Buffering:** Buffer mínimo de 1024 amostras para evitar falhas na reprodução
- **Latência:** Máximo de 50ms entre comando do usuário e resposta sonora
- **Controle de volume:** Escala logarítmica de 0.0 a 1.0 (convertida para dB)

#### 4.1.2 NAudio Integration

```csharp
// Exemplo de implementação do reprodutor de áudio
public sealed class AudioPlayer : IDisposable
{
    private AudioFileReader _reader;
    private WaveOutEvent _outputDevice;
    
    // Buffer de amostras para visualização
    private float[] _sampleBuffer = new float[1024];
    
    // Propriedades para controle de volume e estado
    public float Volume { get; set; } = 1.0f;
    public bool IsPlaying { get; private set; }
    
    // Implementação dos métodos...
}
```

### 4.2 Visualização de Ondas Sonoras

#### 4.2.1 Requisitos de Renderização

- **Frequência de atualização:** 30-60 FPS
- **Método de renderização:** Polyline dinâmica para WinUI
- **Eficiência de cálculo:** Amostragem adaptativa baseada em FFT
- **Cores temáticas:** Adaptação automática ao tema do sistema

#### 4.2.2 Algoritmo de Visualização

```csharp
// Exemplo de algoritmo para processamento de visualização
public void ProcessAudioVisualization(float[] samples)
{
    // Aplicar janela Hanning
    for (int i = 0; i < samples.Length; i++)
    {
        float hannWindow = 0.5f * (1 - MathF.Cos(2 * MathF.PI * i / (samples.Length - 1)));
        samples[i] *= hannWindow;
    }
    
    // Calcular amplitudes para visualização
    // Atualizar pontos da Polyline em thread de UI
}
```

### 4.3 Persistência de Dados

#### 4.3.1 Armazenamento de Configurações

- **Método:** ApplicationData.Current.LocalSettings
- **Formato:** JSON serializado
- **Dados armazenados:**
  - Volumes de cada som
  - Configurações de equalização
  - Configurações de efeitos
  - Tema selecionado
  - Estado de atalhos de teclado

#### 4.3.2 Armazenamento de Perfis

- **Método:** Arquivos JSON individuais
- **Localização:** ApplicationData.Current.LocalFolder
- **Estrutura do perfil:**
  ```json
  {
    "Nome": "Nome do perfil",
    "Descricao": "Descrição do perfil",
    "DataCriacao": "2025-04-28T10:00:00Z",
    "ConfiguracoesSom": {
      "chuva": 0.8,
      "ondas": 0.5,
      "fogueira": 0.3
    }
  }
  ```

### 4.4 Padrões de Design

#### 4.4.1 Singleton

Aplicado para gerenciadores globais que precisam de acesso centralizado:

```csharp
public sealed class AudioManager
{
    private static readonly Lazy<AudioManager> _instance = new(() => new AudioManager());
    public static AudioManager Instance => _instance.Value;
    
    private AudioManager() { /* inicialização */ }
}
```

#### 4.4.2 Repository Pattern

Para acesso persistência de dados:

```csharp
public interface IPerfilRepository
{
    Task<IEnumerable<PerfilSom>> GetAllAsync();
    Task<PerfilSom> GetByNameAsync(string nome);
    Task SaveAsync(PerfilSom perfil);
    Task DeleteAsync(string nome);
}
```

#### 4.4.3 Command Pattern

Para operações da UI:

```csharp
public class PlaySoundCommand : ICommand
{
    private readonly string _soundId;
    
    public PlaySoundCommand(string soundId) => _soundId = soundId;
    
    public void Execute(object parameter)
    {
        // Lógica para reproduzir o som específico
    }
}
```

### 4.5 Interface e Acessibilidade

#### 4.5.1 Requisitos de Temas

- Implementação completa de temas claro e escuro
- Transições suaves entre temas
- Detecção automática do tema do sistema
- Opção de substituição manual do tema

#### 4.5.2 Requisitos de Acessibilidade

- Suporte completo para narrador (screen reader)
- Contraste adequado para todos os elementos visuais
- Navegação completa por teclado
- Tamanho adequado para elementos interativos (mínimo 32x32px)
- Rótulos descritivos para todos os controles

## 5. Requisitos de Desempenho

### 5.1 Benchmarks de Performance

| Métrica | Alvo | Máximo Aceitável |
|---------|------|------------------|
| Tempo de inicialização | < 2s | 3s |
| Tempo de resposta da UI | < 50ms | 100ms |
| Uso de memória | < 150MB | 200MB |
| Uso de CPU | < 5% (idle) | 15% (idle) |
| Consumo de bateria | < 2% por hora | 5% por hora |

### 5.2 Otimizações

- **Carregamento assíncrono** de recursos de áudio
- **Buffering** adequado para evitar falhas na reprodução
- **Lazy loading** para recursos não utilizados imediatamente
- **Throttling** para atualizações frequentes de UI
- **Caching** de configurações frequentemente acessadas

## 6. Segurança e Privacidade

### 6.1 Permissões Necessárias

- Acesso ao dispositivo de áudio
- Armazenamento local para configurações
- Inicialização com o Windows (opcional)

### 6.2 Considerações de Privacidade

- Nenhum dado pessoal coletado ou armazenado
- Análise anônima de uso (opcional, com consentimento explícito)
- Nenhuma conexão de rede necessária para operação

## 7. Testes

### 7.1 Estratégia de Testes

- **Testes unitários:** Para lógica de negócios e processamento de áudio
- **Testes de integração:** Para interação entre componentes
- **Testes de UI:** Para verificar comportamento da interface
- **Testes de desempenho:** Para garantir responsividade e eficiência

### 7.2 Plano de Testes

| Categoria | Escopo | Ferramentas |
|-----------|--------|-------------|
| Unitários | AudioManager, EffectsManager | MSTest, Moq |
| Integração | Integração de componentes | MSTest |
| UI | Interações de usuário | WinAppDriver |
| Desempenho | Consumo de recursos, responsividade | Windows Performance Toolkit |

## 8. Implantação e Distribuição

### 8.1 Requisitos de Empacotamento

- **Tipo de pacote:** MSIX
- **Tamanho máximo:** 100MB
- **Arquitetura:** x64, arm64

### 8.2 Processo de Distribuição

1. Compilação do aplicativo
2. Geração do pacote MSIX
3. Assinatura digital do pacote
4. Testes de implantação
5. Submissão para Microsoft Store
6. Gerenciamento de atualizações

### 8.3 Requisitos de Certificação

- Conformidade com as diretrizes da Microsoft Store
- Políticas de privacidade claras
- Descrições precisas de funcionalidades
- Screenshots atualizados

## 9. Considerações Futuras

### 9.1 Escalabilidade

- Arquitetura modular para fácil adição de novos sons
- Sistema de plugins para extensibilidade
- API para integração com outros aplicativos

### 9.2 Internacionalização

- Suporte para múltiplos idiomas (recursos já estruturados)
- Adaptação para diferentes regiões
- Considerações culturais para sons e interfaces

## 10. Apêndices

### 10.1 Glossário

- **NAudio:** Biblioteca de processamento de áudio para .NET
- **WinUI 3:** Framework de interface do usuário para Windows
- **MSIX:** Formato de empacotamento para aplicativos Windows
- **FFT:** Fast Fourier Transform, algoritmo para análise de frequência