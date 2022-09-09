<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CsvToContentChannelConvertor.ascx.cs" Inherits="RockWeb.Plugins.com_thecrossingchurch.Cms.CsvToContentChannelConvertor" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <Rock:NotificationBox ID="nbMessage" runat="server" Visible="false" />
        <div class="container">
            <div class="panel panel-block">
                <div class="panel-heading" id="pnlTitle" runat="server">
                    <h5><i class="fa fa-cog"></i>Configuration</h5>
                </div>
                <div class="panel-body">
                    <asp:Panel ID="pnlConfiguration" runat="server">
                        <div class="row mb-4">
                            <div class="col-xs-12">
                                Select the content channel your items should be added to. The CSV file should begin with a header row that will be used to configure how the data will be imported.
                            </div>
                        </div>
                        <div class="row">
                            <div class="col-xs-12">
                                <Rock:RockDropDownList ID="ddlContentChannel" runat="server" Label="Content Channel" OnSelectedIndexChanged="ddlContentChannel_SelectedIndexChanged" />
                            </div>
                        </div>
                        <div class="row">
                            <div class="col-xs-12">
                                <Rock:FileUploader ID="fileCsv" runat="server" Label="CSV File" IsBinaryFile="false" RootFolder="~/App_Data/TemporaryFiles" />
                            </div>
                        </div>
                        <div class="row">
                            <div class="col-xs-12">
                                <Rock:BootstrapButton ID="pnlConfigNext" CssClass="btn btn-primary pull-right" runat="server" OnClick="pnlConfigNext_Click">Next</Rock:BootstrapButton>
                            </div>
                        </div>
                    </asp:Panel>
                    <asp:Panel ID="pnlFieldConfiguration" runat="server" Visible="false">
                        <Rock:Grid ID="configGrid" runat="server" AllowPaging="false" ShowActionRow="false">
                            <Columns>
                                <Rock:RockBoundField HeaderText="Content Channel Field" DataField="CCField"></Rock:RockBoundField>
                                <Rock:RockBoundField HeaderText="Data Type" DataField="CCDataType"></Rock:RockBoundField>
                                <Rock:RockTemplateField HeaderText="CSV Column">
                                    <ItemTemplate>
                                        <Rock:RockDropDownList ID="ddlCSV" DataSource='<%# Eval("ddlDataSource") %>' runat="server"></Rock:RockDropDownList>
                                    </ItemTemplate>
                                </Rock:RockTemplateField>
                            </Columns>
                        </Rock:Grid>
                        <div class="row mt-4">
                            <div class="col-xs-12">
                                <Rock:BootstrapButton ID="btnFieldConfigNext" CssClass="btn btn-primary pull-right" runat="server" OnClick="btnFieldConfigNext_Click">Import Items</Rock:BootstrapButton>
                            </div>
                        </div>
                    </asp:Panel>
                    <asp:Panel ID="pnlResults" runat="server" Visible="false">
                        <div class="alert alert-success" role="alert" runat="server" id="alertSuccess"></div>
                        <div class="alert alert-warning" role="alert" runat="server" id="alertError" visible="false"></div>
                    </asp:Panel>
                </div>
            </div>
        </div>

    </ContentTemplate>
</asp:UpdatePanel>
