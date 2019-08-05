# Dintero.Checkout.Episerver Plugin


## Description

Dintero.Checkout.Episerver is a library for integrating Dintero Payment as the checkout solution for sites based on EPiServer Commerce technology.

Version supported: 10.9 and higher

Functionality supported in EpiServer admin: 
* Capture
* Refund and partial refund (on item level)

NB! Do not use Dintero Backoffice for capture and refund, since it is not synced with EpiServer.


## Integration

How to install? - Add ASAP module is ready on nuget.episerver.com

How to use?

After you have install a module. It is required to create:
1. Page layout for EPiServer Commerce Dintero payment settings - ConfigurePayment.ascx.
   It will manage the following parameters: account id, client id, client secret id, profile id.
   If you are OK with default one, you may just copy this page (add a link).
   This page is to create by the following path: Apps/Order/Payments/Plugins/Dintero in your EPiServer Commerce web project

2. Make sure you do not have "DinteroSessionId" meta field.

3. Create a new instance of DinteroPage page type in EPiServer/CMS.

4. Add start page properties: "DinteroPaymentCancelPage" and "DinteroPaymentLandingPage" - pages where user will be redirected after failed/successful Dintero Authosization.

5. Create new payment Dintero method. Please make sure, you set "Dintero" as a payment system word.

6. You may define an clased with IPostProcessDinteroPayment interface and register it in EPiServer DI. You may add you custom operation after seesion authorization, payment capturing or refunding.

