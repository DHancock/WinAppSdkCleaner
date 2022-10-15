# WinAppSdkCleaner
 A GUI utility for removing unwanted Windows Application Sdk versions, written using WPF.
 
## :warning: Caution
 
 This is a developer only utility. The utility will list all packages that depend on a particular WinAppSdk framework package. If you remove the WinAppSdk version that contains it, **the code will remove all the dependent packages first**. Use at your own risk.

 By default, the utility allows the removal of WinAppSdk packages for the current user. If you only install WinAppSdk versions using that user account, removing a WinAppSdk version will recover the disk space.
 
 To remove a WinAppSdk that has been provisioned, you have to start the utility elevated. That will allow the complete removal of a WinAppSdk for all users.
 
 If you have a mix of provisioned and user installs, you should use this program elevated[^1].

 
 A typical scenario:
 
![Screenshot 2022-10-15 095122](https://user-images.githubusercontent.com/28826959/195978170-9390ae44-96a9-470b-9ba0-44c4c47ccf74.png)

[^1]:If not, you run the risk of ending up with orphaned framework packages that cannot be removed without reinstalling the WinAppSdk version that they were installed with. This occurs if you install a provisioned WinAppSdk, followed by a non provisioned WinAppSdk that only has a higher patch version e.g. WinAppSdk version 1.1.3 provisioned, followed by 1.1.4 non provisioned. This results in the provisioned packages having a dependency on the non provisioned framework packages. If you then attempt to remove the WinAppSdk 1.1.4 version using the utility running non elevated, both WinAppSdk versions will be removed for the current user. However if you then start the utility elevated, it appears that both WinAppSdk versions still exist. If you attempt to remove the WinAppSdk version 1.1.4 for all users, the removal fails with file not found errors. However the Package Manager still indicates that the WinAppSdk 1.1.4 framework packages remain. Further attempts to remove them will also fail, but without errors.
