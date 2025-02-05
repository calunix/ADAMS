using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.DirectoryServices.AccountManagement;
using System.Reflection;
using ADAMS.Classes;
using System.Security.Cryptography.X509Certificates;
using System.DirectoryServices;

namespace ADAMS
{
    sealed internal class AccountMaintenanceWorker
    {
        private readonly IConfiguration _config;
        private readonly ILogger _logger;
        private List<UserPrincipal> _users;
        private DomainTransactor _transactor;

        //throw exceptions from methods in this class

        public AccountMaintenanceWorker(IConfiguration config, ILogger<WindowsBackgroundService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public void PerformMaintenance()
        {
            _transactor = new(_config);
            _users = _transactor.GetUsers();

            // add a try catch will email me in case a failure occurs
            NotifyUserPwExpiry();
            DisableAccounts();
        }

        
        private void NotifyUserPwExpiry()
        {
            DateTime startTime = DateTime.Now;
            string scopeName = $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}";
            _logger.LogInformation($"Running {scopeName}");

            DateTime expiry;
            DateTime currTime = DateTime.Now;
            TimeSpan timeToExpire;
            List<int> thresholds = _config.GetSection("Notifications:Thresholds").Get<List<int>>();
            EmailSender mailClient = new(_config);
            string noun;
            for (int i = 0; i < _users.Count; i++)
            {
                _logger.LogInformation($"Checking {_users[i].SamAccountName}, {_users[i].EmailAddress}");

                expiry = DateTime.Parse(DomainTransactor.GetPasswordExpiry(_users[i].SamAccountName)); // how to deal with null values
                timeToExpire = expiry - currTime;
                noun = (timeToExpire.Days == 1) ? "day" : "days";
                _logger.LogInformation($"Password expires {expiry.ToString()}; Time to expire: {timeToExpire.Days} {noun}");

                for (int j = 0; j < thresholds.Count; j++)
                {
                    if (thresholds[j] == timeToExpire.Days)
                    {
                        _logger.LogInformation($"Emailing {_users[i].EmailAddress}; password expires in {thresholds[j]} {noun}");
                        //mailClient.SendNotification(_users[i].EmailAddress, expiry.ToString(), timeToExpire);
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

            DateTime? lastLogon;
            TimeSpan timeSinceLastLogon;
            TimeSpan maxLogonAge = new(_config.GetValue<int>("MaxLogonAge"), 0, 0, 0);
            string disabledAccountsOU = _config.GetValue<string>("ActiveDirectory:DisabledContainer");
            string currentOU;

            for (int i = 0; i < _users.Count; i++)
            {
                lastLogon = _users[i].LastLogon;
                if (!lastLogon.HasValue) { continue; }

                timeSinceLastLogon = DateTime.Now - (DateTime)lastLogon;
                if (timeSinceLastLogon > maxLogonAge) {
                    _logger.LogInformation($"Last logon greater than {maxLogonAge.Days} days ago; disabling account: {_users[i].SamAccountName}({_users[i].EmailAddress})");
                    _users[i].Enabled = false;
                    _users[i].Save();

                    currentOU = _users[i].Context.Container.ToString();
                    MoveToOrgUnit(_users[i].SamAccountName, currentOU, disabledAccountsOU);
                }
            }

            DateTime endTime = DateTime.Now;
            _logger.LogInformation($"{scopeName} elapsed time: {(endTime - startTime).TotalSeconds.ToString()} seconds");
        }

        private void MoveToOrgUnit(string username, string oldOrgUnit, string newOrgUnit)
        {
            string? domain = _config.GetValue<string>("ActiveDirectory:DomainName");
            string? accessUser = _config.GetValue<string>("ActiveDirectory:ServiceAccountUser");
            string? accessPass = _config.GetValue<string>("ActiveDirectory:ServiceAccountPass");

            DirectoryEntry oldEntry = new(domain, accessUser, accessPass);
            oldEntry.Path = $"LDAP://{oldOrgUnit}";

            DirectoryEntry newEntry = new(domain, accessUser, accessPass);
            newEntry.Path = $"LDAP://{newOrgUnit}";

            DirectorySearcher searcher = new DirectorySearcher(oldEntry);
            searcher.Filter = $"(samaccountname={username})";
            SearchResult results = searcher.FindOne();

            DirectoryEntry currUserEntry = results.GetDirectoryEntry();
            currUserEntry.MoveTo(newEntry);
            currUserEntry.CommitChanges();
        }
    }
}
