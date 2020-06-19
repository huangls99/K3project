using System;
using System.Collections.Generic;
using System.Linq;
using Kingdee.BOS.Contracts;
using System;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.Metadata.FormElement;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using System.Data;

namespace YBG.K3Cloud.AllBusiness.PlugIn
{
    [Description("执行计划更新业务模式)")]
    [Kingdee.BOS.Util.HotUpdate]
    public  class YBG_OrderBusinessModel_IScheduleService : IScheduleService
    {

        public void Run(Kingdee.BOS.Context ctx, Schedule schedule)
        {
            try
            {
                string sql = string.Empty;
                #region
                sql = string.Format(@"select  FID, F_YBG_WAREHOUSE ,F_YBG_SUPPLIER from  T_SAL_ORDER where FDOCUMENTSTATUS='C' or FDOCUMENTSTATUS='B' ");
                DataSet ds = DBServiceHelper.ExecuteDataSet(ctx, sql);
                DataTable dt = ds.Tables[0];
                if (dt.Rows.Count > 0)
                {
                    string upsql = "";
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        string FID = dt.Rows[i]["FID"].ToString();
                        //供应商
                        string F_YBG_Supplier = dt.Rows[i]["F_YBG_SUPPLIER"].ToString();
                        //仓库
                        string F_YBG_Warehouse = dt.Rows[i]["F_YBG_WAREHOUSE"].ToString();
                        string F_YBG_BUSINESSMODEL = ""; //其他仓库默认01
                        //仓库编码
                        sql = string.Format(@"select  FNumber from t_BD_Stock where FSTOCKID='{0}'", F_YBG_Warehouse);
                        string CKFNumber = DBServiceHelper.ExecuteScalar<string>(ctx, sql, "0", null);
                        //供应商编码 ---自营VEN00057  VEN00099 VEN00256
                        sql = string.Format(@"select  FNumber from t_BD_Supplier where FSUPPLIERID='{0}'", F_YBG_Supplier);
                        string SPFNUMBER = DBServiceHelper.ExecuteScalar<string>(ctx, sql, "0", null);
                        if (CKFNumber == "0" || SPFNUMBER == "0")
                        {
                            

                        }
                        else
                        {
                            //非嘉里开头的
                            if (CKFNumber.StartsWith("ZF")) //挂靠01
                            {
                                F_YBG_BUSINESSMODEL = "01";
                                upsql += string.Format(@"/*dialect*/ update T_SAL_ORDER set F_YBG_BUSINESSMODEL='{0}' where FID ={1}", F_YBG_BUSINESSMODEL, FID);

                            }
                            //嘉里开头 
                            else if (CKFNumber.StartsWith("JLZF")) //04 挂靠自发
                            {
                                F_YBG_BUSINESSMODEL = "04";
                                upsql += string.Format(@"/*dialect*/ update T_SAL_ORDER set F_YBG_BUSINESSMODEL='{0}' where FID ={1}", F_YBG_BUSINESSMODEL, FID);
                            }
                            //嘉里物流主仓 嘉里苏宁移动仓 
                            else if (CKFNumber.StartsWith("JL002") || CKFNumber.StartsWith("JLSN001"))
                            {
                                // 是壹办公供应商或者自营供应商
                                if (SPFNUMBER.Contains("VEN00057") || SPFNUMBER.Contains("VEN00099") || SPFNUMBER.Contains("P451")) //自营 05
                                {
                                    F_YBG_BUSINESSMODEL = "05";
                                    upsql += string.Format(@"/*dialect*/ update T_SAL_ORDER set F_YBG_BUSINESSMODEL='{0}' where FID ={1}", F_YBG_BUSINESSMODEL, FID);
                                }
                                else //代采 06
                                {
                                    F_YBG_BUSINESSMODEL = "06";
                                    upsql += string.Format(@"/*dialect*/ update T_SAL_ORDER set F_YBG_BUSINESSMODEL='{0}' where FID ={1}", F_YBG_BUSINESSMODEL, FID);
                                }
                            }
                            //珠海仓
                            else if (CKFNumber.StartsWith("YBG001") || CKFNumber.StartsWith("YBG015") || CKFNumber.StartsWith("YBG017"))
                            {
                                F_YBG_BUSINESSMODEL = "07"; //珠海自营 07
                                upsql += string.Format(@"/*dialect*/ update T_SAL_ORDER set F_YBG_BUSINESSMODEL='{0}' where FID ={1}", F_YBG_BUSINESSMODEL, FID);

                            }
                            // 观澜仓 车公庙仓  
                            else if (CKFNumber.StartsWith("YBG002") || CKFNumber.StartsWith("YBG003"))
                            {
                                //壹办公供应商或者自营供应商
                                if (SPFNUMBER.Contains("VEN00057") || SPFNUMBER.Contains("VEN00099") || SPFNUMBER.Contains("P451")) //自营直发 05
                                {
                                    F_YBG_BUSINESSMODEL = "02";
                                    upsql += string.Format(@"/*dialect*/ update T_SAL_ORDER set F_YBG_BUSINESSMODEL='{0}' where FID ={1}", F_YBG_BUSINESSMODEL, FID);
                                }
                                else //代采直发 03
                                {
                                    F_YBG_BUSINESSMODEL = "03";
                                    upsql += string.Format(@"/*dialect*/ update T_SAL_ORDER set F_YBG_BUSINESSMODEL='{0}' where FID ={1}", F_YBG_BUSINESSMODEL, FID);
                                }

                            }
                            else //其他仓库默认01
                            {
                                //壹办公供应商或者自营供应商
                                if (SPFNUMBER.Contains("VEN00057") || SPFNUMBER.Contains("VEN00099")  || SPFNUMBER.Contains("P451")) //自营 05
                                {
                                    F_YBG_BUSINESSMODEL = "05";
                                    upsql += string.Format(@"/*dialect*/ update T_SAL_ORDER set F_YBG_BUSINESSMODEL='{0}' where FID ={1}", F_YBG_BUSINESSMODEL, FID);
                                }
                                else
                                {
                                    F_YBG_BUSINESSMODEL = "01";
                                    upsql += string.Format(@"/*dialect*/ update T_SAL_ORDER set F_YBG_BUSINESSMODEL='{0}' where FID ={1}", F_YBG_BUSINESSMODEL, FID);
                                }
                            }
                        }
                    }
                    //更新业务模式
                    DBServiceHelper.Execute(ctx, upsql);
                }
                #endregion
            }
            catch (Exception ex)
            {
                throw new Exception("更新报错：" + ex.ToString());
            }

        }
    }
}
