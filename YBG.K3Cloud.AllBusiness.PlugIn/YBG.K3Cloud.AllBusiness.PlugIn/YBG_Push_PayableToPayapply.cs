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
                    sql = string.Format(@"select ar.arFALLAMOUNTFOR as 应收金额,sk.skFREALRECAMOUNT as 收款金额, sal.F_YBG_BUSINESSMODEL as 业务模式 from t_PUR_POOrderEntry c   
                                              inner join T_PUR_POORDERENTRY_LK d on d.FENTRYID=c.FENTRYID 
                                              inner join T_SAL_ORDERENTRY f on d.FSID=f.FENTRYID 
                                              inner join T_SAL_ORDERENTRY_F g on f.FENTRYID=g.FENTRYID
											  left  join T_SAL_ORDER sal on sal.FID=f.FENTRYID
											  left join (select sum(are.FALLAMOUNTFOR) as  arFALLAMOUNTFOR  ,FORDERENTRYID  from  T_AR_RECEIVABLE ar 
                                              inner join T_AR_RECEIVABLEENTRY arE  on ar.FID=are.FID where ar.FDOCUMENTSTATUS='C' and ar.FBILLTYPEID='5d18aa0e58407c' and ar.FSETACCOUNTTYPE='3' 
											  group by FORDERENTRYID)ar on ar.FORDERENTRYID=f.FENTRYID
				                              left  join (select FORDERENTRYID, sum(a.FREALRECAMOUNT) as  skFREALRECAMOUNT from T_AR_RECEIVEBILLSRCENTRY  a 
				                              left join T_AR_RECEIVEBILL b on a.FId=b.FID where b.FDOCUMENTSTATUS='C' group by FORDERENTRYID) sk on sk.FORDERENTRYID=f.FENTRYID
                                              where c.FENTRYID='{0}'", FORDERENTRYID);
                    DataSet ds = DBServiceHelper.ExecuteDataSet(this.Context, sql);
                    DataTable dt = ds.Tables[0];
                    if (dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            //业务模式
                            string F_YBG_BusinessModel = string.IsNullOrEmpty(dt.Rows[i]["业务模式"].ToString()) ? "" : dt.Rows[i]["业务模式"].ToString();
                            //只有挂靠的才会收到货款的时候就付款
                            if(F_YBG_BusinessModel == "01" || F_YBG_BusinessModel == "04")
                            {
                                //价税合计
                                decimal FALLAMOUNT = Convert.ToDecimal(dt.Rows[i]["应收金额"].ToString());

                                //收款金额
                                decimal FREALRECAMOUNT = Convert.ToDecimal(dt.Rows[i]["收款金额"].ToString());

                                //比例
                                decimal proportion = FREALRECAMOUNT / FALLAMOUNT;

                                if (proportion == 0)
                                {
                                    Item.RemoveAt(a - 1);
                                }
                                else
                                {
                                    //付款申请金额
                                    Item[a - 1]["FAPPLYAMOUNTFOR"] = Convert.ToDecimal(Item[a - 1]["FAPPLYAMOUNTFOR"]) * proportion;
                                }
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
