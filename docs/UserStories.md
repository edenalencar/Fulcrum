# Fulcrum - User Stories e Epics

**Versão:** 1.0  
**Data:** 28/04/2025  
**Autor:** Equipe de Desenvolvimento Fulcrum

## 1. Visão Geral

Este documento descreve as histórias de usuário (User Stories) e épicos (Epics) para o aplicativo Fulcrum. Estas histórias capturam as necessidades dos usuários em um formato centrado no usuário e servem como base para o planejamento de desenvolvimento iterativo.

## 2. Épicos (Epics)

Os épicos representam grandes áreas funcionais do aplicativo que serão desenvolvidas ao longo de múltiplas iterações.

### EPIC-01: Reprodução de Sons Ambientes

**Descrição:** Como usuário, quero reproduzir e mixar diferentes sons ambientes para criar um ambiente sonoro personalizado que me ajude a relaxar, concentrar ou dormir melhor.

**Escopo:**
- Reprodução de sons individuais
- Controle de volume por som
- Reprodução em loop contínuo
- Mixagem de múltiplos sons simultâneos
- Controles globais de reprodução

### EPIC-02: Visualização de Áudio

**Descrição:** Como usuário, quero visualizar os sons que estou ouvindo para ter feedback visual da minha experiência auditiva e tornar a interação com o aplicativo mais envolvente.

**Escopo:**
- Visualização em tempo real de formas de onda
- Representações visuais distintas para cada som
- Animações responsivas
- Compatibilidade com temas claro e escuro

### EPIC-03: Gerenciamento de Perfis

**Descrição:** Como usuário, quero salvar e carregar configurações de sons para poder alternar rapidamente entre diferentes ambientes sonoros que configurei para diferentes propósitos.

**Escopo:**
- Criação de perfis personalizados
- Armazenamento de configurações de volume e sons ativos
- Aplicação rápida de perfis salvos
- Gerenciamento (edição/exclusão) de perfis

### EPIC-04: Temporizador de Sono

**Descrição:** Como usuário, quero configurar um temporizador para que os sons parem automaticamente após um período definido, especialmente para uso durante o sono ou meditação.

**Escopo:**
- Configuração de duração
- Contagem regressiva
- Parada automática da reprodução
- Notificações de conclusão

### EPIC-05: Personalização de Som

**Descrição:** Como usuário, quero personalizar o áudio com equalizações e efeitos para criar exatamente a experiência sonora que estou buscando.

**Escopo:**
- Equalização de áudio
- Efeitos (reverb, eco, etc.)
- Ajustes específicos por som
- Presets de equalização

### EPIC-06: Configurações do Sistema

**Descrição:** Como usuário, quero personalizar o comportamento e aparência do aplicativo para que ele se adapte às minhas preferências e estilo de uso.

**Escopo:**
- Seleção de temas
- Configuração de teclas de atalho
- Opções de inicialização
- Preferências de interface

### EPIC-07: Acessibilidade

**Descrição:** Como usuário com necessidades especiais, quero que o aplicativo seja acessível para que eu possa utilizá-lo independentemente de minhas limitações.

**Escopo:**
- Suporte a leitores de tela
- Navegação por teclado
- Alto contraste e legibilidade
- Recursos de acessibilidade do Windows

## 3. Histórias de Usuário (User Stories)

As histórias de usuário descrevem funcionalidades específicas do ponto de vista do usuário final.

### EPIC-01: Reprodução de Sons Ambientes

#### US-101: Reprodução de Som Individual

**Como** usuário do Fulcrum,  
**Eu quero** poder iniciar e parar a reprodução de cada som ambiente individualmente,  
**Para que** eu possa personalizar exatamente quais sons estou ouvindo.

**Critérios de Aceitação:**
- Cada som deve ter um botão claro de play/pause
- A reprodução deve começar imediatamente quando o botão é pressionado
- O estado do som (ativo/inativo) deve ser claramente visível
- A reprodução deve ser em loop contínuo sem interrupções perceptíveis
- Ao clicar novamente no botão, o som deve parar imediatamente

**Estimativa:** 3 pontos

#### US-102: Controle de Volume Individual

**Como** usuário do Fulcrum,  
**Eu quero** ajustar o volume de cada som individualmente,  
**Para que** eu possa criar a mistura perfeita para minhas necessidades.

