# SaltedCaramel
 Apfell implant written in C#. This is just for learning purposes, probably not ready for production use.
 
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
