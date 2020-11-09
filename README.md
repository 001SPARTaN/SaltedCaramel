# SaltedCaramel
 Apfell implant written in C#. SaltedCaramel was rewritten by [@djhohnstein](https://twitter.com/djhohnstein) as [Apollo](https://github.com/MythicAgents/Apollo). His code is better, but I wanted to provide this as a peek behind the curtain into the original project. This was a learning experience, and my first project written in C#. Be gentle.
 
**NOTE:** This is no longer compatible with current versions of Mythic. This code will not work anymore, and is provided for reference purposes only.
 
## Usage
**SaltedCaramel.exe** [Apfell server URL] [AES PSK] [Payload UUID]

## Supported commands
- **cd** - Change current directory
- **download** - Download file from implant to Apfell server
- **execute-assembly** - Execute a .NET assembly
- **exit** - Exit implant
- **get** - Get a URL
- **kill** - Kill a process
- **upload** - Upload a file from Apfell server to implant
- **post** - Send an HTTP POST request
- **ps** - List processes on the current system
- **ls** - List a directory
- **powershell** - Run a PowerShell command
- **rev2self** - Revert to implant's primary token
- **run** - Execute a binary on the current system
- **screencapture** - Take a screenshot of the current desktop session
- **sleep** - Change the implant's checkin interval
- **steal_token** - Steal a token from a process