**Critérios de Aceitação:**
- Cada som deve ter um controle deslizante (slider) para ajuste de volume
- O volume deve ser ajustável de 0% a 100%
- As alterações de volume devem ser aplicadas em tempo real
- O nível atual de volume deve ser visualmente indicado
- O ajuste deve ser suave sem cortes ou ruídos

**Estimativa:** 2 pontos

#### US-103: Reprodução Global

**Como** usuário do Fulcrum,  
**Eu quero** iniciar ou pausar todos os sons com um único comando,  
**Para que** eu possa controlar a reprodução de forma rápida e eficiente.

**Critérios de Aceitação:**
- Deve existir um botão "Reproduzir Todos" facilmente acessível
- Ao pressionar, todos os sons com volume acima de zero devem começar a tocar
- Deve existir um botão "Pausar Todos" facilmente acessível
- Ao pressionar, todos os sons em reprodução devem pausar simultaneamente
- O estado dos botões deve refletir o estado atual do sistema

**Estimativa:** 2 pontos

#### US-104: Persistência de Estado

**Como** usuário do Fulcrum,  
**Eu quero** que o aplicativo lembre-se das minhas configurações de volume e sons ativos,  
**Para que** eu não precise reconfigurar tudo cada vez que abrir o aplicativo.

**Critérios de Aceitação:**
- O estado de volume de cada som deve ser salvo automaticamente
- O estado de reprodução (ativo/inativo) deve ser salvo automaticamente
- Ao iniciar o aplicativo, as configurações anteriores devem ser restauradas
- As configurações devem persistir mesmo após o fechamento do aplicativo

**Estimativa:** 3 pontos

### EPIC-02: Visualização de Áudio

#### US-201: Visualização de Forma de Onda

**Como** usuário do Fulcrum,  
**Eu quero** ver representações visuais das ondas sonoras para cada som ativo,  
**Para que** eu tenha uma experiência multissensorial e feedback visual do que estou ouvindo.

**Critérios de Aceitação:**
- Cada som deve ter sua própria visualização de forma de onda
- A visualização deve ser animada e responder ao áudio em tempo real
- A amplitude da visualização deve corresponder ao volume do som
- A visualização deve usar cores distintas para cada tipo de som
- Quando o som está pausado, a visualização deve permanecer estática

**Estimativa:** 5 pontos

#### US-202: Indicadores Visuais de Estado

**Como** usuário do Fulcrum,  
**Eu quero** ver claramente quais sons estão ativos e quais estão pausados,  
**Para que** eu possa entender rapidamente o estado atual do sistema.

**Critérios de Aceitação:**
- Sons ativos devem ter indicação visual diferente dos sons pausados
- As transições entre estados devem ter animações suaves
- Os indicadores devem ser visíveis mesmo à distância
- Os indicadores devem ter contraste adequado em ambos os temas (claro/escuro)

**Estimativa:** 2 pontos

### EPIC-03: Gerenciamento de Perfis

#### US-301: Criação de Perfil

**Como** usuário do Fulcrum,  
**Eu quero** salvar minha configuração atual de sons como um perfil nomeado,  
**Para que** eu possa facilmente retornar a esta configuração no futuro.

**Critérios de Aceitação:**
- Deve haver um botão "Novo Perfil" facilmente acessível
- Ao clicar, deve ser exibido um diálogo para inserir nome e descrição
- O sistema deve capturar automaticamente quais sons estão ativos e seus volumes
- O perfil criado deve aparecer imediatamente na lista de perfis disponíveis
- Deve haver uma confirmação visual de que o perfil foi salvo com sucesso

**Estimativa:** 3 pontos

#### US-302: Aplicação de Perfil

**Como** usuário do Fulcrum,  
**Eu quero** aplicar um perfil salvo anteriormente,  
**Para que** eu possa alternar rapidamente entre diferentes ambientes sonoros.

**Critérios de Aceitação:**
- A lista de perfis disponíveis deve ser facilmente acessível
- Cada perfil deve mostrar seu nome e descrição
- Deve haver um botão "Aplicar" para cada perfil
- Ao aplicar um perfil, todos os sons devem ser ajustados de acordo com suas configurações
- Deve haver uma confirmação visual de que o perfil foi aplicado com sucesso
- O perfil ativo deve ser claramente indicado na interface

**Estimativa:** 3 pontos

