# Fulcrum - Documento de Requisitos Funcionais (FRD)

**Versão:** 1.0  
**Data:** 28/04/2025  
**Autor:** Equipe de Desenvolvimento Fulcrum

## 1. Introdução

### 1.1 Visão Geral do Documento

Este documento descreve em detalhes os requisitos funcionais do aplicativo Fulcrum, um reprodutor de sons ambientes para Windows 10/11. Ele serve como referência para todas as funcionalidades a serem implementadas, estabelecendo o comportamento esperado do sistema do ponto de vista do usuário.

### 1.2 Escopo

Este documento abrange todas as funcionalidades da aplicação Fulcrum, incluindo:
- Reprodução e mixagem de sons ambientes
- Visualização de ondas sonoras
- Gerenciamento de perfis de som
- Temporizador de sono
- Configurações do sistema
- Equalização e efeitos de áudio

### 1.3 Definições, Acrônimos e Abreviações

- **UI:** Interface do Usuário
- **UX:** Experiência do Usuário
- **EQ:** Equalizador
- **dB:** Decibéis, unidade de medida para intensidade sonora
- **PCM:** Pulse Code Modulation, formato de representação digital de sinais analógicos

## 2. Funcionalidades Principais

### 2.1 Reprodução de Som

#### 2.1.1 Iniciar/Parar Reprodução de Sons Individuais

**Descrição:** O sistema deve permitir que o usuário inicie ou pare a reprodução de cada som ambiente individualmente.

**Requisitos:**
- Cada som deve possuir um controle de reprodução individual
- A reprodução deve iniciar imediatamente após o clique (latência < 50ms)
- A reprodução deve ser em loop contínuo e sem falhas perceptíveis
- O estado de reprodução (ativo/inativo) deve ser claramente indicado visualmente

**Fluxo de Interação:**
1. Usuário clica no botão de reprodução associado a um som específico
2. O sistema inicia a reprodução do som selecionado
3. O indicador visual muda para mostrar que o som está ativo
4. Para parar, o usuário clica novamente no botão
5. O sistema interrompe a reprodução e atualiza o indicador visual

#### 2.1.2 Controle de Volume Individual

**Descrição:** O sistema deve permitir o ajuste de volume independente para cada som ambiente.

**Requisitos:**
- Cada som deve possuir um controle deslizante (slider) para ajuste de volume
- O volume deve ser ajustável em escala contínua de 0% a 100%
- Alterações de volume devem ser aplicadas em tempo real
- A posição atual do controle deslizante deve refletir com precisão o volume atual

**Fluxo de Interação:**
1. Usuário movimenta o controle deslizante associado a um som específico
2. O sistema ajusta o volume do som em tempo real
3. O sistema exibe visualmente o nível de volume atual

#### 2.1.3 Reprodução/Pausa Global

**Descrição:** O sistema deve fornecer controles para iniciar ou pausar simultaneamente todos os sons ativos.

**Requisitos:**
- Botão "Reproduzir Todos" para iniciar todos os sons com volumes diferentes de zero
- Botão "Pausar Todos" para pausar simultaneamente todos os sons em reprodução
- O estado dos botões deve refletir o estado atual do sistema (habilitado/desabilitado)

**Fluxo de Interação:**
1. Usuário clica no botão "Reproduzir Todos"
2. O sistema inicia a reprodução de todos os sons com volume > 0
3. Alternativamente, o usuário clica em "Pausar Todos"
4. O sistema pausa todos os sons em reprodução

### 2.2 Visualização de Ondas Sonoras

#### 2.2.1 Renderização de Forma de Onda

**Descrição:** O sistema deve exibir a representação visual das ondas sonoras para cada som ativo.

**Requisitos:**
- Cada som deve ter sua visualização de onda sonora correspondente
- A visualização deve ser atualizada em tempo real (30-60 FPS)
- A intensidade e forma da visualização deve corresponder ao áudio reproduzido
- As cores da visualização devem ser distintas para cada tipo de som
- A visualização deve ser compatível com os temas claro e escuro

**Comportamento:**
- A visualização deve permanecer estática quando o som estiver pausado
- A visualização deve ser animada quando o som estiver em reprodução
- A amplitude da visualização deve corresponder ao volume configurado

#### 2.2.2 Indicador de Atividade

**Descrição:** O sistema deve fornecer indicadores visuais claros do estado de cada som.

**Requisitos:**
- Indicação visual distinta entre sons ativos e inativos
- Transições suaves entre estados
- Indicadores acessíveis (com contraste adequado)

### 2.3 Temporizador de Sono

#### 2.3.1 Configuração de Temporizador

**Descrição:** O sistema deve permitir configurar um temporizador para interromper automaticamente a reprodução após um período determinado.

**Requisitos:**
- Interface para seleção de duração do temporizador
- Opções predefinidas: 5min, 15min, 30min, 45min, 1h, 2h, 3h, 4h, 8h
- Confirmação visual da configuração do temporizador
- Capacidade de modificar ou cancelar um temporizador ativo

