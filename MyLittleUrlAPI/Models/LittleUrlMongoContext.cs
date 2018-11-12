using System;
using System.Collections.Generic;
using System.IO;

using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;

namespace MyLittleUrlAPI.Models
{
    public class LittleUrlMongoContext
    {
        private static IMongoDatabase _myMongoDb;
        private string _mongoCollection;
        private int _retentionDays;
        private DateTime _lastPurgeDate;
        private DateTime _lastRetentionDate;

        private static IMongoDatabase GetMongoDatabase(string mongoClient, string mongoPort, string mongoDatabase)
        {
            MongoClient mongoClientObj = new MongoClient("mongodb://" + mongoClient + ":" + mongoPort);
            return mongoClientObj.GetDatabase(mongoDatabase);
        }

        public List<LittleUrl> littleUrlList => _myMongoDb.GetCollection<LittleUrl>(_mongoCollection)
              .Find<LittleUrl>(FilterDefinition<LittleUrl>.Empty).ToList<LittleUrl>();

        public int maxId => _myMongoDb.GetCollection<LittleUrl>(_mongoCollection)
                                        .Find<LittleUrl>(FilterDefinition<LittleUrl>.Empty)
                                        .SortByDescending(url => url.UrlId)
                                        .FirstOrDefault().UrlId;

        public LittleUrlMongoContext()
        {
            // Read from config file
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");   

            // Read purge window from config; if not valid, use 90 as default
            string purgeAfter = configBuilder.Build().GetValue<string>("MongoConnection:RetentionDays");
            _retentionDays = int.TryParse(purgeAfter, out _retentionDays) ? _retentionDays : 90;
           
            string mongoClient = configBuilder.Build().GetValue<string>("MongoConnection:MongoClient");
            string mongoPort = configBuilder.Build().GetValue<string>("MongoConnection:MongoPort");
            string mongoDatabase = configBuilder.Build().GetValue<string>("MongoConnection:MongoDatabase");
            _mongoCollection = configBuilder.Build().GetValue<string>("MongoConnection:MongoCollection");

            _myMongoDb = GetMongoDatabase(mongoClient, mongoPort, mongoDatabase);
            _lastPurgeDate = DateTime.MinValue;
            _lastRetentionDate = DateTime.MinValue;
        }

