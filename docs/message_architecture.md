# Arquitetura de Consumo de Mensagens

## Objetivo

A implementação atual foi desenhada para desacoplar o consumo de mensagens do broker específico, permitindo que diferentes tecnologias (ex.: RabbitMQ, Kafka, Azure Service Bus) possam ser usadas sem alterar o worker que processa as mensagens.

---

## Abstração principal: `IMessageConsumer`

O contrato `IMessageConsumer` define o ponto de extensão para qualquer broker:

### O que isso permite

- Trocar o broker sem impactar o worker.
- Definir qualquer lógica de processamento via delegate `onMessageReceived`.
- Manter o worker focado em **processar mensagem**, e não em detalhes de conexão/protocolo.

---

## Implementação atual: `RabbitMQConsumer`

A classe `RabbitMQConsumer` implementa `IMessageConsumer` usando `RabbitMQ.Client`.

### Fluxo atual no `ConsumeAsync`

1. Cria conexão com RabbitMQ (`HostName = "localhost"`).
2. Cria canal (`IChannel`).
3. Declara a fila `log_queue`.
4. Cria um `AsyncEventingBasicConsumer`.
5. Ao receber mensagem:
   - converte bytes para `string` (UTF-8),
   - executa o callback `onMessageReceived`,
   - confirma o processamento com `BasicAckAsync`.
6. Inicia consumo com `autoAck: false` (ack manual).

---

## Worker consumidor: `MessageConsumerWorker`

O `MessageConsumerWorker` herda de `BackgroundService` e depende apenas de `IMessageConsumer`.

No `ExecuteAsync`, ele inicia o consumo.

Isso reforça a arquitetura: o worker não conhece RabbitMQ, apenas o contrato de consumo.

---

## Registro no DI (`Program.cs`)

A composição atual registra:

- `IMessageConsumer` → `RabbitMQConsumer` (singleton)
- `MessageConsumerWorker` como hosted service

Com isso, ao iniciar a aplicação, o worker é executado e passa a consumir mensagens da fila.

---

## Resumo da arquitetura atual

- Há uma **abstração de consumo** (`IMessageConsumer`).
- O broker atual é **RabbitMQ** (`RabbitMQConsumer`).
- O processamento da mensagem é injetado por função (`Func<string, Task>`), permitindo qualquer método de tratamento.
- A arquitetura já está preparada para evolução com novos brokers sem alterar o worker