**Fluxo de Interação:**
1. Usuário seleciona a opção "Definir Timer"
2. O sistema exibe um diálogo com opções de duração
3. Usuário seleciona a duração desejada
4. O sistema confirma a configuração e inicia a contagem regressiva
5. O sistema exibe um indicador do tempo restante

#### 2.3.2 Execução do Temporizador

**Descrição:** O sistema deve executar corretamente a contagem regressiva e realizar as ações configuradas ao término.

**Requisitos:**
- Contagem regressiva precisa
- Exibição do tempo restante atualizada periodicamente
- Pausa automática de todos os sons quando o temporizador chegar a zero
- Notificação visual quando o temporizador for concluído

**Comportamento:**
- O temporizador deve continuar funcionando mesmo se o usuário navegar para outras telas
- Quando concluído, deve pausar todos os sons e mostrar uma notificação
- O usuário deve poder cancelar o temporizador a qualquer momento

### 2.4 Perfis de Som

#### 2.4.1 Criação de Perfis

**Descrição:** O sistema deve permitir que o usuário salve as configurações atuais de som como um perfil nomeado.

**Requisitos:**
- Interface para criação de novo perfil
- Campos para nome e descrição do perfil
- Captura automática das configurações atuais (sons ativos e volumes)
- Validação para impedir nomes duplicados ou inválidos

**Fluxo de Interação:**
1. Usuário configura os sons e volumes desejados
2. Usuário seleciona "Novo Perfil"
3. O sistema exibe um diálogo solicitando nome e descrição
4. Usuário insere as informações e confirma
5. O sistema salva o perfil e exibe confirmação visual

#### 2.4.2 Aplicação de Perfis

**Descrição:** O sistema deve permitir aplicar rapidamente um perfil salvo anteriormente.

**Requisitos:**
- Lista de perfis salvos com nomes e descrições
- Mecanismo para aplicar um perfil selecionado
- Transição suave entre configurações ao aplicar um perfil
- Indicação visual do perfil atualmente ativo

**Fluxo de Interação:**
1. Usuário navega para a tela de perfis
2. O sistema exibe a lista de perfis salvos
3. Usuário seleciona um perfil e escolhe "Aplicar"
4. O sistema carrega as configurações do perfil
5. Os sons e volumes são ajustados de acordo com o perfil
6. O sistema exibe confirmação visual

#### 2.4.3 Exclusão de Perfis

**Descrição:** O sistema deve permitir a remoção de perfis salvos.

**Requisitos:**
- Opção para excluir perfis existentes
- Confirmação antes da exclusão permanente
- Tratamento adequado quando o perfil ativo for excluído

**Fluxo de Interação:**
1. Usuário seleciona um perfil na lista
2. Usuário escolhe "Remover"
3. O sistema solicita confirmação
4. Após confirmação, o sistema remove o perfil
5. O sistema atualiza a lista de perfis

### 2.5 Equalização e Efeitos

#### 2.5.1 Ajuste de Equalização

**Descrição:** O sistema deve permitir o ajuste de equalização para cada som individual.

**Requisitos:**
- Controles para ajuste de frequências baixas, médias e altas
- Faixa de ajuste de -12dB a +12dB
- Visualização gráfica da curva de equalização
- Aplicação em tempo real dos ajustes de equalização

**Comportamento:**
- As alterações na equalização devem ser aplicadas imediatamente
- O sistema deve manter a configuração de equalização para cada som individualmente
- O sistema deve oferecer opção para restaurar configurações padrão

#### 2.5.2 Efeitos de Áudio

**Descrição:** O sistema deve oferecer efeitos de processamento de áudio para melhorar a experiência sonora.

**Requisitos:**
- Efeitos disponíveis: Reverberação, Echo, Pitch, Flanger
- Controles ajustáveis para parâmetros de cada efeito
- Ativação/desativação independente para cada efeito
- Aplicação em tempo real dos ajustes de efeitos

**Parâmetros por Efeito:**
- **Reverberação:** Mix (0-100%), Tempo (0.1-10s)
- **Echo:** Delay (50-1000ms), Mix (0-100%)
- **Pitch:** Fator (0.5-2.0)
- **Flanger:** Taxa (0.1-10Hz), Profundidade (0.001-0.01)

### 2.6 Configurações do Sistema

#### 2.6.1 Temas

**Descrição:** O sistema deve permitir a seleção entre temas claro, escuro ou automático (baseado no tema do sistema).

**Requisitos:**
- Opções para tema claro, escuro ou automático
- Aplicação imediata da alteração de tema
- Persistência da configuração entre sessões
- Atualização automática quando o tema do sistema é alterado (no modo automático)

**Comportamento:**
- Todos os elementos da UI devem responder adequadamente à mudança de tema
- As visualizações de ondas sonoras devem manter o contraste adequado em ambos os temas

#### 2.6.2 Atalhos de Teclado

**Descrição:** O sistema deve oferecer atalhos de teclado globais para controle rápido das funções principais.

**Requisitos:**
- Opção para habilitar/desabilitar atalhos globais
- Atalhos para reproduzir/pausar todos os sons
- Atalhos para ajustar o volume principal
- Visualização clara dos atalhos disponíveis

