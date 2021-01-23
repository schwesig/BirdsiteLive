﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using BirdsiteLive.Common.Extensions;
using BirdsiteLive.Common.Settings;
using BirdsiteLive.DAL.Contracts;
using BirdsiteLive.DAL.Models;
using BirdsiteLive.Pipeline.Contracts;
using Microsoft.Extensions.Logging;

namespace BirdsiteLive.Pipeline.Processors
{
    public class RetrieveTwitterUsersProcessor : IRetrieveTwitterUsersProcessor
    {
        private readonly ITwitterUserDal _twitterUserDal;
        private readonly ILogger<RetrieveTwitterUsersProcessor> _logger;
        private readonly InstanceSettings _instanceSettings;
        
        public int WaitFactor = 1000 * 60; //1 min
        private int StartUpWarming = 4;

        #region Ctor
        public RetrieveTwitterUsersProcessor(ITwitterUserDal twitterUserDal, InstanceSettings instanceSettings, ILogger<RetrieveTwitterUsersProcessor> logger)
        {
            _twitterUserDal = twitterUserDal;
            _instanceSettings = instanceSettings;
            _logger = logger;
        }
        #endregion

        public async Task GetTwitterUsersAsync(BufferBlock<SyncTwitterUser[]> twitterUsersBufferBlock, CancellationToken ct)
        {
            for (; ; )
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    var maxUsers = StartUpWarming > 0
                        ? _instanceSettings.MaxUsersCapacity / 4
                        : _instanceSettings.MaxUsersCapacity;
                    StartUpWarming--;
                    var users = await _twitterUserDal.GetAllTwitterUsersAsync(maxUsers);

                    var userCount = users.Any() ? users.Length : 1;
                    var splitNumber = (int) Math.Ceiling(userCount / 15d);
                    var splitUsers = users.Split(splitNumber).ToList();

                    foreach (var u in splitUsers)
                    {
                        ct.ThrowIfCancellationRequested();

                        await twitterUsersBufferBlock.SendAsync(u.ToArray(), ct);

                        await Task.Delay(WaitFactor, ct);
                    }

                    var splitCount = splitUsers.Count();
                    if (splitCount < 15) await Task.Delay((15 - splitCount) * WaitFactor, ct);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failing retrieving Twitter Users.");
                }
            }
        }
    }
}