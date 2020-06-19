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
using Kingdee.BOS.Core.Metadata.ConvertElement;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Util;
using Kingdee.BOS.Core.Metadata.ConvertElement.ServiceArgs;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Core.Interaction;
using Kingdee.BOS.Core.DynamicForm.Operation;
using Kingdee.BOS.Core.Metadata.FieldElement;

namespace YBG.K3Cloud.AllBusiness.PlugIn
{
    [Description("收款单审核应付单下推付款申请单")]
    [Kingdee.BOS.Util.HotUpdate]
    public class YBG_Audit_AR_RECEIVEBILL : AbstractOperationServicePlugIn
    {

        /// <summary>
        /// 定制加载指定字段到实体里<p>
        /// </summary>
        /// <param name="e">事件对象</param>
        public override void OnPreparePropertys(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("FSRCORDERENTRYID");//销售订单内码
            e.FieldKeys.Add("FREALRECAMOUNT"); //本次收款金额
        }
        /// <summary>
        /// 审核操作服务
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
                        //收款单id
                        string Fid = item["Id"].ToString();
                        #region 注释
                        //sql = string.Format(@"select FORDERBILLNO,FORDERENTRYID,b.FREALRECAMOUNT from T_AR_RECEIVEBILL a 
                        //                      inner join T_AR_RECEIVEBILLSRCENTRY b  on a.FID=b.fid 
                        //                      where a.FID='{0}'", Fid);
                        //DataSet ds = DBServiceHelper.ExecuteDataSet(this.Context,sql);
                        //DataTable dt = ds.Tables[0];
                        //if (dt.Rows.Count > 0)
                        //{
                        //    string upsql = "";
                        //    for (int i = 0; i < dt.Rows.Count; i++)
                        //    {
                        //        //订单明细内码
                        //        string FORDERENTRYID = dt.Rows[i]["FORDERENTRYID"].ToString();
                        //        //本次收款金额
                        //        decimal FREALRECAMOUNT = Convert.ToDecimal(dt.Rows[i]["FREALRECAMOUNT"].ToString());
                        //        upsql += string.Format(@"/*dialect*/ update T_SAL_ORDERENTRY set FREALRECAMOUNT={0} where FENTRYID={1}", FREALRECAMOUNT, FORDERENTRYID);
                        //    }
                        //    //更新销售订单
                        //    DBServiceHelper.Execute(Context,upsql);
                        //}
                        #endregion
                        string upsql = "";
                        //收款单源单明细
                        DynamicObjectCollection RECEIVEBILLSRCENTRYList = item["RECEIVEBILLSRCENTRY"] as DynamicObjectCollection;
                        foreach (var entry in RECEIVEBILLSRCENTRYList)
                        {
                            //销售订单明细内码
                            string FORDERENTRYID = entry["FSRCORDERENTRYID"].ToString();
                            //本次收款金额
                            decimal FREALRECAMOUNT = Convert.ToDecimal(entry["REALRECAMOUNT"].ToString());
                            upsql += string.Format(@"/*dialect*/ update T_SAL_ORDERENTRY set FREALRECAMOUNT={0}+FREALRECAMOUNT  where FENTRYID={1}", FREALRECAMOUNT, FORDERENTRYID);
                        }
                        //更新销售订单
                        DBServiceHelper.Execute(Context,upsql);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new KDException("", "审核失败：" + ex.ToString());
            }

        }
       
        
        
        /// <summary>
        /// 审核结束自动下推（应付单下推付款申请单）
        /// </summary>
        /// <param name="e"></param>
        public override void AfterExecuteOperationTransaction(AfterExecuteOperationTransaction e)
        {
            base.AfterExecuteOperationTransaction(e);
            try
            {
                string sql = string.Empty;
                if (e.DataEntitys != null && e.DataEntitys.Count<DynamicObject>() > 0)
                {
                    foreach (DynamicObject item in e.DataEntitys)
                    {
                        //收款单id
                        string Fid = item["Id"].ToString();
                        string sql2 = "";
                        //收款单源单明细
                        DynamicObjectCollection RECEIVEBILLSRCENTRYList = item["RECEIVEBILLSRCENTRY"] as DynamicObjectCollection;
                        foreach(var entry in RECEIVEBILLSRCENTRYList)
                        {
                            //销售订单明细内码
                            string FORDERENTRYID = entry["FSRCORDERENTRYID"].ToString();
                            //本次收款金额
                            decimal FREALRECAMOUNT = Convert.ToDecimal(entry["REALRECAMOUNT"].ToString());
                            if (!string.IsNullOrEmpty(FORDERENTRYID))
                            {
                                //查询采购订单
                                sql = string.Format(@"select a.FBILLNO,b.FENTRYID as 采购订单明细内码 ,f.F_YBG_BUSINESSMODEL as 业务模式  from  t_PUR_POOrder a  
                                                       inner join   t_PUR_POOrderEntry b on a.fID=b.FID 
                                                       inner join T_PUR_POORDERENTRY_LK c on c.FENTRYID=b.FENTRYID 
                                                       left join T_SAL_ORDERENTRY d on d.FENTRYID=c.FSID
                                                       left  join T_SAL_ORDER f on f.FID=d.FID 
                                                       where  FSID='{0}'", FORDERENTRYID);
                                DataSet ds = DBServiceHelper.ExecuteDataSet(this.Context, sql);
                                DataTable dt = ds.Tables[0];
                                if (dt.Rows.Count > 0)
                                {
                                    for (int i = 0; i < dt.Rows.Count; i++)
                                    {
                                        string F_YBG_BUSINESSMODEL = dt.Rows[i]["采购订单明细内码"].ToString();
                                        if (F_YBG_BUSINESSMODEL == "01"|| F_YBG_BUSINESSMODEL == "04") //挂靠的采用自动生成付款申请单
                                        {
                                            string POFENTRYID = dt.Rows[i]["采购订单明细内码"].ToString();
                                            if (string.IsNullOrEmpty(sql2))
                                            {
                                                sql2 += "  FPAYABLEENTRYID='" + POFENTRYID + "' ";
                                            }
                                            else
                                            {
                                                sql2 += "  or  FPAYABLEENTRYID='" + POFENTRYID + "' ";
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (!string.IsNullOrEmpty(sql2))
                        {
                            #region  应付单下推付款申请单
                            string srcFormId = "AP_Payable"; //应付单
                            string destFormId = "CN_PAYAPPLY"; //付款申请单
                            IMetaDataService mService = Kingdee.BOS.App.ServiceHelper.GetService<IMetaDataService>();
                            IViewService vService = Kingdee.BOS.App.ServiceHelper.GetService<IViewService>();
                            FormMetadata destmeta = mService.Load(this.Context, destFormId) as FormMetadata;
                            //转换规则的唯一标识
                            string ruleKey = "AP_PayableToPayApply";
                            //var rules = ConvertServiceHelper.GetConvertRules(Context, srcFormId, destFormId);
                            //var rule = rules.FirstOrDefault(t => t.IsDefault);
                            ConvertRuleElement rule = GetDefaultConvertRule(Context, srcFormId, destFormId, ruleKey);
                            List<ListSelectedRow> lstRows = new List<ListSelectedRow>();
                            string strsql = "select a.FID , a.FENTRYID  from T_AP_PAYABLEPLAN  a left join T_AP_PAYABLE b on a.FID=b.FID   where b.FDOCUMENTSTATUS='C' and  (" + sql2 + ") ";
                            DataSet ds2 = DBServiceHelper.ExecuteDataSet(Context, strsql);
                            if (ds2.Tables[0].Rows.Count > 0)
                            {
                                HashSet<string> hasset = new HashSet<string>();
                                for (int j = 0; j < ds2.Tables[0].Rows.Count; j++)
                                {
                                    hasset.Add(ds2.Tables[0].Rows[j]["FID"].ToString());
                                    long entryId = Convert.ToInt64(ds2.Tables[0].Rows[j]["FENTRYID"]);
                                    //源单单据标识
                                    ListSelectedRow row = new ListSelectedRow(ds2.Tables[0].Rows[j]["FID"].ToString(), entryId.ToString(), 0, "AP_Payable");
                                    //源单单据体标识
                                    row.EntryEntityKey = "FEntityPlan";
                                    lstRows.Add(row);
                                }

                                PushArgs pargs = new PushArgs(rule, lstRows.ToArray());
                                IConvertService cvtService = Kingdee.BOS.App.ServiceHelper.GetService<IConvertService>();
                                OperateOption option = OperateOption.Create();
                                option.SetIgnoreWarning(true);
                                option.SetVariableValue("ignoreTransaction", false);
                                option.SetIgnoreInteractionFlag(true);
                                #region 提交审核
                                //OperateOption option2 = OperateOption.Create();
                                //option2.SetIgnoreWarning(true);
                                //option2.SetVariableValue("ignoreTransaction", true);
                                //foreach (var hid in hasset)
                                //{
                                //    //如果应付单没有提交先提交审核
                                //    IMetaDataService BomService = Kingdee.BOS.App.ServiceHelper.GetService<IMetaDataService>();
                                //    //应付单元素包
                                //    FormMetadata APMeta = BomService.Load(Context, "AP_Payable") as FormMetadata;
                                //    IViewService APVService = Kingdee.BOS.App.ServiceHelper.GetService<IViewService>();
                                //    //应付单数据包
                                //    DynamicObject APmd = APVService.LoadSingle(Context, hid, APMeta.BusinessInfo.GetDynamicObjectType());

                                //    DynamicObject[] dy = new DynamicObject[] { APmd };

                                //    object[] items = dy.Select(p => p["Id"]).ToArray();

                                //    ISubmitService submitService = Kingdee.BOS.App.ServiceHelper.GetService<ISubmitService>();
                                //    IOperationResult submitresult = submitService.Submit(Context, APMeta.BusinessInfo, items, "Submit", option2);

                                //    IAuditService auditService = Kingdee.BOS.App.ServiceHelper.GetService<IAuditService>();
                                //    IOperationResult auditresult = auditService.Audit(Context, APMeta.BusinessInfo, items, option2);
                                //}
                                #endregion

                                ConvertOperationResult cvtResult = cvtService.Push(Context, pargs, option, false);
                                if (cvtResult.IsSuccess)
                                {
                                    DynamicObject[] dylist = (from p in cvtResult.TargetDataEntities select p.DataEntity).ToArray();
                                    //修改应收单里面数据
                                    for (int K = 0; K < dylist.Length; K++)
                                    {
                                        //付款原因
                                        dylist[K]["F_YBG_Remarks"] = "供应商付款";
                                        //明细信息
                                        DynamicObjectCollection RECEIVEBILLENTRYList = dylist[K]["FPAYAPPLYENTRY"] as DynamicObjectCollection;
                                        foreach (var Entry in RECEIVEBILLENTRYList)
                                        {
                                            //结算方式
                                            BaseDataField FSETTLETYPEID = destmeta.BusinessInfo.GetField("FSETTLETYPEID") as BaseDataField;
                                            Entry["FSETTLETYPEID_Id"] = 4;
                                            Entry["FSETTLETYPEID"] = vService.LoadSingle(Context, 4, FSETTLETYPEID.RefFormDynamicObjectType);

                                        }
                                    }
                                    //保存
                                    ISaveService saveService = Kingdee.BOS.App.ServiceHelper.GetService<ISaveService>();
                                    IOperationResult saveresult = saveService.Save(Context, destmeta.BusinessInfo, dylist, option);
                                    bool reult = CheckResult(saveresult, out string mssg);
                                    if (!reult)
                                    {
                                        throw new Exception("收款款单审核成功，生成付款申请单失败：");
                                    }
                                    else
                                    {
                                        //纪录核销的纪录
                                        OperateResultCollection operateResults = saveresult.OperateResult;
                                        string fnmber = operateResults[0].Number;
                                        string fid = operateResults[0].PKValue.ToString();
                                    }
                                }
                            }
                            else
                            {
                                throw new KDException("", "应付单不存在或者未审核，下推付款申请单失败");
                            }
                            #endregion
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new KDException("", "应付单下推付款申请单失败：" + ex.ToString());
            }
        }
        /// <summary>
        /// 校验操作
        /// </summary>
        /// <param name="result"></param>
        private bool CheckResult(IOperationResult result, out string mssg)
        {
            if (!result.IsSuccess)
            {
                mssg = "";
                foreach (var item in result.ValidationErrors)
                {
                    mssg = mssg + item.Message;
                }
                if (!result.InteractionContext.IsNullOrEmpty())
                {
                    mssg = mssg + result.InteractionContext.SimpleMessage;
                }

                return false;

            }
            else
            {
                OperateResultCollection operateResults = result.OperateResult;
                string fnmber = operateResults[0].Number;
                mssg = "生成收款单单号：" + fnmber;
                return true;
            }

        }

        /// <summary>
        /// 单据转换方法
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="srcFormId"></param>
        /// <param name="destFormId"></param>
        /// <param name="ruleKey"></param>
        /// <returns></returns>
        private ConvertRuleElement GetDefaultConvertRule(Context ctx, string srcFormId, string destFormId, string ruleKey)
        {
            IMetaDataService mService = Kingdee.BOS.App.ServiceHelper.GetService<IMetaDataService>();
            var rules = mService.GetConvertRules(ctx, srcFormId, destFormId);
            var rule = ruleKey.IsNullOrEmptyOrWhiteSpace() ? rules.FirstOrDefault(p => p.IsDefault) :
                rules.FirstOrDefault(t => t.Key.EqualsIgnoreCase(ruleKey) || t.Id.EqualsIgnoreCase(ruleKey));

            return rule;
        }
    }
}
