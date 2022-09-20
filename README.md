# WinAppSdkCleaner
 A GUI utility for removing unwanted Windows Application Sdk versions, written using WPF.
 
### :warning:
 
 This is a developer only utility. The utility lists all packages that depend on a particular WinAppSdk framework package. If you remove the Sdk that contains it, the code will remove all the dependent packages first. Use at your own risk.

 By default, the utility removes packages for the current user. To remove user scoped packages for all users, select the appropriate option in the settings and start the utility in elevated mode using "Run as administrator".

 To list and remove provisioned packages, the utility also requires elevation.
