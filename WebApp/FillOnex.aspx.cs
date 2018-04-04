using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace WebApp
{
    public partial class FillOnex : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            API.receiveAndInsertBets(now);
        }
    }
}