#### US-303: Remoção de Perfil

**Como** usuário do Fulcrum,  
**Eu quero** remover perfis que não uso mais,  
**Para que** eu possa manter organizada minha lista de perfis.

**Critérios de Aceitação:**
- Cada perfil deve ter uma opção de remoção
- Ao selecionar remover, deve ser exibida uma confirmação
- Após a remoção, o perfil deve desaparecer imediatamente da lista
- Se o perfil removido estiver ativo, o sistema deve lidar corretamente com essa situação
- Deve haver uma confirmação visual de que o perfil foi removido com sucesso

**Estimativa:** 2 pontos

### EPIC-04: Temporizador de Sono

#### US-401: Configuração de Temporizador

**Como** usuário do Fulcrum,  
**Eu quero** configurar um temporizador para que a reprodução pare automaticamente,  
**Para que** eu possa adormecer ouvindo os sons sem me preocupar em desligá-los manualmente.

**Critérios de Aceitação:**
- Deve haver um botão "Definir Timer" facilmente acessível
- Ao clicar, deve ser exibido um diálogo com opções de duração
- As opções devem incluir: 5min, 15min, 30min, 45min, 1h, 2h, 3h, 4h, 8h
- Após a seleção, o temporizador deve iniciar imediatamente
- Deve haver uma confirmação visual de que o temporizador foi iniciado

**Estimativa:** 3 pontos

#### US-402: Exibição de Tempo Restante

**Como** usuário do Fulcrum,  
**Eu quero** ver quanto tempo resta no temporizador de sono,  
**Para que** eu possa saber quando a reprodução será interrompida.

**Critérios de Aceitação:**
- Quando um temporizador estiver ativo, deve haver um indicador visível do tempo restante
- O tempo restante deve ser atualizado periodicamente (a cada minuto)
- O formato deve ser claro e legível (ex: "45:12" para 45 minutos e 12 segundos)
- O indicador deve permanecer visível mesmo ao navegar para outras seções do aplicativo

**Estimativa:** 2 pontos

#### US-403: Cancelamento de Temporizador

**Como** usuário do Fulcrum,  
**Eu quero** cancelar um temporizador ativo,  
**Para que** eu possa continuar ouvindo os sons indefinidamente.

**Critérios de Aceitação:**
- Quando um temporizador estiver ativo, deve haver um botão "Cancelar Timer"
- Ao clicar, o temporizador deve ser imediatamente cancelado
- A reprodução dos sons deve continuar normalmente
- O indicador de tempo restante deve desaparecer
- Deve haver uma confirmação visual de que o temporizador foi cancelado

**Estimativa:** 1 ponto

### EPIC-05: Personalização de Som

#### US-501: Ajuste de Equalização

**Como** usuário do Fulcrum,  
**Eu quero** ajustar a equalização para cada som individual,  
**Para que** eu possa personalizar a qualidade sonora de acordo com minhas preferências.

**Critérios de Aceitação:**
- Cada som deve ter acesso a controles de equalização
- Deve ser possível ajustar no mínimo três bandas (graves, médios, agudos)
- Cada banda deve ser ajustável na faixa de -12dB a +12dB
- As alterações devem ser aplicadas em tempo real
- Deve haver uma opção para restaurar configurações padrão
- As configurações de equalização devem ser salvas automaticamente

**Estimativa:** 5 pontos

#### US-502: Aplicação de Efeitos

**Como** usuário do Fulcrum,  
**Eu quero** aplicar diferentes efeitos aos sons,  
**Para que** eu possa criar atmosferas sonoras únicas e personalizadas.

**Critérios de Aceitação:**
- Deve ser possível selecionar entre diferentes tipos de efeitos
- Os efeitos disponíveis devem incluir: Reverberação, Echo, Pitch, Flanger
- Cada efeito deve ter parâmetros ajustáveis
- Deve ser possível ativar/desativar cada efeito individualmente
- As alterações devem ser aplicadas em tempo real
- As configurações de efeitos devem ser salvas automaticamente

**Estimativa:** 8 pontos

### EPIC-06: Configurações do Sistema

#### US-601: Seleção de Tema

**Como** usuário do Fulcrum,  
**Eu quero** escolher entre temas claro, escuro ou automático,  
**Para que** o aplicativo se adapte às minhas preferências visuais e ao ambiente.

