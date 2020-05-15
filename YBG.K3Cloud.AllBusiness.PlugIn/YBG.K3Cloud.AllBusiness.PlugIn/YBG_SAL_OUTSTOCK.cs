using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YBG.K3Cloud.AllBusiness.PlugIn
{
    [Description("销售出库单保存更新总修改金额")]
    [Kingdee.BOS.Util.HotUpdate]
    public class YBG_SAL_OUTSTOCK : AbstractOperationServicePlugIn
    {
        public override void BeginOperationTransaction(BeginOperationTransactionArgs e)
        {
            try
            {
                if (e.DataEntitys != null && e.DataEntitys.Count<DynamicObject>() > 0)
                {
                    foreach (DynamicObject item in e.DataEntitys)
                    {
                        string sql = string.Empty;
                        //销售出库单id
                        string Fid = item["Id"].ToString();
                        sql += string.Format(@"/*dialect*/ update T_SAL_OUTSTOCKENTRY set FTotalARFNOTAXAMOUNTFOR=FAMOUNT,FTotalARFALLAMOUNTFOR=FALLAMOUNT
                                               from T_SAL_OUTSTOCKENTRY a inner join T_SAL_OUTSTOCKENTRY_F b on b.FENTRYID=a.FENTRYID where a.FID={0} and  FTotalARFNOTAXAMOUNTFOR=0", Fid);
                        sql += string.Format(@"/*dialect*/ update T_SAL_OUTSTOCK set FTotalARFNOTAXAMOUNTFOR_H=FBILLAMOUNT_LC,FTotalARFALLAMOUNTFOR_H=FBILLALLAMOUNT_LC from 
                                  T_SAL_OUTSTOCK a inner join  T_SAL_OUTSTOCKFIN b on a.fid=b.fid  where a.FID={0} and FTotalARFNOTAXAMOUNTFOR_H=0", Fid);
                        //更新销售出库单
                        DBServiceHelper.Execute(Context, sql);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new KDException("", "保存失败：" + ex.ToString());
            }
        }
    }
}
