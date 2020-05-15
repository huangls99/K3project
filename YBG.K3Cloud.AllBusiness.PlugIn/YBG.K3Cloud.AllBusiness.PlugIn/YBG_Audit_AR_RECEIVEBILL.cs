using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.K3.Core.SCM.STK;
using Kingdee.K3.MFG.App;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kingdee.K3.SCM.ServiceHelper;
using System.ComponentModel;
using Kingdee.BOS.ServiceHelper;
using System.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.DynamicForm;

namespace YBG.K3Cloud.AllBusiness.PlugIn
{
    [Description("收款单审核反写收款金额到销售订单")]
    [Kingdee.BOS.Util.HotUpdate]
    public class YBG_Audit_AR_RECEIVEBILL : AbstractOperationServicePlugIn
    {
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            try
            {
                string sql = string.Empty;
                if (e.DataEntitys != null && e.DataEntitys.Count<DynamicObject>() > 0)
                {
                    foreach (DynamicObject item in e.DataEntitys)
                    {
                        //收款单id
                        string Fid = item["Id"].ToString();
                        sql = string.Format(@"select FORDERBILLNO,FORDERENTRYID,b.FREALRECAMOUNT from T_AR_RECEIVEBILL a 
                                              inner join T_AR_RECEIVEBILLSRCENTRY b  on a.FID=b.fid 
                                              where a.FID='{0}'", Fid);
                        DataSet ds = DBServiceHelper.ExecuteDataSet(this.Context,sql);
                        DataTable dt = ds.Tables[0];
                        if (dt.Rows.Count > 0)
                        {
                            string upsql = "";
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                //订单明细内码
                                string FORDERENTRYID = dt.Rows[i]["FORDERENTRYID"].ToString();
                                //本次收款金额
                                decimal FREALRECAMOUNT = Convert.ToDecimal(dt.Rows[i]["FREALRECAMOUNT"].ToString());
                                upsql += string.Format(@"/*dialect*/ update T_SAL_ORDERENTRY set FREALRECAMOUNT={0} where FENTRYID={1}", FREALRECAMOUNT, FORDERENTRYID);
                            }
                            //更新销售订单
                            DBServiceHelper.Execute(Context,upsql);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new KDException("", "审核失败：" + ex.ToString());
            }

        }
    }
}
