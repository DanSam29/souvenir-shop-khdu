using System.Collections.Concurrent;

namespace KhduSouvenirShop.API.Services
{
    // Повідомлення черги на відправлення листа (містить уже відрендерений HTML)
    public class EmailMessage
    {
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string HtmlBody { get; set; } = string.Empty;
        public string? Cc { get; set; }
        public string? Bcc { get; set; }
        public string? TemplateType { get; set; }
        public string? ReferenceId { get; set; }
    }

    public interface IEmailQueue
    {
        // Додає лист у чергу та будить фоновий обробник
        void Enqueue(EmailMessage message);
        // Очікує появи хоч одного повідомлення (використовується фоновим сервісом)
        Task WaitForMessageAsync(CancellationToken cancellationToken);
        // Витягує наступне повідомлення з черги
        bool TryDequeue(out EmailMessage message);
    }

    // Черга листів у пам'яті на базі ConcurrentQueue із семафором-сигналом (потокобезпечна)
    public class EmailQueue : IEmailQueue
    {
        private readonly ConcurrentQueue<EmailMessage> _queue = new();
        private readonly SemaphoreSlim _signal = new(0);

        public void Enqueue(EmailMessage message)
        {
            _queue.Enqueue(message);
            _signal.Release();
        }

        public Task WaitForMessageAsync(CancellationToken cancellationToken) =>
            _signal.WaitAsync(cancellationToken);

        public bool TryDequeue(out EmailMessage message) => _queue.TryDequeue(out message!);
    }
}
