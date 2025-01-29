using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public void NotifyUserPwExpiry()
        {
            _logger.LogInformation("Running InformUserPwExpiry");
        }

        public void DisableAccounts()
        {
            _logger.LogInformation("Running DisableAccounts");
        }
    }
}
