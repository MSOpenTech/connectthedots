# Libraries #

This file describes how to build libraries for Intel Galileo Windows project under Windows 8.

##Requirements for OpenSSL
* Install Git (chose option to add Git to the system PATH during installation) - [Download Git][1]
* Install Perl (chose option to add Perl to the system PATH during installation) - [Download Perl][2]
* Install Visual Studio 2013 

##OpenSSL
* Create a folder `C:\Source`
* Copy `lib\openssl-debug.bat`, `lib\openssl-release.bat`, `lib\windowsiot.patch`, and `lib\windowsiot_dbg.patch` to `C:\Source`
* Launch `lib\openssl-release.bat` or `lib\openssl-debug.bat` (depend on your needs)
* Find the build of `openssl-1.0.2-beta3` in `C:\Source\openssl\builds` (or `C:\Source\openssl\builds_dbg` if it was built in the Debug configuration)

##Requirements for Apache Qpid Proton C
* Build OpenSSL
* Install Cmake (chose option to add Cmake to the system PATH during installation) - [Download Cmake][3]
* Install Python 2.7 (chose option to add Python to the system PATH during installation) - [Download Python][4]

##Apache Qpid Proton C
* Copy `lib\proton-release.bat`, `proton-debug.bat`, `windowsproton06.patch` to `C:\Source`
* Launch `proton-release.bat` or `proton-debug.bat` (depends on your needs)
* Find the build of `qpid-proton 0.8` in `C:\Source\proton\builds` (or `C:\Source\proton\builds_dbg` if it was built with the Debug configuration)

##NuGet Native Package
* Build OpenSSL and Apache Qpid Proton with the Release configuration
* Copy `lib\qpidproton.autopkg` to `C:\Source`
* Install CoApp PowerShell tools - [Download CoApp tools][5]
* Open PowerShell and change a directory to `C:\Source`
* Invoke `Write-NuGetPackage .\qpidproton.autopkg` command

##References 
To understand better, please, read following articles:
* [Porting Open Source Libraries to Windows for IoT (mincore)][6]
* [Using Apache Qpid Proton C with Azure Service Bus on Linux and Windows][7]

##Product Versions
Scripts have been tested on Windows 8.1 in the environment with: 
* CMake 3.1.0
* git 1.9.5
* Perl 5.20.1
* Python 2.7.9
* Visual Studio Ultimate 2013 Update 4

  [1]: http://git-scm.com/download/win
  [2]: https://www.perl.org/get.html
  [3]: http://www.cmake.org/download/
  [4]: https://www.python.org/downloads/windows/
  [5]: http://coapp.org/pages/releases.html
  [6]: http://pjdecarlo.com/2015/01/porting-open-source-libraries-to-windows-for-iot-mincore.html
  [7]: https://code.msdn.microsoft.com/windowsazure/Using-Apache-Qpid-Proton-C-afd76504