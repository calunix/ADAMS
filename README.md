# ADAMS - Active Directory Account Maintenance Service

## About

ADAMS is a configurable .NET Core background service that runs as a windows service. It automates 
work that would typically be performed by a systems administrator. That work is two-fold: Notifying users 
that their password will be imminently expiring, and disabling accounts that have not been used for a 
specified period of time. It is deployed as a single file executable, eliminating the need for a .NET Core 
runtime installation in the target environment.

## Compatibility
ADAMS has been developed and tested on Windows Server 2022 and only requires the default installation of 
PowerShell as a dependency.

## Tutorial Reference
The linked Microsoft tutorial can provide additional guidance for development, building, and deployment.

https://learn.microsoft.com/en-us/dotnet/core/extensions/windows-service

## How to Build

### Prerequisites
- Visual Studio 2022
- .Net Core 9 SDK

### Procedure
1. Open the solution in Visual Studio
2. Right click the solution in solution explorer and select Publish
3. If necessary, create a publish profile
4. Ensure the publish profile enforces the following by selecting Show all settings:
   - Deployment mode is self-contained
   - File publish options > Produce single file is checked
   - NOTE: Trim unsused code should not be selected as it will cause COM exceptions when the service is deployed
     and started
5. Save the profile settings
6. Select the Publish button
7. Verify the Output pane shows no failures

## Deployment

### Prerequisites
- Windows Server 2022 machine with minimal resources: 4 GB RAM, 2 CPU cores, 50 GB Disk
  - for an even more minimal set up run this service on an instance of Windows Server without the desktop installed

### Procedure
1. Copy ADAMS.exe and appsettings.json from the publish folder configured in the Build Procedure, to the target host
2. In the directory where ADAMS.exe and appsettings.json reside, using PowerShell, run:  
   ```sc.exe create "ADAMS" binpath= "C:\Path\To\ADAMS.exe"```
3. Edit appsettings.json by filling in appropriate values (see the Configuration section).
4. Start the service:
   - OPTION 1: Run Services.msc, locate the ADAMS service, then select Start
   - OPTION 2: In PowerShell, run ```sc.exe start "ADAMS"```

## Configuration

### Options

*TimeToStart* (string)  
The time of day the service should run. Should be a string of 4 digits from 0000 to 2359.

*ActiveDirectory.DomainName* (string)  
The domain suffix identifying the active directory domain ADAMS communicates with.

*ActiveDirectory.ServiceAccountUser* (string)  
Username for service account that ADAMS uses for active directory authentication.

*ActiveDirectory.ServiceAccountPass* (string)  
Password for service account that ADAMS uses for active directory authentication.

*ActiveDirectory.UserContainers* (array[string])  
List of Distinguished Names for each Organizational Unit containing User Principals ADAMS can operate on.

*ActiveDirectory.DisabledContainer* (string)  
Distinguished Name of container where disabled user accounts should be moved to.

*Email.SmtpServer* (string)  
DNS name of the mail server to forward email notifications to.

*Email.Sender* (string)  
Email address that email notificatiosn should be sent from.

*Email.CopyRecipients* (array[string])  
List of email addresses that should be on CC for email notifications.

*Notifications.Thresholds* (array[integer])  
List of notification thresholds in days at which users should be emailed when their password will expire. For example, 
[3,2,1] will cause ADAMS to email a user that their password will expire at 3 days, 2 days, and 1 day prior to expiration.

*MaxLogonAge* (integer)  
The maximum number of days an account can go without a logon being recorded before the account is disabled by ADAMS.

### Example Configuration
```
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "TimeToStart": "0100",
  "ActiveDirectory": {
    "DomainName": "google.com",
    "ServiceAccountUser": "serviceUser",
    "ServiceAccountPass": "mySecretPassword",
    "UserContainers": [
      "OU=Domain Users,OU=Users,DC=google,DC=com",
      "OU=Domain Admins,OU=Users,DC=google,DC=com"
    ],
    "DisabledContainer": "OU=Disabled Accounts,OU=Users,DC=google,DC=com"
  },
  "Email": {
    "SmtpServer": "smtp.google.com",
    "Sender": "services@google.com",
    "CopyRecipients": [
      "john.smith@google.com",
      "jane.rogers@google.com"
    ]
  },
  "Notifications": {
    "Thresholds": [ 14, 7, 2, 1 ]
  },
  "MaxLogonAge": 120
}
```
