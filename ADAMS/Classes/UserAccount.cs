using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADAMS.Classes
{
    struct UserAccount
    {
        public string? Username { get; set; }
        public string? Email { get; set; }

        public UserAccount() { }

        public string? GetPasswordExpiry()
        {
            string? expiry = null;

            Process netCommand = new();
            netCommand.StartInfo.FileName = "powershell.exe";
            netCommand.StartInfo.Arguments = $"net user {this.Username} /domain";
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