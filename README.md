```
https://www.archaicbinary.net
telnet://bbs.archaicbinary.net

Okay I use a very customized version of GameSrv for my BBS but this *should* work for any BBS that can run DOOR32 stuff, generate a DOOR32.SYS file and can run .NET 2.0 stuff.

Here is some information on how you need to run this, if you understand all this you can do as you please!

Here is a batch file I launch BBSLink with. Replace the <codes> below with your BBSLink information! No spaces from argument to your code.

---------------[ START.BAT ]---------------
@ECHO OFF
CLS
C:
CD \GAMESRV\DOORS\BBSLINK
BBSLINK -D%1 -W%2 -X<syscode> -Y<authcode> -Z<schemecode>

I call the START.BAT from my BBS (im using gamesrv remember) like the following for example. This example will run BBSLink and load LORD for the caller.

---------------[ BBSLINK.INI ]---------------
[DOOR]
Name=BBSLink
Command=c:\**********\bbslink\start.bat
Parameters=*DOOR32 lord
Native=True
ForceQuitDelay=5
WatchDTR=False
WindowStyle=Normal

In the end all you need to do is run the START.BAT with the first argument (parameter) as the full path and filename of your DOOR32.SYS and the second argument as the game you want to play on BBSLink. As of writing this I was sent the list:

lord
lord2
teos
ooii
tw
pimp
luna
bbsc

You could do this manually as well:
BBSLINK -DC:\BBS\NODE1\DOOR32.SYS -Wlord -X<syscode> -Y<authcode> -Z<schemecode>
```
