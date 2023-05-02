# WinAppSdkCleaner
 A GUI utility for removing unwanted Windows Application Sdk versions, written using WPF.
 
## :warning: Caution
 
 This is a developer only utility. The utility will list all packages that depend on a particular WinAppSdk framework package. If you remove the WinAppSdk version that contains it, **the code will remove all the dependent packages first**. Use at your own risk.

 By default, the utility allows the removal of WinAppSdk packages for the current user. If you only install WinAppSdk versions using that user account, removing a WinAppSdk version will recover the disk space.
 
 To remove a WinAppSdk that has been provisioned, you have to start the utility elevated. That will allow the removal of a WinAppSdk for all users. However if the package manager has staged a WinAppSdk framework package, it cannot be removed by this utility and as such will be omitted from the search results.
 
 A typical scenario:
 
 ![Screenshot 2023-04-15 174436](https://user-images.githubusercontent.com/28826959/232238992-3df0bd4d-e373-45e0-9401-142a7c3eaa0b.png)