        public void InsertUrl(LittleUrl url)
        {
            try
            {
                _myMongoDb.GetCollection<LittleUrl>(_mongoCollection).InsertOne(url);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return;
        }

        public LittleUrl ToggleDelete(string shortUrl, bool deleteFlag)
        {
            LittleUrl returnUrl = null;

            try
            {
                DateTime purgeDate = DateTime.UtcNow.AddDays(_retentionDays);

                // Logical Delete or Undelete
                if (deleteFlag)
                {
                    // Delete
                    var urlUpdates = Builders<LittleUrl>.Update
                        //  Set IsDeleted = True
                        .Set(url => url.IsDeleted, deleteFlag)
                        //  Set DeletedDate = Now
                        .Set(url => url.DeletedTime, DateTime.UtcNow)
                        //  Set PurgeDate = 90 days out (at midnight)
                        .Set(url => url.PurgeDate, new DateTime(purgeDate.Year,
                                                                purgeDate.Month,
                                                                purgeDate.Day,
                                                                23,
                                                                59,
                                                                59,
                                                                999,
                                                                DateTimeKind.Utc));

                    // Update if found
                    returnUrl = _myMongoDb.GetCollection<LittleUrl>(_mongoCollection)
                                          .FindOneAndUpdate<LittleUrl>(
                                              url => url.ShortUrl == shortUrl,
                                              urlUpdates);
                }
                else 
                {
                    // Undelete
                    var urlUpdates = Builders<LittleUrl>.Update
                        //  Set IsDeleted = False
                        .Set(url => url.IsDeleted, deleteFlag)
                        //  Set DeletedDate = Reset
                        .Set(url => url.DeletedTime, DateTime.MinValue)
                        //  Set PurgeDate = Reset
                        .Set(url => url.PurgeDate, DateTime.MinValue);
                
                    // Update if found
                    returnUrl = _myMongoDb.GetCollection<LittleUrl>(_mongoCollection)
                                          .FindOneAndUpdate<LittleUrl>(
                                              url => url.ShortUrl == shortUrl,
                                              urlUpdates);
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return returnUrl;
        }

        public LittleUrl GetUrl(string shortUrl, bool deleted)
        {
            try
            {
                // Call purge when a fetch operation is called
                //   Purge runs just once a day
                if (_lastPurgeDate.AddDays(1) < DateTime.UtcNow)
                    PurgeUrls();

                if (!string.IsNullOrEmpty(shortUrl))
                {
                    // Find matching and undeleted URL
                    var query = Builders<LittleUrl>.Filter.And(
                        Builders<LittleUrl>.Filter.Where(url => url.ShortUrl == shortUrl),
                        Builders<LittleUrl>.Filter.Or(
                            Builders<LittleUrl>.Filter.Exists(url => url.IsDeleted, false),
                            Builders<LittleUrl>.Filter.Where(url => url.IsDeleted == deleted)
                        )
                    );

                    LittleUrl retUrl = _myMongoDb.GetCollection<LittleUrl>(_mongoCollection)
                                                 .Find<LittleUrl>(query)
                                                 .FirstOrDefault();

                    if (retUrl != null)
                    {
                        // Update last accessed time
                        TouchUrl(shortUrl);
                        return retUrl;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return null;
        }

        public LittleUrl CheckUrl(string longUrl)
        {
            try
            {
                if (!string.IsNullOrEmpty(longUrl))
                {
                    LittleUrl retUrl = _myMongoDb.GetCollection<LittleUrl>(_mongoCollection)
                                                 .Find<LittleUrl>(url => url.LongUrl == longUrl).FirstOrDefault();
                    if (retUrl != null)
                    {
                        // If the Url is deleted, undelete it before returning
                        if (retUrl.IsDeleted)
                            retUrl = ToggleDelete(retUrl.ShortUrl, false);
                        
                        return retUrl;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return null;
        }

        private void PurgeOneUrl(string shortUrl)
        {
            try
            {
                _myMongoDb.GetCollection<LittleUrl>(_mongoCollection)
                          .DeleteOne<LittleUrl>(url => url.ShortUrl == shortUrl);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return;
        }

        private void PurgeUrls()
        {
            try{
                // Purge all logically deleted urls needing purge today
                var filter = Builders<LittleUrl>.Filter;
                var query = filter.And(
                                filter.Eq(url => url.IsDeleted, true),
                                filter.Lte(url => url.PurgeDate, _lastPurgeDate));
                
                _myMongoDb.GetCollection<LittleUrl>(_mongoCollection)
                          .DeleteMany(query);

                // Set last run date
                _lastPurgeDate = DateTime.UtcNow;
            }
            catch (Exception ex){
                throw ex;
            }
            return;
        }

        private void RemoveStaleUrls()
        {
            // Logically delete Urls not accessed for last x (90) days
            try
            {
                DateTime purgeDate = DateTime.UtcNow.AddDays(_retentionDays);

                // Logical Delete
                var urlUpdates = Builders<LittleUrl>.Update
                        //  Set IsDeleted = True
                        .Set(url => url.IsDeleted, true)
                        //  Set DeletedDate = Now
                        .Set(url => url.DeletedTime, DateTime.UtcNow)
                        //  Set PurgeDate = 90 days out (midnight)
                        .Set(url => url.PurgeDate, new DateTime(purgeDate.Year,
                                                                purgeDate.Month,
                                                                purgeDate.Day,
                                                                23,
                                                                59,
                                                                59,
                                                                999,
                                                                DateTimeKind.Utc));                

                // Update all that weren't accessed for the past x (90) days
                var filter = Builders<LittleUrl>.Filter;
                var query = filter.Or(
                                filter.Exists(url => url.LastAccessedTime, false),
                                filter.Gte(url => url.LastAccessedTime, DateTime.UtcNow.AddDays(_retentionDays)));
                
                _myMongoDb.GetCollection<LittleUrl>(_mongoCollection)
                          .UpdateMany(
                              query,
                              urlUpdates
                             );

                // Set last run date
                _lastRetentionDate = DateTime.UtcNow;

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return;
            
        }

        private void TouchUrl(string shortUrl)
        {
            try
            {
                // Logical Delete
                var urlUpdates = Builders<LittleUrl>.Update
                    //  Set LastAccessedTime = Now
                    .Set(url => url.LastAccessedTime, DateTime.UtcNow);
                        
                // Update if found
                _myMongoDb.GetCollection<LittleUrl>(_mongoCollection)
                          .FindOneAndUpdate<LittleUrl>(
                              url => url.ShortUrl == shortUrl,
                              urlUpdates
                             );

                // Perform retention management once a day
                if (_lastRetentionDate.AddDays(1) < DateTime.UtcNow)
                    RemoveStaleUrls();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return;
        }

 
    }
}
