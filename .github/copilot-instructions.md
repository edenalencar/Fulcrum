# Instruções para GitHub Copilot

## Visão Geral do Projeto

Este é um aplicativo de reprodutor de sons ambientes para relaxamento e concentração, desenvolvido para Windows 10/11 usando WinUI 3 (Windows App SDK) com C# (.NET 8.0). O aplicativo permite aos usuários reproduzir e mixar diferentes sons ambientes, visualizar ondas sonoras e personalizar a experiência com temas claro/escuro.

## Stack Tecnológica

- **Linguagem**: C# (.NET 8.0)
- **Framework UI**: WinUI 3 (Windows App SDK)
- **Plataforma-alvo**: Windows 10/11 (Desktop)
- **Empacotamento**: MSIX para distribuição via Microsoft Store
- **Padrões de Design**: Singleton (implementado no AudioManager)
- **Estrutura do Projeto**: Separação entre lógica de negócios (pasta Bu) e interface do usuário (pasta View)

## Bibliotecas Principais

- **NAudio**: Utilizada para processamento, reprodução e manipulação de áudio
- **CommunityToolkit.WinUI**: Componentes UI, controles e animações
- **Microsoft.WindowsAppSDK**: Framework principal para desenvolvimento do aplicativo

## Diretrizes de Código

### Padrões de Nomenclatura

- **Classes**: PascalCase (ex: `AudioManager`, `ThemeService`)
- **Métodos**: PascalCase (ex: `PlaySound`, `StopAllTracks`)
- **Variáveis e Parâmetros**: camelCase (ex: `currentVolume`, `audioFile`)
- **Constantes**: UPPER_SNAKE_CASE (ex: `MAX_VOLUME`, `DEFAULT_THEME`)
- **Interfaces**: Prefixo "I" + PascalCase (ex: `IAudioProcessor`, `IThemeManager`)

### Estrutura de Pastas

- **/Bu**: Lógica de negócios (Business)
  - **/Audio**: Classes relacionadas ao gerenciamento de áudio
  - **/Models**: Modelos de dados e entidades
  - **/Services**: Serviços de aplicativo (configurações, temas, etc.)
- **/View**: Interface do usuário
  - **/Controls**: Controles personalizados
  - **/Pages**: Páginas da aplicação
  - **/Themes**: Recursos de temas (claro/escuro)
- **/Assets**: Recursos estáticos (imagens, sons, ícones)
- **/Helpers**: Classes utilitárias

### Implementação do AudioManager (Singleton)

Ao implementar funcionalidades de áudio, siga o padrão Singleton estabelecido:

```csharp
public sealed class AudioManager
{
    private static readonly Lazy<AudioManager> _instance = new(() => new AudioManager());
    public static AudioManager Instance => _instance.Value;

    private AudioManager()
    {
        // Inicialização
    }

    // Métodos públicos
}
```

### Padrões de Tratamento de Áudio

- Use NAudio para processamento e reprodução de áudio
- Implemente fade in/out para transições suaves entre sons
- Permita mixagem de múltiplos sons com controle de volume individual
- Disponibilize visualização de ondas de áudio usando `WaveFormRenderer` ou método equivalente
- Considere a implementação de efeitos como equalização e reverberação

### UI e UX

- Suporte completamente os temas claro e escuro
- Implemente transições suaves entre elementos da UI usando as animações do CommunityToolkit
- Siga as diretrizes de design Fluent UI
- Garanta que a interface seja responsiva e funcione bem em diferentes resoluções de tela
- Implemente acessibilidade (contraste adequado, suporte a leitor de tela, navegação por teclado)

### Gerenciamento de Recursos

- Libere corretamente os recursos de áudio quando não estiverem em uso
- Implemente o padrão disposable (IDisposable) para componentes que usam recursos não gerenciados
- Carregue os recursos de áudio de forma assíncrona para não bloquear a UI

### XAML

Prefira o uso de recursos para cores e estilos:

```xml
<Page.Resources>
    <ResourceDictionary>
        <SolidColorBrush x:Key="PrimaryBackgroundBrush" Color="{ThemeResource SystemAccentColor}" />
        <Style x:Key="DefaultButtonStyle" TargetType="Button">
            <!-- Propriedades de estilo -->
        </Style>
    </ResourceDictionary>
</Page.Resources>
```

### Exemplos de Implementação

#### Exemplo de Carregamento de Áudio Assíncrono

```csharp
public async Task<bool> LoadAudioFileAsync(string filePath)
{
    try
    {
        await Task.Run(() => {
            using var reader = new AudioFileReader(filePath);
            // Configuração do áudio
        });
        return true;
    }
    catch (Exception ex)
    {
        // Tratamento de erro
        return false;
    }
}
```

#### Exemplo de Controle de Volume

```csharp
public void SetTrackVolume(string trackId, float volume)
{
    if (_audioTracks.TryGetValue(trackId, out var track))
    {
        track.Volume = Math.Clamp(volume, 0f, 1f);
    }
}
```

## Empacotamento e Distribuição

- Configure o projeto para empacotamento MSIX
- Inclua ícones apropriados em vários tamanhos
- Defina as permissões necessárias no manifesto
- Garanta que o aplicativo passe na validação da Microsoft Store

## Testes

- Implemente testes unitários para a lógica de negócios
- Teste em diferentes versões do Windows (10 e 11)
- Verifique o comportamento com diferentes configurações de áudio
- Teste o aplicativo em diferentes tamanhos de tela e resoluções

## Otimizações

- Minimize o uso de memória ao carregar e processar arquivos de áudio
- Implemente buffering adequado para reprodução suave
- Otimize a renderização da visualização de ondas de áudio para não impactar a performance

## Referências Úteis

- [Documentação do Windows App SDK](https://docs.microsoft.com/windows/apps/windows-app-sdk/)
- [Documentação do NAudio](https://github.com/naudio/NAudio/wiki)
- [Documentação do Community Toolkit](https://docs.microsoft.com/windows/communitytoolkit/)
- [Diretrizes de Design Fluent UI](https://docs.microsoft.com/windows/apps/design/signature-experiences/design-principles)