**Atalhos Padrão:**
- Play/Pause Global: Ctrl+Space
- Aumentar Volume: Ctrl+Up
- Diminuir Volume: Ctrl+Down
- Aplicar Perfil Padrão: Ctrl+1

## 3. Requisitos de Interface

### 3.1 Interface Principal

#### 3.1.1 Navegação

**Descrição:** O sistema deve fornecer navegação clara e intuitiva entre as diferentes seções.

**Requisitos:**
- Menu de navegação lateral com ícones e rótulos
- Destaque visual para a seção atualmente selecionada
- Transições animadas entre seções
- Persistência do estado de reprodução durante a navegação

**Seções Principais:**
- Home (reprodução e controle de sons)
- Perfis
- Equalização e Efeitos
- Configurações
- Sobre

#### 3.1.2 Layout Responsivo

**Descrição:** A interface deve se adaptar a diferentes tamanhos e resoluções de tela.

**Requisitos:**
- Suporte para redimensionamento da janela
- Adaptação do layout para diferentes proporções de tela
- Visualização adequada em resoluções de 1280x720 até 4K
- Suporte para diferentes densidades de pixels (DPI)

### 3.2 Controles de Som

#### 3.2.1 Cartões de Som

**Descrição:** Cada som ambiente deve ser representado por um cartão visual distinto.

**Requisitos:**
- Imagem/ícone representativo para cada tipo de som
- Nome do som claramente visível
- Botão de reprodução/pausa
- Controle deslizante de volume
- Visualização da onda sonora
- Acesso rápido à equalização específica

**Layout do Cartão:**
- Ícone/imagem na parte superior
- Nome do som abaixo do ícone
- Botão de play/pause no canto superior direito
- Visualização de onda na parte central
- Controle deslizante de volume na parte inferior
- Botão de acesso à equalização no canto inferior direito

## 4. Requisitos de Usabilidade

### 4.1 Acessibilidade

#### 4.1.1 Requisitos de Acessibilidade Visual

**Descrição:** O sistema deve ser utilizável por pessoas com deficiência visual.

**Requisitos:**
- Suporte completo para leitores de tela (compatibilidade com narrador do Windows)
- Texto alternativo para todas as imagens e ícones
- Alto contraste para texto e elementos interativos
- Possibilidade de navegação completa por teclado
- Tamanho adequado para todos os elementos interativos

#### 4.1.2 Requisitos de Acessibilidade Auditiva

**Descrição:** O sistema deve fornecer feedback visual para todas as ações importantes.

**Requisitos:**
- Indicadores visuais para alterações de estado
- Notificações visuais para eventos do sistema
- Alternativas visuais para alertas sonoros

### 4.2 Feedback do Sistema

#### 4.2.1 Mensagens de Confirmação

**Descrição:** O sistema deve fornecer confirmação visual para ações importantes.

**Requisitos:**
- Notificações claras para ações concluídas (salvamento de perfil, aplicação de perfil, etc.)
- Estilo visual distinto para confirmações de sucesso
- Duração adequada da exibição (3-5 segundos)
- Posicionamento não intrusivo na interface

#### 4.2.2 Mensagens de Erro

**Descrição:** O sistema deve informar claramente sobre erros e problemas.

**Requisitos:**
- Notificações claras para erros ou problemas
- Linguagem compreensível para mensagens de erro
- Sugestões de resolução quando aplicável
- Estilo visual distinto para erros (vermelho/laranja)

## 5. Limitações e Restrições

### 5.1 Compatibilidade

- Compatível apenas com Windows 10 versão 1809 ou superior e Windows 11
- Requer dispositivo de áudio funcional
- Não disponível para modo Windows S

### 5.2 Desempenho

- Reprodução simultânea limitada a 10 sons de alta qualidade
- Efeitos avançados podem aumentar o consumo de CPU em hardware antigo
- Visualizações complexas podem impactar o desempenho em hardware de entrada

## 6. Apêndices

### 6.1 Mapeamento de Sons

| ID do Som | Nome Exibido | Tipo de Som | Arquivo de Áudio |
|-----------|--------------|-------------|------------------|
| chuva | Chuva | Natureza | chuva.mp3 |
| fogueira | Fogueira | Natureza | fogueira.mp3 |
| ondas | Ondas do Mar | Natureza | ondas.mp3 |
| passaros | Pássaros | Natureza | passaros.mp3 |
| praia | Praia | Natureza | praia.mp3 |
| trem | Trem | Ambiente | trem.mp3 |
| ventos | Ventos | Natureza | ventos.mp3 |
| cafeteria | Cafeteria | Ambiente | cafeteria.mp3 |
| lancha | Lancha | Ambiente | lancha.mp3 |

### 6.2 Mockups e Wireframes

Os wireframes e mockups de referência para as interfaces estão disponíveis nos seguintes locais:
- [Wireframes Iniciais](./wireframes/inicial/)
- [Mockups de Alta Fidelidade](./wireframes/finais/)
- [Fluxo de Navegação](./wireframes/fluxo.png)