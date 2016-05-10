:: set up the Visual Studio environment
call "C:\Program Files (x86)\Microsoft Visual Studio 12.0\VC\bin\vcvars32.bat"
:: clone library's sources (qpid-proton 0.8)
git clone -b 0.8 https://github.com/apache/qpid-proton.git proton
cd proton
:: set path of openssl for cmake search scripts
set OPENSSL_ROOT_DIR=C:\Source\openssl\builds
:: generate solutions and projects files (without java and testing)
cmake -DCMAKE_CXX_STANDARD_LIBRARIES:STRING=mincore.lib ^
 -DCMAKE_CXX_FLAGS:STRING=" /arch:IA32 /DWIN32 /D_WINDOWS /W3 /GR /EHsc " ^
 -DCMAKE_C_FLAGS:STRING=" /arch:IA32 /DWIN32 /D_WINDOWS /W3 " ^
 -DCMAKE_C_STANDARD_LIBRARIES:STRING=mincore.lib ^
 -DBUILD_JAVA:BOOL=OFF ^
  -DBUILD_TESTING:BOOL=OFF ^
 -DCMAKE_INSTALL_PREFIX:PATH="C:\Source\proton\builds" ^
 -DCMAKE_EXE_LINKER_FLAGS:STRING=" /debug /machine:X86 /NODEFAULTLIB:kernel32.lib /NODEFAULTLIB:ws2_32.lib /NODEFAULTLIB:gdi32.lib /NODEFAULTLIB:advapi32.lib /NODEFAULTLIB:crypt32.lib /NODEFAULTLIB:user32.lib " ^
 -DCMAKE_MODULE_LINKER_FLAGS:STRING=" /debug /machine:X86 /NODEFAULTLIB:kernel32.lib /NODEFAULTLIB:ws2_32.lib /NODEFAULTLIB:gdi32.lib /NODEFAULTLIB:advapi32.lib /NODEFAULTLIB:crypt32.lib /NODEFAULTLIB:user32.lib " ^
 -DCMAKE_SHARED_LINKER_FLAGS:STRING=" /debug /machine:X86 /NODEFAULTLIB:kernel32.lib /NODEFAULTLIB:ws2_32.lib /NODEFAULTLIB:gdi32.lib /NODEFAULTLIB:advapi32.lib /NODEFAULTLIB:crypt32.lib /NODEFAULTLIB:user32.lib "
:: compile sources
msbuild Proton.sln /property:Configuration=Release
:: install binaries to the builds\ folder
msbuild INSTALL.vcxproj /property:Configuration=Release
cd ..