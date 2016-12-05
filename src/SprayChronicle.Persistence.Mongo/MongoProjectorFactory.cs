using System;
using Microsoft.Extensions.Logging;
using SprayChronicle.EventHandling;
using SprayChronicle.EventHandling.Projecting;

namespace SprayChronicle.Persistence.Mongo
{
    public sealed class MongoProjectorFactory : IBuildProjectors
    {
        readonly ILogger<IStream> _logger;

        readonly MongoRepositoryFactory _repositoryFactory;

        public MongoProjectorFactory(ILogger<IStream> logger, MongoRepositoryFactory repositoryFactory)
        {
            _logger = logger;
            _repositoryFactory = repositoryFactory;
        }

        public TProjector Build<TProjection,TProjector>() where TProjector : Projector<TProjection>
        {
            return (TProjector) Activator.CreateInstance(
                typeof(TProjector),
                new BufferedRepository<TProjection>(
                    _logger,
                    _repositoryFactory.Build<TProjection>()
                )
            );
        }

        public TProjector Build<TProjection,TProjector>(string projectionReference) where TProjector : Projector<TProjection>
        {
            return (TProjector) Activator.CreateInstance(
                typeof(TProjector),
                new BufferedRepository<TProjection>(
                    _logger,
                    _repositoryFactory.Build<TProjection>(projectionReference)
                )
            );
        }

        public TProjector Build<TProjection,TProjector>(IProjectionRepository<TProjection> repository) where TProjector : Projector<TProjection>
        {
            return (TProjector) Activator.CreateInstance(
                typeof(TProjector),
                new BufferedRepository<TProjection>(
                    _logger,
                    repository
                )
            );
        }
    }
}
