# WinAppSdkCleaner
 A GUI utility for removing unwanted Windows Application Sdk versions, written using WPF.
 
### :warning:
 
 This is a developer only utility. The utility will list all packages that depend on a particular WinAppSdk framework package. If you remove the WinAppSdk that contains it, the code will remove all the dependent packages first. Use at your own risk.

 By default, the utility allows the removal of WinAppSdk packages for the current user. If you installed the WinAppSdk using that user account, removing the WinAppSdk will recover the disk space.
 
 To remove a WinAppSdk that has been provisioned, you have to start the utility elevated. That will allow the complete removal of a WinAppSdk for all users.
 
 A typical scenario:

![Capture](https://user-images.githubusercontent.com/28826959/192302176-1b20e67c-acd7-4645-9e7d-b6ef2326fae6.PNG)
