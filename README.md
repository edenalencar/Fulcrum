# Fulcrum 🎵

<div align="center">
  <img src="Fulcrum/Assets/StoreLogo.backup.png" alt="Fulcrum Logo" width="100" />
  <h3>Reprodutor de Sons Ambientes para Relaxamento e Concentração</h3>
</div>

## 📝 Sobre o Projeto

Fulcrum é um aplicativo Windows moderno que permite reproduzir e mixar diferentes sons ambientes para criar o ambiente sonoro perfeito para relaxamento, meditação, estudo ou concentração. Desenvolvido com WinUI 3 (Windows App SDK) e .NET 8.0, o Fulcrum oferece uma experiência de usuário fluida e elegante com suporte completo aos temas claro e escuro do Windows.

Este projeto é **código aberto** sob a licença Mozilla Public License 2.0, mas também está disponível como um aplicativo pago na Microsoft Store para apoiar o desenvolvimento contínuo.

## ✨ Características

- **Mixagem de Múltiplos Sons**: Combine diferentes sons ambientes como chuva, fogueira, ventos e ondas do mar
- **Controle de Volume Individual**: Ajuste o volume de cada som para criar a mixagem perfeita
- **Visualização de Ondas Sonoras**: Interface visual para monitorar cada faixa de áudio
- **Temporizador de Sono**: Configure o aplicativo para parar a reprodução após um tempo determinado
- **Temas Claro/Escuro**: Integração perfeita com o tema do sistema Windows
- **Interface Moderna**: Design Fluent UI que segue as diretrizes de design da Microsoft

## 🛠️ Tecnologias Utilizadas

- **Linguagem**: C# (.NET 8.0)
- **Framework UI**: WinUI 3 (Windows App SDK)
- **Plataforma-alvo**: Windows 10/11 (Desktop)
- **Processamento de Áudio**: NAudio
- **Componentes UI**: CommunityToolkit.WinUI
- **Empacotamento**: MSIX para distribuição via Microsoft Store

## 📋 Requisitos do Sistema

- Windows 10 versão 1809 (build 17763) ou superior
- Windows 11
- Placa de som compatível
- Mínimo de 4GB de RAM
- 100MB de espaço em disco

## 📥 Instalação

### Microsoft Store
A maneira mais fácil de instalar o Fulcrum é através da Microsoft Store:

1. Abra a Microsoft Store no seu dispositivo Windows
2. Pesquise por "Fulcrum"
3. Clique em "Obter" para baixar e instalar
4. A compra apoia diretamente o desenvolvimento contínuo do aplicativo

### Compilação a partir do Código-fonte
Como projeto de código aberto, você pode compilar o Fulcrum diretamente a partir do código-fonte:

1. Clone este repositório: `git clone https://github.com/edenalencar/Fulcrum.git`
2. Abra a solução `Fulcrum.sln` no Visual Studio 2022 ou posterior
3. Compile e execute o projeto

### Instalação Manual
Alternativamente, você pode baixar o pacote MSIX do release mais recente:

1. Baixe o arquivo `.msix` da seção de releases
2. Clique duas vezes no arquivo baixado
3. Siga as instruções para completar a instalação

## 🎮 Instruções de Uso

1. **Iniciar Reprodução**: Clique no ícone do som desejado para iniciar a reprodução
2. **Mixagem**: Ative múltiplos sons e ajuste o volume individual de cada um
3. **Ajuste de Volume**: Use os controles deslizantes para ajustar o volume de cada som
4. **Temporizador**: Configure o temporizador de sono através das opções no menu
5. **Tema**: O aplicativo segue automaticamente o tema do sistema, ou você pode escolher manualmente

## 📸 Capturas de Tela

<div align="center">
  <h3>Tema Claro</h3>
  <img src="Fulcrum/Assets/screenshots/Fulcrum Claro -Sons Ambiente.png" alt="Fulcrum - Tema Claro" width="700" />
  
  <h3>Tema Escuro</h3>
  <img src="Fulcrum/Assets/screenshots/Fulcrum Escuro - Sons Ambiente.png" alt="Fulcrum - Tema Escuro" width="700" />
  
  <h3>Perfil de Som</h3>
  <img src="Fulcrum/Assets/screenshots/Fulcrum Perfil- Sons Ambiente.png" alt="Fulcrum - Perfil de Som" width="700" />
</div>

## 🧩 Arquitetura do Projeto

O Fulcrum segue uma arquitetura clara com separação entre lógica de negócios e interface de usuário:

- **/Bu**: Contém toda a lógica de negócios
  - **/Audio**: Gerenciamento de áudio e reprodução
  - **/Models**: Modelos de dados
  - **/Services**: Serviços como notificações e gerenciamento de tema
- **/View**: Interface de usuário
  - **/Controls**: Controles personalizados
  - **/Pages**: Páginas do aplicativo
- **/Assets**: Recursos como sons, imagens e ícones

## 🤝 Contribuição

Contribuições são bem-vindas! Como projeto de código aberto, seu envolvimento é valioso. Para contribuir:

1. Faça um fork do repositório
2. Crie uma branch para sua feature (`git checkout -b feature/MinhaFeature`)
3. Commit suas mudanças (`git commit -m 'Adicionando nova funcionalidade'`)
4. Push para a branch (`git push origin feature/MinhaFeature`)
5. Abra um Pull Request

Sua contribuição beneficia todos os usuários, tanto os que compilam o código quanto os que adquirem o aplicativo na loja!

## 📄 Licença

Este projeto está licenciado sob a licença Mozilla Public License 2.0 (MPL-2.0) - veja o arquivo LICENSE.txt para detalhes.

### 🤔 Código Aberto e Microsoft Store?

Sim! O Fulcrum é um projeto de código aberto disponível gratuitamente no GitHub, mas também é oferecido como um aplicativo pago na Microsoft Store. Aqui está o que isso significa:

- **Código completamente aberto**: Todo o código-fonte está disponível neste repositório sob a licença MPL-2.0
- **Contribuições bem-vindas**: Você pode modificar, melhorar e contribuir com o projeto
- **Suporte ao desenvolvedor**: A compra na Microsoft Store ajuda a financiar o desenvolvimento contínuo
- **Conveniência**: A versão da loja oferece instalação fácil, atualizações automáticas e experiência otimizada

Você pode escolher construir o aplicativo a partir do código-fonte ou adquiri-lo na Microsoft Store para uma experiência mais conveniente e para apoiar o desenvolvedor.

## 📞 Contato

Se você tiver dúvidas ou sugestões, sinta-se à vontade para abrir uma issue ou entrar em contato.

---

<div align="center">
  <p>Criado com ❤️ para um ambiente sonoro perfeito</p>
  <p>Código aberto + Aplicativo na Loja = Sustentabilidade</p>
</div>