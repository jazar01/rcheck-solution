echo signing %1 using Orasi cert
"C:\Program Files (x86)\Windows Kits\10\bin\10.0.16299.0\x64\signtool" sign /f C:\data\projects\rcheck-solution\rcheck-signing\OrasiCodeSigningCert.pfx /d "Orasi rcheck" /p 0raC*2018! /fd sha256 /td sha256 /tr http://timestamp.comodoca.com  /v %1  

