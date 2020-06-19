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
    [Description("暂估应收保存锁定单价")]
    [Kingdee.BOS.Util.HotUpdate]
    public class YBG_Save_AR_Receivable : AbstractOperationServicePlugIn
   {

        /// <summary>
        /// 定制加载指定字段到实体里<p>
        /// </summary>
        /// <param name="e">事件对象</param>
        public override void OnPreparePropertys(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("F_YBG_Assistant");//平台代码
            e.FieldKeys.Add("FSetAccountType");//立账类型
        }
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            try
            {
                if (e.DataEntitys != null && e.DataEntitys.Count<DynamicObject>() > 0)
                {
                    foreach (DynamicObject item in e.DataEntitys)
                    {

                        string FID= item["Id"].ToString();
                        string FSetAccountType = item["FSetAccountType"].ToString();
                        if(FSetAccountType=="2"||FSetAccountType == "1" )//暂估
                        {
                            string upsql = "";
                            string F_YBG_Assistant = item["F_YBG_Assistant_Id"].ToString();
                            string sql = string.Format(@"select FNUMBER  From T_BAS_ASSISTANTDATAENTRY  where FENTRYID='{0}'", F_YBG_Assistant);
                            //编码
                            string FNUMBER = DBServiceHelper.ExecuteScalar<string>(this.Context, sql, "0", null);
                            if (!FNUMBER.Contains("SN"))
                            {
                              //更新暂估应收
                               upsql= string.Format(@"/*dialect*/ update t_AR_receivableEntry set FIsLockPrice=1  where FID ='{0}'", FID);
                            }
                            else
                            {
                                //更新暂估应收
                                upsql = string.Format(@"/*dialect*/ update t_AR_receivableEntry set FIsLockPrice=0  where FID ='{0}'", FID);
                                
                            }
                            //更新锁库字段
                            DBServiceHelper.Execute(Context, upsql);
                        }
                        //else
                        //{
                        //    string upsql = "";
                        //    string sql = string.Format(@"select arel.FSID from t_AR_receivable ar inner join t_AR_receivableEntry arE on ar.FID=arE.FID
                        //                           inner join T_AR_RECEIVABLEENTRY_LK arel on arel.FENTRYID=are.FENTRYID where ar.FID='{0}'", FID);
                        //    DataSet ds = DBServiceHelper.ExecuteDataSet(this.Context, sql);
                        //    DataTable dt = ds.Tables[0];
                        //    if (dt.Rows.Count > 0)
                        //    {
                        //        for (int i = 0; i < dt.Rows.Count; i++)
                        //        {
                        //            string FSID = dt.Rows[i]["FSID"].ToString();
                        //            //更新暂估应收
                        //            upsql += string.Format(@"/*dialect*/ update t_AR_receivableEntry set FIsLockPrice=1  where FENTRYID ='{0}'", FSID);
                        //        }
                        //    }
                        //    //锁定价格
                        //    DBServiceHelper.Execute(Context, upsql);
                        //}
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
