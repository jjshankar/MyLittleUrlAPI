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

        private static IMongoDatabase GetMongoDatabase(string mongoClient, string mongoPort, string mongoDatabase)
        {
            MongoClient mongoClientObj = new MongoClient("mongodb://" + mongoClient + ":" + mongoPort);
            return mongoClientObj.GetDatabase(mongoDatabase);
        }


        public LittleUrlMongoContext()
        {
            // Read from config file
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");   

            string mongoClient = configBuilder.Build().GetValue<string>("MongoConnection:MongoClient");
            string mongoPort = configBuilder.Build().GetValue<string>("MongoConnection:MongoPort");
            string mongoDatabase = configBuilder.Build().GetValue<string>("MongoConnection:MongoDatabase");
            _mongoCollection = configBuilder.Build().GetValue<string>("MongoConnection:MongoCollection");

            _myMongoDb = GetMongoDatabase(mongoClient, mongoPort, mongoDatabase);
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

        public LittleUrl GetUrl(string shortUrl)
        {
            try
            {
                if (!string.IsNullOrEmpty(shortUrl))
                {
                    LittleUrl retUrl = _myMongoDb.GetCollection<LittleUrl>(_mongoCollection)
                                                 .Find<LittleUrl>(url => url.ShortUrl == shortUrl).FirstOrDefault();
                    if (retUrl != null)
                        return retUrl;
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
                        return retUrl;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return null;
        }

        public List<LittleUrl> littleUrlList => 
                    _myMongoDb.GetCollection<LittleUrl>(_mongoCollection)
                              .Find<LittleUrl>(FilterDefinition<LittleUrl>.Empty).ToList<LittleUrl>();
 
    }
}
