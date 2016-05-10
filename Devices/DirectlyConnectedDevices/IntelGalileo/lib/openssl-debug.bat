:: set up the Visual Studio environment
call "C:\Program Files (x86)\Microsoft Visual Studio 12.0\VC\bin\vcvars32.bat"
:: clone library's sources (OpenSSL 1.0.2 beta3)
git clone -b OpenSSL_1_0_2-beta3 https://github.com/openssl/openssl.git openssl
cd openssl
:: generate makefiles for nmake
perl Configure debug-VC-WIN32 no-asm --prefix="C:\Source\openssl\builds_dbg"
call "ms\do_ms.bat"
:: apply the patch to sources and the makefile to make Windows for IoT compatible 
git apply ..\windowsiot_dbg.patch
:: compile sources
nmake -f ms\ntdll.mak
:: install binaries to the builds\ folder
nmake -i -f ms\ntdll.mak install
cd ..