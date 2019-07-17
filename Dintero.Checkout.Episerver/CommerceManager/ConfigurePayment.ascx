<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ConfigurePayment.ascx.cs" Inherits="Dintero.Checkout.Episerver.CommerceManager.ConfigurePayment" %>
<div id="DataForm">
    <table cellpadding="0" cellspacing="2">
        <tr>
            <td class="FormLabelCell" colspan="2"><b>
                <asp:Literal ID="Literal1" runat="server" Text="Configure Dintero Account" /></b></td>
        </tr>
    </table>
    <br />
    <table class="DataForm">
        <tr>
            <td class="FormLabelCell">
                <asp:Literal ID="AccountIdLabel" runat="server" Text="Account Id" />:
            </td>
            <td class="FormFieldCell">
                <asp:TextBox runat="server" ID="AccountId" Width="230"></asp:TextBox><br>
                <asp:RequiredFieldValidator ControlToValidate="AccountId" Display="dynamic" Font-Name="verdana"
                                            Font-Size="9pt" ErrorMessage="Account Id is required" runat="server"
                                            ID="AccountIdValidator">
                </asp:RequiredFieldValidator>
            </td>
        </tr>     
        <tr>
            <td colspan="2" class="FormSpacerCell"></td>
        </tr>
        <tr>
            <td class="FormLabelCell">
                <asp:Literal ID="ClientIdLabel" runat="server" Text="Client Id" />:</td>
            <td class="FormFieldCell">
                <asp:TextBox runat="server" ID="ClientId" Width="300px"></asp:TextBox><br>
                <asp:RequiredFieldValidator ControlToValidate="ClientId" Display="dynamic" Font-Name="verdana"
                                            Font-Size="9pt" ErrorMessage="Client Id is required" runat="server"
                                            ID="ClientIdValidator">
                </asp:RequiredFieldValidator>
            </td>
        </tr>
        <tr>
            <td colspan="2" class="FormSpacerCell"></td>
        </tr>
        <tr>
            <td class="FormLabelCell">
                <asp:Literal ID="ClientSecretIdLabel" runat="server" Text="Client Secret Id" />:</td>
            <td class="FormFieldCell">
                <asp:TextBox runat="server" ID="ClientSecretId" Width="300px"></asp:TextBox><br>
                <asp:RequiredFieldValidator ControlToValidate="ClientSecretId" Display="dynamic" Font-Name="verdana"
                                            Font-Size="9pt" ErrorMessage="Client Secret Id is required" runat="server"
                                            ID="ClientSecretIdValidator">
                </asp:RequiredFieldValidator>
            </td>
        </tr>
        <tr>
            <td colspan="2" class="FormSpacerCell"></td>
        </tr>
        <tr>
            <td class="FormLabelCell">
                <asp:Literal ID="ProfileIdLabel" runat="server" Text="Profile Id" />:</td>
            <td class="FormFieldCell">
                <asp:TextBox runat="server" ID="ProfileId" Width="300px"></asp:TextBox><br>
                <asp:RequiredFieldValidator ControlToValidate="ProfileId" Display="dynamic" Font-Name="verdana"
                                            Font-Size="9pt" ErrorMessage="Profile Id is required" runat="server"
                                            ID="ProfileIdValidator">
                </asp:RequiredFieldValidator>
            </td>
        </tr>
        <tr>
            <td colspan="2" class="FormSpacerCell"></td>
        </tr>
    </table>
</div>
