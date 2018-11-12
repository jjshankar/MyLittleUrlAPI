using System;
using MongoDB.Bson;

namespace MyLittleUrlAPI.Models
{
    public class LittleUrl
    {       
        public ObjectId _id
        {
            get;
            set;
        }
        public int UrlId
        {
            get;
            set;
        }

        public string LongUrl
        {
            get;
            set;
        }

        public string ShortUrl
        {
            get;
            set;
        }

        public DateTime CreationTime => _id.CreationTime;

        public bool IsDeleted
        {
            get;
            set;
        }

        public DateTime LastAccessedTime
        {
            get;
            set;
        }

        public DateTime DeletedTime
        {
            get;
            set;
        }

        public DateTime PurgeDate
        {
            get;
            set;
        }
    }
}
