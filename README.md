# WinAppSdkCleaner
 A GUI utility for removing unwanted Windows Application Sdk versions. Written using WPF for obvious reasons...
 
 ### Caution: 
 
 This is a developer only utility. The utility does list all packages that depend on a particular WinAppSdk framework package. If you remove the Sdk that contains it, the code will remove all the dependent packages first. Use at your own risk.
 
 By default, the utility removes packages for the current user. To remove packages for all users, select the appropriate option in the settings and start the utility in elevated mode using "Run as Administrator".
 
