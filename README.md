# WinAppSdkCleaner
 A GUI utility for removing unwanted Windows Application Sdk versions, written using WPF.
 
## :warning: Caution
 
 This is a developer only utility. The utility will list all packages that depend on a particular WinAppSdk framework package. If you remove the WinAppSdk version that contains it, the code will remove **all the dependent packages first**. Use at your own risk.

 By default, the utility allows the removal of WinAppSdk packages for the current user. If you only install WinAppSdk versions using that user account, removing a WinAppSdk version will recover the disk space.
 
 To remove a WinAppSdk that has been provisioned, you have to start the utility elevated (using "Run as administrator"). That will allow the complete removal of a WinAppSdk for all users.
 
 If you have a mix of provisioned and user installs, you should use this program elevated.