
## 🔄 Fluxo de Funcionamento

1. **Geração de Logs**: A aplicação X gera logs durante sua execução
2. **Coleta**: O sistema de logging captura e formata os logs
3. **Enfileiramento**: Os logs são enviados para uma fila no RabbitMQ para processamento assíncrono
4. **Processamento**: Um worker consome a fila, parseia cada linha e persiste os registros estruturados no MongoDB (`logs`)
5. **Análise ML**: O ML.NET lê os dados da collection `logs` e executa análises de anomalia e tendência
6. **Geração de Insights**: Os resultados são gravados na collection `insights` (schema flexível por tipo de análise) e expostos via API

## 🛠️ Tecnologias Utilizadas

- **.NET 9**: Framework principal
- **RabbitMQ**: Sistema de filas de mensagens
- **MongoDB**: Armazenamento dos logs parseados (`logs`) e dos insights gerados pelo ML (`insights`)
- **ML.NET**: Análise inteligente de logs — detecção de anomalias, spikes e tendências

## 🚀 Benefícios

- **Processamento Assíncrono**: Uso de filas para não impactar a performance da aplicação principal
- **Parse único**: Cada linha de log é parseada uma vez na ingestão; o MongoDB armazena dados estruturados e indexáveis
- **Insights Inteligentes**: Análise automatizada através de ML.NET sobre dados já persistidos
- **Schema flexível para insights**: A collection `insights` acomoda formatos distintos por tipo de análise sem migrações

## 📊 Componentes

### Log Processor (Worker)
- Consome mensagens da fila RabbitMQ
- Parseia cada linha no formato Apache Combined Log com campo de response time
- Persiste registros estruturados (`OriginIP`, `RequestURL`, `ResponseTime`) na collection `logs`

### Engine de Machine Learning
- Lê dados da collection `logs` via repositório
- Detecta anomalias, spikes e tendências de response time por URL
- Grava os resultados na collection `insights` com schema variável por tipo de análise

### API
- Expõe os insights armazenados no MongoDB
- Permite consultas por URL, período e tipo de análise
