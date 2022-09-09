using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI.Controls;
using Rock.Attribute;
using Z.EntityFramework.Plus;
using System.Data.Entity.Migrations;
using CsvHelper;
using CsvHelper.Configuration;
using System.IO;
using Newtonsoft.Json;

namespace RockWeb.Plugins.com_thecrossingchurch.Cms
{
    [DisplayName( "CSV To Content Channel Convertor" )]
    [Category( "com_thecrossingchurch > Tools" )]
    [Description( "Turn a CSV file into content channel item in a content channel" )]
    [MemoField( "Structured Content Template", "The template used when adding structured content, RockDateTime and Content will be reaplced with the actual values", true, "{ \"time\": {{RockDateTime.Now.Ticks}}, \"blocks\": [{ \"type\": \"paragraph\", \"data\": { \"text\": \"{{Content}}\" } }], \"version\": \"2.16.1\"}" )]

    public partial class CsvToContentChannelConvertor : Rock.Web.UI.RockBlock
    {
        #region Variables
        private RockContext _context { get; set; }
        private int? _ccId { get; set; }
        private BinaryFile file { get; set; }
        private List<DataConfiguration> configList { get; set; }
        #endregion

        #region Base Control Methods

        protected void Page_Load( object sender, EventArgs e )
        {
            ScriptManager scriptManager = ScriptManager.GetCurrent( this.Page );
        }

        /// <summary>
        /// Raises the <see cref="E:Init" /> event.
        /// </summary>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
        }

