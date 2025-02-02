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
        private readonly PrincipalContext _domainContext;

        public DomainTransactor(IConfiguration config, string container)
        {
            _config = config;
            string domain = _config.GetValue<string>("ActiveDirectory:DomainName");
            string accessUser = _config.GetValue<string>("ActiveDirectory:ServiceAccountUser");
            string accessPass = _config.GetValue<string>("ActiveDirectory:ServiceAccountPass");
            _domainContext = new(ContextType.Domain, domain, container, accessUser, accessPass);
        }

        public List<UserAccount> GetUsers()
        {
            PrincipalSearcher ps = new();
            UserPrincipal up = new(_domainContext);
            ps.QueryFilter = up;
            PrincipalSearchResult<Principal> principals = ps.FindAll();

            List<UserAccount> users = new List<UserAccount>();
            foreach (UserPrincipal p in principals)
            {
                UserAccount ua = new();
                ua.Username = p.SamAccountName;
                ua.Email = p.EmailAddress;
                users.Add(ua);
            }

            return users;
        }
    }
}
