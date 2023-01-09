# Dockord
Discord Bot for running Docker containers, written in .NET 7.0

# Outline
Docker is a neat technology for quickly spinning up virtual machines that interface directly with the hardware. Its use as a tool for running dedicated servers for games means with an interface on hand like a Discord Bot, these servers can be managed entirely through commands to the bot.

Dockord aims to achieve the following goals:
1. Being written in .NET (Need to add C# to my CV somehow!)
2. Being able to build, start, restart and terminate containers for games written with a common definition layer (i.e abstracting away the technical parameters needed for spinning up containers)
3. Runnable on Windows or Linux and exposes its functionality via a Discord Bot interface

# Setup
Git Clone the repository to your machine. Use dotnet publish to compile for whatever OS you want to run the bot on.
On the Discord end, you will need the following:
- A Discord Bot created from the Developer Portal https://discord.com/developers/applications with Message Intent enabled
- A server role that allows execution of commands
- (Optional) A discord channel for logging, otherwise the bot will just log to the console only

On first startup, the bot will generate a dockord.db file. Using sqlite, open the .db file, and with your approver role ID on hand (go to server settings on Discord, under roles, right click the role and Copy ID) run
"UPDATE config SET config_value = <discord role ID> WHERE config_key = 'ApproverRoleId';"
Similarly, if you have a logging channel on hand repeat the above clause but with <discord role> as your channel ID and config_key as 'LoggingChannelId'
Restart the bot, and now you should be able to execute commands on it!



