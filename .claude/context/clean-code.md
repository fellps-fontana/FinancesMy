# Clean Code — padroes do projeto

Este documento alimenta DOIS modos: ao codar, aplique; ao revisar, cobre.
A fonte e a mesma — codigo escrito e codigo revisado seguem o mesmo criterio.

---

## Principios gerais

- Priorize legibilidade e manutencao sobre esperteza.
- Prefira programacao tradicional e solucoes pragmaticas antes de abstracoes
  complexas. Nao introduza padrao de projeto sem necessidade real.
- Nomes claros e descritivos. O nome deve explicar a intencao sem comentario.
- Funcao faz UMA coisa. Se precisa de "e" para descrever, divida.
- Evite duplicacao, mas nao crie abstracao prematura para dois casos.

## Comentarios

- Use comentario apenas quando necessario (o "porque", nao o "o que").
- Em comentarios de codigo fonte, NAO usar acentuacao.

## Organizacao (.NET)

- Camadas separadas: Controller (entrada) -> Service (regra) -> Repository
  (dados). Regra de negocio mora no Service, nunca no Controller.
- DTOs para entrada/saida da API; nunca exponha a entity diretamente.
- A regra de negocio (regra-de-negocio.md) vive no codigo de dominio/Service,
  de forma explicita e testavel. Ex: a regra de sinal (DEBIT/CREDIT) deve ser
  uma funcao nomeada, nao um if solto espalhado.

## Organizacao (React)

- Componente de apresentacao (burro) separado da logica de estado/dados.
- Um componente por feature/rota, espelhando os modulos do app.
- Estado de servidor (dados da API) separado de estado de UI. Preferir um
  data layer dedicado (ex: hook de fetch ou React Query) em vez de espalhar
  fetch nos componentes.
- Tipagem forte; evitar `any`.
- Logica de calculo (sinal, projecao) NAO vive no componente — vem do back ou
  de funcao util testavel.

## Tratamento de erro

- Falha de sync ou de API externa nao pode quebrar o app silenciosamente.
- Logar com contexto suficiente para diagnostico.

## O que evitar

- Numeros e strings magicos soltos. Use constantes/enums nomeados
  (ex: status PENDENTE/SUGERIDO/PAGO como enum, nao string crua espalhada).
- Metodo gigante que faz sync + dedup + conciliacao + categorizacao junto.
  Separe cada etapa do sync em metodo proprio (ver regra item 11).
