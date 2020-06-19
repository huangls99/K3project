using System.ComponentModel;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System.Linq;
using System;

namespace YBG.K3Cloud.AllBusiness.PlugIn
{
    [Description("采购订单提交自动计算业务模式")]
    [Kingdee.BOS.Util.HotUpdate]
    public class YBG_Commit_PurchaseOrder : AbstractOperationServicePlugIn
    {

        /// <summary>
        /// 定制加载指定字段到实体里<p>
        /// </summary>
        /// <param name="e">事件对象</param>
        public override void OnPreparePropertys(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("FSupplierId");//供应商
            e.FieldKeys.Add("F_YBG_Warehouse"); //仓库
            e.FieldKeys.Add("F_YBG_BusinessModel"); //业务模式
        }
        public override void BeginOperationTransaction(BeginOperationTransactionArgs e)
        {
            try
            {
                string sql = string.Empty;
                if (e.DataEntitys != null && e.DataEntitys.Count<DynamicObject>() > 0)
                {

                    foreach (DynamicObject item in e.DataEntitys)
                    {
                        string upsql = "";
                        string FID = item["Id"].ToString();
                        //供应商
                        string F_YBG_Supplier = item["SupplierId_Id"].ToString();
                        //仓库
                        string F_YBG_Warehouse = item["F_YBG_Warehouse_Id"].ToString();
                        //业务模式
                        string CRBusinessModel = item["F_YBG_BusinessModel"].ToString().Trim();
                        string F_YBG_BUSINESSMODEL = ""; //其他仓库默认01
                        if (string.IsNullOrEmpty(CRBusinessModel))
                        {

                            if (F_YBG_Supplier == "0" || F_YBG_Warehouse == "0")
                            {
                                throw new Exception("提交失败：未填写仓库和供应商！");
                            }
                            else
                            {
                                //仓库编码
                                sql = string.Format(@"select  FNumber from t_BD_Stock where FSTOCKID='{0}'", F_YBG_Warehouse);
                                string CKFNumber = DBServiceHelper.ExecuteScalar<string>(this.Context, sql, null, null);
                                //供应商编码 ---自营VEN00057  VEN00099 VEN00256
                                sql = string.Format(@"select  FNumber from t_BD_Supplier where FSUPPLIERID='{0}'", F_YBG_Supplier);
                                string SPFNUMBER = DBServiceHelper.ExecuteScalar<string>(this.Context, sql, null, null);
                               
                                //珠海仓
                                if (CKFNumber.StartsWith("YBG001") || CKFNumber.StartsWith("YBG015") || CKFNumber.StartsWith("YBG017"))
                                {
                                    F_YBG_BUSINESSMODEL = "07"; //珠海自营 07
                                    upsql = string.Format(@"/*dialect*/ update t_PUR_POOrder set F_YBG_BUSINESSMODEL='{0}' where FID ={1}", F_YBG_BUSINESSMODEL, FID);

                                }
                                //自营
                                else 
                                {
                                    F_YBG_BUSINESSMODEL = "05"; //自营 05
                                    upsql = string.Format(@"/*dialect*/ update t_PUR_POOrder set F_YBG_BUSINESSMODEL='{0}' where FID ={1}", F_YBG_BUSINESSMODEL, FID);
                                }
                            }
                            //更新业务模式
                            DBServiceHelper.Execute(this.Context, upsql);
                        }
                        else
                        {
                            #region 注释
                            //if (F_YBG_Supplier == "0" || F_YBG_Warehouse == "0")
                            //{
                            //    throw new Exception("提交失败：未填写仓库和供应商！");
                            //}
                            ////仓库编码
                            //sql = string.Format(@"select  FNumber from t_BD_Stock where FSTOCKID='{0}'", F_YBG_Warehouse);
                            //string CKFNumber = DBServiceHelper.ExecuteScalar<string>(this.Context, sql, null, null);
                            ////供应商编码 ---自营VEN00057  VEN00099 VEN00256
                            //sql = string.Format(@"select  FNumber from t_BD_Supplier where FSUPPLIERID='{0}'", F_YBG_Supplier);
                            //string SPFNUMBER = DBServiceHelper.ExecuteScalar<string>(this.Context, sql, null, null);
                            ////非嘉里开头的
                            //if (CKFNumber.StartsWith("ZF")) //挂靠01
                            //{
                            //    F_YBG_BUSINESSMODEL = "01";
                            //}
                            ////嘉里开头 
                            //else if (CKFNumber.StartsWith("JLZF")) //04 挂靠自发
                            //{
                            //    F_YBG_BUSINESSMODEL = "04";

                            //}
                            ////嘉里物流主仓 嘉里苏宁移动仓 
                            //else if (CKFNumber.StartsWith("JL002") || CKFNumber.StartsWith("JLSN001"))
                            //{
                            //    // 是壹办公供应商或者自营供应商
                            //    if (SPFNUMBER.Contains("VEN00057") || SPFNUMBER.Contains("VEN00099")) //自营 05
                            //    {
                            //        F_YBG_BUSINESSMODEL = "05";

                            //    }
                            //    else //代采 06
                            //    {
                            //        F_YBG_BUSINESSMODEL = "06";

                            //    }
                            //}
                            ////珠海仓
                            //else if (CKFNumber.StartsWith("YBG001") || CKFNumber.StartsWith("YBG015") || CKFNumber.StartsWith("YBG017"))
                            //{
                            //    F_YBG_BUSINESSMODEL = "07"; //珠海自营 07


                            //}
                            //// 观澜仓 车公庙仓  
                            //else if (CKFNumber.StartsWith("YBG002") || CKFNumber.StartsWith("YBG003"))
                            //{
                            //    //壹办公供应商或者自营供应商
                            //    if (SPFNUMBER.Contains("VEN00057") || SPFNUMBER.Contains("VEN00099")) //自营直发02
                            //    {
                            //        F_YBG_BUSINESSMODEL = "02";

                            //    }
                            //    else //代采直发 03
                            //    {
                            //        F_YBG_BUSINESSMODEL = "03";

                            //    }

                            //}
                            //else //其他仓库默认01
                            //{
                            //    //壹办公供应商或者自营供应商
                            //    if (SPFNUMBER.Contains("VEN00057") || SPFNUMBER.Contains("VEN00099")) //自营 05
                            //    {
                            //        F_YBG_BUSINESSMODEL = "05"; //自营

                            //    }
                            //    else
                            //    {
                            //        F_YBG_BUSINESSMODEL = "01";
                            //    }
                            //}
                            //if (F_YBG_BUSINESSMODEL != CRBusinessModel)
                            //{
                            //    throw new Exception("采购订单业务模式改变了，不能提交！");
                            //}


                            #endregion

                        }
                      
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("提交失败：" + ex.ToString());
            }
        }
    }
}
