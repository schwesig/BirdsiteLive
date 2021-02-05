﻿using System;
using System.Linq;
using System.Threading.Tasks;
using BirdsiteLive.DAL.Contracts;
using BirdsiteLive.DAL.Models;
using BirdsiteLive.DAL.Postgres.DataAccessLayers.Base;
using BirdsiteLive.DAL.Postgres.Settings;
using BirdsiteLive.DAL.Postgres.Tools;
using Dapper;
using Npgsql;

namespace BirdsiteLive.DAL.Postgres.DataAccessLayers
{
    public class TwitterUserPostgresDal : PostgresBase, ITwitterUserDal
    {
        #region Ctor
        public TwitterUserPostgresDal(PostgresSettings settings) : base(settings)
        {
            
        }
        #endregion

        public async Task CreateTwitterUserAsync(string acct, long lastTweetPostedId)
        {
            acct = acct.ToLowerInvariant();

            using (var dbConnection = Connection)
            {
                dbConnection.Open();

                await dbConnection.ExecuteAsync(
                    $"INSERT INTO {_settings.TwitterUserTableName} (acct,lastTweetPostedId,lastTweetSynchronizedForAllFollowersId) VALUES(@acct,@lastTweetPostedId,@lastTweetSynchronizedForAllFollowersId)",
                    new { acct, lastTweetPostedId, lastTweetSynchronizedForAllFollowersId = lastTweetPostedId });
            }
        }

        public async Task<SyncTwitterUser> GetTwitterUserAsync(string acct)
        {
            var query = $"SELECT * FROM {_settings.TwitterUserTableName} WHERE acct = @acct";

            acct = acct.ToLowerInvariant();

            using (var dbConnection = Connection)
            {
                dbConnection.Open();

                var result = (await dbConnection.QueryAsync<SyncTwitterUser>(query, new { acct = acct })).FirstOrDefault();
                return result;
            }
        }

        public async Task DeleteTwitterUserAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<int> GetTwitterUsersCountAsync()
        {
            var query = $"SELECT COUNT(*) FROM {_settings.TwitterUserTableName}";

            using (var dbConnection = Connection)
            {
                dbConnection.Open();

                var result = (await dbConnection.QueryAsync<int>(query)).FirstOrDefault();
                return result;
            }
        }

        public async Task<SyncTwitterUser[]> GetAllTwitterUsersAsync(int maxNumber)
        {
            var query = $"SELECT * FROM {_settings.TwitterUserTableName} ORDER BY lastSync ASC LIMIT @maxNumber";

            using (var dbConnection = Connection)
            {
                dbConnection.Open();

                var result = await dbConnection.QueryAsync<SyncTwitterUser>(query, new { maxNumber });
                return result.ToArray();
            }
        }

        public async Task<SyncTwitterUser[]> GetAllTwitterUsersAsync()
        {
            throw new NotImplementedException();
        }

        public async Task UpdateTwitterUserAsync(int id, long lastTweetPostedId, long lastTweetSynchronizedForAllFollowersId, DateTime lastSync)
        {
            if(id == default) throw new ArgumentException("id");
            if(lastTweetPostedId == default) throw new ArgumentException("lastTweetPostedId");
            if(lastTweetSynchronizedForAllFollowersId == default) throw new ArgumentException("lastTweetSynchronizedForAllFollowersId");
            if(lastSync == default) throw new ArgumentException("lastSync");

            var query = $"UPDATE {_settings.TwitterUserTableName} SET lastTweetPostedId = @lastTweetPostedId, lastTweetSynchronizedForAllFollowersId = @lastTweetSynchronizedForAllFollowersId, lastSync = @lastSync WHERE id = @id";

            using (var dbConnection = Connection)
            {
                dbConnection.Open();

                await dbConnection.QueryAsync(query, new { id, lastTweetPostedId,  lastTweetSynchronizedForAllFollowersId, lastSync = lastSync.ToUniversalTime() });
            }
        }

        public async Task DeleteTwitterUserAsync(string acct)
        {
            if (acct == default) throw new ArgumentException("acct");

            acct = acct.ToLowerInvariant();

            var query = $"DELETE FROM {_settings.TwitterUserTableName} WHERE acct = @acct";

            using (var dbConnection = Connection)
            {
                dbConnection.Open();

                await dbConnection.QueryAsync(query, new { acct });
            }
        }
    }
}