**Critérios de Aceitação:**
- Deve haver uma opção para selecionar o tema na página de configurações
- As opções devem incluir: Claro, Escuro, Automático (baseado no sistema)
- A mudança de tema deve ser aplicada imediatamente
- Todos os elementos da interface devem responder corretamente à mudança de tema
- A configuração deve persistir entre sessões

**Estimativa:** 3 pontos

#### US-602: Configuração de Atalhos de Teclado

**Como** usuário do Fulcrum,  
**Eu quero** habilitar e usar atalhos de teclado para funções comuns,  
**Para que** eu possa controlar o aplicativo mais rapidamente.

**Critérios de Aceitação:**
- Deve haver uma opção para habilitar/desabilitar atalhos de teclado globais
- Os atalhos disponíveis devem ser claramente listados
- Deve ser possível usar atalhos mesmo quando o aplicativo não está em foco
- Os atalhos devem responder rapidamente às teclas pressionadas
- A configuração deve persistir entre sessões

**Estimativa:** 3 pontos

### EPIC-07: Acessibilidade

#### US-701: Compatibilidade com Leitor de Tela

**Como** usuário com deficiência visual,  
**Eu quero** que o aplicativo funcione bem com o Narrador do Windows,  
**Para que** eu possa usar todas as funcionalidades independentemente de minha visão.

**Critérios de Aceitação:**
- Todos os controles interativos devem ter rótulos adequados para leitores de tela
- A ordem de navegação por teclado deve ser lógica
- Todas as imagens e ícones devem ter texto alternativo
- As mudanças de estado devem ser anunciadas pelo leitor de tela
- O aplicativo deve passar nos testes de acessibilidade do Windows

**Estimativa:** 5 pontos

#### US-702: Navegação por Teclado

**Como** usuário com mobilidade limitada,  
**Eu quero** navegar e controlar todo o aplicativo usando apenas o teclado,  
**Para que** eu não precise usar um mouse ou tela touch.

**Critérios de Aceitação:**
- Todos os elementos interativos devem ser acessíveis por teclado
- Deve haver indicadores visuais claros do foco atual
- Deve ser possível iniciar/parar sons usando apenas teclado
- Deve ser possível ajustar volumes usando apenas teclado
- Deve ser possível navegar entre todas as seções usando apenas teclado

**Estimativa:** 3 pontos

## 4. Mapa de Histórias

Abaixo está o mapeamento de histórias para as diferentes iterações planejadas:

### MVP (Minimum Viable Product)
- US-101: Reprodução de Som Individual
- US-102: Controle de Volume Individual
- US-103: Reprodução Global
- US-104: Persistência de Estado
- US-201: Visualização de Forma de Onda
- US-601: Seleção de Tema

### Versão 1.1
- US-301: Criação de Perfil
- US-302: Aplicação de Perfil
- US-303: Remoção de Perfil
- US-202: Indicadores Visuais de Estado

### Versão 1.2
- US-401: Configuração de Temporizador
- US-402: Exibição de Tempo Restante
- US-403: Cancelamento de Temporizador
- US-501: Ajuste de Equalização

### Versão 1.3
- US-502: Aplicação de Efeitos
- US-602: Configuração de Atalhos de Teclado
- US-701: Compatibilidade com Leitor de Tela
- US-702: Navegação por Teclado

## 5. Backlog Priorizado

1. US-101: Reprodução de Som Individual (Alta)
2. US-102: Controle de Volume Individual (Alta)
3. US-104: Persistência de Estado (Alta)
4. US-103: Reprodução Global (Média)
5. US-201: Visualização de Forma de Onda (Média)
6. US-601: Seleção de Tema (Média)
7. US-301: Criação de Perfil (Média)
8. US-302: Aplicação de Perfil (Média)
9. US-401: Configuração de Temporizador (Média)
10. US-202: Indicadores Visuais de Estado (Baixa)
11. US-303: Remoção de Perfil (Baixa)
12. US-402: Exibição de Tempo Restante (Baixa)
13. US-403: Cancelamento de Temporizador (Baixa)
14. US-501: Ajuste de Equalização (Baixa)
15. US-502: Aplicação de Efeitos (Baixa)
16. US-602: Configuração de Atalhos de Teclado (Baixa)
17. US-701: Compatibilidade com Leitor de Tela (Baixa)
18. US-702: Navegação por Teclado (Baixa)