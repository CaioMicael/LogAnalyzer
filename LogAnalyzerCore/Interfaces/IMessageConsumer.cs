using RabbitMQ.Client;

namespace LogAnalyzer.LogAnalyzerCore.Interfaces
{
    /// <summary>
    /// Abstração para o consumo de mensagens.
    /// Essa abstração permite com que o broker seja trocado (RabbitMQ, Kafka, etc.) 
    /// sem impactar os workers que consomem as mensagens.
    /// </summary>
    public interface IMessageConsumer
    {
        /// <summary>
        /// Inicia o consumo de mensagens.
        /// A interface permite que seja aplicado por RabbitMQ, Kafka ou qualquer outro broker de mensagens, 
        /// desde que a implementação do método ConsumeAsync seja feita de acordo com o broker escolhido.
        /// </summary>
        /// <param name="onMessageReceived">Delegate que executa o processamento da mensagem</param>
        Task ConsumeAsync(Func<string, Task> onMessageReceived, CancellationToken cancellationToken);
    }
}
