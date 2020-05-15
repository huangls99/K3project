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
    [Description("财务应收单反审核反写金额到销售出库单")]
    [Kingdee.BOS.Util.HotUpdate]
   public class YBG_UnAudit_AR_Receivable : AbstractOperationServicePlugIn
   {
        public override void BeginOperationTransaction(BeginOperationTransactionArgs e)
        {
            try
            {
                string sql = string.Empty;
                if (e.DataEntitys != null && e.DataEntitys.Count<DynamicObject>() > 0)
                {
                    foreach (DynamicObject item in e.DataEntitys)
                    {
                        //财务应收fid
                        string Fid = item["Id"].ToString();
                        //单据类型
                        string FBILLTYPEID = item["BillTypeID_Id"].ToString();
                        //只有财务应收的采用反写
                        #region
                        if (FBILLTYPEID == "5d18aa0e58407c")
                        {
                            string upsql = "";
                            string UpdatesqlH = "";
                            sql = string.Format(@"select ar.FMODIFYDATE as 修改时间, arf.FALLAMOUNT as 表头价税合计,arel.FBASICUNITQTY,arf.FNOTAXAMOUNT as 表头不含税金额 , are.FALLAMOUNTFOR as 表体价税合计,arE.FNOTAXAMOUNTFOR as 表体不含税金额 ,arE.FPRICEQTY as 计价数量 ,arel.FSTABLENAME,arel.FSID
                                                   from t_AR_receivable ar inner join t_AR_receivableEntry arE on ar.FID=arE.FID
                                                   inner join T_AR_RECEIVABLEENTRY_LK arel on arel.FENTRYID=are.FENTRYID
                                                   left join t_AR_receivableFIN  arf on arf.FID=ar.FID  where ar.FID='{0}'", Fid);
                            DataSet ds = DBServiceHelper.ExecuteDataSet(this.Context, sql);
                            DataTable dt = ds.Tables[0];
                            if (dt.Rows.Count > 0)
                            {

                                for (int i = 0; i < dt.Rows.Count; i++)
                                {
                                    string FSID = dt.Rows[i]["FSID"].ToString();
                                    //decimal FALLAMOUNT = System.Math.Abs(Convert.ToDecimal(dt.Rows[i]["表头价税合计"].ToString()));
                                    //decimal FNOTAXAMOUNT = Math.Abs(Convert.ToDecimal(dt.Rows[i]["表头不含税金额"].ToString()));
                                    decimal FALLAMOUNTFOR = Math.Abs(Convert.ToDecimal(dt.Rows[i]["表体价税合计"].ToString()));
                                    decimal FNOTAXAMOUNTFOR = Math.Abs(Convert.ToDecimal(dt.Rows[i]["表体不含税金额"].ToString()));
                                    decimal FPRICEQTY = Math.Abs(Convert.ToDecimal(dt.Rows[i]["计价数量"].ToString()));
                                    decimal FBASICUNITQTY = Math.Abs(Convert.ToDecimal(dt.Rows[i]["FBASICUNITQTY"].ToString()));//暂估基本数量
                                    string FMODIFYDATE = dt.Rows[i]["修改时间"].ToString();
                                    if (FPRICEQTY != FBASICUNITQTY)
                                    {
                                        FALLAMOUNTFOR = FALLAMOUNTFOR * (FBASICUNITQTY / FPRICEQTY);
                                        FNOTAXAMOUNTFOR = FNOTAXAMOUNTFOR * (FBASICUNITQTY / FPRICEQTY);
                                        FPRICEQTY = FBASICUNITQTY;
                                    }
                                    sql = string.Format(@"select FSID as 销售出库单FENTRYID ,FSBILLID as 销售出库单id,FSTABLENAME 
                                                          from  T_AR_RECEIVABLEENTRY_LK where FENTRYID={0}", FSID);

                                    DataSet ds2 = DBServiceHelper.ExecuteDataSet(this.Context, sql);
                                    DataTable dt2 = ds2.Tables[0];
                                    if (dt2.Rows.Count > 0)
                                    {
                                        for (int j = 0; j < dt2.Rows.Count; j++)
                                        {
                                            string FSTABLENAME = dt2.Rows[j]["FSTABLENAME"].ToString();
                                            if (FSTABLENAME == "T_SAL_OUTSTOCKENTRY")
                                            {
                                                //销售出库单表头
                                                UpdatesqlH += string.Format(@"/*dialect*/ update T_SAL_OUTSTOCK set FARFNOTAXAMOUNTFOR_H=FARFNOTAXAMOUNTFOR_H-{1},FARFALLAMOUNTFOR_H=FARFALLAMOUNTFOR_H-{2},FARFMODIFYDATE='{3}'  where FID ='{0}'", dt2.Rows[j]["销售出库单id"].ToString(), FNOTAXAMOUNTFOR, FALLAMOUNTFOR, FMODIFYDATE);
                                                //销售出库单表体
                                                upsql += string.Format(@"/*dialect*/ update T_SAL_OUTSTOCKENTRY set FARFNOTAXAMOUNTFOR=FARFNOTAXAMOUNTFOR-{1},FARFALLAMOUNTFOR=FARFALLAMOUNTFOR-{2} ,FARFQty=FARFQty-{3}  where FENTRYID ='{0}'", dt2.Rows[j]["销售出库单FENTRYID"].ToString(), FNOTAXAMOUNTFOR, FALLAMOUNTFOR, FPRICEQTY);
                                            }
                                            else
                                            {
                                                //销售退货单表头
                                                UpdatesqlH += string.Format(@"/*dialect*/ update T_SAL_RETURNSTOCK set FARFNOTAXAMOUNTFOR_H=FARFNOTAXAMOUNTFOR_H-{1},FARFALLAMOUNTFOR_H=FARFALLAMOUNTFOR_H-{2},FARFMODIFYDATE='{3}'   where FID ='{0}'", dt2.Rows[j]["销售出库单id"].ToString(), FNOTAXAMOUNTFOR, FALLAMOUNTFOR, FMODIFYDATE);
                                                //销售退货表体
                                                upsql += string.Format(@"/*dialect*/ update T_SAL_RETURNSTOCKENTRY set FARFNOTAXAMOUNTFOR=FARFNOTAXAMOUNTFOR-{1},FARFALLAMOUNTFOR=FARFALLAMOUNTFOR-{2} ,FARFQty=FARFQty-{3}  where FENTRYID ='{0}'", dt2.Rows[j]["销售出库单FENTRYID"].ToString(), FNOTAXAMOUNTFOR, FALLAMOUNTFOR, FPRICEQTY);
                                            }
                                        }
                                    }
                                }
                                //更新销售出库单表头
                                DBServiceHelper.Execute(Context, UpdatesqlH);
                                //更新销售出库单表体
                                DBServiceHelper.Execute(Context, upsql);

                            }
                        }
                        #endregion
                    }
                }
            }
            catch (Exception ex)
            {
                throw new KDException("", "反审核失败：" + ex.ToString());
            }

        }
        /// <summary>
        /// 反审核更新金额
        /// </summary>
        /// <param name="e"></param>
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            try
            {
                string sql = string.Empty;
                if (e.DataEntitys != null && e.DataEntitys.Count<DynamicObject>() > 0)
                {
                    foreach (DynamicObject item in e.DataEntitys)
                    {
                        HashSet<string> hsset = new HashSet<string>();
                        //财务应收fid
                        string Fid = item["Id"].ToString();
                        //单据类型
                        string FBILLTYPEID = item["BillTypeID_Id"].ToString();
                        //只有财务应收的采用反写
                        #region
                        if (FBILLTYPEID == "5d18aa0e58407c")
                        {
                            string upsql = "";

                            string FSTABLENAME = "";
                            sql = string.Format(@"select ar.FMODIFYDATE as 修改时间,are.FENTRYID as AreFENTRYID,arel.FBASICUNITQTY, arf.FALLAMOUNT as 表头价税合计,arf.FNOTAXAMOUNT as 表头不含税金额 , are.FALLAMOUNTFOR as 表体价税合计,arE.FNOTAXAMOUNTFOR as 表体不含税金额 ,arE.FPRICEQTY as 计价数量 ,arel.FSTABLENAME,arel.FSID
                                                   from t_AR_receivable ar inner join t_AR_receivableEntry arE on ar.FID=arE.FID
                                                   inner join T_AR_RECEIVABLEENTRY_LK arel on arel.FENTRYID=are.FENTRYID
                                                   left join t_AR_receivableFIN  arf on arf.FID=ar.FID  where ar.FID='{0}'", Fid);
                            DataSet ds = DBServiceHelper.ExecuteDataSet(this.Context, sql);
                            DataTable dt = ds.Tables[0];
                            if (dt.Rows.Count > 0)
                            {

                                for (int i = 0; i < dt.Rows.Count; i++)
                                {
                                    string FSID = dt.Rows[i]["FSID"].ToString();
                                    //decimal FALLAMOUNT = Convert.ToDecimal(dt.Rows[i]["表头价税合计"].ToString());
                                    // decimal FNOTAXAMOUNT = Convert.ToDecimal(dt.Rows[i]["表头不含税金额"].ToString());
                                    decimal FALLAMOUNTFOR = Math.Abs(Convert.ToDecimal(dt.Rows[i]["表体价税合计"].ToString()));
                                    decimal FNOTAXAMOUNTFOR = Math.Abs(Convert.ToDecimal(dt.Rows[i]["表体不含税金额"].ToString()));
                                    decimal FPRICEQTY = Math.Abs(Convert.ToDecimal(dt.Rows[i]["计价数量"].ToString()));
                                    decimal FBASICUNITQTY = Math.Abs(Convert.ToDecimal(dt.Rows[i]["FBASICUNITQTY"].ToString()));//暂估基本数量
                                    string FMODIFYDATE = dt.Rows[i]["修改时间"].ToString();
                                    if (FPRICEQTY != FBASICUNITQTY)
                                    {
                                        FALLAMOUNTFOR = FALLAMOUNTFOR * (FBASICUNITQTY / FPRICEQTY);
                                        FNOTAXAMOUNTFOR = FNOTAXAMOUNTFOR * (FBASICUNITQTY / FPRICEQTY);
                                        FPRICEQTY = FBASICUNITQTY;
                                    }
                                    sql = string.Format(@"select FSID as 销售出库单FENTRYID ,FSBILLID as 销售出库单id,FSTABLENAME 
                                                          from  T_AR_RECEIVABLEENTRY_LK where FENTRYID={0}", FSID);
                                    DataSet ds2 = DBServiceHelper.ExecuteDataSet(this.Context, sql);
                                    DataTable dt2 = ds2.Tables[0];
                                    if (dt2.Rows.Count > 0)
                                    {
                                        for (int j = 0; j < dt2.Rows.Count; j++)
                                        {
                                            FSTABLENAME = dt2.Rows[j]["FSTABLENAME"].ToString();
                                            if (FSTABLENAME == "T_SAL_OUTSTOCKENTRY")
                                            {
                                                hsset.Add(dt2.Rows[j]["销售出库单id"].ToString());
                                                //销售出库单表体
                                                upsql += string.Format(@"/*dialect*/ update T_SAL_OUTSTOCKENTRY set FTotalARFNOTAXAMOUNTFOR=FARFNOTAXAMOUNTFOR+(b.FSALUNITQTY-FARFQty)*b.FPRICE,
                                                                             FTotalARFALLAMOUNTFOR=FARFALLAMOUNTFOR+(b.FSALUNITQTY-FARFQty)*b.FTAXPRICE
                                                                          from T_SAL_OUTSTOCKENTRY a inner join T_SAL_OUTSTOCKENTRY_F b on b.FENTRYID=a.FENTRYID where a.FENTRYID={0}", dt2.Rows[j]["销售出库单FENTRYID"].ToString());
                                            }
                                            else
                                            {
                                                hsset.Add(dt2.Rows[j]["销售出库单id"].ToString());
                                                //销售退货表体
                                                upsql += string.Format(@"/*dialect*/ update T_SAL_RETURNSTOCKENTRY set FTotalARFNOTAXAMOUNTFOR=FARFNOTAXAMOUNTFOR+(b.FSALUNITQTY-FARFQty)*b.FPRICE,
                                                                             FTotalARFALLAMOUNTFOR=FARFALLAMOUNTFOR+(b.FSALUNITQTY-FARFQty)*b.FTAXPRICE
                                                                          from T_SAL_RETURNSTOCKENTRY a inner join T_SAL_RETURNSTOCKENTRY_F b on b.FENTRYID=a.FENTRYID where a.FENTRYID={0}", dt2.Rows[j]["销售出库单FENTRYID"].ToString());
                                            }
                                        }
                                    }
                                }
                                //更新销售出库单表体
                                DBServiceHelper.Execute(Context, upsql);

                            }
                            string UpdatesqlH = "";
                            //更新表头
                            foreach (var hs in hsset)
                            {
                                if (FSTABLENAME == "T_SAL_OUTSTOCKENTRY")
                                {
                                    sql = string.Format(@"select sum(FTotalARFNOTAXAMOUNTFOR) as FTotalARFNOTAXAMOUNTFOR_H,
                                            sum(FTotalARFALLAMOUNTFOR) as FTotalARFALLAMOUNTFOR_H  from T_SAL_OUTSTOCKENTRY where FID={0}", hs);

                                    DataSet ds2 = DBServiceHelper.ExecuteDataSet(this.Context, sql);
                                    DataTable dt2 = ds2.Tables[0];
                                    if (dt2.Rows.Count > 0)
                                    {
                                        for (int j = 0; j < dt2.Rows.Count; j++)
                                        {
                                            decimal FTotalARFNOTAXAMOUNTFOR_H = Convert.ToDecimal(dt2.Rows[j]["FTotalARFNOTAXAMOUNTFOR_H"].ToString());
                                            decimal FTotalARFALLAMOUNTFOR_H = Convert.ToDecimal(dt2.Rows[j]["FTotalARFALLAMOUNTFOR_H"].ToString());
                                            UpdatesqlH += string.Format(@"/*dialect*/ update T_SAL_OUTSTOCK set FTotalARFNOTAXAMOUNTFOR_H={1},
                                                     FTotalARFALLAMOUNTFOR_H={2} where FID={0}", hs, FTotalARFNOTAXAMOUNTFOR_H, FTotalARFALLAMOUNTFOR_H);
                                        }
                                    }
                                }
                                else
                                {
                                    sql = string.Format(@"select sum(FTotalARFNOTAXAMOUNTFOR) as FTotalARFNOTAXAMOUNTFOR_H,
                                            sum(FTotalARFALLAMOUNTFOR) as FTotalARFALLAMOUNTFOR_H  from T_SAL_RETURNSTOCKENTRY where FID={0}", hs);
                                    DataSet ds2 = DBServiceHelper.ExecuteDataSet(this.Context, sql);
                                    DataTable dt2 = ds2.Tables[0];
                                    if (dt2.Rows.Count > 0)
                                    {
                                        for (int j = 0; j < dt2.Rows.Count; j++)
                                        {
                                            decimal FTotalARFNOTAXAMOUNTFOR_H = Convert.ToDecimal(dt2.Rows[j]["FTotalARFNOTAXAMOUNTFOR_H"].ToString());
                                            decimal FTotalARFALLAMOUNTFOR_H = Convert.ToDecimal(dt2.Rows[j]["FTotalARFALLAMOUNTFOR_H"].ToString());
                                            UpdatesqlH += string.Format(@"/*dialect*/ update T_SAL_RETURNSTOCK set FTotalARFNOTAXAMOUNTFOR_H={1},
                                                     FTotalARFALLAMOUNTFOR_H={2} where FID={0}", hs, FTotalARFNOTAXAMOUNTFOR_H, FTotalARFALLAMOUNTFOR_H);
                                        }
                                    }
                                }
                            }
                            //更新销售出库单表头/销售退货单
                            DBServiceHelper.Execute(Context, UpdatesqlH);
                        }
                        #endregion
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
