# Fulcrum 🎵

<div align="center">
  <img src="Assets/StoreLogo.png" alt="Fulcrum Logo" width="100" />
  <h3>Reprodutor de Sons Ambientes para Relaxamento e Concentração</h3>
</div>

## 📝 Sobre o Projeto

Fulcrum é um aplicativo Windows moderno que permite reproduzir e mixar diferentes sons ambientes para criar o ambiente sonoro perfeito para relaxamento, meditação, estudo ou concentração. Desenvolvido com WinUI 3 (Windows App SDK) e .NET 8.0, o Fulcrum oferece uma experiência de usuário fluida e elegante com suporte completo aos temas claro e escuro do Windows.

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

Contribuições são bem-vindas! Para contribuir:

1. Faça um fork do repositório
2. Crie uma branch para sua feature (`git checkout -b feature/MinhaFeature`)
3. Commit suas mudanças (`git commit -m 'Adicionando nova funcionalidade'`)
4. Push para a branch (`git push origin feature/MinhaFeature`)
5. Abra um Pull Request

## 📄 Licença

Este projeto está licenciado sob a licença MIT - veja o arquivo LICENSE para detalhes.

## 📞 Contato

Se você tiver dúvidas ou sugestões, sinta-se à vontade para abrir uma issue ou entrar em contato.

---

<div align="center">
  <p>Criado com ❤️ para um ambiente sonoro perfeito</p>
</div>