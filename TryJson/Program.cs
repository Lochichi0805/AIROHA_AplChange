using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TryJson.Models;

namespace TryJson
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // 讀取工號對照表
            Console.WriteLine("Load account mapping started.");
            var dicAccount = ReadAccountMappingList();
            Console.WriteLine("Load account mapping completed. Loaded Rows: " + dicAccount.Count);

            // 讀取 NUP_UI_RenderData 
            Console.WriteLine("Load RenderData started.");
            var renderList = ReadRenderDataList();
            Console.WriteLine("Load RenderData completed. Loaded Rows: " + renderList.Count);
            Console.WriteLine();

            // 每筆資料都執行一次，檢查是否要替換
            foreach (var item in renderList)
            {
                Console.WriteLine($"讀取資料：{item.RequisitionID} ({item.BPMProfileUniqueID})");

                string jsonStr = item.RenderData;

                JToken json = JObject.Parse(jsonStr);
                var jtoken = json["data"]; // 跳至根節點

                // 解析 JSON ，並取得要替換的目標
                var resultList = ParseJson(item.RequisitionID, jtoken, dicAccount);
                foreach (var result in resultList)
                {
                    Console.WriteLine($"{result.Path} 可能包含工號，原始值為： {result.OrgValue} ，即將換成 {result.NewValue} ");
                }


                // 回寫 RenderData
                WriteNupRenderData(item);
            }

            if (IsDev())
            {
                Console.WriteLine("執行結束，請按任意鍵結束");
                Console.Read();
            }
        }

        /// <summary>
        /// 找出 JSON 中是否有包含工號
        /// </summary>
        /// <param name="reqID"></param>
        /// <param name="dicAccountMapping"></param>
        /// <param name="jtoken"></param>
        /// <returns></returns>
        private static IEnumerable<ResultObject> ParseJson(string reqID, JToken jtoken, Dictionary<string, string> dicAccountMapping)
        {
            foreach (JToken item in jtoken)
            {
                string name = string.Empty;
                string path = string.Empty;
                string val = string.Empty;

                if (item.Type == JTokenType.Property)
                {
                    JProperty prop = item as JProperty;
                    name = prop.Name;
                    path = prop.Path;

                    // 解析一般欄位
                    if (prop.Value.Type == JTokenType.String)
                    {
                        val = (string)prop.Value;
                        if (IsAccount(val, dicAccountMapping))
                        {
                            var newVal = dicAccountMapping[val];
                            var model = new ResultObject(reqID, path, name, val, newVal);

                            yield return model;
                        }
                    }
                    // 解析陣列
                    else if (prop.Value.Type == JTokenType.Array)
                    {
                        foreach (var subItem in prop.Value)
                        {
                            // 一般欄位是否包含工號的判斷
                            if (subItem.Type == JTokenType.String)
                            {
                                if (dicAccountMapping.ContainsKey(val))
                                {
                                    var newVal = dicAccountMapping[val];
                                    var model = new ResultObject(reqID, path, name, val, newVal);

                                    yield return model;
                                }
                            }
                            else
                            {
                                var tempResultEnm = ParseJson(reqID, subItem, dicAccountMapping).ToList();
                                foreach (var subModel in tempResultEnm)
                                {
                                    yield return subModel;
                                }
                            }
                        }
                    }
                    // 解析物件
                    else if (prop.Value.Type == JTokenType.Object)
                    {
                        foreach (var subItem in prop.Value)
                        {
                            var tempResultEnm = ParseJson(reqID, subItem, dicAccountMapping).ToList();
                            foreach (var subModel in tempResultEnm)
                            {
                                yield return subModel;
                            }
                        }
                    }
                }
                else if (item.Type == JTokenType.String)
                {
                    path = item.Path;
                    val = item.ToString();

                    // 一般欄位是否包含工號的判斷
                    if (IsAccount(val, dicAccountMapping))
                    {
                        var newVal = dicAccountMapping[val];
                        var model = new ResultObject(reqID, path, name, val, newVal);
                        yield return model;
                    }
                }
                else if (item.Type == JTokenType.Object)
                {
                    foreach (var subItem in item)
                    {
                        var tempResultEnm = ParseJson(reqID, subItem, dicAccountMapping).ToList();
                        foreach (var subModel in tempResultEnm)
                        {
                            yield return subModel;
                        }
                    }
                }
                else if (item.Type == JTokenType.Array)
                {
                    foreach (var subItem in item)
                    {
                        // 一般欄位是否包含工號的判斷
                        if (subItem.Type == JTokenType.String)
                        {
                            if (IsAccount(val, dicAccountMapping))
                            {
                                var newVal = dicAccountMapping[val];
                                var model = new ResultObject(reqID, path, name, val, newVal);
                                yield return model;
                            }
                        }
                        else
                        {
                            var tempResultEnm = ParseJson(reqID, subItem, dicAccountMapping).ToList();
                            foreach (var subModel in tempResultEnm)
                            {
                                yield return subModel;
                            }
                        }
                    }
                }
            }
        }

        private static bool IsAccount(string val, Dictionary<string, string> dicAccountMapping)
        {
            if (dicAccountMapping.ContainsKey(val))
                return true;
            else
                return false;
        }


        #region DB Method
        private static string GetFakeData()
        {
            return " {\"data\":{\"fileID\":\"79ab6fff-86ee-444c-a374-f230af63b9c8\",\"applicantInfoScope\":{\"data\":{\"processID\":\"Start01\",\"applicantID\":\"60243\",\"applicantName\":\"Rita Cheng (鄭曉男)\",\"applicantDept\":\"ARHT, CS/SSP/LS\",\"applicantDeptID\":\"50232060\",\"site\":\"\",\"extensionNumber\":\"64861\",\"costCenter\":\"AH103711\",\"isShowChangeApplicantButton\":\"0\",\"isShowCostcenter\":\"0\",\"applicantInfoSite\":\"ARHT\",\"isEnglish\":\"1\",\"tabTitle\":\"Applicant Information\",\"Deptname\":\"\"},\"failedVerification\":{},\"bpm\":{}},\"site\":\"ARHT\",\"system\":\"ARHT\",\"isOpen\":\"0\",\"subject\":\"\",\"extensionNumber\":\"\",\"isShowUploadButton\":\"0\",\"isShowChangeApplicantButton\":\"0\",\"subCustomerType\":\"Z003\",\"customerType\":\"Z001\",\"relProductCodeList\":\"[\\\"EN8801SIN/ACC-ECU-L\\\"]\",\"notERsSite\":\"ARHT\",\"customerSalesSite\":\"ARHT\",\"optionSite\":\"ARHT\",\"contactMemberGroup\":\"Andy Weng (翁子超)(Ext.18059)\",\"pqeList\":\"60150\",\"ppList\":\"61076\",\"pqeIsInApproveHistory\":\"0\",\"pqeDepartment\":\"50217025\",\"projectLeaderIsInApproveHistory\":\"0\",\"salesIsInApproveHistory\":\"0\",\"peEngineerIsInApproveHistory\":\"0\",\"ptEngineerIsInApproveHistory\":\"0\",\"teEngineerIsInApproveHistory\":\"0\",\"saEngineerIsInApproveHistory\":\"0\",\"firstCheck\":\"1\",\"FromProductCodedata\":\"\",\"MApplicantName\":\"Rita Cheng (鄭曉男)\",\"MApplicantID\":\"60243\",\"MApplicantDeptName\":\"CS/SSP/LS\",\"MApplicantDept\":\"50232060\",\"checkReleaseInformaiton\":\"\",\"hidden1\":\"\",\"pqeSpace\":[{\"pqeEngineerID\":\"60150\",\"pqeEngineer\":\"Andy Weng (翁子超)\"}],\"pqeSpace2\":{\"pqeEngineerID\":\"60150\",\"pqeEngineer\":\"Andy Weng (翁子超)\"},\"pqeCheck\":\"\",\"returnMsg\":\"\",\"projectLeaderCheck\":\"\",\"salesCheck\":\"\",\"peEngineerCheck\":\"\",\"ptEngineerCheck\":\"\",\"teEngineerCheck\":\"\",\"cpmCheck\":\"\",\"cqeEngineerCheck\":\"\",\"spacePP\":[{\"ppID\":\"61076\",\"pp\":\"Lucas-L Lee (李國玄)\"}],\"ppCheck\":\"\",\"space58\":[{\"guid\":\"3ed6a9e0-8c1d-4c8b-ae98-a322b2bc3cbe\",\"releaseInformationList\":\"[{\\\"fromProductCode\\\":\\\"\\\",\\\"lotHashtag\\\":\\\"CMKW62M-11B1B1\\\",\\\"lotQty\\\":\\\"10\\\",\\\"customer\\\":\\\"100463\\\",\\\"subCustomer\\\":\\\"\\\"}]\",\"Release_Info_No_D\":\"1\",\"space21\":[],\"table4\":{\"head\":[{}],\"body\":[{\"guid\":\"5d2030de-ece6-491c-9015-5d464465acae\",\"Release_Info_No_D3\":\"1\",\"lotHashtag\":\"CMKW62M-11B1B1\",\"lotQty\":\"10\"}],\"bodyAttr\":{\"pageIndex\":1,\"pageSize\":0}},\"lotCheck\":\"\",\"relNote\":\"\",\"table2\":{\"head\":[{}],\"body\":[{\"guid\":\"aef64411-273a-4e45-b213-1c2d5c9dcdd7\",\"Release_Info_No_D4\":\"1\",\"customer\":\"100463\"}],\"bodyAttr\":{\"pageIndex\":1,\"pageSize\":0}},\"customerCheck\":\"\",\"dateCodeFrom\":\"\",\"dateCodeTo\":\"\",\"relProductCode\":\"EN8801SIN/ACC-ECU-L\",\"validPeriodFrom\":\"2023-09-28\",\"validPeriodTo\":\"2023-10-27\"}],\"requestTitle\":\"出貨解605 hold (DN:8300051935)\",\"requestDescription\":\"Customer : 100463\",\"riskAssessment\":\"no risk\",\"projectLeaderID\":\"60914\",\"projectLeader\":\"Kangbo Hsu (徐康博)\",\"salesID\":\"60986\",\"sales\":\"Darian Chiu (邱奕銘)\",\"peEngineerID\":\"61521\",\"peEngineer\":\"Wayne-sw Chang (張書維)\",\"ptEngineerID\":\"60942\",\"ptEngineer\":\"James Chang (張程皓)\",\"teEngineerID\":\"60948\",\"teEngineer\":\"Shih-Lin Chen (陳世林)\",\"saEngineerID\":\"\",\"saEngineer\":\"\",\"cpmID\":\"60936\",\"cpm\":\"Chun-Hung Liu (劉峻宏)\",\"cqeEngineerID\":\"61229\",\"cqeEngineer\":\"Vica Yang (楊秀怡)\",\"uiLoader2\":{\"data\":{\"isShowUploadButton\":\"0\",\"isEnglish\":\"1\",\"fileID\":\"79ab6fff-86ee-444c-a374-f230af63b9c8\",\"uploadStep\":\"RtSpcLst07\",\"uploadIdentify\":\"AIROHA12\",\"isRequireAttachment\":\"\",\"uploadStepName\":\"\",\"requisitionID\":\"\",\"diagramID\":\"AIROHA12\",\"fileListName\":\"\",\"place20\":\"\",\"draftFlag\":0,\"fileList\":{\"head\":[{}],\"body\":[],\"bodyAttr\":{\"pageIndex\":1,\"pageSize\":0}},\"input1\":\"\",\"uploader\":\"60117\",\"uploaderName\":\"LY Chen (陳亮宇)\",\"identify\":\"\",\"tabTitle\":\"Attachment\"},\"failedVerification\":{},\"bpm\":{},\"preloadData\":{\"fileID\":\"79ab6fff-86ee-444c-a374-f230af63b9c8\",\"applicantID\":\"60243\",\"applicantName\":\"Rita Cheng (鄭曉男)\",\"processID\":\"RtSpcLst07\",\"identify\":\"AIROHA12\",\"diagramID\":\"AIROHA12\",\"isShowUploadButton\":\"0\"}},\"category\":\"ER\",\"checkbox1\":[\"1\"],\"requestPurpose\":\"rdDevelopment\",\"company\":\"ARHT\",\"erProductCode\":\"EN8801SIN/ACC-ECU-L\",\"requestDescription3\":\"\",\"others\":\"\",\"eFuseProgramIsReady\":\"Y\",\"fwIsReadyAtCustomerSites\":\"Y\",\"passCustomersProductionLineTest\":\"Y\",\"needCustomerSpecialHandle\":\"N\",\"needProjectLeader\":\"\",\"needSales\":\"\",\"needPeEngineer\":\"\",\"needPtEngineer\":\"\",\"needTeEngineer\":\"\",\"needSaEngineer\":\"\"},\"failedVerification\":{},\"bpm\":{},\"preloadData\":{}} ";
        }


        /// <summary>
        /// 從資料庫取得對照工號
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, string> ReadAccountMappingList()
        {
            if (IsDev())
            {
                Dictionary<string, string> names = new Dictionary<string, string>();

                // 測試資料
                names.Add("60243", "102243");
                names.Add("60150", "102150");
                names.Add("61076", "103076");
                names.Add("61521", "103521");
                names.Add("60942", "102942");
                names.Add("60948", "102948");
                names.Add("60936", "102936");
                names.Add("61229", "103229");

                return names;
            }
            else
            {
                // 取得工號對照表
                var dbCommand =
                   $@" 
                    SELECT
                        [Ori_Account],
                        [New_Account],
                        [DisplayName],
                        [MidInitial],
                        [HireDate]
                    FROM [AIROHA_MAPPING_ACCOUNT]
                    ";


                try
                {
                    // 連線至資料庫
                    string connStr = GetConnString();
                    using (var conn = new SqlConnection(connStr))
                    {
                        var result = conn.Query<AIROHA_MAPPING_ACCOUNT>(dbCommand);
                        var dic = result.ToDictionary(obj => obj.Ori_Account, obj => obj.New_Account);
                        return dic;
                    }
                }
                catch (Exception ex)
                {
                    LogService.KeepException(ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// 從 NUP 資料庫取得 RenderData
        /// </summary>
        /// <returns></returns>
        private static List<NUP_UI_RenderData> ReadRenderDataList()
        {
            if (IsDev())
            {
                return new List<NUP_UI_RenderData>()
                {
                    new NUP_UI_RenderData()
                    {
                        RequisitionID = Guid.NewGuid().ToString(),
                        UniqueID = Guid.NewGuid().ToString(),
                        RenderData = GetFakeData(),
                        BPMProfileUniqueID = "AIROHA06"
                    }
                };
            }

            // 讀取資料
            var totalRow = TakeRows();
            string topRow = "";
            if (totalRow != -1)
                topRow = $" TOP {totalRow} ";

            // 取得工號對照表
            var dbCommand =
                $@" 
                    SELECT 
                        {topRow}
                        [RequisitionID], 
                        [UniqueID],
                        [RenderData],
                        [BPMProfileUniqueID]
                    FROM 
                        [NUP_UI_RenderData]
                ";


            try
            {
                // 連線至資料庫
                string connStr = GetConnString("NUP");
                using (var conn = new SqlConnection(connStr))
                {
                    var result = conn.Query<NUP_UI_RenderData>(dbCommand);
                    var list = result.ToList();
                    return list;
                }
            }
            catch (Exception ex)
            {
                LogService.KeepException(ex);
                throw;
            }
        }

        /// <summary> 將內容回寫 NUP </summary>
        /// <param name="model"></param>
        private static void WriteNupRenderData(NUP_UI_RenderData model)
        {
            // 開發版不回寫
            if (IsDev())
                return;
            
            // UPDATE
            var dbCommand =
                $@" 
                    UPDATE 
                        [NUP_UI_RenderData]
                    SET
                        [RenderData] = @RenderData,
                    WHERE 
                        [RequisitionID] = @RequisitionID, 
                ";

            try
            {
                // 連線至資料庫
                string connStr = GetConnString("WriteNup");
                using (var conn = new SqlConnection(connStr))
                {
                    var result = conn.Execute(dbCommand, model);
                }
            }
            catch (Exception ex)
            {
                LogService.KeepException(ex);
                throw;
            }
        }
        #endregion

        #region Helper Method
        private static string GetConnString(string dbName = "BPMPro")
        {
            if (string.Compare("BPMPro", dbName, true) == 0)
                return ConfigurationManager.ConnectionStrings["BPMProConnection"].ConnectionString;
            else if (string.Compare("NUP", dbName, true) == 0)
                return ConfigurationManager.ConnectionStrings["NupConnection"].ConnectionString;
            else if (string.Compare("WriteNup", dbName, true) == 0)
                return ConfigurationManager.ConnectionStrings["WriteNupConnection"].ConnectionString;
            else
                throw new Exception(" Fail db name ");
        }

        private static bool IsDev()
        {
            string config = ConfigurationManager.AppSettings["Environment"];
            if (string.IsNullOrWhiteSpace(config))
                return true;

            if (string.Compare("Product", config, true) == 0)
                return false;
            else
                return true;
        }

        private static int TakeRows()
        {
            string config = ConfigurationManager.AppSettings["TakeRows"];
            if (string.IsNullOrWhiteSpace(config))
                return -1;

            if (string.Compare("-1", config, true) == 0)
                return -1;


            int.TryParse(config, out int tempInt);

            if (tempInt > 0)
                return tempInt;
            else
                return -1;
        }
        #endregion
    }
}
