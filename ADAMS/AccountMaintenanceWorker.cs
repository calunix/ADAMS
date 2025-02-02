using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.DirectoryServices.AccountManagement;
using System.Reflection;
using ADAMS.Classes;
using System.Security.Cryptography.X509Certificates;

namespace ADAMS
{
    sealed internal class AccountMaintenanceWorker
    {
        private readonly IConfiguration _config;
        private readonly ILogger _logger;

        //throw exceptions from methods in this class

        public AccountMaintenanceWorker(IConfiguration config, ILogger<WindowsBackgroundService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public void PerformMaintenance()
        {
            NotifyUserPwExpiry();
            DisableAccounts();
        }

        private void NotifyUserPwExpiry()
        {
            DateTime startTime = DateTime.Now;
            string scopeName = $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}";
            _logger.LogInformation($"Running {scopeName}");

            List<DomainTransactor> dt = new();
            List<string> orgUnits = _config.GetSection("ActiveDirectory:UserContainers").Get<List<string>>();
            // containers cannot be empty, how to force this or abort the method if it is

            foreach (string orgUnit in orgUnits)
            {
                DomainTransactor transactor = new(_config, orgUnit);
                dt.Add(transactor);
            }

            List<UserAccount> users = new List<UserAccount>();
            List<UserAccount> temp = new List<UserAccount>();
            foreach (DomainTransactor t in dt)
            {
                temp = t.GetUsers();
                foreach (UserAccount user in temp)
                {
                    users.Add(user);
                }
            }

            DateTime expiry;
            DateTime currTime = DateTime.Now;
            TimeSpan timeToExpire;
            List<int> thresholds = _config.GetSection("Notifications:Thresholds").Get<List<int>>();
            EmailSender mailClient = new(_config);
            string noun;
            for (int i = 0; i < users.Count; i++)
            {
                _logger.LogInformation($"Checking {users[i].Username}, {users[i].Email}");

                expiry = DateTime.Parse(users[i].GetPasswordExpiry()); // how to deal with null values
                timeToExpire = expiry - currTime;
                _logger.LogInformation($"Password expires {expiry.ToString()}; Time to expire: {timeToExpire.Days} days");

                for (int j = 0; j < thresholds.Count; j++)
                {
                    if (thresholds[j] == timeToExpire.Days)
                    {
                        noun = thresholds[j] == 1 ? "day" : "days";
                        _logger.LogInformation($"Emailing {users[i].Email}; password expires in {thresholds[j]} {noun}");
                        mailClient.SendNotification(users[i].Email, expiry.ToString(), timeToExpire);
                        break;
                    }
                }
            }

            DateTime endTime = DateTime.Now;
            _logger.LogInformation($"{scopeName} elapsed time: {(endTime - startTime).TotalSeconds.ToString()} seconds");
        }

        public void DisableAccounts()
        {
            DateTime startTime = DateTime.Now;
            string scopeName = $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}";
            _logger.LogInformation($"Running {scopeName}");

            DateTime endTime = DateTime.Now;
            _logger.LogInformation($"{scopeName} elapsed time: {(endTime - startTime).TotalSeconds.ToString()} seconds");
        }
    }
}
