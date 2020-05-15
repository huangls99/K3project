using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.JSON;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;

namespace YBG.K3Cloud.AllBusiness.PlugIn
{
    [Description("应付单下推付款申请单单据转换控制")]
    [Kingdee.BOS.Util.HotUpdate]
    public class YBG_Push_PayableToPayapply : AbstractConvertPlugIn
    {
        /// <summary>
        /// 下推
        /// </summary>
        /// <param name="e"></param>
        public override void OnAfterCreateLink(CreateLinkEventArgs e)
        {
            try
            {
                string sql = "";
                // 源单单据体元数据
                var sourceEntitylist = e.SourceBusinessInfo.GetEntity("FBillHead");
                //目标单据体数据
                var extend = e.TargetExtendedDataEntities.FindByEntityKey("FBillHead").FirstOrDefault().DataEntity;
                if (extend == null)
                {
                    return;
                }
                //获取付款单明细数据
                var Item = extend["FPAYAPPLYENTRY"] as DynamicObjectCollection;
                for (int a = Item.Count; a > 0; a--)
                {
                    //订单明细内码
                    string FORDERENTRYID = Item[a - 1]["FORDERENTRYID"].ToString();
                    //物料
                    string FMATERIALID = Item[a - 1]["FMATERIALID_Id"].ToString();
                    sql = string.Format(@"select g.FALLAMOUNT as 价税合计,f.FREALRECAMOUNT as 收款金额  from t_PUR_POOrderEntry c   
                                              inner join T_PUR_POORDERENTRY_LK d on d.FENTRYID=c.FENTRYID 
                                              inner join T_SAL_ORDERENTRY f on d.FSID=f.FENTRYID 
                                              inner join T_SAL_ORDERENTRY_F g on f.FENTRYID=g.FENTRYID
                                              where c.FENTRYID='{0}'", FORDERENTRYID);
                    DataSet ds = DBServiceHelper.ExecuteDataSet(this.Context, sql);
                    DataTable dt = ds.Tables[0];
                    if (dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            //价税合计
                            decimal FALLAMOUNT = Convert.ToDecimal(dt.Rows[i]["价税合计"].ToString());

                            //收款金额
                            decimal FREALRECAMOUNT = Convert.ToDecimal(dt.Rows[i]["收款金额"].ToString());

                            //比例
                            decimal proportion = FREALRECAMOUNT / FALLAMOUNT;

                            if (proportion == 0)
                            {
                                Item.RemoveAt(a-1);
                            }
                            else
                            {
                                //付款申请金额
                                Item[a - 1]["FAPPLYAMOUNTFOR"] = Convert.ToDecimal(Item[a - 1]["FAPPLYAMOUNTFOR"]) * proportion;
                            }
                        }
                    }
                }
                if (Item.Count == 0)
                {
                    throw new Exception("未收到回款，禁止下推付款申请单" );
                }     
            }
            catch (Exception ex)
            {
                throw new Exception("当前单据下推报错：" + ex.ToString());
            }


        }
    }
}
