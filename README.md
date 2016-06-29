# Multi-tenant Web App for Creating a Service Principal

This web app allows the creation of a Service Principal using Azure Active Directory Graph APIs. It implements an OAuth Grant-Flow that allows the creation of Service Principals in target directories of their End-Users if the Users do have the required permissions in that target directory.

Usage of the App
----------------
will come soon

Deploying the App in your Subscription/Tenant
---------------------------------------------
You can always clone this repository and run the application in your own tenant. These are the steps for setting up the App in your own Azure Active Directory Tenant.

1. Navigate to the classic [Azure Management Portal](https://manage.windowsazure.com).

2. In the left navigation bar, scroll down and select Azure Active Directory.

3. Select your target Azure AD in the list of AD tenants.

4. Switch to the Applications-Tab in your tenant.

5. Add a new Application and follow the steps in the Wizard: 
  * Select "Add an application my organization is developing"
  * Give the Application a Display Name, e.g. "AzureServicePrincipalWeb"
  * As Application-Type, select "Web Application And/Or Web API"
  * Enter a Sign-On URL that matches the final target URL. E.g. if you plan to deploy it as an Azure Web App, then select "https://yourazurewebapp.azurewebsites.net/".
  * Enter an App ID URI that fits in your Azure AD Tenant Domain or a custom domain you're using. E.g. if your tenant is called "mytenant.onmicrosoft.com" select something like "https://mytenant.onmicrosoft.com/yourappname".

6. After you've created the app, open it in the Azure AD Portal and switch to the configure tab.

7. Make sure you have configured the app as a multi-tenant app!

    ![figure1](https://raw.githubusercontent.com/mszcool/azureAdMultiTenantServicePrincipal/master/Docs/Figure01-App-Registration-Multi-Tenant.png)

8. Next check the permissions for your app. For creating a Service Principal the following permissions are needed. Also check your App ID URI and whether it matches the Azure AD Tenant Domain (or a custom, verified domain you own):

    ![figure2](https://raw.githubusercontent.com/mszcool/azureAdMultiTenantServicePrincipal/master/Docs/Figure02-App-Permissions.png)

9. Now create a secret key for the app in the Azure AD admin portal. Take note of the key because the portal won't display it afterwards, again!

    ![figure3](https://raw.githubusercontent.com/mszcool/azureAdMultiTenantServicePrincipal/master/Docs/Figure03-Save-App-Key.png)

10. Take note of the Client ID (see Figure at point 7. above) as well.

11. Now Open up the web.config of the cloned source code repository. Update the following configuration settings:
    * `<add key="ida:ClientId" value="your client secret goes here" />`
    * `<add key="ida:AppKey" value="your app key (from step 9.) goes here" />`

12. Finally you are ready to deploy your web app wherever you want to run it.
    * Make sure the reply URL in the Azure AD config matches your target host/DNS-name!
    * When using localhost (for local debugging), make sure the reply URL and IIS Express Ports do match! 