        /// <summary>
        /// Raises the <see cref="E:Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            _context = new RockContext();
            if ( !Page.IsPostBack )
            {
                LoadCcDropDown();
            }
        }

        #endregion

        #region Methods
        private void LoadCcDropDown()
        {
            var channels = new ContentChannelService( _context ).Queryable().ToList().Where( cc => cc.IsAuthorized( "Edit", CurrentPerson ) ).OrderBy( cc => cc.Name ).ToList();
            ddlContentChannel.DataSource = channels;
            ddlContentChannel.DataTextField = "Name";
            ddlContentChannel.DataValueField = "Id";
            ddlContentChannel.DataBind();
        }

        private void AddRow( string fieldName, string fieldType, List<string> headerOptions )
        {
            List<string> data = new List<string>();
            data.AddRange( headerOptions );
            configList.Add( new DataConfiguration() { CCField = fieldName, CCDataType = fieldType, ddlID = "ddl" + fieldName, ddlDataSource = data } );
        }
        #endregion

        #region Events

        protected void pnlConfigNext_Click( object sender, EventArgs e )
        {
            pnlConfiguration.Visible = false;
            pnlFieldConfiguration.Visible = true;
            configList = new List<DataConfiguration>();

            string filePath = this.Request.MapPath( fileCsv.UploadedContentFilePath );
            using ( StreamReader sr = new StreamReader( filePath ) )
            {
                if ( _ccId.HasValue )
                {
                    var config = new CsvConfiguration()
                    {
                        HasHeaderRecord = true,
                        WillThrowOnMissingField = false,
                        IgnoreBlankLines = true,
                        IgnoreHeaderWhiteSpace = true
                    };

                    CsvReader csvReader = new CsvReader( sr, config );

                    List<Object> raw = csvReader.GetRecords<Object>().ToList();
                    var records = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>( JsonConvert.SerializeObject( raw ) );
                    string[] headers = csvReader.FieldHeaders;

                    ContentChannel channel = new ContentChannelService( _context ).Get( _ccId.Value );
                    ContentChannelItem item = new ContentChannelItem() { ContentChannelId = _ccId.Value, ContentChannelTypeId = channel.ContentChannelTypeId };

                    ViewState["content"] = JsonConvert.SerializeObject( records );
                    ViewState["headers"] = JsonConvert.SerializeObject( headers );
                    ViewState["ccId"] = _ccId.Value;
                    ViewState["cctId"] = channel.ContentChannelTypeId;
                    ViewState["filePath"] = filePath;

                    //Configuration for Item Fields
                    AddRow( "Title", "Text", headers.ToList() );
                    var list = new List<string>() { "Now" };
                    list.AddRange( headers );
                    AddRow( "Created Date Time", "DateTime", list );
                    AddRow( "Start Date Time", "DateTime", list );
                    list[0] = "Current Person (" + CurrentPerson.FullName + ")";
                    AddRow( "Created By Person Alias Id", "Integer", list );
                    list[0] = "";
                    AddRow( "Content", "Html", list );

                    item.LoadAttributes();
                    //Configuration for Item Attributes
                    for ( int i = 0; i < item.Attributes.Count(); i++ )
                    {
                        AddRow( item.Attributes.ElementAt( i ).Value.Name, item.Attributes.ElementAt( i ).Value.FieldType.Name, list );
                    }
                    configGrid.DataSource = configList;
                    configGrid.DataBind();
                }
            }
        }

        protected void btnFieldConfigNext_Click( object sender, EventArgs e )
        {
            List<string> headers = JsonConvert.DeserializeObject<string[]>( ViewState["headers"].ToString() ).ToList();
            List<Dictionary<string, string>> records = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>( ViewState["content"].ToString() );

            string titleCol = "", startDateVal = "", createDateVal = "", createByVal = "", contentCol = "";
            Dictionary<string, string> attrCols = new Dictionary<string, string>();

            var successCount = 0;

            //Get Configuration
            for ( int i = 0; i < configGrid.Rows.Count; i++ )
            {
                var ddlist = ( RockDropDownList ) configGrid.Rows[i].Cells[2].Controls[1];
                switch ( i )
                {
                    case 0:
                        titleCol = ddlist.SelectedValue;
                        break;
                    case 1:
                        createDateVal = ddlist.SelectedValue;
                        break;
                    case 2:
                        startDateVal = ddlist.SelectedValue;
                        break;
                    case 3:
                        createByVal = ddlist.SelectedValue;
                        break;
                    case 4:
                        contentCol = ddlist.SelectedValue;
                        break;
                    default:
                        attrCols.Add( configGrid.Rows[i].Cells[0].Text, ddlist.SelectedValue );
                        break;
                }
            }

            List<string> errors = new List<string>();
            var channel = new ContentChannelService( _context ).Get( Int32.Parse( ViewState["ccId"].ToString() ) );
            var template = GetAttributeValue( "StructuredContentTemplate" );
            //Process Data
            for ( int i = 0; i < records.Count(); i++ )
            {
                try
                {
                    var row = records[i];
                    ContentChannelItem item = new ContentChannelItem() { ContentChannelId = Int32.Parse( ViewState["ccId"].ToString() ), ContentChannelTypeId = Int32.Parse( ViewState["cctId"].ToString() ) };
                    item.LoadAttributes();
                    item.Title = row[titleCol].Trim();
                    if ( createDateVal == "Now" )
                    {
                        item.CreatedDateTime = RockDateTime.Now;
                    }
                    else
                    {
                        item.CreatedDateTime = DateTime.Parse( row[createDateVal].Trim() );
                    }
                    if ( startDateVal == "Now" )
                    {
                        item.StartDateTime = RockDateTime.Now;
                    }
                    else
                    {
                        item.StartDateTime = DateTime.Parse( row[startDateVal].Trim() );
                    }
                    if ( createByVal == "Current Person (" + CurrentPerson.FullName + ")" )
                    {
                        item.CreatedByPersonAliasId = CurrentPerson.PrimaryAliasId;
                    }
                    else
                    {
                        item.CreatedByPersonAliasId = Int32.Parse( row[createByVal].Trim() );
                    }
                    if ( !String.IsNullOrEmpty( contentCol ) )
                    {
                        if ( channel.IsStructuredContent )
                        {
                            var Content = row[contentCol].Trim();
                            var templateResult = template;
                            templateResult = templateResult.Replace( "{{RockDateTime.Now.Ticks}}", RockDateTime.Now.Ticks.ToString() );
                            item.StructuredContent = templateResult.Replace( "{{Content}}", Content );
                        }
                        else
                        {
                            item.Content = row[contentCol].Trim();
                        }
                    }

                    var validAttrs = attrCols.Where( a => !String.IsNullOrEmpty( a.Value ) ).ToList();
                    for ( int k = 0; k < validAttrs.Count(); k++ )
                    {
                        var attr = item.Attributes.FirstOrDefault( a => a.Value.Name == validAttrs[k].Key ).Value;
                        if ( attr != null )
                        {
                            item.SetAttributeValue( attr.Key, row[validAttrs[k].Value].Trim() );
                        }
                    }

                    //Save everything
                    _context.ContentChannelItems.AddOrUpdate( item );
                    _context.SaveChanges();
                    item.SaveAttributeValues( _context );
                    successCount++;
                }
                catch ( Exception ex )
                {
                    errors.Add( $"Error on Row {i + 1}: " + ex.Message );
                }
            }
            string message = successCount == records.Count() ? ( "Imported all " + successCount + " items." ) : ( "Imported " + successCount + " of " + records.Count() + " items." );
            alertSuccess.InnerText = message;
            if ( errors.Count() > 0 )
            {
                alertError.InnerHtml = String.Join( "<br>", errors );
                alertError.Visible = true;
            }
            pnlFieldConfiguration.Visible = false;
            pnlTitle.InnerHtml = "<h5>Results</h5>";
            pnlResults.Visible = true;

            //Clean-up Delete Temp File
            File.Delete( ViewState["filePath"].ToString() );
        }

        protected void ddlContentChannel_SelectedIndexChanged( object sender, EventArgs e )
        {
            DropDownList ddl = ( DropDownList ) sender;
            _ccId = ddl.SelectedValue.AsInteger();
        }

        #endregion

        private class DataConfiguration
        {
            public string CCField { get; set; }
            public string CCDataType { get; set; }
            public string ddlID { get; set; }
            public List<string> ddlDataSource { get; set; }
        }
    }
}