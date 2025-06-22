using PlaylistsApi.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Bson;

namespace PlaylistsApi.Services;

public class MongoDBService 
{
    private readonly IMongoCollection<Playlist> _playlistCollection;

    public MongoDBService(IOptions<MongoDBSettings> mongoDBSettings) 
    {
    
        MongoClient client = new MongoClient(mongoDBSettings.Value.ConnectionURI);
        
    
        IMongoDatabase database = client.GetDatabase(mongoDBSettings.Value.DatabaseName);
        
    
        _playlistCollection = database.GetCollection<Playlist>(mongoDBSettings.Value.CollectionName);
    }

    public async Task CreateAsync(Playlist playlist) 
    {
        await _playlistCollection.InsertOneAsync(playlist);
    }
    public async Task<List<Playlist>> GetAsync() 
    {
        return await _playlistCollection.Find(new BsonDocument()).ToListAsync();
    }
    public async Task<Playlist?> GetByIdAsync(string id)
    {
        return await _playlistCollection.Find(p => p.Id == id).FirstOrDefaultAsync();
    }

    public async Task UpdateAsync(string id, Playlist updatedPlaylist)
    {
        await _playlistCollection.ReplaceOneAsync(p => p.Id == id, updatedPlaylist);
    }

    public async Task DeleteAsync(string id) 
    {
        await _playlistCollection.DeleteOneAsync(p => p.Id == id);
    }
}
