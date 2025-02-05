using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADAMS.Classes
{
    internal class DomainTransactor
    {
        private readonly IConfiguration _config;
        private PrincipalContext[] _domainContexts;

        public DomainTransactor(IConfiguration config)
        {
            _config = config;

            string domain = _config.GetValue<string>("ActiveDirectory:DomainName");
            string accessUser = _config.GetValue<string>("ActiveDirectory:ServiceAccountUser");
            string accessPass = _config.GetValue<string>("ActiveDirectory:ServiceAccountPass");
            List<string> orgUnits = _config.GetSection("ActiveDirectory:UserContainers").Get<List<string>>();
            _domainContexts = new PrincipalContext[orgUnits.Count];

            for (int i = 0; i < orgUnits.Count; i++)
            {
                _domainContexts[i] = new PrincipalContext(ContextType.Domain, domain, orgUnits[i], accessUser, accessPass);
            }
        }

        public List<UserPrincipal> GetUsers()
        {
            PrincipalSearcher ps;
            UserPrincipal up;
            PrincipalSearchResult<Principal> results;
            List<UserPrincipal> users = [];

            for (int i = 0; i < _domainContexts.Length; i++)
            {
                ps = new();
                up = new(_domainContexts[i]);
                ps.QueryFilter = up;
                results = ps.FindAll();
                foreach (UserPrincipal user in results) { users.Add(user); }
                up.Dispose();
                ps.Dispose();
            }

            return users;
        }

        public static string? GetPasswordExpiry(string username)
        {
            string? expiry = null;

            Process netCommand = new();
            netCommand.StartInfo.FileName = "powershell.exe";
            netCommand.StartInfo.Arguments = $"net user {username} /domain";
            netCommand.StartInfo.UseShellExecute = false;
            netCommand.StartInfo.RedirectStandardOutput = true;
            netCommand.StartInfo.RedirectStandardError = true;
            netCommand.Start();
            string netCommandResults = netCommand.StandardOutput.ReadToEnd() + "\n" + netCommand.StandardError.ReadToEnd();
            netCommand.WaitForExit();

            if (netCommandResults.Contains("The user name could not be found."))
            {
                return expiry;
            }

            string[] lines = netCommandResults.Split('\n');
            foreach (string line in lines)
            {
                if (line.Contains("Password expires"))
                {
                    expiry = line;
                    break;
                }
            }

            for (int i = 0; i < expiry.Length; ++i)
            {
                if (char.IsDigit(expiry[i]))
                {
                    expiry = expiry.Substring(i);
                    break;
                }
            }

            return expiry;
        }
    }
}
