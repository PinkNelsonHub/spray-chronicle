using MongoDB.Driver;

namespace SprayChronicle.EventHandling.MongoDB
{
    public sealed class MongoRepositoryFactory
    {
        readonly IMongoDatabase _database;

        public MongoRepositoryFactory(IMongoDatabase database)
        {
            _database = database;
        }

        public IProjectionRepository<T> Build<T>()
        {
            return Build<T>(typeof(T).Name);
        }

        public IProjectionRepository<T> Build<T>(string reference)
        {
            return new MongoRepository<T>(_database.GetCollection<T>(reference));
        }
    }
}
