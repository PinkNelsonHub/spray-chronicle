using System;
using System.Text;
using Microsoft.Extensions.Logging;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Newtonsoft.Json;
using SprayChronicle.EventHandling;
using SprayChronicle.EventSourcing;

namespace SprayChronicle.Persistence.Ouro
{
    public sealed class PersistentStream : IStream
    {
        readonly ILogger<IEventStore> _logger;

        readonly IEventStoreConnection _eventStore;

        readonly UserCredentials _credentials;

        readonly ILocateTypes _typeLocator;

        readonly string _streamName;

        readonly string _groupName;

        public PersistentStream(
            ILogger<IEventStore> logger,
            IEventStoreConnection eventStore,
            UserCredentials credentials,
            ILocateTypes typeLocator,
            string streamName,
            string groupName)
        {
            _logger = logger;
            _eventStore = eventStore;
            _credentials = credentials;
            _typeLocator = typeLocator;
            _streamName = streamName;
            _groupName = groupName;
        }

        public void OnEvent(Action<object,DateTime> callback)
        {
             try {
                _eventStore.CreatePersistentSubscriptionAsync(
                    _streamName,
                    _groupName,
                    PersistentSubscriptionSettings.Create()
                        .ResolveLinkTos()
                        .StartFromBeginning()
                        .Build(),
                    _credentials
                ).Wait();
            } catch (AggregateException) {
                _logger.LogDebug("Persistent subscription {0}_{1} already exists!", _streamName, _groupName);
            }

            _eventStore.ConnectToPersistentSubscription(
                _streamName,
                _groupName,
                (subscription, resolvedEvent) => {
                    try {
                        var type = _typeLocator.Locate(resolvedEvent.Event.EventType);

                        if (null == type) {
                            _logger.LogDebug("[{0}] unknown type", _streamName);
                            subscription.Acknowledge(resolvedEvent);
                            return;
                        }

                        callback(
                            JsonConvert.DeserializeObject(
                                Encoding.UTF8.GetString(resolvedEvent.Event.Data),
                                type
                            ),
                            resolvedEvent.Event.Created
                        );
                    
                        subscription.Acknowledge(resolvedEvent);
                    } catch (Exception error) {
                        _logger.LogWarning("Persistent subscription {0}_{1} failure: {2}", _streamName, _groupName, error);
                        subscription.Fail(resolvedEvent, PersistentSubscriptionNakEventAction.Park, error.ToString());
                        return;
                    }
                },
                (subscription, reason, error) => {
                    _logger.LogCritical("Persistent subscription {0}_{1} error: {2}, {3}", _streamName, _groupName, reason.ToString(), error.ToString());
                },
                _credentials
            );
        }
    }
}
