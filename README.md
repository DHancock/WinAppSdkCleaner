# WinAppSdkCleaner
 A GUI utility for removing unwanted Windows Application Sdk versions.
 
> [!WARNING] 
> This is a developer only utility. The utility will list all packages that depend on a particular WinAppSdk framework package. If you remove the WinAppSdk that contains them, **the code will remove all the dependent packages first**. This includes windows utilities installed as part of the OS. The app will not be able to detect if a framework dependant unpackaged app has a dependencey on any WinAppSdk version. Deleteing that version may break the app. Use at your own risk.

 By default, the utility allows the removal of WinAppSdk packages for the current user. If you only install WinAppSdks using that user account, removing the WinAppSdk will recover the disk space.
 
 If you start utility elevated, it will allow the removal of an installed WinAppSdk for all users. Staged WinAppSdk packages cannot be removed by this utility and as such are omitted from the search results.

<dl><dd><dl><dd><dl><dd>
<img width="470" height="483" alt="Untitled80" src="https://github.com/user-attachments/assets/7df7cefa-3c81-4734-8f63-492473f7ce0f" />
</dd></dl></dd></dl></dd></dl>


