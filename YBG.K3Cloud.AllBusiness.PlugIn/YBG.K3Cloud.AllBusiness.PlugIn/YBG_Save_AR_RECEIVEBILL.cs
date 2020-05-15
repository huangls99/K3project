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

namespace YBG.K3Cloud.AllBusiness.PlugIn
{
    [Description("收款单保存更新销售订单数量")]
    [Kingdee.BOS.Util.HotUpdate]
    public class YBG_Save_AR_RECEIVEBILL : AbstractOperationServicePlugIn
    {
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
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
                        sql = string.Format(@"/*dialect*/ update T_AR_RECEIVEBILLSRCENTRY  SET FORDERQTY=t2.FQTY FROM T_AR_RECEIVEBILLSRCENTRY t1 
                                                inner join T_SAL_ORDERENTRY  t2 on t1.FORDERENTRYID=t2.FENTRYID 
                                                left join T_AR_RECEIVEBILL t3 on t3.FID=t1.FID where t3.FID={0}", Fid);

                        //更新收款单上的销售订单数量
                        DBServiceHelper.Execute(Context, sql);
                        
                    }
                }
            }
            catch (Exception ex)
            {
                throw new KDException("", "更新收款单上的销售订单数量失败：" + ex.ToString());
            }

        }
    }
}
