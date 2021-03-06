using System;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Newtonsoft.Json;
using SprayChronicle.EventSourcing;

namespace SprayChronicle.Persistence.Ouro
{
    public sealed class OuroEventStore : IEventStore
    {
        readonly ILogger<IEventStore> _logger;

        readonly IEventStoreConnection _eventStore;

        readonly UserCredentials _credentials;

        public OuroEventStore(
            ILogger<IEventStore> logger,
            IEventStoreConnection eventStore,
            UserCredentials credentials)
        {
            _logger = logger;
            _eventStore = eventStore;
            _credentials = credentials;
        }

        public void Append<T>(string identity, IEnumerable<DomainMessage> domainMessages)
        {
            if (0 == domainMessages.Count()) {
                return;
            }
            
            try {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                _eventStore.AppendToStreamAsync(
                    Stream<T>(identity),
                    domainMessages.First().Sequence - 1,
                    domainMessages.Select(dm => BuildEventData(dm)),
                    _credentials
                ).Wait();

                stopwatch.Stop();
                _logger.LogDebug("[{0}::append] {1}ms", Stream<T>(identity), stopwatch.ElapsedMilliseconds);
            } catch (AggregateException error) {
                throw new ConcurrencyException(string.Format(
                    "Concurrency detected: {0}",
                    error.InnerException.Message
                ));
            }
        }

        public IEnumerable<DomainMessage> Load<T>(string identity)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            bool eos = false;
            int current = 0;

            do {
                var slice = _eventStore.ReadStreamEventsForwardAsync(Stream<T>(identity), current, 50, false, _credentials).Result;
                foreach (DomainMessage domainMessage in slice.Events.Select(ev => BuildDomainMessage(ev))) {
                    yield return domainMessage;
                    current++;
                }
                eos = slice.IsEndOfStream;
            } while (!eos);

            stopwatch.Stop();
            _logger.LogDebug("[{0}::load] {1}ms", Stream<T>(identity), stopwatch.ElapsedMilliseconds);
        }

        string Stream<T>(string identity)
        {
            return string.Format(
                "{0}-{1}",
                typeof(T).Namespace.Split('.').First(),
                identity
            );
        }

        EventData BuildEventData(DomainMessage domainMessage)
        {
            return new EventData(
                Guid.NewGuid(),
                domainMessage.Payload.GetType().Name,
                true,
                Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(domainMessage.Payload)),
                Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new Metadata(domainMessage.Payload.GetType())))
            );
        }

        DomainMessage BuildDomainMessage(ResolvedEvent resolvedEvent)
        {
            var metadata = JsonConvert.DeserializeObject<Metadata>(Encoding.UTF8.GetString(resolvedEvent.Event.Metadata));

            return new DomainMessage(
                resolvedEvent.Event.EventNumber,
                resolvedEvent.Event.Created,
                JsonConvert.DeserializeObject(
                    Encoding.UTF8.GetString(resolvedEvent.Event.Data),
                    Type.GetType(metadata.OriginalFqn)
                )
            );
        }

        public class Metadata
        {
            public readonly string OriginalFqn;

            public Metadata(Type originalFqn)
            {
                OriginalFqn = string.Format(
                    "{0}, {1}",
                    originalFqn.ToString(),
                    originalFqn.GetTypeInfo().Assembly
                );
            }
        }
    }
}
