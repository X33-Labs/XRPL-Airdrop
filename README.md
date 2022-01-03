# XRPL-Airdrop
A Token Creation and Airdrop Tool Suite for the XRPL.

*****Disclaimer: This software is provided as is without any express or implied warranties. X33 Labs/authors/maintainers/contributors to this repo assume no responsibility for errors or omissions, or for damages resulting from the use of this software contained herein. There is no guarantee as to the suitability of said source code. X33 Labs/authors/maintainers/contributors, will not be held responsible for any damages or costs which might occur as a result. USE AT YOUR OWN RISK*****

### Features
+ Create Tokens on the XRPL
+ Set the Email Hash and Domain for your Issuer Account
+ Blackhole Issuer Account
+ Control token distribution and air drops
+ XRPLVerify.com integration to filter out bots/script trustline farmers
+ Export Reports to show current token balances for your users and airdrop status.

### Requirements

+ [NodeJs and Git](https://nodejs.org/en/)
+ [.NET 5.0 Core Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/5.0)
+ [Visual Studio 2019 or greater](https://visualstudio.microsoft.com/downloads/) (Optional)

## Getting Started
Open a command prompt or Powershell prompt and issue the following commands

```
git clone https://github.com/X33-Labs/XRPL-Airdrop
```
Navigate to the directory where you cloned the project.

Build the project
```
dotnet build --configuration Release
```
Navigate to the Config directory and open up the settings.json file to change settings variables. Reference the Settings.Json section further down in this document.
```
cd [User Path]\XRPL-Airdrop\XRPLAirdrop\bin\Release\net5.0\config
```
Open the settings.json file located at: [User Path]\XRPL-Airdrop\XRPLAirdrop\bin\Release\net5.0\config

Navigate back one directory to the main build directory
```
cd [User Path]\XRPL-Airdrop\XRPLAirdrop\bin\Release\net5.0
```

Now Run the App
```
dotnet XRPLAirdrop.dll
```

## Tool Feature instructions

### 1. Update Trustline Accounts
When this feature is initiated, the app will attempt to pull into local storage all of the addresses/balances that have a trustline setup with the configured issuer address that was set in the settings.json file. This is the 1st step that must be taken in order to run an airdrop campaign. It's also useful for taking snapshots of trustline accounts at a certain point in time or viewing account balances for your token. ***If an airdrop campaign has already been initiated, DO NOT update trustline accounts as it will overwrite all airdrop statuses and data.

### 2. Update XRPForensics
When this feature is initiated, the app will pull in Forensics data to identify Bots/Scripts/Trustline Farmers as provided by XRPlorer. Contact for more info: Xrplorer(https://xrplorer.com/contact) ***Deprecated as of 1/1/22

### 3. View Current Settings
Outputs various trustline # stats and rudimentary settings data. Refer to the Settings.json file for all settings data.

### 4. Start Airdrop
Starts the airdrop campaign based on the current config. If at any time the airdrop campaign gets interrupted, the airdrop will start where the app was previously interrupted.

### 5. Export Report
Outputs either a CSV or XLSX report on the balance and status of airdrop accounts (configured in the settings.json file). For more info, refer to the Report section of this document.






