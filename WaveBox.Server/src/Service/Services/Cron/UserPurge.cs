using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.IO;
using Ninject;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Core.OperationQueue;
using WaveBox.Static;
using WaveBox.Core.Model.Repository;
using WaveBox.Core;

namespace WaveBox.Service.Services.Cron {
    /// <summary>
    /// Purge all users and sessions which are out of date, using WaveBox settings
    /// </summary>
    public static class UserPurge {
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // Create operation queue for the session scrubber
        public static DelayedOperationQueue Queue = new DelayedOperationQueue();

        /// <summary>
        /// Start user purge operation
        /// </summary>
        public static void Start() {
            // Delete all expired users
            foreach (User u in Injection.Kernel.Get<IUserRepository>().ExpiredUsers()) {
                if (u.Delete()) {
                    logger.IfInfo(String.Format("Purged expired user: [id: {0}, name: {1}]", u.UserId, u.UserName));
                } else {
                    logger.IfInfo(String.Format("Failed to purge expired user: [id: {0}, name: {1}]", u.UserId, u.UserName));
                }
            }

            // Grab a list of all sessions
            var sessions = Injection.Kernel.Get<ISessionRepository>().AllSessions();

            // Grab the current UNIX time
            long unixTime = DateTime.UtcNow.ToUnixTime();

            // Purge any sessions which have not been updated in a predefined period of time
            foreach (Session s in sessions) {
                // Check current time and last update, purge if the diff is higher than SessionTimeout minutes
                if ((unixTime - Convert.ToInt32(s.UpdateTime)) >= (Injection.Kernel.Get<IServerSettings>().SessionTimeout * 60)) {
                    if (s.Delete()) {
                        logger.IfInfo(String.Format("Purged session: [id: {0}, user: {1}]", s.RowId, s.UserId));
                    } else {
                        logger.IfInfo(String.Format("Failed to purge session: [id: {0}, user: {1}]", s.RowId, s.UserId));
                    }
                }
            }
        }
    }
}
