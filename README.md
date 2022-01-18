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
When this feature is initiated, the app will attempt to pull into local storage all of the addresses/balances that have a trustline setup with the configured issuer address that was set in the settings.json file. This is the 1st step that must be taken in order to run an airdrop campaign. It's also useful for taking snapshots of trustline accounts at a certain point in time or viewing account balances for your token. ***If an airdrop campaign has already been initiated, DO NOT update trustline accounts as it will overwrite all airdrop statuses and data.***

### 2. Update XRPForensics
When this feature is initiated, the app will pull in Forensics data to identify Bots/Scripts/Trustline Farmers as provided by XRPlorer. Contact for more info: Xrplorer(https://xrplorer.com/contact) ***Deprecated as of 1/1/22***

### 3. View Current Settings
Outputs various trustline # stats and rudimentary settings data. Refer to the Settings.json file for all settings data.

### 4. Start Airdrop
Starts the airdrop campaign based on the current config. If at any time the airdrop campaign gets interrupted, the airdrop will start where the app was previously interrupted.

### 5. Export Report
Outputs either a CSV or XLSX report on the balance and status of airdrop accounts (configured in the settings.json file). For more info, refer to the Report section of this document. Output Directory: [User Path]\XRPL-Airdrop\XRPLAirdrop\bin\Release\net5.0\Reports

### 6. Requeue Address
Manually resets the airdrop/verify status in local memory for an address. Useful if an address was non-qualifying at first or didn't properly receive their airdrop.

### 7. View Current Network Fees
Outputs the current Network fees.

### 8. Re-Verify Failed Transactions Checks
If there are non-verified transactions on the Export Report, this function will attempt to re-verify those transaction hashes to confirm that the transactions were validated and included in a ledger.

### 9. Issuer New Currency
Issues a new token on the XRPL based on the configured fields in the settings.json file. Required fields: Issuer_Address, Issuer_Address_Secret, Airdrop_Address, Airdrop_Address_Secret, Currency_Code, Supply. In this case, the Airdrop_Address acts as the hot wallet address.

### 10. Blackhole Issuer Account
Blackholes the Issuer Account. ***WARNING: by initiating this function, you will lose all access to the issuer account configured in the settings.json file. Make sure all of your issuer account changes are correct before blackholing (email hash, domain, currency, supply, ect).

### 11. Update domain/email on issuer (Gravatar)
Updates the email hash and the domain for the issuer account based on the Domain and Email settings in the settings.json file.


## Settings.json configuration fields

### WebSocket_URL
The websocket connection URL. Change this to specify Mainnet, Testnet or Devnet

### Issuer_Address
The issuer address for your token

### Issuer_Address_Secret
The issuer address secret **only required when issueing a new currency or changing the email hash/domain or blackholing an issuer account

### Airdrop_Address
The address where your airdrop tokens are located for running an airdrop campaign. This also acts as the hot wallet when issuing a new currency.

### Airdrop_Address_Secret
The secret for the airdrop address

### Currency_Code
The ASCII currency code for your existing or new currency

### Domain
The domain for your currency ex. pixelaperowboat.club

### Email
The email for your gravatar account. Shows a custom image for your issuer address in a block explorer. See for more info: https://en.gravatar.com/

### Supply
integer value for the total supply of your currency (Only used when creating a new currency)

### TransferFee
Sets the transfer fee (as a percentage) for your new currency. Default is set to 0. Can be 0 to 100 (Only used when creating a new currency)

### Airdrop_Token_Amt
Amount of token to drop to each trustline

### Exclude_Bots
Only used with XRPForensics data. Default is false. ***deprecated as of 1/1/22***

### XRPForensics_URL
URL for XRPlorer Bot data api. Do not change. ***deprecated as of 1/1/22***

### XRPForensics_API_Key
The API Key for XRPlorer. Contact for more info: https://xrplorer.com/contact ***deprecated as of 1/1/22***

### AccountLinesThrottle
Throttle when pulling in trustlines to keep load to a minimum on ledger RPC servers. (in seconds)

### TxnThrottle
Trottle when sending transactions in an airdrop campaign (in seconds)

### Exclude_if_user_has_a_balance
Excludes addresses that have a balance of the token. Useful for excluding team wallets and other addresses that already have a balance before running your airdrop.

### Include_only_holders
Includes only holders of your token in an airdrop campaign

### Include_only_holders_num_Tokens
The number of tokens a user has to hold in order to qualify for the airdrop when Include_only_holders is set to true

### Max_number_of_trustlines
The number of trustlines to send to

### FeeMultiplier
The multiplier for sending transactions on the ledger for reliable inclusion in ledgers. Default is 1.1. Fees you pay per txn = (Open Ledger Fee) * 1.1

### MaximumFee
The maximum fee in drops you are willing to pay per transaction. If the current Open Ledger Fee * FeeMultiplier is greater than the MaximumFee, the app will pause the airdrop campaign with a status message of the current fees and wait until fees are reduced down to your Maximum Fee that you set. Default is 11 drops.

### XRPLVerify_Enabled
true or false. This is X33 Labs solution to combating bots/scripts/farm accounts when running an airdrop campaign. Verifiable human interaction. See https://www.xrplverify.com/ for more information

### XRLVerify_Password
The password you were assigned when signing up for X33 Labs XRPLVerify service.

### Report_Format
CSV or XLSX file format for Report Exports


## Reports
The XRPLAirdrop app has built in local memory and storage for trustline accounts. Reports allow the user to quickly check balances and status of accounts pre or post airdrop campaign. If an airdrop gets interrupted for any reason, it allows the user to see the status of each address in the trustline list. See Below for Report Fields.

### id
the integer primary key of the record

### Address
Address of the trustline user

### Balance
Current token balance of the user

### dropped
1 = a token transaction has been initiated for this address for the current airdrop campaign
0 = no transaction has been initiated
-1 = Error or address was excluded based on criteria in the configuration file

### datetime
The date and time when the transaction was initiated

### txn_verified
1 = the transaction for this address for an airdrop was validated/verified on the ledger
0 = nothing was verified
-1 = error or address was excluded from the airdrop

### xrplverify.com
1 = this address's trustline was verified through xrplverify.com
0 = not verified through xrplverify.com

### txn_message
The status message for the transaction

### txn_detail
The detail message for the transaction

### txn_hash
The transaction hash after the transaction was initiated


