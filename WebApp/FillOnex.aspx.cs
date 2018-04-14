using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using OneX.Core;

namespace WebApp
{
    public partial class FillOnex : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["Onex"].ConnectionString;
            API.receiveAndInsertBets(connectionString, DateTime.Now);
        }
    }
}