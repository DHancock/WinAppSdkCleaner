# WinAppSdkCleaner
 A GUI utility for removing unwanted Windows Application Sdk versions, written using WPF.
 
> [!WARNING] 
> This is a developer only utility. The utility will list all packages that depend on a particular WinAppSdk framework package. If you remove the WinAppSdk that contains them, **the code will remove all the dependent packages first**. This includes windows utilities installed as part of the OS. Use at your own risk.

 By default, the utility allows the removal of WinAppSdk packages for the current user. If you only install WinAppSdks using that user account, removing the WinAppSdk will recover the disk space.
 
 If you start utility elevated, it will allow the removal of an installed WinAppSdk for all users. Staged WinAppSdk packages cannot be removed by this utility and as such are omitted from the search results.
 
 A typical scenario:
 
 ![Screenshot 2023-04-15 174436](https://user-images.githubusercontent.com/28826959/232238992-3df0bd4d-e373-45e0-9401-142a7c3eaa0b.png)
