<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
    CodeBehind="Default.aspx.cs" Inherits="ContosoExpenseWebSite.Default" %>

<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">
</asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">
    <h2>
    </h2>
    <p>
        Current Role Instance Id: <b><asp:Label ID="RoleInstanceLabel" runat="server" Text="" /></b>
    </p>
    <p>
        Current Machine Name: <b><%: Environment.MachineName %></b>
    </p>
</asp:Content>