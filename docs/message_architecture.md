# Arquitetura de Consumo de Mensagens

## Objetivo

A implementação foi desenhada para desacoplar o consumo de mensagens do broker específico e o parse de logs do formato específico, permitindo que diferentes tecnologias (ex.: RabbitMQ, Kafka, Azure Service Bus) e diferentes formatos de log possam ser trocados sem alterar o worker que processa as mensagens.

---

## Abstrações principais

### `IMessageConsumer`

Define o ponto de extensão para qualquer broker de mensagens:

- Trocar o broker sem impactar o worker.
- Definir qualquer lógica de processamento via delegate `onMessageReceived`.
- Manter o worker focado em **processar mensagem**, e não em detalhes de conexão/protocolo.

### `ILogParser`

Define o ponto de extensão para qualquer formato de arquivo de log:

- Trocar o formato de log sem impactar o worker.
- Cada implementação é responsável por interpretar um formato específico.
- Novos formatos (Nginx, IIS, etc.) devem ser novas implementações, sem modificar as existentes.

---

## Implementações atuais

### `RabbitMQConsumer`

Implementa `IMessageConsumer` usando `RabbitMQ.Client`. Lê `RABBITMQ_HOST` e `RABBITMQ_QUEUE` de variáveis de ambiente.

**Fluxo no `ConsumeAsync`:**

1. Cria conexão com RabbitMQ.
2. Cria canal (`IChannel`).
3. Declara a fila configurada.
4. Cria um `AsyncEventingBasicConsumer`.
5. Ao receber mensagem: converte bytes para `string` (UTF-8), executa o callback `onMessageReceived`, confirma com `BasicAckAsync`.
6. Inicia consumo com `autoAck: false` (ack manual).

### `LogParserResponseTime`

Implementa `ILogParser` para o **Apache Combined Log Format** com campo extra de tempo de resposta:

```
{ip} - - [{timestamp}] "{method} {url} {protocol}" {status} {bytes} "{referer}" "{user-agent}" {responseTime}
```

Utiliza parser posicional baseado em `Span<char>`, navegando pelos delimitadores estruturais do formato (`[`, `]`, `"`) em vez de regex. Extrai: `OriginIP`, `RequestURL` e `ResponseTime`.

---

## Worker consumidor: `MessageConsumerWorker`

O `MessageConsumerWorker` herda de `BackgroundService` e depende de `IMessageConsumer`, `ILogParser` e `ILogRepository`.

**Fluxo no `ExecuteAsync`:**

1. Inicia o consumo via `IMessageConsumer`.
2. Ao receber uma mensagem (arquivo de log completo), divide por `\n` — cada linha é um registro.
3. Para cada linha, chama `ILogParser.TryParseLog`.
4. Registros parseados com sucesso são persistidos no MongoDB via `ILogRepository` (collection `logs`).
5. Falhas são registradas como `Warning` com a linha original.
6. Ao final do lote, loga um resumo com total processado, sucessos e tempo médio por registro.

O worker não conhece RabbitMQ, o formato Apache nem o MongoDB — apenas os contratos de consumo, parse e persistência.

---

## Registro no DI (`Program.cs`)

| Interface | Implementação | Ciclo de vida |
|---|---|---|
| `IMessageConsumer` | `RabbitMQConsumer` | Singleton |
| `ILogParser` | `LogParserResponseTime` | Singleton |
| `ILogRepository` | `MongoLogRepository` | Singleton |
| `MessageConsumerWorker` | — | Hosted Service |

---

## Docker Compose

A solução roda com um único comando:

```bash
docker compose up --build
```

Serviços orquestrados:

| Serviço | Porta |
|---|---|
| RabbitMQ (AMQP) | 5672 |
| RabbitMQ Management | 15672 |
| LogAnalyzer.API | 8080 |
| LogAnalyzer.Worker | — |

O Worker aguarda o RabbitMQ estar saudável antes de iniciar (`healthcheck` + `depends_on`).

---

## Armazenamento: MongoDB

Duas collections com propósitos distintos:

| Collection | Schema | Conteúdo |
|---|---|---|
| `logs` | Fixo | Registros parseados: `OriginIP`, `RequestURL`, `ResponseTime` |
| `insights` | Flexível | Resultados do ML.NET — estrutura varia por tipo de análise (spike, trend, cluster, etc.) |

A collection `logs` tem schema fixo e é indexada por `RequestURL` e `ResponseTime` para suportar as queries de treinamento do ML.NET. A collection `insights` usa schema flexível porque cada algoritmo do ML.NET produz documentos estruturalmente diferentes.

O acesso ao MongoDB é abstraído por `ILogRepository` — a implementação `MongoLogRepository` fica em Infrastructure, e o Worker depende apenas da interface.

---

## Resumo da arquitetura

- **Três abstrações independentes**: broker (`IMessageConsumer`), formato de log (`ILogParser`) e persistência (`ILogRepository`).
- **Mensagem = arquivo completo**: cada mensagem na fila contém N linhas; o worker faz o split e processa linha a linha.
- **Parse único**: cada linha é parseada uma vez na ingestão e armazenada como documento estruturado — o ML.NET consome dados limpos sem re-parsing.
- **Performance**: parser posicional com `Span<char>` opera em ~7µs/registro em steady-state (após JIT warmup). O gargalo em desenvolvimento é o logging de Debug (~125µs/registro).
- A arquitetura está preparada para evolução com novos brokers, novos formatos de log e troca de banco sem alterar o worker.
