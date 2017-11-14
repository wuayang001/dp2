﻿using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Diagnostics;
using System.Net.Mail;
using System.Web;

using Ionic.Zip;

using DigitalPlatform;	// Stop类
using DigitalPlatform.rms.Client;
using DigitalPlatform.Xml;
using DigitalPlatform.IO;
using DigitalPlatform.Text;
using DigitalPlatform.Script;
using DigitalPlatform.MarcDom;
using DigitalPlatform.Marc;
using DigitalPlatform.Range;

using DigitalPlatform.Message;
using DigitalPlatform.rms.Client.rmsws_localhost;

namespace DigitalPlatform.LibraryServer
{
    /// <summary>
    /// 本部分是和数据库管理有关的代码
    /// </summary>
    public partial class LibraryApplication
    {
        // 管理数据库
        // parameters:
        //      strLibraryCodeList  当前用户的管辖分馆代码列表
        //      strAction   动作。getinfo create delete change initialize backup refresh
        // return:
        //      -1  出错
        //      0   没有找到
        //      1   成功
        public int ManageDatabase(
            SessionInfo sessioninfo,
            RmsChannelCollection Channels,
            string strLibraryCodeList,
            string strAction,
            string strDatabaseNames,
            string strDatabaseInfo,
            out string strOutputInfo,
            out string strError)
        {
            strOutputInfo = "";
            strError = "";

            // 列出数据库名
            if (strAction == "getinfo")
            {
                return GetDatabaseInfo(
                    strLibraryCodeList,
                    strDatabaseNames,
                    out strOutputInfo,
                    out strError);
            }

            // 创建数据库
            if (strAction == "create")
            {
                return CreateDatabase(
                    sessioninfo,
                    Channels,
                    strLibraryCodeList,
                    // strDatabaseNames,
                    strDatabaseInfo,
                    false,
                    out strOutputInfo,
                    out strError);
            }

            // 重新创建数据库
            if (strAction == "recreate")
            {
                return CreateDatabase(
                    sessioninfo,
                    Channels,
                    strLibraryCodeList,
                    // strDatabaseNames,
                    strDatabaseInfo,
                    true,
                    out strOutputInfo,
                    out strError);
            }

            // 删除数据库
            if (strAction == "delete")
            {
                return DeleteDatabase(
                    sessioninfo,
                    Channels,
                    strLibraryCodeList,
                    strDatabaseNames,
                    out strOutputInfo,
                    out strError);
            }

            if (strAction == "initialize")
            {
                return InitializeDatabase(
                    sessioninfo,
                    Channels,
                    strLibraryCodeList,
                    strDatabaseNames,
                    out strOutputInfo,
                    out strError);
            }

            // 2008/11/16
            if (strAction == "refresh")
            {
                return RefreshDatabaseDefs(
                    sessioninfo,
                    Channels,
                    strLibraryCodeList,
                    strDatabaseNames,
                    strDatabaseInfo,
                    out strOutputInfo,
                    out strError);
            }

            // 修改数据库
            if (strAction == "change")
            {
                return ChangeDatabase(
                    Channels,
                    strLibraryCodeList,
                    strDatabaseNames,
                    strDatabaseInfo,
                    out strOutputInfo,
                    out strError);
            }

            strError = "未知的strAction值 '" + strAction + "'";
            return -1;
        }

        // 获得一个数据库的全部配置文件
        int GetConfigFiles(RmsChannel channel,
            string strDbName,
            string strLogFileName,
            out string strError)
        {
            strError = "";

            string strTempDir = "";

            if (string.IsNullOrEmpty(strLogFileName) == false)
            {
                strTempDir = Path.Combine(this.TempDir, "~" + Guid.NewGuid().ToString());
                PathUtil.TryCreateDir(strTempDir);
            }

            try
            {
                DirLoader loader = new DirLoader(channel,
                    null,
                    strDbName + "/cfgs");
                foreach (ResInfoItem item in loader)
                {
                    string strTargetFilePath = Path.Combine(strTempDir, strDbName, "cfgs\\" + item.Name);
                    PathUtil.TryCreateDir(Path.GetDirectoryName(strTargetFilePath));

                    using (Stream exist_stream = File.Create(strTargetFilePath))
                    {
                        string strPath = strDbName + "/cfgs/" + item.Name;
                        string strStyle = "content,data,metadata,timestamp,outputpath";
                        byte[] timestamp = null;
                        string strOutputPath = "";
                        string strMetaData = "";
                        long lRet = channel.GetRes(
                            strPath,    // item.Name,
                            exist_stream,
                            null,	// stop,
                            strStyle,
                            null,	// byte [] input_timestamp,
                            out strMetaData,
                            out timestamp,
                            out strOutputPath,
                            out strError);
                        if (lRet == -1)
                        {
                            // 配置文件不存在，怎么返回错误码的?
                            if (channel.ErrorCode == ChannelErrorCode.NotFound)
                                continue;
                            return -1;
                        }

#if NO
                    exist_stream.Seek(0, SeekOrigin.Begin);
                    using (StreamReader sr = new StreamReader(exist_stream, Encoding.UTF8))
                    {
                        strExistContent = ConvertCrLf(sr.ReadToEnd());
                    }
#endif

                    }

                }

                if (string.IsNullOrEmpty(strTempDir) == false)
                {
                    int nRet = CompressDirectory(
                        strTempDir,
                        strTempDir,
                        strLogFileName,
                        Encoding.UTF8,
                        true,
                        out strError);
                    if (nRet == -1)
                        return -1;
                }

                return 0;
            }
            finally
            {
                if (string.IsNullOrEmpty(strTempDir) == false)
                    PathUtil.DeleteDirectory(strTempDir);
            }
        }

        // 如果数据库不存在会当作出错-1来报错
        int InitializeDatabase(RmsChannel channel,
            string strDbName,
            string strLogFileName,
            out string strError)
        {
            strError = "";

            // 获得一个数据库的全部配置文件
            if (string.IsNullOrEmpty(strLogFileName) == false)
            {
                int nRet = GetConfigFiles(channel,
                strDbName,
                strLogFileName,
                out strError);
                if (nRet == -1)
                    return -1;
            }

            // 初始化书目库
            long lRet = channel.DoInitialDB(strDbName, out strError);
            return (int)lRet;
        }


        // 删除一个数据库，并删除library.xml中相关OPAC检索库定义
        // 如果数据库不存在会当作出错-1来报错
        int DeleteDatabase(RmsChannel channel,
            string strDbName,
            string strLogFileName,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            // 获得一个数据库的全部配置文件
            if (string.IsNullOrEmpty(strLogFileName) == false)
            {
                nRet = GetConfigFiles(channel,
                    strDbName,
                    strLogFileName,
                    out strError);
                if (nRet == -1)
                    return -1;
            }

            long lRet = channel.DoDeleteDB(strDbName, out strError);
            if (lRet == -1)
            {
                if (channel.ErrorCode != ChannelErrorCode.NotFound)
                    return -1;
            }

            // 删除一个数据库在OPAC可检索库中的定义
            // return:
            //      -1  error
            //      0   not change
            //      1   changed
            nRet = RemoveOpacDatabaseDef(
                channel.Container,
                strDbName,
                out strError);
            if (nRet == -1)
            {
                this.Changed = true;
                return -1;
            }

            return 0;
        }

#if NO
        // 包装后的版本
        int ChangeDbName(
    RmsChannel channel,
    string strOldDbName,
    string strNewDbName,
    out string strError)
        {
            return ChangeDbName(
            channel,
            strOldDbName,
            strNewDbName,
            () => { },
            out strError);
        }
#endif

        // parameters:
        //      change_complte  数据名修改成功后的收尾工作
        int ChangeDbName(
            RmsChannel channel,
            string strOldDbName,
            string strNewDbName,
            Action change_complete,
            out string strError)
        {
            strError = "";

            // TODO: 要对 strNewDbName 进行查重，看看是不是已经有同名的数据库存在了
            // 另外 DoSetDBInfo() API 是否负责查重？

            List<string[]> log_names = new List<string[]>();
            string[] one = new string[2];
            one[0] = strNewDbName;
            one[1] = "zh";
            log_names.Add(one);

            // 修改数据库信息
            // parameters:
            //		logicNames	逻辑库名。ArrayList。每个元素为一个string[2]类型。其中第一个字符串为名字，第二个为语言代码
            // return:
            //		-1	出错
            //		0	成功(基于WebService接口CreateDb的返回值)
            long lRet = channel.DoSetDBInfo(
                    strOldDbName,
                    log_names,
                    null,   // string strType,
                    null,   // string strSqlDbName,
                    null,   // string strKeysDef,
                    null,   // string strBrowseDef,
                    out strError);
            if (lRet == -1)
                return -1;

            // 数据名修改成功后的收尾工作
            change_complete();

            // 修改一个数据库在OPAC可检索库中的定义的名字
            // return:
            //      -1  error
            //      0   not change
            //      1   changed
            int nRet = RenameOpacDatabaseDef(
                channel.Container,
                strOldDbName,
                strNewDbName,
                out strError);
            if (nRet == -1)
                return -1;

            return 0;
        }

        // 修改数据库
        // return:
        //      -1  出错
        //      0   没有找到
        //      1   成功
        int ChangeDatabase(
            RmsChannelCollection Channels,
            string strLibraryCodeList,
            string strDatabaseNames,
            string strDatabaseInfo,
            out string strOutputInfo,
            out string strError)
        {
            strOutputInfo = "";
            strError = "";

            int nRet = 0;
            // long lRet = 0;

            bool bDbNameChanged = false;

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.LoadXml(strDatabaseInfo);
            }
            catch (Exception ex)
            {
                strError = "strDatabaseInfo内容装入XMLDOM时出错: " + ex.Message;
                return -1;
            }

            // 核对strDatabaseNames中包含的数据库名数目是否和dom中的<database>元素数相等
            string[] names = strDatabaseNames.Split(new char[] { ',' });
            XmlNodeList nodes = dom.DocumentElement.SelectNodes("database");
            if (names.Length != nodes.Count)
            {
                strError = "strDatabaseNames 参数中包含的数据库名个数 " + names.Length.ToString() + " 和 strDatabaseInfo 参数中包含的 <database> 元素数 " + nodes.Count.ToString() + " 不等";
                return -1;
            }

            RmsChannel channel = Channels.GetChannel(this.WsUrl);

            for (int i = 0; i < names.Length; i++)
            {
                string strName = names[i].Trim();
                if (String.IsNullOrEmpty(strName) == true)
                {
                    strError = "strDatabaseNames 参数中不能包含空的名字";
                    return -1;
                }

                // 来自strDatabaseInfo
                XmlElement nodeNewDatabase = nodes[i] as XmlElement;

                // 修改书目库名、或者书目库从属的其他数据库名
                if (this.IsBiblioDbName(strName) == true)
                {
                    // 获得相关配置小节
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@biblioDbName='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "配置DOM中名字为 '" + strName + "' 的书目库(biblioDbName属性)相关<database>元素没有找到";
                        return 0;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许修改书目库定义";
                        return -1;
                    }

                    /*
                     * <database>元素中的name/entityDbName/orderDbName/issueDbName
                     * 传递了改名请求。如果属性值为空，不表明要删除数据库，而是该属性无效。
                     * 实际上这里不响应删除数据库的动作，只能改名
                     * 将来可以改造为响应删除数据库的请求，那么，只要属性具备，就有请求。如果不想请求，则不要包含那个属性
                     * */
                    {
                        // 书目库名
                        string strOldBiblioDbName = strName;

                        // 来自strDatabaseInfo
                        string strNewBiblioDbName = DomUtil.GetAttr(nodeNewDatabase,
                            "name");

                        // 如果strNewBiblioDbName为空，表示不想改变名字
                        if (String.IsNullOrEmpty(strNewBiblioDbName) == false
                            && strOldBiblioDbName != strNewBiblioDbName)
                        {
                            if (String.IsNullOrEmpty(strOldBiblioDbName) == true
                                && String.IsNullOrEmpty(strNewBiblioDbName) == false)
                            {
                                strError = "要创建书目库 '" + strNewBiblioDbName + "'，请使用create功能，而不能使用change功能";
                                goto ERROR1;
                            }

                            nRet = ChangeDbName(
                                channel,
                                strOldBiblioDbName,
                                strNewBiblioDbName,
                                () =>
                                {
                                    DomUtil.SetAttr(nodeDatabase, "biblioDbName", strNewBiblioDbName);
                                    bDbNameChanged = true;
                                    this.Changed = true;
                                },
                                out strError);
                            if (nRet == -1)
                                goto ERROR1;
#if NO
                            bDbNameChanged = true;
                            DomUtil.SetAttr(nodeDatabase, "biblioDbName", strNewBiblioDbName);
                            this.Changed = true;
#endif
                        }
                    }

                    {
                        // 实体库名
                        string strOldEntityDbName = DomUtil.GetAttr(nodeDatabase,
                            "name");

                        // 来自strDatabaseInfo
                        string strNewEntityDbName = DomUtil.GetAttr(nodeNewDatabase,
                            "entityDbName");

                        if (String.IsNullOrEmpty(strNewEntityDbName) == false
                            && strOldEntityDbName != strNewEntityDbName)
                        {
                            if (String.IsNullOrEmpty(strOldEntityDbName) == true
                                && String.IsNullOrEmpty(strNewEntityDbName) == false)
                            {
                                strError = "要创建实体库 '" + strNewEntityDbName + "'，请使用create功能，而不能使用change功能";
                                goto ERROR1;
                            }

                            nRet = ChangeDbName(
                                channel,
                                strOldEntityDbName,
                                strNewEntityDbName,
                                () =>
                                {
                                    DomUtil.SetAttr(nodeDatabase, "name", strNewEntityDbName);
                                    bDbNameChanged = true;
                                    this.Changed = true;
                                },
                                out strError);
                            if (nRet == -1)
                                goto ERROR1;

#if NO
                            bDbNameChanged = true;

                            DomUtil.SetAttr(nodeDatabase, "name", strNewEntityDbName);
                            this.Changed = true;
#endif
                        }
                    }


                    {
                        // 订购库名
                        string strOldOrderDbName = DomUtil.GetAttr(nodeDatabase,
                            "orderDbName");

                        // 来自strDatabaseInfo
                        string strNewOrderDbName = DomUtil.GetAttr(nodeNewDatabase,
                            "orderDbName");
                        if (String.IsNullOrEmpty(strNewOrderDbName) == false
                            && strOldOrderDbName != strNewOrderDbName)
                        {
                            if (String.IsNullOrEmpty(strOldOrderDbName) == true
                                && String.IsNullOrEmpty(strNewOrderDbName) == false)
                            {
                                strError = "要创建订购库 '" + strNewOrderDbName + "'，请使用create功能，而不能使用change功能";
                                goto ERROR1;
                            }

                            nRet = ChangeDbName(
                                channel,
                                strOldOrderDbName,
                                strNewOrderDbName,
                                () =>
                                {
                                    DomUtil.SetAttr(nodeDatabase, "orderDbName", strNewOrderDbName);
                                    bDbNameChanged = true;
                                    this.Changed = true;
                                },
                                out strError);
                            if (nRet == -1)
                                goto ERROR1;

#if NO
                            bDbNameChanged = true;

                            DomUtil.SetAttr(nodeDatabase, "orderDbName", strNewOrderDbName);
                            this.Changed = true;
#endif
                        }
                    }

                    {
                        // 期库名
                        string strOldIssueDbName = DomUtil.GetAttr(nodeDatabase,
                            "issueDbName");

                        // 来自strDatabaseInfo
                        string strNewIssueDbName = DomUtil.GetAttr(nodeNewDatabase,
                            "issueDbName");
                        if (String.IsNullOrEmpty(strNewIssueDbName) == false
                            && strOldIssueDbName != strNewIssueDbName)
                        {
                            if (String.IsNullOrEmpty(strOldIssueDbName) == true
                                && String.IsNullOrEmpty(strNewIssueDbName) == false)
                            {
                                strError = "要创建期库 '" + strNewIssueDbName + "'，请使用create功能，而不能使用change功能";
                                goto ERROR1;
                            }

                            nRet = ChangeDbName(
                                channel,
                                strOldIssueDbName,
                                strNewIssueDbName,
                                () =>
                                {
                                    DomUtil.SetAttr(nodeDatabase, "issueDbName", strNewIssueDbName);
                                    bDbNameChanged = true;
                                    this.Changed = true;
                                },
                                out strError);
                            if (nRet == -1)
                                goto ERROR1;

#if NO
                            bDbNameChanged = true;

                            DomUtil.SetAttr(nodeDatabase, "issueDbName", strNewIssueDbName);
                            this.Changed = true;
#endif
                        }
                    }


                    {
                        // 评注库名
                        string strOldCommentDbName = DomUtil.GetAttr(nodeDatabase,
                            "commentDbName");

                        // 来自strDatabaseInfo
                        string strNewCommentDbName = DomUtil.GetAttr(nodeNewDatabase,
                            "commentDbName");
                        if (String.IsNullOrEmpty(strNewCommentDbName) == false
                            && strOldCommentDbName != strNewCommentDbName)
                        {
                            if (String.IsNullOrEmpty(strOldCommentDbName) == true
                                && String.IsNullOrEmpty(strNewCommentDbName) == false)
                            {
                                strError = "要创建评注库 '" + strNewCommentDbName + "'，请使用create功能，而不能使用change功能";
                                goto ERROR1;
                            }

                            nRet = ChangeDbName(
                                channel,
                                strOldCommentDbName,
                                strNewCommentDbName,
                                () =>
                                {
                                    DomUtil.SetAttr(nodeDatabase, "commentDbName", strNewCommentDbName);
                                    bDbNameChanged = true;
                                    this.Changed = true;
                                },
                                out strError);
                            if (nRet == -1)
                                goto ERROR1;

#if NO
                            bDbNameChanged = true;

                            DomUtil.SetAttr(nodeDatabase, "commentDbName", strNewCommentDbName);
                            this.Changed = true;
#endif
                        }
                    }

                    // 是否参与流通
                    if (DomUtil.HasAttr(nodeNewDatabase, "inCirculation") == true)
                    {
                        string strOldInCirculation = DomUtil.GetAttr(nodeDatabase,
                            "inCirculation");
                        if (String.IsNullOrEmpty(strOldInCirculation) == true)
                            strOldInCirculation = "true";

                        string strNewInCirculation = DomUtil.GetAttr(nodeNewDatabase,
                            "inCirculation");
                        if (String.IsNullOrEmpty(strNewInCirculation) == true)
                            strNewInCirculation = "true";

                        if (strOldInCirculation != strNewInCirculation)
                        {
                            DomUtil.SetAttr(nodeDatabase, "inCirculation",
                                strNewInCirculation);
                            this.Changed = true;
                        }
                    }

                    // 角色
                    // TODO: 是否要进行检查?
                    if (DomUtil.HasAttr(nodeNewDatabase, "role") == true)
                    {
                        string strOldRole = DomUtil.GetAttr(nodeDatabase,
                            "role");

                        string strNewRole = DomUtil.GetAttr(nodeNewDatabase,
                            "role");

                        if (strOldRole != strNewRole)
                        {
                            DomUtil.SetAttr(nodeDatabase, "role",
                                strNewRole);
                            this.Changed = true;
                        }
                    }

                    // 2012/4/30
                    // 联合编目特性
                    if (DomUtil.HasAttr(nodeNewDatabase, "unionCatalogStyle") == true)
                    {
                        string strOldUnionCatalogStyle = DomUtil.GetAttr(nodeDatabase,
                            "unionCatalogStyle");

                        string strNewUnionCatalogStyle = DomUtil.GetAttr(nodeNewDatabase,
                            "unionCatalogStyle");

                        if (strOldUnionCatalogStyle != strNewUnionCatalogStyle)
                        {
                            DomUtil.SetAttr(nodeDatabase, "unionCatalogStyle",
                                strNewUnionCatalogStyle);
                            this.Changed = true;
                        }
                    }

                    // 复制
                    if (DomUtil.HasAttr(nodeNewDatabase, "replication") == true)
                    {
                        string strOldReplication = DomUtil.GetAttr(nodeDatabase,
                            "replication");

                        string strNewReplication = DomUtil.GetAttr(nodeNewDatabase,
                            "replication");

                        if (strOldReplication != strNewReplication)
                        {
                            DomUtil.SetAttr(nodeDatabase, "replication",
                                strNewReplication);
                            this.Changed = true;
                        }
                    }

                    // <itemdbgroup>内容更新，刷新配套的内存结构
                    nRet = this.LoadItemDbGroupParam(this.LibraryCfgDom,
                        out strError);
                    if (nRet == -1)
                    {
                        this.WriteErrorLog(strError);
                        return -1;
                    }

                    this.Changed = true;
                    continue;
                } // end of if 书目库名


                // 单独修改实体库名
                // 能修改是否参与流通
                if (this.IsItemDbName(strName) == true)
                {
                    // 获得相关配置小节
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@name='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "配置DOM中名字为 '" + strName + "' 的实体库(name属性)相关<database>元素没有找到";
                        return 0;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许修改实体库定义";
                        return -1;
                    }

                    // 实体库名
                    string strOldEntityDbName = DomUtil.GetAttr(nodeDatabase,
                        "name");

                    // 来自strDatabaseInfo
                    string strNewEntityDbName = DomUtil.GetAttr(nodeNewDatabase,
                        "name");

                    if (strOldEntityDbName != strNewEntityDbName)
                    {
                        if (String.IsNullOrEmpty(strOldEntityDbName) == true
                            && String.IsNullOrEmpty(strNewEntityDbName) == false)
                        {
                            strError = "要创建实体库 '" + strNewEntityDbName + "'，请使用create功能，而不能使用change功能";
                            goto ERROR1;
                        }

                        if (String.IsNullOrEmpty(strOldEntityDbName) == false
                            && String.IsNullOrEmpty(strNewEntityDbName) == true)
                        {
                            strError = "要删除实体库 '" + strNewEntityDbName + "'，请使用delete功能，而不能使用change功能";
                            goto ERROR1;
                        }

                        nRet = ChangeDbName(
                            channel,
                            strOldEntityDbName,
                            strNewEntityDbName,
                                () =>
                                {
                                    DomUtil.SetAttr(nodeDatabase, "name", strNewEntityDbName);
                                    bDbNameChanged = true;
                                    this.Changed = true;
                                },
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

#if NO
                        bDbNameChanged = true;

                        DomUtil.SetAttr(nodeDatabase, "name", strNewEntityDbName);
                        this.Changed = true;
#endif
                    }

                    // 是否参与流通
                    {
                        string strOldInCirculation = DomUtil.GetAttr(nodeDatabase,
                            "inCirculation");
                        if (String.IsNullOrEmpty(strOldInCirculation) == true)
                            strOldInCirculation = "true";

                        string strNewInCirculation = DomUtil.GetAttr(nodeNewDatabase,
                            "inCirculation");
                        if (String.IsNullOrEmpty(strNewInCirculation) == true)
                            strNewInCirculation = "true";

                        if (strOldInCirculation != strNewInCirculation)
                        {
                            DomUtil.SetAttr(nodeDatabase, "inCirculation",
                                strNewInCirculation);
                            this.Changed = true;
                        }

                    }

                    // <itemdbgroup>内容更新，刷新配套的内存结构
                    nRet = this.LoadItemDbGroupParam(this.LibraryCfgDom,
                        out strError);
                    if (nRet == -1)
                    {
                        this.WriteErrorLog(strError);
                        return -1;
                    }

                    this.Changed = true;
                    continue;
                }

                // 单独修改订购库名
                if (this.IsOrderDbName(strName) == true)
                {
                    // 获得相关配置小节
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@orderDbName='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "配置DOM中名字为 '" + strName + "' 的订购库(orderDbName属性)相关<database>元素没有找到";
                        return 0;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许修改订购库定义";
                        return -1;
                    }

                    // 来自LibraryCfgDom
                    string strOldOrderDbName = DomUtil.GetAttr(nodeDatabase,
                        "orderDbName");

                    // 来自strDatabaseInfo
                    string strNewOrderDbName = DomUtil.GetAttr(nodeNewDatabase,
                        "name");

                    if (strOldOrderDbName != strNewOrderDbName)
                    {
                        if (String.IsNullOrEmpty(strOldOrderDbName) == true
                            && String.IsNullOrEmpty(strNewOrderDbName) == false)
                        {
                            strError = "要创建订购库 '" + strNewOrderDbName + "'，请使用create功能，而不能使用change功能";
                            goto ERROR1;
                        }

                        if (String.IsNullOrEmpty(strOldOrderDbName) == false
                            && String.IsNullOrEmpty(strNewOrderDbName) == true)
                        {
                            strError = "要删除订购库 '" + strNewOrderDbName + "'，请使用delete功能，而不能使用change功能";
                            goto ERROR1;
                        }

                        nRet = ChangeDbName(
                            channel,
                            strOldOrderDbName,
                            strNewOrderDbName,
                                () =>
                                {
                                    DomUtil.SetAttr(nodeDatabase, "orderDbName", strNewOrderDbName);
                                    bDbNameChanged = true;
                                    this.Changed = true;
                                },
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

#if NO
                        bDbNameChanged = true;

                        DomUtil.SetAttr(nodeDatabase, "orderDbName", strNewOrderDbName);
                        this.Changed = true;
#endif
                    }

                    // <itemdbgroup>内容更新，刷新配套的内存结构
                    nRet = this.LoadItemDbGroupParam(this.LibraryCfgDom,
                        out strError);
                    if (nRet == -1)
                    {
                        this.WriteErrorLog(strError);
                        return -1;
                    }

                    this.Changed = true;
                    continue;
                }

                // 单独修改期库名
                if (this.IsIssueDbName(strName) == true)
                {
                    // 获得相关配置小节
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@issueDbName='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "配置DOM中名字为 '" + strName + "' 的期库(issueDbName属性)相关<database>元素没有找到";
                        return 0;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许修改期库定义";
                        return -1;
                    }

                    // 来自LibraryCfgDom
                    string strOldIssueDbName = DomUtil.GetAttr(nodeDatabase,
                        "issueDbName");
                    // 来自strDatabaseInfo
                    string strNewIssueDbName = DomUtil.GetAttr(nodeNewDatabase,
                        "name");    // 2012/4/30 changed

                    if (strOldIssueDbName != strNewIssueDbName)
                    {
                        if (String.IsNullOrEmpty(strOldIssueDbName) == true
                            && String.IsNullOrEmpty(strNewIssueDbName) == false)
                        {
                            strError = "要创建期库 '" + strNewIssueDbName + "'，请使用create功能，而不能使用change功能";
                            goto ERROR1;
                        }

                        if (String.IsNullOrEmpty(strOldIssueDbName) == false
                            && String.IsNullOrEmpty(strNewIssueDbName) == true)
                        {
                            strError = "要删除期库 '" + strNewIssueDbName + "'，请使用delete功能，而不能使用change功能";
                            goto ERROR1;
                        }

                        nRet = ChangeDbName(
                            channel,
                            strOldIssueDbName,
                            strNewIssueDbName,
                                () =>
                                {
                                    DomUtil.SetAttr(nodeDatabase, "issueDbName", strNewIssueDbName);
                                    bDbNameChanged = true;
                                    this.Changed = true;
                                },
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

#if NO
                        bDbNameChanged = true;

                        DomUtil.SetAttr(nodeDatabase, "issueDbName", strNewIssueDbName);
                        this.Changed = true;
#endif
                    }

                    // <itemdbgroup>内容更新，刷新配套的内存结构
                    nRet = this.LoadItemDbGroupParam(this.LibraryCfgDom,
                        out strError);
                    if (nRet == -1)
                    {
                        this.WriteErrorLog(strError);
                        return -1;
                    }

                    this.Changed = true;
                    continue;
                }

                // 单独修改评注库名
                if (this.IsCommentDbName(strName) == true)
                {
                    // 获得相关配置小节
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@commentDbName='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "配置DOM中名字为 '" + strName + "' 的评注库(commentDbName属性)相关<database>元素没有找到";
                        return 0;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许修改评注库定义";
                        return -1;
                    }

                    // 来自LibraryCfgDom
                    string strOldCommentDbName = DomUtil.GetAttr(nodeDatabase,
                        "commentDbName");
                    // 来自strDatabaseInfo
                    string strNewCommentDbName = DomUtil.GetAttr(nodeNewDatabase,
                        "name");    // 2012/4/30 changed

                    if (strOldCommentDbName != strNewCommentDbName)
                    {
                        if (String.IsNullOrEmpty(strOldCommentDbName) == true
                            && String.IsNullOrEmpty(strNewCommentDbName) == false)
                        {
                            strError = "要创建评注库 '" + strNewCommentDbName + "'，请使用create功能，而不能使用change功能";
                            goto ERROR1;
                        }

                        if (String.IsNullOrEmpty(strOldCommentDbName) == false
                            && String.IsNullOrEmpty(strNewCommentDbName) == true)
                        {
                            strError = "要删除评注库 '" + strNewCommentDbName + "'，请使用delete功能，而不能使用change功能";
                            goto ERROR1;
                        }

                        nRet = ChangeDbName(
                            channel,
                            strOldCommentDbName,
                            strNewCommentDbName,
                                () =>
                                {
                                    DomUtil.SetAttr(nodeDatabase, "commentDbName", strNewCommentDbName);
                                    bDbNameChanged = true;
                                    this.Changed = true;
                                },
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

#if NO
                        bDbNameChanged = true;

                        DomUtil.SetAttr(nodeDatabase, "commentDbName", strNewCommentDbName);
                        this.Changed = true;
#endif
                    }

                    // <itemdbgroup>内容更新，刷新配套的内存结构
                    nRet = this.LoadItemDbGroupParam(this.LibraryCfgDom,
                        out strError);
                    if (nRet == -1)
                    {
                        this.WriteErrorLog(strError);
                        return -1;
                    }

                    this.Changed = true;
                    continue;
                }

                // 修改读者库名
                if (this.IsReaderDbName(strName) == true)
                {
                    // 获得相关配置小节
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("readerdbgroup/database[@name='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "配置DOM中名字为 '" + strName + "' 的读者库(name属性)相关<database>元素没有找到";
                        return 0;
                    }

                    // 2012/9/9
                    // 分馆用户只允许修改属于管辖分馆的读者库
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        string strExistLibraryCode = DomUtil.GetAttr(nodeDatabase, "libraryCode");

                        if (string.IsNullOrEmpty(strExistLibraryCode) == true
                            || StringUtil.IsInList(strExistLibraryCode, strLibraryCodeList) == false)
                        {
                            strError = "修改读者库 '" + strName + "' 定义被拒绝。当前用户只能修改图书馆代码完全属于 '" + strLibraryCodeList + "' 范围的读者库";
                            return -1;
                        }
                    }

                    // 来自LibraryCfgDom
                    string strOldReaderDbName = DomUtil.GetAttr(nodeDatabase,
                        "name");
                    // 来自strDatabaseInfo
                    string strNewReaderDbName = DomUtil.GetAttr(nodeNewDatabase,
                        "name");

                    if (strOldReaderDbName != strNewReaderDbName)
                    {
                        if (String.IsNullOrEmpty(strOldReaderDbName) == true
                            && String.IsNullOrEmpty(strNewReaderDbName) == false)
                        {
                            strError = "要创建读者库 '" + strNewReaderDbName + "'，请使用create功能，而不能使用change功能";
                            goto ERROR1;
                        }

                        if (String.IsNullOrEmpty(strOldReaderDbName) == false
                            && String.IsNullOrEmpty(strNewReaderDbName) == true)
                        {
                            strError = "要删除读者库 '" + strNewReaderDbName + "'，请使用delete功能，而不能使用change功能";
                            goto ERROR1;
                        }

                        nRet = ChangeDbName(
                            channel,
                            strOldReaderDbName,
                            strNewReaderDbName,
                                () =>
                                {
                                    DomUtil.SetAttr(nodeDatabase, "name", strNewReaderDbName);
                                    bDbNameChanged = true;
                                    this.Changed = true;
                                },
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

#if NO
                        bDbNameChanged = true;

                        DomUtil.SetAttr(nodeDatabase, "name", strNewReaderDbName);
                        this.Changed = true;
#endif
                    }

                    // 是否参与流通
                    // 只有当提交的 XML 片断中具有 inCirculation 属性的时候，才会发生修改
                    if (nodeNewDatabase.GetAttributeNode("inCirculation") != null)
                    {
                        // 来自LibraryCfgDom
                        string strOldInCirculation = DomUtil.GetAttr(nodeDatabase,
                            "inCirculation");
                        if (String.IsNullOrEmpty(strOldInCirculation) == true)
                            strOldInCirculation = "true";

                        // 来自strDatabaseInfo
                        string strNewInCirculation = DomUtil.GetAttr(nodeNewDatabase,
                            "inCirculation");
                        if (String.IsNullOrEmpty(strNewInCirculation) == true)
                            strNewInCirculation = "true";

                        if (strOldInCirculation != strNewInCirculation)
                        {
                            DomUtil.SetAttr(nodeDatabase,
                                "inCirculation",
                                strNewInCirculation);
                            this.Changed = true;
                        }

                    }

                    // 2012/9/7
                    // 图书馆代码
                    // 只有当提交的 XML 片断中具有 libraryCode 属性的时候，才会发生修改
                    if (nodeNewDatabase.GetAttributeNode("libraryCode") != null)
                    {
                        // 来自LibraryCfgDom
                        string strOldLibraryCode = DomUtil.GetAttr(nodeDatabase,
                            "libraryCode");
                        // 来自strDatabaseInfo
                        string strNewLibraryCode = DomUtil.GetAttr(nodeNewDatabase,
                            "libraryCode");

                        if (strOldLibraryCode != strNewLibraryCode)
                        {
                            if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                            {
                                if (string.IsNullOrEmpty(strNewLibraryCode) == true
                                    || StringUtil.IsInList(strNewLibraryCode, strLibraryCodeList) == false)
                                {
                                    strError = "修改读者库 '" + strName + "' 定义被拒绝。修改后的新图书馆代码必须完全属于 '" + strLibraryCodeList + "' 范围";
                                    return -1;
                                }
                            }

                            // 检查一个单独的图书馆代码是否格式正确
                            // 要求不能为 '*'，不能包含逗号
                            // return:
                            //      -1  校验函数本身出错了
                            //      0   校验正确
                            //      1   校验发现问题。strError中有描述
                            nRet = VerifySingleLibraryCode(strNewLibraryCode,
                out strError);
                            if (nRet != 0)
                            {
                                strError = "图书馆代码 '" + strNewLibraryCode + "' 格式错误: " + strError;
                                goto ERROR1;
                            }

                            DomUtil.SetAttr(nodeDatabase,
                                "libraryCode",
                                strNewLibraryCode);
                            this.Changed = true;
                        }
                    }

                    // <readerdbgroup>内容更新，刷新配套的内存结构
                    this.LoadReaderDbGroupParam(this.LibraryCfgDom);

                    this.Changed = true;
                    continue;
                }

                // 修改预约到书库名
                if (this.ArrivedDbName == strName)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许修改预约到书库定义";
                        return -1;
                    }

                    string strOldArrivedDbName = this.ArrivedDbName;

                    // 来自strDatabaseInfo
                    string strNewArrivedDbName = DomUtil.GetAttr(nodeNewDatabase,
                        "name");

                    if (strOldArrivedDbName != strNewArrivedDbName)
                    {
                        if (String.IsNullOrEmpty(strOldArrivedDbName) == true
                            && String.IsNullOrEmpty(strNewArrivedDbName) == false)
                        {
                            strError = "要创建预约到书库 '" + strNewArrivedDbName + "'，请使用create功能，而不能使用change功能";
                            goto ERROR1;
                        }

                        if (String.IsNullOrEmpty(strOldArrivedDbName) == false
                            && String.IsNullOrEmpty(strNewArrivedDbName) == true)
                        {
                            strError = "要删除预约到书库 '" + strNewArrivedDbName + "'，请使用delete功能，而不能使用change功能";
                            goto ERROR1;
                        }

                        nRet = ChangeDbName(
                            channel,
                            strOldArrivedDbName,
                            strNewArrivedDbName,
                                () =>
                                {
                                    this.ArrivedDbName = strNewArrivedDbName;
                                    this.Changed = true;
                                },
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

#if NO
                        this.Changed = true;
                        this.ArrivedDbName = strNewArrivedDbName;
#endif
                    }

                    continue;
                }

                // 修改违约金库名
                if (this.AmerceDbName == strName)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许修改违约金库定义";
                        return -1;
                    }

                    string strOldAmerceDbName = this.AmerceDbName;

                    // 来自strDatabaseInfo
                    string strNewAmerceDbName = DomUtil.GetAttr(nodeNewDatabase,
                        "name");

                    if (strOldAmerceDbName != strNewAmerceDbName)
                    {
                        if (String.IsNullOrEmpty(strOldAmerceDbName) == true
                            && String.IsNullOrEmpty(strNewAmerceDbName) == false)
                        {
                            strError = "要创建违约金库 '" + strNewAmerceDbName + "'，请使用create功能，而不能使用change功能";
                            goto ERROR1;
                        }

                        if (String.IsNullOrEmpty(strOldAmerceDbName) == false
                            && String.IsNullOrEmpty(strNewAmerceDbName) == true)
                        {
                            strError = "要删除违约金库 '" + strNewAmerceDbName + "'，请使用delete功能，而不能使用change功能";
                            goto ERROR1;
                        }

                        nRet = ChangeDbName(
                            channel,
                            strOldAmerceDbName,
                            strNewAmerceDbName,
                                () =>
                                {
                                    this.AmerceDbName = strNewAmerceDbName;
                                    this.Changed = true;
                                },
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

#if NO
                        this.Changed = true;
                        this.AmerceDbName = strNewAmerceDbName;
#endif
                    }

                    continue;
                }

                // 修改发票库名
                if (this.InvoiceDbName == strName)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许修改发票库定义";
                        return -1;
                    }

                    string strOldInvoiceDbName = this.InvoiceDbName;

                    // 来自strDatabaseInfo
                    string strNewInvoiceDbName = DomUtil.GetAttr(nodeNewDatabase,
                        "name");

                    if (strOldInvoiceDbName != strNewInvoiceDbName)
                    {
                        if (String.IsNullOrEmpty(strOldInvoiceDbName) == true
                            && String.IsNullOrEmpty(strNewInvoiceDbName) == false)
                        {
                            strError = "要创建发票库 '" + strNewInvoiceDbName + "'，请使用create功能，而不能使用change功能";
                            goto ERROR1;
                        }

                        if (String.IsNullOrEmpty(strOldInvoiceDbName) == false
                            && String.IsNullOrEmpty(strNewInvoiceDbName) == true)
                        {
                            strError = "要删除发票库 '" + strNewInvoiceDbName + "'，请使用delete功能，而不能使用change功能";
                            goto ERROR1;
                        }

                        nRet = ChangeDbName(
                            channel,
                            strOldInvoiceDbName,
                            strNewInvoiceDbName,
                                () =>
                                {
                                    this.InvoiceDbName = strNewInvoiceDbName;
                                    this.Changed = true;
                                },
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

#if NO
                        this.Changed = true;
                        this.InvoiceDbName = strNewInvoiceDbName;
#endif
                    }

                    continue;
                }

                // 修改消息库名
                if (this.MessageDbName == strName)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许修改消息库定义";
                        return -1;
                    }

                    string strOldMessageDbName = this.MessageDbName;

                    // 来自strDatabaseInfo
                    string strNewMessageDbName = DomUtil.GetAttr(nodeNewDatabase,
                        "name");

                    if (strOldMessageDbName != strNewMessageDbName)
                    {
                        if (String.IsNullOrEmpty(strOldMessageDbName) == true
                            && String.IsNullOrEmpty(strNewMessageDbName) == false)
                        {
                            strError = "要创建消息库 '" + strNewMessageDbName + "'，请使用create功能，而不能使用change功能";
                            goto ERROR1;
                        }

                        if (String.IsNullOrEmpty(strOldMessageDbName) == false
                            && String.IsNullOrEmpty(strNewMessageDbName) == true)
                        {
                            strError = "要删除消息库 '" + strNewMessageDbName + "'，请使用delete功能，而不能使用change功能";
                            goto ERROR1;
                        }

                        nRet = ChangeDbName(
                            channel,
                            strOldMessageDbName,
                            strNewMessageDbName,
                                () =>
                                {
                                    this.MessageDbName = strNewMessageDbName;
                                    this.Changed = true;
                                },
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

#if NO
                        this.Changed = true;
                        this.MessageDbName = strNewMessageDbName;
#endif
                    }

                    continue;
                }

                // 修改拼音库名
                if (this.PinyinDbName == strName
                    || this.GcatDbName == strName
                    || this.WordDbName == strName)
                {
                    string strTypeCaption = "";
                    string strType = "";
                    string strDbName = "";
                    if (strName == this.PinyinDbName)
                    {
                        strType = "pinyin";
                        strTypeCaption = "拼音";
                        strDbName = this.PinyinDbName;
                    }
                    if (strName == this.GcatDbName)
                    {
                        strType = "gcat";
                        strTypeCaption = "著者号码";
                        strDbName = this.GcatDbName;
                    }
                    if (strName == this.WordDbName)
                    {
                        strType = "word";
                        strTypeCaption = "词";
                        strDbName = this.WordDbName;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许修改" + strTypeCaption + "库定义";
                        return -1;
                    }

                    string strOldMessageDbName = strDbName;

                    // 来自strDatabaseInfo
                    string strNewMessageDbName = DomUtil.GetAttr(nodeNewDatabase,
                        "name");

                    if (strOldMessageDbName != strNewMessageDbName)
                    {
                        if (String.IsNullOrEmpty(strOldMessageDbName) == true
                            && String.IsNullOrEmpty(strNewMessageDbName) == false)
                        {
                            strError = "要创建" + strTypeCaption + "库 '" + strNewMessageDbName + "'，请使用create功能，而不能使用change功能";
                            goto ERROR1;
                        }

                        if (String.IsNullOrEmpty(strOldMessageDbName) == false
                            && String.IsNullOrEmpty(strNewMessageDbName) == true)
                        {
                            strError = "要删除" + strTypeCaption + "库 '" + strNewMessageDbName + "'，请使用delete功能，而不能使用change功能";
                            goto ERROR1;
                        }

                        nRet = ChangeDbName(
                            channel,
                            strOldMessageDbName,
                            strNewMessageDbName,
                                () =>
                                {
                                    if (strType == "pinyin")
                                        this.PinyinDbName = strNewMessageDbName;
                                    if (strType == "gcat")
                                        this.GcatDbName = strNewMessageDbName;
                                    if (strType == "word")
                                        this.WordDbName = strNewMessageDbName;
                                    this.Changed = true;
                                },
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                    }

                    continue;
                }

                // 修改实用库名
                if (IsUtilDbName(strName) == true)
                {
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("utilDb/database[@name='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "不存在name属性值为 '" + strName + "' 的<utilDb/database>的元素";
                        return 0;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许修改实用库定义";
                        return -1;
                    }

                    string strOldUtilDbName = strName;

                    // 来自strDatabaseInfo
                    string strNewUtilDbName = DomUtil.GetAttr(nodeNewDatabase,
                        "name");

                    if (strOldUtilDbName != strNewUtilDbName)
                    {
                        if (String.IsNullOrEmpty(strOldUtilDbName) == true
                            && String.IsNullOrEmpty(strNewUtilDbName) == false)
                        {
                            strError = "要创建实用库 '" + strNewUtilDbName + "'，请使用create功能，而不能使用change功能";
                            goto ERROR1;
                        }

                        if (String.IsNullOrEmpty(strOldUtilDbName) == false
                            && String.IsNullOrEmpty(strNewUtilDbName) == true)
                        {
                            strError = "要删除实用库 '" + strNewUtilDbName + "'，请使用delete功能，而不能使用change功能";
                            goto ERROR1;
                        }

                        nRet = ChangeDbName(
                            channel,
                            strOldUtilDbName,
                            strNewUtilDbName,
                                () =>
                                {
                                    DomUtil.SetAttr(nodeDatabase, "name", strNewUtilDbName);
                                    this.Changed = true;
                                },
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

#if NO
                        this.Changed = true;
                        DomUtil.SetAttr(nodeDatabase, "name", strNewUtilDbName);
#endif
                    }

                    string strOldType = DomUtil.GetAttr(nodeDatabase, "type");
                    string strNewType = DomUtil.GetAttr(nodeNewDatabase, "type");

                    if (strOldType != strNewType)
                    {
                        DomUtil.SetAttr(nodeDatabase, "type", strNewType);
                        this.Changed = true;
                        // TODO: 类型修改后，是否要应用新的模板来修改数据库定义？这是个大问题
                    }

                    continue;
                }

                strError = "数据库名 '" + strName + "' 不属于 dp2library 目前管辖的范围...";
                return 0;
            }

            if (this.Changed == true)
                this.ActivateManagerThread();

            if (bDbNameChanged == true)
            {
                nRet = InitialKdbs(
                    Channels,
                    out strError);
                if (nRet == -1)
                    return -1;
                // 重新初始化虚拟库定义
                this.vdbs = null;
                nRet = this.InitialVdbs(Channels,
                    out strError);
                if (nRet == -1)
                    return -1;
            }

            return 1;
        ERROR1:
            // 2015/1/29
            if (this.Changed == true)
                this.ActivateManagerThread();

            // 2015/1/29
            if (bDbNameChanged == true)
            {
                {
                    string strError1 = "";
                    nRet = InitialKdbs(
                        Channels,
                        out strError1);
                    if (nRet == -1)
                        strError += "; 在收尾的时候进行 InitialKdbs() 调用又出错：" + strError1;
                }

                {
                    string strError1 = "";
                    // 重新初始化虚拟库定义
                    this.vdbs = null;
                    nRet = this.InitialVdbs(Channels,
                        out strError1);
                    if (nRet == -1)
                        strError += "; 在收尾的时候进行 InitialVdbs() 调用又出错：" + strError1;
                }
            }
            return -1;
        }

        // 检查一个单独的图书馆代码是否格式正确
        // 要求不能为 '*'，不能包含逗号
        // return:
        //      -1  校验函数本身出错了
        //      0   校验正确
        //      1   校验发现问题。strError中有描述
        public static int VerifySingleLibraryCode(string strText,
            out string strError)
        {
            strError = "";

            if (string.IsNullOrEmpty(strText) == true)
                return 0;

            strText = strText.Trim();
            if (strText == "*")
            {
                strError = "单个图书馆代码不允许使用通配符";
                return 1;
            }

            if (strText.IndexOf(",") != -1)
            {
                strError = "单个图书馆代码中不允许出现逗号";
                return 1;
            }

            return 0;
        }

        // 删除一个数据库在OPAC可检索库中的定义
        // return:
        //      -1  error
        //      0   not change
        //      1   changed
        int RemoveOpacDatabaseDef(
            RmsChannelCollection Channels,
            string strDatabaseName,
            out string strError)
        {
            strError = "";

            bool bChanged = false;

            XmlNode root = this.LibraryCfgDom.DocumentElement.SelectSingleNode("virtualDatabases");
            if (root != null)
            {
                // 虚拟成员库定义
                XmlNodeList nodes = root.SelectNodes("virtualDatabase/database[@name='" + strDatabaseName + "']");
                for (int i = 0; i < nodes.Count; i++)
                {
                    XmlNode node = nodes[i];

                    node.ParentNode.RemoveChild(node);
                    bChanged = true;
                }

                // 普通成员库定义
                nodes = root.SelectNodes("database[@name='" + strDatabaseName + "']");
                for (int i = 0; i < nodes.Count; i++)
                {
                    XmlNode node = nodes[i];

                    node.ParentNode.RemoveChild(node);
                    bChanged = true;
                }

                // 重新初始化虚拟库定义
                string strWarning = "";
                this.vdbs = null;
                int nRet = this.InitialVdbs(Channels,
                    out strWarning,
                    out strError);
                if (nRet == -1)
                    return -1;

            }

            root = this.LibraryCfgDom.DocumentElement.SelectSingleNode("browseformats");
            if (root != null)
            {
                // 显示定义中的库名
                XmlNodeList nodes = root.SelectNodes("database[@name='" + strDatabaseName + "']");
                for (int i = 0; i < nodes.Count; i++)
                {
                    XmlNode node = nodes[i];

                    node.ParentNode.RemoveChild(node);
                    bChanged = true;
                }
            }

            if (bChanged == true)
                return 1;

            return 0;
        }

        // 修改一个数据库在OPAC可检索库中的定义的名字
        // return:
        //      -1  error
        //      0   not change
        //      1   changed
        int RenameOpacDatabaseDef(
            RmsChannelCollection Channels,
            string strOldDatabaseName,
            string strNewDatabaseName,
            out string strError)
        {
            strError = "";

            bool bChanged = false;

            XmlNode root = this.LibraryCfgDom.DocumentElement.SelectSingleNode("virtualDatabases");
            if (root != null)
            {
                // TODO: 是否需要检查一下新名字是否已经存在了?

                // 虚拟成员库定义
                XmlNodeList nodes = root.SelectNodes("virtualDatabase/database[@name='" + strOldDatabaseName + "']");
                for (int i = 0; i < nodes.Count; i++)
                {
                    XmlNode node = nodes[i];

                    DomUtil.SetAttr(node, "name", strNewDatabaseName);
                    bChanged = true;
                }

                // 普通成员库定义
                nodes = root.SelectNodes("database[@name='" + strOldDatabaseName + "']");
                for (int i = 0; i < nodes.Count; i++)
                {
                    XmlNode node = nodes[i];

                    DomUtil.SetAttr(node, "name", strNewDatabaseName);
                    bChanged = true;
                }

                // 重新初始化虚拟库定义
                this.vdbs = null;   // 强制初始化
                int nRet = this.InitialVdbs(Channels,
                    out strError);
                if (nRet == -1)
                    return -1;

            }

            root = this.LibraryCfgDom.DocumentElement.SelectSingleNode("browseformats");
            if (root != null)
            {
                // 显示定义中的库名
                XmlNodeList nodes = root.SelectNodes("database[@name='" + strOldDatabaseName + "']");
                for (int i = 0; i < nodes.Count; i++)
                {
                    XmlNode node = nodes[i];

                    DomUtil.SetAttr(node, "name", strNewDatabaseName);
                    bChanged = true;
                }
            }

            if (bChanged == true)
                return 1;

            return 0;
        }

        public int BackupDatabaseDefinition(
            RmsChannel channel,
            string strDatabaseNames,
            string strLogFileName,
            out string strError)
        {
            strError = "";

            int nRet = 0;

            if (string.IsNullOrEmpty(strLogFileName))
            {
                strError = "strLogFileName 参数不应为空";
                return -1;
            }

            string[] names = strDatabaseNames.Split(new char[] { ',' });
            for (int i = 0; i < names.Length; i++)
            {
                string strName = names[i].Trim();
                if (String.IsNullOrEmpty(strName) == true)
                    continue;

                // 获得一个数据库的全部配置文件
                {
                    nRet = GetConfigFiles(channel,
                        strName,
                        strLogFileName,
                        out strError);
                    if (nRet == -1)
                        return -1;
                }
            }

            return 0;
        }

        // 删除数据库
        // return:
        //      -1  出错
        //      0   没有找到
        //      1   成功
        int DeleteDatabase(
            SessionInfo sessioninfo,
            RmsChannelCollection Channels,
            string strLibraryCodeList,
            string strDatabaseNames,
            out string strOutputInfo,
            out string strError)
        {
            strOutputInfo = "";
            strError = "";

            int nRet = 0;

            string strLogFileName = this.GetTempFileName("zip");

            bool bDbNameChanged = false;

            RmsChannel channel = Channels.GetChannel(this.WsUrl);

            string[] names = strDatabaseNames.Split(new char[] { ',' });
            for (int i = 0; i < names.Length; i++)
            {
                string strName = names[i].Trim();
                if (String.IsNullOrEmpty(strName) == true)
                    continue;

                // 书目库整体删除，也是可以的
                // TODO: 将来可以考虑单独删除书目库而不删除组内相关库
                if (this.IsBiblioDbName(strName) == true)
                {
                    // 获得相关配置小节
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@biblioDbName='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "配置 DOM 中名字为 '" + strName + "' 的书目库(biblioDbName属性)相关<database>元素没有找到";
                        return 0;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许删除书目库";
                        return -1;
                    }

                    // 删除书目库
                    nRet = DeleteDatabase(channel, strName, strLogFileName,
out strError);
                    if (nRet == -1)
                    {
                        strError = "删除书目库 '" + strName + "' 时发生错误: " + strError;
                        return -1;
                    }

                    bDbNameChanged = true;

                    // 删除实体库
                    string strEntityDbName = DomUtil.GetAttr(nodeDatabase, "name");
                    if (String.IsNullOrEmpty(strEntityDbName) == false)
                    {
                        nRet = DeleteDatabase(channel,
                            strEntityDbName,
                            strLogFileName,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = "删除书目库 '" + strName + "' 所从属的实体库 '" + strEntityDbName + "' 时发生错误: " + strError;
                            return -1;
                        }
                    }

                    // 删除订购库
                    string strOrderDbName = DomUtil.GetAttr(nodeDatabase, "orderDbName");
                    if (String.IsNullOrEmpty(strOrderDbName) == false)
                    {
                        nRet = DeleteDatabase(channel, strOrderDbName, strLogFileName,
out strError);
                        if (nRet == -1)
                        {
                            strError = "删除书目库 '" + strName + "' 所从属的订购库 '" + strOrderDbName + "' 时发生错误: " + strError;
                            return -1;
                        }
                    }

                    // 删除期库
                    string strIssueDbName = DomUtil.GetAttr(nodeDatabase, "issueDbName");
                    if (String.IsNullOrEmpty(strIssueDbName) == false)
                    {
                        nRet = DeleteDatabase(channel, strIssueDbName, strLogFileName,
out strError);
                        if (nRet == -1)
                        {
                            strError = "删除书目库 '" + strName + "' 所从属的期库 '" + strIssueDbName + "' 时发生错误: " + strError;
                            return -1;
                        }
                    }

                    // 删除评注库
                    string strCommentDbName = DomUtil.GetAttr(nodeDatabase, "commentDbName");
                    if (String.IsNullOrEmpty(strCommentDbName) == false)
                    {
                        nRet = DeleteDatabase(channel, strCommentDbName, strLogFileName,
out strError);
                        if (nRet == -1)
                        {
                            strError = "删除书目库 '" + strName + "' 所从属的评注库 '" + strCommentDbName + "' 时发生错误: " + strError;
                            return -1;
                        }
                    }

                    nodeDatabase.ParentNode.RemoveChild(nodeDatabase);

                    // <itemdbgroup>内容更新，刷新配套的内存结构
                    nRet = this.LoadItemDbGroupParam(this.LibraryCfgDom,
                        out strError);
                    if (nRet == -1)
                    {
                        this.WriteErrorLog(strError);
                        return -1;
                    }

                    this.Changed = true;
                    // this.ActivateManagerThread();
                    continue;
                }

                // 单独删除实体库
                if (this.IsItemDbName(strName) == true)
                {
                    // 获得相关配置小节
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@name='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "配置 DOM 中名字为 '" + strName + "' 的实体库(name属性)相关<database>元素没有找到";
                        return 0;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许删除实体库";
                        return -1;
                    }

                    // 删除实体库
                    nRet = DeleteDatabase(channel, strName, strLogFileName,
out strError);
                    if (nRet == -1)
                    {
                        strError = "删除实体库 '" + strName + "' 时发生错误: " + strError;
                        return -1;
                    }

                    bDbNameChanged = true;

                    DomUtil.SetAttr(nodeDatabase, "name", null);

                    // <itemdbgroup>内容更新，刷新配套的内存结构
                    nRet = this.LoadItemDbGroupParam(this.LibraryCfgDom,
                        out strError);
                    if (nRet == -1)
                    {
                        this.WriteErrorLog(strError);
                        return -1;
                    }

                    this.Changed = true;
                    // this.ActivateManagerThread();
                    continue;
                }

                // 单独删除订购库
                if (this.IsOrderDbName(strName) == true)
                {
                    // 获得相关配置小节
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@orderDbName='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "配置DOM中名字为 '" + strName + "' 的订购库(orderDbName属性)相关<database>元素没有找到";
                        return 0;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许删除订购库";
                        return -1;
                    }

                    // 删除订购库
                    nRet = DeleteDatabase(channel, strName, strLogFileName,
out strError);
                    if (nRet == -1)
                    {
                        strError = "删除订购库 '" + strName + "' 时发生错误: " + strError;
                        return -1;
                    }

                    bDbNameChanged = true;

                    DomUtil.SetAttr(nodeDatabase, "orderDbName", null);

                    // <itemdbgroup>内容更新，刷新配套的内存结构
                    nRet = this.LoadItemDbGroupParam(this.LibraryCfgDom,
                        out strError);
                    if (nRet == -1)
                    {
                        this.WriteErrorLog(strError);
                        return -1;
                    }

                    this.Changed = true;
                    // this.ActivateManagerThread();
                    continue;
                }

                // 单独删除期库
                if (this.IsIssueDbName(strName) == true)
                {
                    // 获得相关配置小节
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@issueDbName='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "配置 DOM 中名字为 '" + strName + "' 的期库(issueDbName属性)相关<database>元素没有找到";
                        return 0;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许删除期库";
                        return -1;
                    }

                    // 删除期库
                    nRet = DeleteDatabase(channel, strName, strLogFileName,
out strError);
                    if (nRet == -1)
                    {
                        strError = "删除期库 '" + strName + "' 时发生错误: " + strError;
                        return -1;
                    }

                    bDbNameChanged = true;

                    DomUtil.SetAttr(nodeDatabase, "issueDbName", null);

                    // <itemdbgroup>内容更新，刷新配套的内存结构
                    nRet = this.LoadItemDbGroupParam(this.LibraryCfgDom,
                        out strError);
                    if (nRet == -1)
                    {
                        this.WriteErrorLog(strError);
                        return -1;
                    }

                    this.Changed = true;
                    // this.ActivateManagerThread();
                    continue;
                }

                // 单独删除评注库
                if (this.IsCommentDbName(strName) == true)
                {
                    // 获得相关配置小节
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@commentDbName='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "配置DOM中名字为 '" + strName + "' 的评注库(commentDbName属性)相关<database>元素没有找到";
                        return 0;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许删除评注库";
                        return 0;
                    }

                    nRet = DeleteDatabase(channel, strName, strLogFileName,
out strError);
                    if (nRet == -1)
                    {
                        strError = "删除评注库 '" + strName + "' 时发生错误: " + strError;
                        return -1;
                    }

                    bDbNameChanged = true;

                    DomUtil.SetAttr(nodeDatabase, "commentDbName", null);

                    // <itemdbgroup>内容更新，刷新配套的内存结构
                    nRet = this.LoadItemDbGroupParam(this.LibraryCfgDom,
                        out strError);
                    if (nRet == -1)
                    {
                        this.WriteErrorLog(strError);
                        return -1;
                    }

                    this.Changed = true;
                    // this.ActivateManagerThread();
                    continue;
                }

                // 删除读者库
                if (this.IsReaderDbName(strName) == true)
                {
                    // 获得相关配置小节
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("readerdbgroup/database[@name='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "配置 DOM 中名字为 '" + strName + "' 的读者库(name属性)相关<database>元素没有找到";
                        return 0;
                    }

                    // 2012/9/9
                    // 分馆用户只允许删除属于管辖分馆的读者库
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        string strExistLibraryCode = DomUtil.GetAttr(nodeDatabase, "libraryCode");

                        if (string.IsNullOrEmpty(strExistLibraryCode) == true
                            || StringUtil.IsInList(strExistLibraryCode, strLibraryCodeList) == false)
                        {
                            strError = "删除读者库 '" + strName + "' 被拒绝。当前用户只能删除图书馆代码完全完全属于 '" + strLibraryCodeList + "' 范围的读者库";
                            return -1;
                        }
                    }

                    // 删除读者库
                    nRet = DeleteDatabase(channel, strName, strLogFileName,
out strError);
                    if (nRet == -1)
                    {
                        strError = "删除读者库 '" + strName + "' 时发生错误: " + strError;
                        return -1;
                    }

                    bDbNameChanged = true;

                    nodeDatabase.ParentNode.RemoveChild(nodeDatabase);

                    // <readerdbgroup>内容更新，刷新配套的内存结构
                    this.LoadReaderDbGroupParam(this.LibraryCfgDom);

                    this.Changed = true;
                    // this.ActivateManagerThread();
                    continue;
                }

                // 删除预约到书库
                if (this.ArrivedDbName == strName)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许删除预约到书库";
                        return -1;
                    }

                    // 删除预约到书库
                    nRet = DeleteDatabase(channel, strName, strLogFileName,
out strError);
                    if (nRet == -1)
                    {
                        strError = "删除预约到书库 '" + strName + "' 时发生错误: " + strError;
                        return -1;
                    }

                    this.ArrivedDbName = "";

                    this.Changed = true;
                    // this.ActivateManagerThread();
                    continue;
                }

                // 删除违约金库
                if (this.AmerceDbName == strName)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许删除违约金库";
                        return -1;
                    }

                    // 删除违约金库
                    nRet = DeleteDatabase(channel, strName, strLogFileName,
out strError);
                    if (nRet == -1)
                    {
                        strError = "删除违约金库 '" + strName + "' 时发生错误: " + strError;
                        return -1;
                    }

                    this.AmerceDbName = "";

                    this.Changed = true;
                    // this.ActivateManagerThread();
                    continue;
                }

                // 删除发票库
                if (this.InvoiceDbName == strName)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许删除发票库";
                        return -1;
                    }

                    nRet = DeleteDatabase(channel, strName, strLogFileName,
out strError);
                    if (nRet == -1)
                    {
                        strError = "删除发票库 '" + strName + "' 时发生错误: " + strError;
                        return -1;
                    }

                    this.InvoiceDbName = "";

                    this.Changed = true;
                    // this.ActivateManagerThread();
                    continue;
                }

                if (this.PinyinDbName == strName)
                {
                    string strTypeCaption = "";
                    string strDbName = "";

                    strTypeCaption = "拼音";
                    strDbName = this.PinyinDbName;

                    nRet = DeleteDatabase(
                        channel,
                        strTypeCaption,
                        strName,
                        strLibraryCodeList,
                        strLogFileName,
                        ref strDbName,
                        out strError);
                    this.PinyinDbName = strDbName;
                    if (nRet == -1)
                        return -1;

                    this.Changed = true;
                    // this.ActivateManagerThread();
                    continue;
                }

                if (this.GcatDbName == strName)
                {
                    string strTypeCaption = "";
                    string strDbName = "";

                    strTypeCaption = "著者号码";
                    strDbName = this.GcatDbName;

                    nRet = DeleteDatabase(
                        channel,
                        strTypeCaption,
                        strName,
                        strLibraryCodeList,
                        strLogFileName,
                        ref strDbName,
                        out strError);
                    this.GcatDbName = strDbName;
                    if (nRet == -1)
                        return -1;

                    this.Changed = true;
                    // this.ActivateManagerThread();
                    continue;
                }

                if (this.WordDbName == strName)
                {
                    string strTypeCaption = "";
                    string strDbName = "";

                    strTypeCaption = "词";
                    strDbName = this.WordDbName;

                    nRet = DeleteDatabase(
                        channel,
                        strTypeCaption,
                        strName,
                        strLibraryCodeList,
                        strLogFileName,
                        ref strDbName,
                        out strError);
                    this.WordDbName = strDbName;
                    if (nRet == -1)
                        return -1;

                    this.Changed = true;
                    // this.ActivateManagerThread();
                    continue;
                }

                // 删除消息库
                if (this.MessageDbName == strName)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许删除消息库";
                        return -1;
                    }

                    // 删除消息库
                    nRet = DeleteDatabase(channel, strName, strLogFileName,
out strError);
                    if (nRet == -1)
                    {
                        strError = "删除消息库 '" + strName + "' 时发生错误: " + strError;
                        return -1;
                    }

                    this.MessageDbName = "";

                    this.Changed = true;
                    // this.ActivateManagerThread();
                    continue;
                }

                // 单独删除实用库
                if (IsUtilDbName(strName) == true)
                {
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("utilDb/database[@name='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "不存在name属性值为 '" + strName + "' 的<utilDb/database>的元素";
                        return 0;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许删除实用库";
                        return -1;
                    }

                    // 删除实用库
                    nRet = DeleteDatabase(channel, strName, strLogFileName,
out strError);
                    if (nRet == -1)
                    {
                        strError = "删除实用库 '" + strName + "' 时发生错误: " + strError;
                        return -1;
                    }

                    nodeDatabase.ParentNode.RemoveChild(nodeDatabase);

                    this.Changed = true;
                    // this.ActivateManagerThread();
                    continue;
                }

                strError = "数据库名 '" + strName + "' 不属于 dp2library 目前管辖的范围...";
                return 0;
            }

            // 写入操作日志
            {
                XmlDocument domOperLog = new XmlDocument();
                domOperLog.LoadXml("<root />");

                DomUtil.SetElementText(domOperLog.DocumentElement,
                    "operation",
                    "manageDatabase");
                DomUtil.SetElementText(domOperLog.DocumentElement,
    "action",
    "deleteDatabase");

                XmlNode new_node = DomUtil.SetElementText(domOperLog.DocumentElement, "databases",
"");
                foreach (string name in names)
                {
                    XmlElement database = domOperLog.CreateElement("database");
                    new_node.AppendChild(database);
                    database.SetAttribute("name", name);
                }

                DomUtil.SetElementText(domOperLog.DocumentElement, "operator",
                    sessioninfo.UserID);

                string strOperTime = this.Clock.GetClock();

                DomUtil.SetElementText(domOperLog.DocumentElement, "operTime",
                    strOperTime);

                if (File.Exists(strLogFileName))
                {

                    using (Stream stream = File.OpenRead(strLogFileName))
                    {
                        nRet = this.OperLog.WriteOperLog(domOperLog,
                            sessioninfo.ClientAddress,
                            stream,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = "ManageDatabase() API deleteDatabase 写入日志时发生错误: " + strError;
                            return -1;
                        }
                    }
                }
                else
                {
                    nRet = this.OperLog.WriteOperLog(domOperLog,
    sessioninfo.ClientAddress,
    out strError);
                }
                if (nRet == -1)
                {
                    strError = "ManageDatabase() API deleteDatabase 写入日志时发生错误: " + strError;
                    return -1;
                }
            }

            // 2017/6/7
            if (this.Changed == true)
                this.ActivateManagerThread();

            if (bDbNameChanged == true)
            {
                nRet = InitialKdbs(
                    Channels,
                    out strError);
                if (nRet == -1)
                    return -1;
                // 重新初始化虚拟库定义
                this.vdbs = null;
                nRet = this.InitialVdbs(Channels,
                    out strError);
                if (nRet == -1)
                    return -1;
            }

            return 1;
        }

        int DeleteDatabase(
            RmsChannel channel,
            string strTypeCaption,
            string strName,
            string strLibraryCodeList,
            string strLogFileName,
            ref string strDbName,
            out string strError)
        {
            strError = "";

            if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
            {
                strError = "当前用户不是全局用户，不允许删除" + strTypeCaption + "库";
                return -1;
            }

            // TODO: 关注数据库不存在时返回什么值
            int nRet = DeleteDatabase(channel,
                strName,
                strLogFileName,
                out strError);
            if (nRet == -1)
            {
                strError = "删除" + strTypeCaption + "库 '" + strName + "' 时发生错误: " + strError;
                return -1;
            }

            strDbName = "";
            return 0;
        }


        // 刷新数据库定义
        // parameters:
        //      strDatabaseInfo 要刷新的下属文件特性。<refreshStyle include="keys,browse" exclude="">(表示只刷新keys和browse两个重要配置文件)或者<refreshStyle include="*" exclude="template">(表示刷新全部文件，但是不要刷新template) 如果参数值为空，表示全部刷新
        //      strOutputInfo   返回keys定义发生改变的数据库名。"<keysChanged dbpaths='http://localhost:8001/dp2kernel?dbname1;http://localhost:8001/dp2kernel?dbname2'/>"
        // return:
        //      -1  出错
        //      0   没有找到
        //      1   成功
        int RefreshDatabaseDefs(
            SessionInfo sessioninfo,
            RmsChannelCollection Channels,
            string strLibraryCodeList,
            string strDatabaseNames,
            string strDatabaseInfo,
            out string strOutputInfo,
            out string strError)
        {
            strOutputInfo = "";
            strError = "";

            int nRet = 0;
            // long lRet = 0;

            string strLogFileName = this.GetTempFileName("zip");

            string strInclude = "";
            string strExclude = "";

            bool bAutoRebuildKeys = false;  // 2014/11/26
            bool bRecoverModeKeys = false;  // 2015/9/28

            if (String.IsNullOrEmpty(strDatabaseInfo) == false)
            {
                XmlDocument style_dom = new XmlDocument();
                try
                {
                    style_dom.LoadXml(strDatabaseInfo);
                }
                catch (Exception ex)
                {
                    strError = "参数 strDatabaseInfo 的值装入 XMLDOM 时出错: " + ex.Message;
                    return -1;
                }
                XmlNode style_node = style_dom.DocumentElement.SelectSingleNode("//refreshStyle");
                if (style_node != null)
                {
                    strInclude = DomUtil.GetAttr(style_node, "include");
                    strExclude = DomUtil.GetAttr(style_node, "exclude");
                    bAutoRebuildKeys = DomUtil.GetBooleanParam(style_node, "autoRebuildKeys", false);
                    bRecoverModeKeys = DomUtil.GetBooleanParam(style_node, "recoverModeKeys", false);
                }
            }

            if (String.IsNullOrEmpty(strInclude) == true)
                strInclude = "*";   // 表示全部

            // bool bKeysDefChanged = false;    // 刷新后，keys配置可能被改变
            List<string> keyschanged_dbnames = new List<string>();  // keys定义发生了改变的数据库名
            List<string> dbnames = new List<string>();  // 已经完成的数据库名集合
            List<string> other_dbnames = new List<string>();    // 其他类型的数据库名集合
            List<string> other_types = new List<string>();  // 和 other_dbnames 锁定对应的数据库类型集合

            RmsChannel channel = Channels.GetChannel(this.WsUrl);

            string[] names = strDatabaseNames.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < names.Length; i++)
            {
                string strName = names[i].Trim();
                if (String.IsNullOrEmpty(strName) == true)
                    continue;

                // 书目库整体刷新，也是可以的
                if (this.IsBiblioDbName(strName) == true)
                {
                    // 获得相关配置小节
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@biblioDbName='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "配置DOM中名字为 '" + strName + "' 的书目库(biblioDbName属性)相关<database>元素没有找到";
                        return 0;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许刷新书目库的定义";
                        goto ERROR1;
                    }

                    string strSyntax = DomUtil.GetAttr(nodeDatabase, "syntax");
                    if (String.IsNullOrEmpty(strSyntax) == true)
                        strSyntax = "unimarc";

                    string strUsage = "";
                    string strIssueDbName = DomUtil.GetAttr(nodeDatabase, "issueDbName");
                    if (String.IsNullOrEmpty(strIssueDbName) == true)
                        strUsage = "book";
                    else
                        strUsage = "series";

                    // 刷新书目库
                    string strTemplateDir = this.DataDir + "\\templates\\" + "biblio_" + strSyntax + "_" + strUsage;

                    nRet = RefreshDatabase(channel,
                        strTemplateDir,
                        strName,
                        strInclude,
                        strExclude,
                        bRecoverModeKeys,
                        strLogFileName,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "刷新小书目库 '" + strName + "' 定义时发生错误: " + strError;
                        goto ERROR1;
                    }
                    if (nRet == 1)
                        keyschanged_dbnames.Add(strName);

                    dbnames.Add(strName);

                    // 刷新实体库
                    string strEntityDbName = DomUtil.GetAttr(nodeDatabase, "name");
                    if (String.IsNullOrEmpty(strEntityDbName) == false)
                    {
                        strTemplateDir = this.DataDir + "\\templates\\" + "item";

                        nRet = RefreshDatabase(channel,
                            strTemplateDir,
                            strEntityDbName,
                            strInclude,
                            strExclude,
                        bRecoverModeKeys,
                        strLogFileName,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = "刷新书目库 '" + strName + "' 所从属的实体库 '" + strEntityDbName + "' 定义时发生错误: " + strError;
                            goto ERROR1;
                        }
                        if (nRet == 1)
                            keyschanged_dbnames.Add(strEntityDbName);
                        dbnames.Add(strEntityDbName);
                    }

                    // 刷新订购库
                    string strOrderDbName = DomUtil.GetAttr(nodeDatabase, "orderDbName");
                    if (String.IsNullOrEmpty(strOrderDbName) == false)
                    {
                        strTemplateDir = this.DataDir + "\\templates\\" + "order";

                        nRet = RefreshDatabase(channel,
                            strTemplateDir,
                            strOrderDbName,
                            strInclude,
                            strExclude,
                        bRecoverModeKeys,
                        strLogFileName,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = "刷新书目库 '" + strName + "' 所从属的订购库 '" + strOrderDbName + "' 定义时发生错误: " + strError;
                            goto ERROR1;
                        }
                        if (nRet == 1)
                            keyschanged_dbnames.Add(strOrderDbName);
                        dbnames.Add(strOrderDbName);
                    }

                    // 刷新期库
                    if (String.IsNullOrEmpty(strIssueDbName) == false)
                    {
                        strTemplateDir = this.DataDir + "\\templates\\" + "issue";

                        nRet = RefreshDatabase(channel,
                            strTemplateDir,
                            strIssueDbName,
                            strInclude,
                            strExclude,
                        bRecoverModeKeys,
                        strLogFileName,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = "刷新书目库 '" + strName + "' 所从属的期库 '" + strIssueDbName + "' 定义时发生错误: " + strError;
                            goto ERROR1;
                        }
                        if (nRet == 1)
                            keyschanged_dbnames.Add(strIssueDbName);
                        dbnames.Add(strIssueDbName);
                    }

                    // 刷新评注库
                    string strCommentDbName = DomUtil.GetAttr(nodeDatabase, "commentDbName");
                    if (String.IsNullOrEmpty(strCommentDbName) == false)
                    {
                        strTemplateDir = this.DataDir + "\\templates\\" + "comment";

                        nRet = RefreshDatabase(channel,
                            strTemplateDir,
                            strCommentDbName,
                            strInclude,
                            strExclude,
                        bRecoverModeKeys,
                        strLogFileName,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = "刷新书目库 '" + strName + "' 所从属的评注库 '" + strCommentDbName + "' 定义时发生错误: " + strError;
                            goto ERROR1;
                        }
                        if (nRet == 1)
                            keyschanged_dbnames.Add(strCommentDbName);
                        dbnames.Add(strCommentDbName);
                    }

                    continue;
                }

                // 单独刷新实体库
                if (this.IsItemDbName(strName) == true)
                {
                    // 获得相关配置小节
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@name='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "配置DOM中名字为 '" + strName + "' 的实体库(name属性)相关<database>元素没有找到";
                        return 0;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许刷新实体库的定义";
                        goto ERROR1;
                    }

                    // 刷新实体库
                    string strTemplateDir = this.DataDir + "\\templates\\" + "item";

                    nRet = RefreshDatabase(channel,
                        strTemplateDir,
                        strName,
                        strInclude,
                        strExclude,
                        bRecoverModeKeys,
                        strLogFileName,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "刷新实体库 '" + strName + "' 定义时发生错误: " + strError;
                        goto ERROR1;
                    }
                    if (nRet == 1)
                        keyschanged_dbnames.Add(strName);

                    dbnames.Add(strName);
                    continue;
                }

                // 单独刷新订购库
                if (this.IsOrderDbName(strName) == true)
                {
                    // 获得相关配置小节
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@orderDbName='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "配置DOM中名字为 '" + strName + "' 的订购库(orderDbName属性)相关<database>元素没有找到";
                        return 0;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许刷新订购库的定义";
                        goto ERROR1;
                    }

                    // 刷新订购库
                    string strTemplateDir = this.DataDir + "\\templates\\" + "order";
                    nRet = RefreshDatabase(channel,
                        strTemplateDir,
                        strName,
                        strInclude,
                        strExclude,
                        bRecoverModeKeys,
                        strLogFileName,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "刷新订购库 '" + strName + "' 定义时发生错误: " + strError;
                        goto ERROR1;
                    }
                    if (nRet == 1)
                        keyschanged_dbnames.Add(strName);

                    dbnames.Add(strName);
                    continue;
                }

                // 单独刷新期库
                if (this.IsIssueDbName(strName) == true)
                {
                    // 获得相关配置小节
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@issueDbName='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "配置DOM中名字为 '" + strName + "' 的期库(issueDbName属性)相关<database>元素没有找到";
                        return 0;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许刷新期库的定义";
                        goto ERROR1;
                    }

                    // 刷新期库
                    string strTemplateDir = this.DataDir + "\\templates\\" + "issue";

                    nRet = RefreshDatabase(channel,
                        strTemplateDir,
                        strName,
                        strInclude,
                        strExclude,
                        bRecoverModeKeys,
                        strLogFileName,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "刷新期库 '" + strName + "' 定义时发生错误: " + strError;
                        goto ERROR1;
                    }
                    if (nRet == 1)
                        keyschanged_dbnames.Add(strName);

                    dbnames.Add(strName);
                    continue;
                }

                // 单独刷新评注库
                if (this.IsCommentDbName(strName) == true)
                {
                    // 获得相关配置小节
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@commentDbName='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "配置DOM中名字为 '" + strName + "' 的评注库(commentDbName属性)相关<database>元素没有找到";
                        return 0;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许刷新评注库的定义";
                        goto ERROR1;
                    }

                    // 刷新评注库
                    string strTemplateDir = this.DataDir + "\\templates\\" + "comment";

                    nRet = RefreshDatabase(channel,
                        strTemplateDir,
                        strName,
                        strInclude,
                        strExclude,
                        bRecoverModeKeys,
                        strLogFileName,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "刷新评注库 '" + strName + "' 定义时发生错误: " + strError;
                        goto ERROR1;
                    }
                    if (nRet == 1)
                        keyschanged_dbnames.Add(strName);

                    dbnames.Add(strName);
                    continue;
                }

                // 刷新读者库
                if (this.IsReaderDbName(strName) == true)
                {
                    // 获得相关配置小节
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("readerdbgroup/database[@name='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "配置DOM中名字为 '" + strName + "' 的读者库(name属性)相关<database>元素没有找到";
                        return 0;
                    }

                    // 2012/9/9
                    // 分馆用户只允许刷新属于管辖分馆的读者库
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        string strExistLibraryCode = DomUtil.GetAttr(nodeDatabase, "libraryCode");

                        if (string.IsNullOrEmpty(strExistLibraryCode) == true
                            || StringUtil.IsInList(strExistLibraryCode, strLibraryCodeList) == false)
                        {
                            strError = "刷新读者库 '" + strName + "' 定义被拒绝。当前用户只能刷新图书馆代码完全完全属于 '" + strLibraryCodeList + "' 范围的读者库定义";
                            goto ERROR1;
                        }
                    }

                    // 刷新读者库
                    string strTemplateDir = this.DataDir + "\\templates\\" + "reader";

                    nRet = RefreshDatabase(channel,
                        strTemplateDir,
                        strName,
                        strInclude,
                        strExclude,
                        bRecoverModeKeys,
                        strLogFileName,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "刷新读者库 '" + strName + "' 定义时发生错误: " + strError;
                        goto ERROR1;
                    }
                    if (nRet == 1)
                        keyschanged_dbnames.Add(strName);

                    dbnames.Add(strName);
                    continue;
                }

                // 刷新预约到书库
                if (this.ArrivedDbName == strName)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许刷新预约到书库的定义";
                        goto ERROR1;
                    }

                    // 刷新预约到书库
                    string strTemplateDir = this.DataDir + "\\templates\\" + "arrived";
                    nRet = RefreshDatabase(channel,
                        strTemplateDir,
                        strName,
                        strInclude,
                        strExclude,
                        bRecoverModeKeys,
                        strLogFileName,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "刷新预约到书库 '" + strName + "' 定义时发生错误: " + strError;
                        goto ERROR1;
                    }
                    if (nRet == 1)
                        keyschanged_dbnames.Add(strName);
                    dbnames.Add(strName);
                    continue;
                }

                // 刷新违约金库
                if (this.AmerceDbName == strName)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许刷新违约金库的定义";
                        goto ERROR1;
                    }

                    // 刷新违约金库
                    string strTemplateDir = this.DataDir + "\\templates\\" + "amerce";
                    nRet = RefreshDatabase(channel,
                        strTemplateDir,
                        strName,
                        strInclude,
                        strExclude,
                        bRecoverModeKeys,
                        strLogFileName,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "刷新违约金库 '" + strName + "' 定义时发生错误: " + strError;
                        goto ERROR1;
                    }
                    if (nRet == 1)
                        keyschanged_dbnames.Add(strName);

                    dbnames.Add(strName);
                    continue;
                }

                // 刷新发票库
                if (this.InvoiceDbName == strName)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许刷新发票库的定义";
                        goto ERROR1;
                    }

                    // 刷新发票库
                    string strTemplateDir = this.DataDir + "\\templates\\" + "invoice";
                    nRet = RefreshDatabase(channel,
                        strTemplateDir,
                        strName,
                        strInclude,
                        strExclude,
                        bRecoverModeKeys,
                        strLogFileName,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "刷新发票库 '" + strName + "' 定义时发生错误: " + strError;
                        goto ERROR1;
                    }
                    if (nRet == 1)
                        keyschanged_dbnames.Add(strName);

                    dbnames.Add(strName);
                    continue;
                }

                // 刷新消息库
                if (this.MessageDbName == strName)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许刷新消息库的定义";
                        goto ERROR1;
                    }

                    // 刷新消息库
                    string strTemplateDir = this.DataDir + "\\templates\\" + "message";
                    nRet = RefreshDatabase(channel,
                        strTemplateDir,
                        strName,
                        strInclude,
                        strExclude,
                        bRecoverModeKeys,
                        strLogFileName,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "刷新消息库 '" + strName + "' 定义时发生错误: " + strError;
                        goto ERROR1;
                    }
                    if (nRet == 1)
                        keyschanged_dbnames.Add(strName);

                    dbnames.Add(strName);
                    continue;
                }

                // 刷新拼音库
                if (this.PinyinDbName == strName
                    || this.GcatDbName == strName
                    || this.WordDbName == strName)
                {
                    string strTypeCaption = "";
                    string strType = "";
                    if (strName == this.PinyinDbName)
                    {
                        strType = "pinyin";
                        strTypeCaption = "拼音";
                    }
                    if (strName == this.GcatDbName)
                    {
                        strType = "gcat";
                        strTypeCaption = "著者号码";
                    }
                    if (strName == this.WordDbName)
                    {
                        strType = "word";
                        strTypeCaption = "词";
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许刷新" + strTypeCaption + "库的定义";
                        goto ERROR1;
                    }

                    // 刷新xxx库
                    string strTemplateDir = this.DataDir + "\\templates\\" + strType;
                    nRet = RefreshDatabase(channel,
                        strTemplateDir,
                        strName,
                        strInclude,
                        strExclude,
                        bRecoverModeKeys,
                        strLogFileName,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "刷新" + strTypeCaption + "库 '" + strName + "' 定义时发生错误: " + strError;
                        goto ERROR1;
                    }
                    if (nRet == 1)
                        keyschanged_dbnames.Add(strName);
                    dbnames.Add(strName);

                    continue;
                }

                // 刷新实用库
                if (IsUtilDbName(strName) == true)
                {
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("utilDb/database[@name='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "不存在name属性值为 '" + strName + "' 的<utilDb/database>的元素";
                        return 0;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许刷新实用库的定义";
                        goto ERROR1;
                    }

                    string strType = DomUtil.GetAttr(nodeDatabase, "type").ToLower();

                    // 刷新实用库
                    string strTemplateDir = this.DataDir + "\\templates\\" + strType;
                    nRet = RefreshDatabase(channel,
                        strTemplateDir,
                        strName,
                        strInclude,
                        strExclude,
                        bRecoverModeKeys,
                        strLogFileName,
                        out strError);
                    if (nRet == -1)
                    {
                        strError = "刷新实用库 '" + strName + "' 定义时发生错误: " + strError;
                        goto ERROR1;
                    }
                    if (nRet == 1)
                        keyschanged_dbnames.Add(strName);
                    dbnames.Add(strName);
                    continue;
                }

                // 刷新访问日志库
                if (this.IsAccessLogDbName(strName) == true)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许刷新" + AccessLogDbName + "库的定义";
                        goto ERROR1;
                    }

                    if (string.IsNullOrEmpty(this.MongoDbConnStr) == true)
                    {
                        strError = "当前尚未启用 MongoDB 功能";
                        return -1;
                    }

                    this.AccessLogDatabase.CreateIndex();
                    other_dbnames.Add(strName);
                    other_types.Add("accessLog");
                    continue;
                }

                // 刷新访问统计库
                if (this.IsHitCountDbName(strName) == true)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许刷新" + HitCountDbName + "库的定义";
                        goto ERROR1;
                    }

                    if (string.IsNullOrEmpty(this.MongoDbConnStr) == true)
                    {
                        strError = "当前尚未启用 MongoDB 功能";
                        return -1;
                    }

                    this.HitCountDatabase.CreateIndex();
                    other_dbnames.Add(strName);
                    other_types.Add("hitcount");
                    continue;
                }

                // 刷新出纳历史库
                if (this.IsChargingHistoryDbName(strName) == true)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许刷新" + ChargingHistoryDbName + "库的定义";
                        goto ERROR1;
                    }

                    if (string.IsNullOrEmpty(this.MongoDbConnStr) == true)
                    {
                        strError = "当前尚未启用 MongoDB 功能";
                        return -1;
                    }

                    this.ChargingOperDatabase.CreateIndex();
                    other_dbnames.Add(strName);
                    other_types.Add("chargingOper");
                    continue;
                }

                // 刷新书目摘要库
                if (this.IsBiblioSummaryDbName(strName) == true)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许刷新" + BiblioSummaryDbName + "库的定义";
                        goto ERROR1;
                    }

                    if (string.IsNullOrEmpty(this.MongoDbConnStr) == true)
                    {
                        strError = "当前尚未启用 MongoDB 功能";
                        return -1;
                    }

                    this.CreateBiblioSummaryIndex();
                    other_dbnames.Add(strName);
                    other_types.Add("biblioSummary");
                    continue;
                }

                strError = "数据库名 '" + strName + "' 不属于 dp2library 目前管辖的范围...";
                return 0;
            }

            // 写入操作日志
            {
                XmlDocument domOperLog = new XmlDocument();
                domOperLog.LoadXml("<root />");

                DomUtil.SetElementText(domOperLog.DocumentElement,
                    "operation",
                    "manageDatabase");
                DomUtil.SetElementText(domOperLog.DocumentElement,
    "action",
    "refreshDatabase");

                XmlNode new_node = DomUtil.SetElementText(domOperLog.DocumentElement, "databases",
"");
                foreach (string name in dbnames)
                {
                    XmlElement database = domOperLog.CreateElement("database");
                    new_node.AppendChild(database);
                    database.SetAttribute("name", name);
                }
                {
                    int i = 0;
                    foreach (string name in other_dbnames)
                    {
                        string type = other_types[i];
                        XmlElement database = domOperLog.CreateElement("database");
                        new_node.AppendChild(database);
                        database.SetAttribute("name", name);
                        database.SetAttribute("type", type);
                        i++;
                    }
                }

                DomUtil.SetElementText(domOperLog.DocumentElement, "operator",
                    sessioninfo.UserID);

                string strOperTime = this.Clock.GetClock();

                DomUtil.SetElementText(domOperLog.DocumentElement, "operTime",
                    strOperTime);

                if (File.Exists(strLogFileName))
                {
                    using (Stream stream = File.OpenRead(strLogFileName))
                    {
                        nRet = this.OperLog.WriteOperLog(domOperLog,
                            sessioninfo.ClientAddress,
                            stream,
                            out strError);
                    }
                }
                else
                {
                    nRet = this.OperLog.WriteOperLog(domOperLog,
                        sessioninfo.ClientAddress,
                        out strError);
                }
                if (nRet == -1)
                {
                    strError = "ManageDatabase() API refreshDatabase 写入日志时发生错误: " + strError;
                    return -1;
                }
            }

            // 2016/12/27
            // 清除缓存的各种配置文件
            this.ClearCacheCfgs("");

            // 2015/6/13
            if (keyschanged_dbnames.Count > 0)
            {
                nRet = InitialKdbs(
                    Channels,
                    out strError);
                if (nRet == -1)
                    return -1;
                // 重新初始化虚拟库定义
                this.vdbs = null;
                nRet = this.InitialVdbs(Channels,
                    out strError);
                if (nRet == -1)
                    return -1;
            }

            if (bAutoRebuildKeys == true
                && keyschanged_dbnames.Count > 0)
            {
                nRet = StartRebuildKeysTask(StringUtil.MakePathList(keyschanged_dbnames, ","),
            out strError);
                if (nRet == -1)
                    return -1;
            }

            {
                // 增加WebServiceUrl部分
                for (int i = 0; i < keyschanged_dbnames.Count; i++)
                {
                    keyschanged_dbnames[i] = this.WsUrl.ToLower() + "?" + keyschanged_dbnames[i];
                }

                XmlDocument dom = new XmlDocument();
                dom.LoadXml("<keysChanged />");
                DomUtil.SetAttr(dom.DocumentElement, "dbpaths", StringUtil.MakePathList(keyschanged_dbnames, ";"));
                strOutputInfo = dom.OuterXml;
            }

            return 1;
        ERROR1:
            if (keyschanged_dbnames.Count > 0)
            {
                // 增加WebServiceUrl部分
                for (int i = 0; i < keyschanged_dbnames.Count; i++)
                {
                    keyschanged_dbnames[i] = this.WsUrl.ToLower() + "?" + keyschanged_dbnames[i];
                }

                XmlDocument dom = new XmlDocument();
                dom.LoadXml("<keysChanged />");
                DomUtil.SetAttr(dom.DocumentElement, "dbpaths", StringUtil.MakePathList(keyschanged_dbnames, ";"));
                strOutputInfo = dom.OuterXml;
            }
            return -1;
        }

        // TODO: 如果当前任务正在运行, 需要把新的任务追加到末尾继续运行
        int StartRebuildKeysTask(string strDbNameList,
            out string strError)
        {
            strError = "";

            BatchTaskInfo info = null;

            // 参数原始存储的时候，为了避免在参数字符串中发生混淆，数据库名之间用 | 间隔
            if (string.IsNullOrEmpty(strDbNameList) == false)
                strDbNameList = strDbNameList.Replace(",", "|");

            BatchTaskStartInfo start_info = new BatchTaskStartInfo();
            start_info.Start = "dbnamelist=" + strDbNameList;

            BatchTaskInfo param = new BatchTaskInfo();
            param.StartInfo = start_info;

            int nRet = StartBatchTask("重建检索点",
                param,
                out info,
                out strError);
            if (nRet == -1)
                return -1;

            return 0;
        }

        // 初始化数据库
        // return:
        //      -1  出错
        //      0   没有找到
        //      1   成功
        int InitializeDatabase(
            SessionInfo sessioninfo,
            RmsChannelCollection Channels,
            string strLibraryCodeList,
            string strDatabaseNames,
            out string strOutputInfo,
            out string strError)
        {
            strOutputInfo = "";
            strError = "";

            int nRet = 0;
            long lRet = 0;

            string strLogFileName = this.GetTempFileName("zip");
            List<string> dbnames = new List<string>();  // 已经完成的数据库名集合
            List<string> other_dbnames = new List<string>();    // 其他类型的数据库名集合
            List<string> other_types = new List<string>();  // 和 other_dbnames 锁定对应的数据库类型集合

            bool bDbNameChanged = false;    // 初始化后，检索途径名等都可能被改变

            RmsChannel channel = Channels.GetChannel(this.WsUrl);

            string[] names = strDatabaseNames.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < names.Length; i++)
            {
                string strName = names[i].Trim();
                if (String.IsNullOrEmpty(strName) == true)
                    continue;

                // 书目库整体初始化，也是可以的
                // TODO: 将来可以考虑单独初始化书目库而不删除组内相关库
                if (this.IsBiblioDbName(strName) == true)
                {
                    // 获得相关配置小节
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@biblioDbName='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "配置DOM中名字为 '" + strName + "' 的书目库(biblioDbName属性)相关<database>元素没有找到";
                        return 0;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许初始化书目库";
                        return -1;
                    }

                    // 初始化书目库
                    lRet = InitializeDatabase(channel,
                        strName,
                        strLogFileName,
                        out strError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError = "初始化小书目库 '" + strName + "' 时发生错误: " + strError;
                        return -1;
                    }

                    dbnames.Add(strName);
                    bDbNameChanged = true;

                    // 初始化实体库
                    string strEntityDbName = DomUtil.GetAttr(nodeDatabase, "name");
                    if (String.IsNullOrEmpty(strEntityDbName) == false)
                    {
                        lRet = InitializeDatabase(channel,
                            strEntityDbName,
                            strLogFileName,
                            out strError);
                        if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                        {
                            strError = "初始化书目库 '" + strName + "' 所从属的实体库 '" + strEntityDbName + "' 时发生错误: " + strError;
                            return -1;
                        }
                        dbnames.Add(strEntityDbName);
                    }

                    // 初始化订购库
                    string strOrderDbName = DomUtil.GetAttr(nodeDatabase, "orderDbName");
                    if (String.IsNullOrEmpty(strOrderDbName) == false)
                    {
                        lRet = InitializeDatabase(channel,
                            strOrderDbName,
                            strLogFileName,
                            out strError);
                        if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                        {
                            strError = "初始化书目库 '" + strName + "' 所从属的订购库 '" + strOrderDbName + "' 时发生错误: " + strError;
                            return -1;
                        }
                        dbnames.Add(strOrderDbName);
                    }

                    // 初始化期库
                    string strIssueDbName = DomUtil.GetAttr(nodeDatabase, "issueDbName");
                    if (String.IsNullOrEmpty(strIssueDbName) == false)
                    {
                        lRet = InitializeDatabase(channel,
                            strIssueDbName,
                            strLogFileName,
                            out strError);
                        if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                        {
                            strError = "初始化书目库 '" + strName + "' 所从属的期库 '" + strIssueDbName + "' 时发生错误: " + strError;
                            return -1;
                        }
                        dbnames.Add(strIssueDbName);
                    }

                    // 初始化评注库
                    string strCommentDbName = DomUtil.GetAttr(nodeDatabase, "commentDbName");
                    if (String.IsNullOrEmpty(strCommentDbName) == false)
                    {
                        lRet = InitializeDatabase(channel,
                            strCommentDbName,
                            strLogFileName,
                            out strError);
                        if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                        {
                            strError = "初始化书目库 '" + strName + "' 所从属的评注库 '" + strCommentDbName + "' 时发生错误: " + strError;
                            return -1;
                        }
                        dbnames.Add(strCommentDbName);
                    }

                    continue;
                }

                // 单独初始化实体库
                if (this.IsItemDbName(strName) == true)
                {
                    // 获得相关配置小节
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@name='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "配置DOM中名字为 '" + strName + "' 的实体库(name属性)相关<database>元素没有找到";
                        return 0;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许初始化实体库";
                        return -1;
                    }

                    // 初始化实体库
                    lRet = InitializeDatabase(channel,
                        strName,
                        strLogFileName,
                        out strError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError = "初始化实体库 '" + strName + "' 时发生错误: " + strError;
                        return -1;
                    }

                    dbnames.Add(strName);
                    bDbNameChanged = true;
                    continue;
                }

                // 单独初始化订购库
                if (this.IsOrderDbName(strName) == true)
                {
                    // 获得相关配置小节
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@orderDbName='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "配置DOM中名字为 '" + strName + "' 的订购库(orderDbName属性)相关<database>元素没有找到";
                        return 0;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许初始化订购库";
                        return -1;
                    }

                    // 初始化订购库
                    lRet = InitializeDatabase(channel,
                        strName,
                        strLogFileName,
                        out strError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError = "初始化订购库 '" + strName + "' 时发生错误: " + strError;
                        return -1;
                    }

                    dbnames.Add(strName);
                    bDbNameChanged = true;
                    continue;
                }

                // 单独初始化期库
                if (this.IsIssueDbName(strName) == true)
                {
                    // 获得相关配置小节
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@issueDbName='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "配置DOM中名字为 '" + strName + "' 的期库(issueDbName属性)相关<database>元素没有找到";
                        return 0;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许初始化期库";
                        return -1;
                    }

                    // 初始化期库
                    lRet = InitializeDatabase(channel,
                        strName,
                        strLogFileName,
                        out strError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError = "初始化期库 '" + strName + "' 时发生错误: " + strError;
                        return -1;
                    }

                    dbnames.Add(strName);
                    bDbNameChanged = true;
                    continue;
                }

                // 单独初始化评注库
                if (this.IsCommentDbName(strName) == true)
                {
                    // 获得相关配置小节
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@commentDbName='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "配置DOM中名字为 '" + strName + "' 的评注库(commentDbName属性)相关<database>元素没有找到";
                        return 0;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许初始化评注库";
                        return -1;
                    }

                    // 初始化评注库
                    lRet = InitializeDatabase(channel,
                        strName,
                        strLogFileName,
                        out strError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError = "初始化评注库 '" + strName + "' 时发生错误: " + strError;
                        return -1;
                    }

                    dbnames.Add(strName);
                    bDbNameChanged = true;
                    continue;
                }

                // 初始化读者库
                if (this.IsReaderDbName(strName) == true)
                {
                    // 获得相关配置小节
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("readerdbgroup/database[@name='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "配置DOM中名字为 '" + strName + "' 的读者库(name属性)相关<database>元素没有找到";
                        return 0;
                    }

                    // 2012/9/9
                    // 分馆用户只允许初始化属于管辖分馆的读者库
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        string strExistLibraryCode = DomUtil.GetAttr(nodeDatabase, "libraryCode");

                        if (string.IsNullOrEmpty(strExistLibraryCode) == true
                            || StringUtil.IsInList(strExistLibraryCode, strLibraryCodeList) == false)
                        {
                            strError = "初始化读者库 '" + strName + "' 被拒绝。当前用户只能初始化图书馆代码完全完全属于 '" + strLibraryCodeList + "' 范围的读者库";
                            return -1;
                        }
                    }

                    // 初始化读者库
                    lRet = InitializeDatabase(channel,
                        strName,
                        strLogFileName,
                        out strError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError = "初始化读者库 '" + strName + "' 时发生错误: " + strError;
                        return -1;
                    }

                    dbnames.Add(strName);
                    bDbNameChanged = true;
                    continue;
                }

                // 初始化预约到书库
                if (this.ArrivedDbName == strName)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许初始化预约到书库";
                        return -1;
                    }

                    // 初始化预约到书库
                    lRet = InitializeDatabase(channel,
                        strName,
                        strLogFileName,
                        out strError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError = "初始化预约到书库 '" + strName + "' 时发生错误: " + strError;
                        return -1;
                    }
                    dbnames.Add(strName);
                    continue;
                }

                // 初始化违约金库
                if (this.AmerceDbName == strName)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许初始化违约金库";
                        return -1;
                    }

                    // 初始化违约金库
                    lRet = InitializeDatabase(channel,
                        strName,
                        strLogFileName,
                        out strError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError = "初始化违约金库 '" + strName + "' 时发生错误: " + strError;
                        return -1;
                    }

                    dbnames.Add(strName);
                    continue;
                }

                // 初始化发票库
                if (this.InvoiceDbName == strName)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许初始化发票库";
                        return -1;
                    }

                    // 初始化发票库
                    lRet = InitializeDatabase(channel,
                        strName,
                        strLogFileName,
                        out strError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError = "初始化发票库 '" + strName + "' 时发生错误: " + strError;
                        return -1;
                    }

                    dbnames.Add(strName);
                    continue;
                }

                // 初始化消息库
                if (this.MessageDbName == strName)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许初始化消息库";
                        return -1;
                    }

                    // 初始化消息库
                    lRet = InitializeDatabase(channel,
                        strName,
                        strLogFileName,
                        out strError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError = "初始化消息库 '" + strName + "' 时发生错误: " + strError;
                        return -1;
                    }

                    dbnames.Add(strName);
                    continue;
                }

                // 初始化拼音库
                if (this.PinyinDbName == strName)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许初始化拼音库";
                        return -1;
                    }

                    // 初始化拼音库
                    lRet = InitializeDatabase(channel,
                        strName,
                        strLogFileName,
                        out strError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError = "初始化拼音库 '" + strName + "' 时发生错误: " + strError;
                        return -1;
                    }

                    dbnames.Add(strName);
                    continue;
                }

                // 初始化著者号码库
                if (this.GcatDbName == strName)
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许初始化著者号码库";
                        return -1;
                    }

                    // 初始化著者号码库
                    lRet = InitializeDatabase(channel,
                        strName,
                        strLogFileName,
                        out strError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError = "初始化著者号码库 '" + strName + "' 时发生错误: " + strError;
                        return -1;
                    }
                    dbnames.Add(strName);
                    continue;
                }

                // 初始化实用库
                if (IsUtilDbName(strName) == true)
                {
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("utilDb/database[@name='" + strName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "不存在name属性值为 '" + strName + "' 的<utilDb/database>的元素";
                        return 0;
                    }

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许初始化实用库";
                        return -1;
                    }

                    // 初始化实用库
                    lRet = InitializeDatabase(channel,
                        strName,
                        strLogFileName,
                        out strError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError = "初始化实用库 '" + strName + "' 时发生错误: " + strError;
                        return -1;
                    }
                    dbnames.Add(strName);
                    continue;
                }

                // 初始化访问日志库
                if (this.IsAccessLogDbName(strName))
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许初始化" + AccessLogDbName + "库";
                        return -1;
                    }

                    if (string.IsNullOrEmpty(this.MongoDbConnStr) == true)
                    {
                        strError = "当前尚未启用 MongoDB 功能";
                        return -1;
                    }

                    // 初始化访问日志库
                    nRet = this.AccessLogDatabase.Clear(out strError);
                    if (nRet == -1)
                    {
                        strError = "初始化" + AccessLogDbName + "库 '" + strName + "' 时发生错误: " + strError;
                        return -1;
                    }
                    other_dbnames.Add(strName);
                    other_types.Add("accessLog");
                    continue;
                }

                // 初始化访问计数库
                if (this.IsHitCountDbName(strName))
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许初始化" + HitCountDbName + "库";
                        return -1;
                    }

                    if (string.IsNullOrEmpty(this.MongoDbConnStr) == true)
                    {
                        strError = "当前尚未启用 MongoDB 功能";
                        return -1;
                    }

                    // 初始化预约到书库
                    nRet = this.HitCountDatabase.Clear(out strError);
                    if (nRet == -1)
                    {
                        strError = "初始化" + HitCountDbName + "库 '" + strName + "' 时发生错误: " + strError;
                        return -1;
                    }
                    other_dbnames.Add(strName);
                    other_types.Add("hitcount");
                    continue;
                }

                // 初始化出纳历史库
                if (this.IsChargingHistoryDbName(strName))
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许初始化" + ChargingHistoryDbName + "库";
                        return -1;
                    }

                    if (string.IsNullOrEmpty(this.MongoDbConnStr) == true)
                    {
                        strError = "当前尚未启用 MongoDB 功能";
                        return -1;
                    }

                    // 初始化出纳历史库
                    nRet = this.ChargingOperDatabase.Clear(out strError);
                    if (nRet == -1)
                    {
                        strError = "初始化" + ChargingHistoryDbName + "库 '" + strName + "' 时发生错误: " + strError;
                        return -1;
                    }
                    other_dbnames.Add(strName);
                    other_types.Add("chargingOper");
                    continue;
                }

                // 初始化书目摘要库
                if (this.IsBiblioSummaryDbName(strName))
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许初始化" + BiblioSummaryDbName + "库";
                        return -1;
                    }

                    if (string.IsNullOrEmpty(this.MongoDbConnStr) == true)
                    {
                        strError = "当前尚未启用 MongoDB 功能";
                        return -1;
                    }

                    // 初始化
                    this.ClearBiblioSummaryDb();
                    other_dbnames.Add(strName);
                    other_types.Add("biblioSummary");
                    continue;
                }

                strError = "数据库名 '" + strName + "' 不属于 dp2library 目前管辖的范围...";
                return 0;
            }


            // 写入操作日志
            {
                XmlDocument domOperLog = new XmlDocument();
                domOperLog.LoadXml("<root />");

                DomUtil.SetElementText(domOperLog.DocumentElement,
                    "operation",
                    "manageDatabase");
                DomUtil.SetElementText(domOperLog.DocumentElement,
    "action",
    "initializeDatabase");

                XmlNode new_node = DomUtil.SetElementText(domOperLog.DocumentElement, "databases",
"");
                foreach (string name in dbnames)
                {
                    XmlElement database = domOperLog.CreateElement("database");
                    new_node.AppendChild(database);
                    database.SetAttribute("name", name);
                }
                {
                    int i = 0;
                    foreach (string name in other_dbnames)
                    {
                        string type = other_types[i];
                        XmlElement database = domOperLog.CreateElement("database");
                        new_node.AppendChild(database);
                        database.SetAttribute("name", name);
                        database.SetAttribute("type", type);
                        i++;
                    }
                }

                DomUtil.SetElementText(domOperLog.DocumentElement, "operator",
                    sessioninfo.UserID);

                string strOperTime = this.Clock.GetClock();

                DomUtil.SetElementText(domOperLog.DocumentElement, "operTime",
                    strOperTime);

                if (File.Exists(strLogFileName))
                {
                    using (Stream stream = File.OpenRead(strLogFileName))
                    {
                        nRet = this.OperLog.WriteOperLog(domOperLog,
                            sessioninfo.ClientAddress,
                            stream,
                            out strError);
                    }
                }
                else
                {
                    nRet = this.OperLog.WriteOperLog(domOperLog,
    sessioninfo.ClientAddress,
    out strError);
                }
                if (nRet == -1)
                {
                    strError = "ManageDatabase() API refreshDatabase 写入日志时发生错误: " + strError;
                    return -1;
                }

            }


            if (bDbNameChanged == true)
            {
                nRet = InitialKdbs(
                    Channels,
                    out strError);
                if (nRet == -1)
                    return -1;
                /*
                // 重新初始化虚拟库定义
                this.vdbs = null;
                nRet = this.InitialVdbs(Channels,
                    out strError);
                if (nRet == -1)
                    return -1;
                 * */
            }

            return 1;
        }

        const string AccessLogDbName = "访问日志";
        bool IsAccessLogDbName(string strName)
        {
            if (strName == AccessLogDbName)
                return true;
            return false;
        }

        const string HitCountDbName = "访问计数";
        bool IsHitCountDbName(string strName)
        {
            if (strName == HitCountDbName)
                return true;
            return false;
        }

        const string ChargingHistoryDbName = "出纳历史";
        bool IsChargingHistoryDbName(string strName)
        {
            if (strName == ChargingHistoryDbName)
                return true;
            return false;
        }

        const string BiblioSummaryDbName = "书目摘要";
        bool IsBiblioSummaryDbName(string strName)
        {
            if (strName == BiblioSummaryDbName)
                return true;
            return false;
        }

        // 请求创建书目库的参数信息
        class RequestBiblioDatabase
        {
            public string Name { get; set; }
            public string Type { get; set; }    // biblio/reader/publisher/inventory/dictionary/zhongcihao/pinyin/word/arrived/entity/order/issue/comment

            public string OwnerDbName { get; set; } // 类型为 entity/order/issue/comment 的 Name 数据库所从属的书目库名

            public string LibraryCode { get; set; }

            public string Syntax { get; set; }
            public string Usage { get; set; }
            public string Role { get; set; }

            public string EntityDbName { get; set; }
            public string OrderDbName { get; set; }
            public string IssueDbName { get; set; }
            public string CommentDbName { get; set; }

            public bool InCirculation { get; set; }
            public string UnionCatalogStyle { get; set; }
            public string Replication { get; set; }

            // 根据本对象的属性，将一个 database 请求元素的属性填充完善
            public void FillRequestNode(XmlElement request_node)
            {
                request_node.SetAttribute("name", this.Name);
                request_node.SetAttribute("type", this.Type.ToLower());

                if (this.Type == "biblio" || this.Type == "reader")
                    request_node.SetAttribute("inCirculation", this.InCirculation ? "true" : "false");

                if (this.Type == "reader")
                    request_node.SetAttribute("libraryCode", this.LibraryCode);

                if (this.Type != "biblio")
                    return;

                request_node.SetAttribute("syntax", this.Syntax);
                if (string.IsNullOrEmpty(this.IssueDbName) == false)
                    request_node.SetAttribute("usage", "series");
                else
                    request_node.SetAttribute("usage", "book");

                SetAttribute(request_node, "role", this.Role);
                SetAttribute(request_node, "entityDbName", this.EntityDbName);
                SetAttribute(request_node, "orderDbName", this.OrderDbName);
                SetAttribute(request_node, "issueDbName", this.IssueDbName);
                SetAttribute(request_node, "commentDbName", this.CommentDbName);

                SetAttribute(request_node, "unionCatalogStyle", this.UnionCatalogStyle);
                SetAttribute(request_node, "replication", this.Replication);
            }

            // 设置属性值，或者删除属性(如果 value 为空)
            static void SetAttribute(XmlElement element, string name, string value)
            {
                if (string.IsNullOrEmpty(value))
                    element.RemoveAttribute(name);
                else
                    element.SetAttribute(name, value);
            }

            // 从请求 XML 中构建
            public static RequestBiblioDatabase FromRequest(XmlElement request_node)
            {
                RequestBiblioDatabase info = new RequestBiblioDatabase();

                info.Type = DomUtil.GetAttr(request_node, "type").ToLower();
                info.Name = DomUtil.GetAttr(request_node, "name");

                if (info.Type == "entity"
                    || info.Type == "order"
                    || info.Type == "issue"
                    || info.Type == "comment")
                {
                    info.OwnerDbName = request_node.GetAttribute("biblioDbName");
                    return info;
                }

                string strInCirculation = DomUtil.GetAttr(request_node, "inCirculation");
                if (String.IsNullOrEmpty(strInCirculation) == true)
                    strInCirculation = "true";  // 缺省为true

                info.InCirculation = DomUtil.IsBooleanTrue(strInCirculation);

                if (info.Type == "reader")
                {
                    info.LibraryCode = DomUtil.GetAttr(request_node, "libraryCode");
                    return info;
                }

                if (info.Type != "biblio")
                    return info;

                info.Syntax = DomUtil.GetAttr(request_node, "syntax");
                if (String.IsNullOrEmpty(info.Syntax) == true)
                    info.Syntax = "unimarc";

                // usage: book series
                info.Usage = DomUtil.GetAttr(request_node, "usage");
                if (String.IsNullOrEmpty(info.Usage) == true)
                    info.Usage = "book";

                info.Role = DomUtil.GetAttr(request_node, "role");
                info.EntityDbName = DomUtil.GetAttr(request_node, "entityDbName");

                info.OrderDbName = DomUtil.GetAttr(request_node, "orderDbName");

                info.IssueDbName = DomUtil.GetAttr(request_node, "issueDbName");

                info.CommentDbName = DomUtil.GetAttr(request_node, "commentDbName");

                if (info.EntityDbName == "<default>")
                    info.EntityDbName = info.Name + "实体";

                if (info.OrderDbName == "<default>")
                    info.OrderDbName = info.Name + "订购";

                if (info.IssueDbName == "<default>")
                    info.IssueDbName = info.Name + "期";

                if (info.CommentDbName == "<default>")
                    info.CommentDbName = info.Name + "评注";

                info.UnionCatalogStyle = DomUtil.GetAttr(request_node, "unionCatalogStyle");

                info.Replication = DomUtil.GetAttr(request_node, "replication");
                return info;
            }

            // 从 library.xml XML 中构建
            public static RequestBiblioDatabase FromBiblioCfgNode(XmlElement cfg_node)
            {
                RequestBiblioDatabase info = new RequestBiblioDatabase();

                info.Type = "biblio";
                info.Name = DomUtil.GetAttr(cfg_node, "biblioDbName");

                info.EntityDbName = DomUtil.GetAttr(cfg_node, "name");
                info.OrderDbName = DomUtil.GetAttr(cfg_node, "orderDbName");
                info.IssueDbName = DomUtil.GetAttr(cfg_node, "issueDbName");
                info.CommentDbName = DomUtil.GetAttr(cfg_node, "commentDbName");

                info.Syntax = DomUtil.GetAttr(cfg_node, "syntax");
                if (String.IsNullOrEmpty(info.Syntax) == true)
                    info.Syntax = "unimarc";

                // usage: book series
                if (string.IsNullOrEmpty(info.IssueDbName))
                    info.Usage = "book";
                else
                    info.Usage = "series";

                info.Role = DomUtil.GetAttr(cfg_node, "role");

                string strInCirculation = DomUtil.GetAttr(cfg_node, "inCirculation");
                if (String.IsNullOrEmpty(strInCirculation) == true)
                    strInCirculation = "true";  // 缺省为true

                info.InCirculation = DomUtil.IsBooleanTrue(strInCirculation);

                info.UnionCatalogStyle = DomUtil.GetAttr(cfg_node, "unionCatalogStyle");

                info.Replication = DomUtil.GetAttr(cfg_node, "replication");
                return info;
            }

            // 从 library.xml XML 中构建
            public static RequestBiblioDatabase FromReaderCfgNode(XmlElement cfg_node)
            {
                RequestBiblioDatabase info = new RequestBiblioDatabase();

                info.Type = "reader";
                info.Name = DomUtil.GetAttr(cfg_node, "name");
                info.LibraryCode = DomUtil.GetAttr(cfg_node, "libraryCode");

                string strInCirculation = DomUtil.GetAttr(cfg_node, "inCirculation");
                if (String.IsNullOrEmpty(strInCirculation) == true)
                    strInCirculation = "true";  // 缺省为true

                info.InCirculation = DomUtil.IsBooleanTrue(strInCirculation);
                return info;
            }

            // 从 library.xml XML 中构建
            public static RequestBiblioDatabase FromUtilityCfgNode(XmlElement cfg_node)
            {
                RequestBiblioDatabase info = new RequestBiblioDatabase();

                info.Name = DomUtil.GetAttr(cfg_node, "name");
                info.Type = DomUtil.GetAttr(cfg_node, "type");

                return info;
            }

            // 在 library.xml 中为 biblio 类型的 database 配置节点写入参数
            public void WriteBiblioCfgNode(XmlElement nodeNewDatabase)
            {
                DomUtil.SetAttr(nodeNewDatabase, "name", this.EntityDbName);
                DomUtil.SetAttr(nodeNewDatabase, "biblioDbName", this.Name);
                if (String.IsNullOrEmpty(this.OrderDbName) == false)
                    DomUtil.SetAttr(nodeNewDatabase, "orderDbName", this.OrderDbName);
                else
                    nodeNewDatabase.RemoveAttribute("orderDbName");

                if (String.IsNullOrEmpty(this.IssueDbName) == false)
                    DomUtil.SetAttr(nodeNewDatabase, "issueDbName", this.IssueDbName);
                else
                    nodeNewDatabase.RemoveAttribute("issueDbName");

                if (String.IsNullOrEmpty(this.CommentDbName) == false)
                    DomUtil.SetAttr(nodeNewDatabase, "commentDbName", this.CommentDbName);
                else
                    nodeNewDatabase.RemoveAttribute("commentDbName");

                DomUtil.SetAttr(nodeNewDatabase, "syntax", this.Syntax);

                // 2009/10/23
                DomUtil.SetAttr(nodeNewDatabase, "role", this.Role);

                DomUtil.SetAttr(nodeNewDatabase, "inCirculation", this.InCirculation ? "true" : "false");

                // 2012/4/30
                if (string.IsNullOrEmpty(this.UnionCatalogStyle) == false)
                    DomUtil.SetAttr(nodeNewDatabase, "unionCatalogStyle", this.UnionCatalogStyle);
                else
                    nodeNewDatabase.RemoveAttribute("unionCatalogStyle");

                if (string.IsNullOrEmpty(this.Replication) == false)
                    DomUtil.SetAttr(nodeNewDatabase, "replication", this.Replication);
                else
                    nodeNewDatabase.RemoveAttribute("replication");
            }

            // 在 library.xml 中为 reader 类型的 database 配置节点写入参数
            public void WriteReaderCfgNode(XmlElement nodeNewDatabase)
            {
                DomUtil.SetAttr(nodeNewDatabase, "name", this.Name);
                DomUtil.SetAttr(nodeNewDatabase, "inCirculation", this.InCirculation ? "true" : "false");
                DomUtil.SetAttr(nodeNewDatabase, "libraryCode", this.LibraryCode);
            }

            // 在 library.xml 中为 utility 类型的 database 配置节点写入参数
            public void WriteUtilityCfgNode(XmlElement nodeNewDatabase)
            {
                DomUtil.SetAttr(nodeNewDatabase, "name", this.Name);
                DomUtil.SetAttr(nodeNewDatabase, "type", this.Type);
            }

            // 在 library.xml 中为 biblio 类型的 database 配置节点写入部分参数
            public void WritePartBiblioCfgNode(XmlElement nodeNewDatabase)
            {
                string strExistingBiblioDbName = nodeNewDatabase.GetAttribute("biblioDbName");
                if (this.OwnerDbName != strExistingBiblioDbName)
                {
                    throw new Exception("已经存在的 biblioDbName 属性值 '" + strExistingBiblioDbName + "' 和 OwnerDbName '" + this.OwnerDbName + "' 不同");
                }
                if (this.Type == "entity")
                {
                    if (String.IsNullOrEmpty(this.Name) == false)
                        DomUtil.SetAttr(nodeNewDatabase, "name", this.Name);
                    else
                        nodeNewDatabase.RemoveAttribute("name");
                }
                if (this.Type == "order")
                {
                    if (String.IsNullOrEmpty(this.Name) == false)
                        DomUtil.SetAttr(nodeNewDatabase, "orderDbName", this.Name);
                    else
                        nodeNewDatabase.RemoveAttribute("orderDbName");
                }
                if (this.Type == "issue")
                {
                    if (String.IsNullOrEmpty(this.Name) == false)
                        DomUtil.SetAttr(nodeNewDatabase, "issueDbName", this.Name);
                    else
                        nodeNewDatabase.RemoveAttribute("issueDbName");
                }
                if (this.Type == "comment")
                {
                    if (String.IsNullOrEmpty(this.Name) == false)
                        DomUtil.SetAttr(nodeNewDatabase, "commentDbName", this.Name);
                    else
                        nodeNewDatabase.RemoveAttribute("commentDbName");
                }
            }

        }

        // 创建数据库
        // parameters:
        //      strLibraryCodeList  当前用户的管辖分馆代码列表
        //      bRecreate   是否为重新创建？如果为重新创建，则允许已经存在定义；如果不是重新创建，即首次创建，则不允许已经存在定义
        //                  注: 重新创建的意思, 是 library.xml 中有定义，但 dp2kernel 中没有对应的数据库，要根据定义重新创建这些 dp2kernel 数据库
        // return:
        //      -1  出错
        //      0   没有找到
        //      1   成功
        int CreateDatabase(
            SessionInfo sessioninfo,
            RmsChannelCollection Channels,
            string strLibraryCodeList,
            string strDatabaseInfo,
            bool bRecreate,
            out string strOutputInfo,
            out string strError)
        {
            strOutputInfo = "";
            strError = "";

            int nRet = 0;

            string strLogFileName = this.GetTempFileName("zip");
            List<XmlNode> database_nodes = new List<XmlNode>(); // 已经创建的数据库的定义节点

            List<string> created_dbnames = new List<string>();  // 过程中，已经创建的数据库名

            bool bDbChanged = false;    // 数据库名是否发生过改变？或者新创建过数据库? 如果发生过，需要重新初始化kdbs

            XmlDocument request_dom = new XmlDocument();
            try
            {
                request_dom.LoadXml(strDatabaseInfo);
            }
            catch (Exception ex)
            {
                strError = "strDatabaseInfo内容装入XMLDOM时出错: " + ex.Message;
                return -1;
            }

            RmsChannel channel = Channels.GetChannel(this.WsUrl);

            XmlNodeList nodes = request_dom.DocumentElement.SelectNodes("database");
            // for (int i = 0; i < nodes.Count; i++)
            foreach (XmlElement request_node in nodes)
            {
                // XmlNode node = nodes[i];
                string strType = DomUtil.GetAttr(request_node, "type").ToLower();

                string strName = DomUtil.GetAttr(request_node, "name");

                string strInfo = request_node.GetAttribute("info");

                #region biblio

                // 创建书目数据库
                if (strType == "biblio")
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许创建或重新创建书目库";
                        return -1;
                    }

                    if (this.TestMode == true)
                    {
                        XmlNodeList existing_nodes = this.LibraryCfgDom.DocumentElement.SelectNodes("itemdbgroup/database");
                        if (existing_nodes.Count >= 4)
                        {
                            strError = "dp2Library XE 评估模式下只能创建最多 4 个书目库";
                            goto ERROR1;
                        }
                    }

                    // 2009/11/13
                    XmlElement exist_database_node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@biblioDbName='" + strName + "']") as XmlElement;
                    if (bRecreate == true && exist_database_node == null)
                    {
                        strError = "library.xml 中并不存在书目库 '" + strName + "' 的定义，无法进行重新创建";
                        return 0;
                    }

                    if (bRecreate == false)
                    {
                        // 检查 cfgdom 中是否已经存在同名的书目库
                        if (this.IsBiblioDbName(strName) == true)
                        {
                            strError = "书目库 '" + strName + "' 的定义已经存在，不能重复创建";
                            goto ERROR1;
                        }
                    }

                    // 检查dp2kernel中是否有和书目库同名的数据库存在
                    {
                        // 数据库是否已经存在？
                        // return:
                        //      -1  error
                        //      0   not exist
                        //      1   exist
                        //      2   其他类型的同名对象已经存在
                        nRet = DatabaseUtility.IsDatabaseExist(
                            channel,
                            strName,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (nRet >= 1)
                            goto ERROR1;
                    }

                    RequestBiblioDatabase info = null;

                    if (strInfo == "*" || strInfo == "existing")    // 使用已经存在的 database 定义
                        info = RequestBiblioDatabase.FromBiblioCfgNode(exist_database_node);
                    else
                        info = RequestBiblioDatabase.FromRequest(request_node);

                    if (String.IsNullOrEmpty(info.EntityDbName) == false)
                    {
                        if (bRecreate == false)
                        {
                            // 检查 cfgdom 中是否已经存在同名的实体库
                            if (this.IsItemDbName(info.EntityDbName) == true)
                            {
                                strError = "实体库 '" + info.EntityDbName + "' 的定义已经存在，不能重复创建";
                                goto ERROR1;
                            }
                        }

                        // 数据库是否已经存在？
                        // return:
                        //      -1  error
                        //      0   not exist
                        //      1   exist
                        //      2   其他类型的同名对象已经存在
                        nRet = DatabaseUtility.IsDatabaseExist(
                            channel,
                            info.EntityDbName,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (nRet >= 1)
                            goto ERROR1;
                    }

                    if (String.IsNullOrEmpty(info.OrderDbName) == false)
                    {
                        if (bRecreate == false)
                        {
                            // 检查cfgdom中是否已经存在同名的订购库
                            if (this.IsOrderDbName(info.OrderDbName) == true)
                            {
                                strError = "订购库 '" + info.OrderDbName + "' 的定义已经存在，不能重复创建";
                                goto ERROR1;
                            }
                        }

                        // 数据库是否已经存在？
                        // return:
                        //      -1  error
                        //      0   not exist
                        //      1   exist
                        //      2   其他类型的同名对象已经存在
                        nRet = DatabaseUtility.IsDatabaseExist(
                            channel,
                            info.OrderDbName,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (nRet >= 1)
                            goto ERROR1;
                    }


                    if (String.IsNullOrEmpty(info.IssueDbName) == false)
                    {
                        if (bRecreate == false)
                        {
                            // 检查cfgdom中是否已经存在同名的期库
                            if (this.IsOrderDbName(info.IssueDbName) == true)
                            {
                                strError = "期库 '" + info.IssueDbName + "' 的定义已经存在，不能重复创建";
                                goto ERROR1;
                            }
                        }

                        // 数据库是否已经存在？
                        // return:
                        //      -1  error
                        //      0   not exist
                        //      1   exist
                        //      2   其他类型的同名对象已经存在
                        nRet = DatabaseUtility.IsDatabaseExist(
                            channel,
                            info.IssueDbName,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (nRet >= 1)
                            goto ERROR1;
                    }

                    if (String.IsNullOrEmpty(info.CommentDbName) == false)
                    {
                        if (bRecreate == false)
                        {
                            // 检查cfgdom中是否已经存在同名的评注库
                            if (this.IsCommentDbName(info.CommentDbName) == true)
                            {
                                strError = "评注库 '" + info.CommentDbName + "' 的定义已经存在，不能重复创建";
                                goto ERROR1;
                            }
                        }

                        // 数据库是否已经存在？
                        // return:
                        //      -1  error
                        //      0   not exist
                        //      1   exist
                        //      2   其他类型的同名对象已经存在
                        nRet = DatabaseUtility.IsDatabaseExist(
                            channel,
                            info.CommentDbName,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (nRet >= 1)
                            goto ERROR1;
                    }

                    // 开始创建

                    // 创建书目库
                    string strTemplateDir = this.DataDir + "\\templates\\" + "biblio_" + info.Syntax + "_" + info.Usage;

                    // 根据预先的定义，创建一个数据库
                    nRet = CreateDatabase(channel,
                        strTemplateDir,
                        strName,
                        strLogFileName,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    created_dbnames.Add(strName);

                    bDbChanged = true;

                    // 创建实体库
                    if (String.IsNullOrEmpty(info.EntityDbName) == false)
                    {
                        strTemplateDir = this.DataDir + "\\templates\\" + "item";

                        // 根据预先的定义，创建一个数据库
                        nRet = CreateDatabase(channel,
                            strTemplateDir,
                            info.EntityDbName,
                            strLogFileName,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

                        created_dbnames.Add(info.EntityDbName);

                        bDbChanged = true;
                    }

                    // 创建订购库
                    if (String.IsNullOrEmpty(info.OrderDbName) == false)
                    {
                        strTemplateDir = this.DataDir + "\\templates\\" + "order";

                        // 根据预先的定义，创建一个数据库
                        nRet = CreateDatabase(channel,
                            strTemplateDir,
                            info.OrderDbName,
                        strLogFileName,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        created_dbnames.Add(info.OrderDbName);

                        bDbChanged = true;
                    }

                    // 创建期库
                    if (String.IsNullOrEmpty(info.IssueDbName) == false)
                    {
                        strTemplateDir = this.DataDir + "\\templates\\" + "issue";

                        // 根据预先的定义，创建一个数据库
                        nRet = CreateDatabase(channel,
                            strTemplateDir,
                            info.IssueDbName,
                         strLogFileName,
                           out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        created_dbnames.Add(info.IssueDbName);

                        bDbChanged = true;
                    }

                    // 创建评注库
                    if (String.IsNullOrEmpty(info.CommentDbName) == false)
                    {
                        strTemplateDir = this.DataDir + "\\templates\\" + "comment";

                        // 根据预先的定义，创建一个数据库
                        nRet = CreateDatabase(channel,
                            strTemplateDir,
                            info.CommentDbName,
                        strLogFileName,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        created_dbnames.Add(info.CommentDbName);

                        bDbChanged = true;
                    }

                    // 在CfgDom中增加相关的配置信息
                    XmlNode root = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup");
                    if (root == null)
                    {
                        root = this.LibraryCfgDom.CreateElement("itemdbgroup");
                        this.LibraryCfgDom.DocumentElement.AppendChild(root);
                    }

                    XmlElement nodeNewDatabase = null;

                    if (bRecreate == false)
                    {
                        nodeNewDatabase = this.LibraryCfgDom.CreateElement("database");
                        root.AppendChild(nodeNewDatabase);
                    }
                    else
                    {
                        nodeNewDatabase = exist_database_node;
                    }

                    info.WriteBiblioCfgNode(nodeNewDatabase);

                    // <itemdbgroup>内容更新，刷新配套的内存结构
                    nRet = this.LoadItemDbGroupParam(this.LibraryCfgDom,
                        out strError);
                    if (nRet == -1)
                    {
                        this.WriteErrorLog(strError);
                        goto ERROR1;
                    }

                    this.Changed = true;
                    // this.ActivateManagerThread();

                    created_dbnames.Clear();

                    //在重新创建时，记载下全部参数。这样可以避免日志恢复时候依赖当时的 library.xml 中的内容
                    if (bRecreate)
                        info.FillRequestNode(request_node);

                    database_nodes.Add(request_node);
                    continue;
                } // end of type biblio
                #endregion
                #region entity
                else if (strType == "entity"
                    || strType == "order"
                    || strType == "issue"
                    || strType == "comment")
                {
                    string strTypeCaption = GetTypeCaption(strType);

                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许创建或重新创建" + strTypeCaption + "库";
                        return -1;
                    }
                    // TODO: 增加recreate能力

                    // 单独创建实体库
                    string strBiblioDbName = DomUtil.GetAttr(request_node, "biblioDbName");
                    if (String.IsNullOrEmpty(strBiblioDbName) == true)
                    {
                        strError = "请求创建" + strTypeCaption + "库的 <database> 元素中，应包含 biblioDbName 属性";
                        goto ERROR1;
                    }

                    // 获得相关配置小节
                    XmlElement nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@biblioDbName='" + strBiblioDbName + "']") as XmlElement;
                    if (nodeDatabase == null)
                    {
                        strError = "配置 DOM 中名字为 '" + strBiblioDbName + "' 的书目库(biblioDbName属性)相关<database>元素没有找到，无法在其下创建实体库 " + strName;
                        return 0;
                    }

                    string strOldEntityDbName = "";

                    if (strType == "entity")
                        strOldEntityDbName = nodeDatabase.GetAttribute("name");
                    else if (strType == "order")
                        strOldEntityDbName = nodeDatabase.GetAttribute("orderDbName");
                    else if (strType == "issue")
                        strOldEntityDbName = nodeDatabase.GetAttribute("issueDbName");
                    else if (strType == "comment")
                        strOldEntityDbName = nodeDatabase.GetAttribute("commentDbName");
                    else
                    {
                        strError = "无法识别的 strType '" + strType + "'";
                        goto ERROR1;
                    }

                    if (strOldEntityDbName == strName)
                    {
                        strError = "从属于书目库 '" + strBiblioDbName + "' 的" + strTypeCaption + "库 '" + strName + "' 定义已经存在，不能重复创建";
                        goto ERROR1;
                    }

                    if (String.IsNullOrEmpty(strOldEntityDbName) == false)
                    {
                        strError = "要创建从属于书目库 '" + strBiblioDbName + "' 的新" + strTypeCaption + "库 '" + strName + "'，必须先删除已经存在的" + strTypeCaption + "库 '"
                            + strOldEntityDbName + "'";
                        goto ERROR1;
                    }

                    string strTemplateDir = Path.Combine(this.DataDir, "templates\\" + (strType.ToLower() == "entity" ? "item" : strType));
                    // this.DataDir + "\\templates\\" + "item";

                    // 根据预先的定义，创建一个数据库
                    nRet = CreateDatabase(channel,
                        strTemplateDir,
                        strName,
                        strLogFileName,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    created_dbnames.Add(strName);

                    bDbChanged = true;

                    string strAttrName = strType + "DbName";
                    if (strType == "entity")
                        strAttrName = "name";   // 例外
                    DomUtil.SetAttr(nodeDatabase, strAttrName, strName);

                    // 2008/12/4
                    // <itemdbgroup>内容更新，刷新配套的内存结构
                    nRet = this.LoadItemDbGroupParam(this.LibraryCfgDom,
                        out strError);
                    if (nRet == -1)
                    {
                        this.WriteErrorLog(strError);
                        goto ERROR1;
                    }

                    this.Changed = true;
                    database_nodes.Add(request_node);
                }
                #endregion
#if NO
                #region order
                else if (strType == "order")
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许创建或重新创建订购库";
                        return -1;
                    }
                    // TODO: 增加recreate能力

                    // 单独创建订购库
                    string strBiblioDbName = DomUtil.GetAttr(request_node, "biblioDbName");
                    if (String.IsNullOrEmpty(strBiblioDbName) == true)
                    {
                        strError = "创建订购库的<database>元素中，应包含biblioDbName属性";
                        goto ERROR1;
                    }

                    // 获得相关配置小节
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@biblioDbName='" + strBiblioDbName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "配置DOM中名字为 '" + strBiblioDbName + "' 的书目库(biblioDbName属性)相关<database>元素没有找到，无法在其下创建订购库 " + strName;
                        return 0;
                    }

                    string strOldOrderDbName = DomUtil.GetAttr(nodeDatabase,
                        "orderDbName");
                    if (strOldOrderDbName == strName)
                    {
                        strError = "从属于书目库 '" + strBiblioDbName + "' 的订购库 '" + strName + "' 定义已经存在，不能重复创建";
                        goto ERROR1;
                    }

                    if (String.IsNullOrEmpty(strOldOrderDbName) == false)
                    {
                        strError = "要创建从属于书目库 '" + strBiblioDbName + "' 的新订购库 '" + strName + "'，必须先删除已经存在的订购库 '"
                            + strOldOrderDbName + "'";
                        goto ERROR1;
                    }

                    string strTemplateDir = this.DataDir + "\\templates\\" + "order";

                    // 根据预先的定义，创建一个数据库
                    nRet = CreateDatabase(channel,
                        strTemplateDir,
                        strName,
                        strLogFileName,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    created_dbnames.Add(strName);

                    bDbChanged = true;

                    DomUtil.SetAttr(nodeDatabase, "orderDbName", strName);

                    // 2008/12/4
                    // <itemdbgroup>内容更新，刷新配套的内存结构
                    nRet = this.LoadItemDbGroupParam(this.LibraryCfgDom,
                        out strError);
                    if (nRet == -1)
                    {
                        this.WriteErrorLog(strError);
                        goto ERROR1;
                    }

                    this.Changed = true;
                    database_nodes.Add(request_node);
                }
                #endregion
                #region issue
                else if (strType == "issue")
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许创建或重新创建期库";
                        goto ERROR1;
                    }
                    // TODO: 增加recreate能力

                    // 单独创建期库
                    string strBiblioDbName = DomUtil.GetAttr(request_node, "biblioDbName");
                    if (String.IsNullOrEmpty(strBiblioDbName) == true)
                    {
                        strError = "创建期库的<database>元素中，应包含biblioDbName属性";
                        goto ERROR1;
                    }

                    // 获得相关配置小节
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@biblioDbName='" + strBiblioDbName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "配置DOM中名字为 '" + strBiblioDbName + "' 的书目库(biblioDbName属性)相关<database>元素没有找到，无法在其下创建期库 " + strName;
                        return 0;
                    }

                    string strOldIssueDbName = DomUtil.GetAttr(nodeDatabase,
                        "issueDbName");
                    if (strOldIssueDbName == strName)
                    {
                        strError = "从属于书目库 '" + strBiblioDbName + "' 的期库 '" + strName + "' 定义已经存在，不能重复创建";
                        goto ERROR1;
                    }

                    if (String.IsNullOrEmpty(strOldIssueDbName) == false)
                    {
                        strError = "要创建从属于书目库 '" + strBiblioDbName + "' 的新期库 '" + strName + "'，必须先删除已经存在的期库 '"
                            + strOldIssueDbName + "'";
                        goto ERROR1;
                    }

                    string strTemplateDir = this.DataDir + "\\templates\\" + "issue";

                    // 根据预先的定义，创建一个数据库
                    nRet = CreateDatabase(channel,
                        strTemplateDir,
                        strName,
                        strLogFileName,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    created_dbnames.Add(strName);

                    bDbChanged = true;

                    DomUtil.SetAttr(nodeDatabase, "issueDbName", strName);

                    // 2008/12/4
                    // <itemdbgroup>内容更新，刷新配套的内存结构
                    nRet = this.LoadItemDbGroupParam(this.LibraryCfgDom,
                        out strError);
                    if (nRet == -1)
                    {
                        this.WriteErrorLog(strError);
                        goto ERROR1;
                    }

                    this.Changed = true;
                    database_nodes.Add(request_node);
                }
                #endregion
                #region comment
                else if (strType == "comment")
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许创建或重新创建评注库";
                        goto ERROR1;
                    }
                    // TODO: 增加recreate能力

                    // 单独创建评注库
                    string strBiblioDbName = DomUtil.GetAttr(request_node, "biblioDbName");
                    if (String.IsNullOrEmpty(strBiblioDbName) == true)
                    {
                        strError = "创建评注库的<database>元素中，应包含biblioDbName属性";
                        goto ERROR1;
                    }

                    // 获得相关配置小节
                    XmlNode nodeDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("itemdbgroup/database[@biblioDbName='" + strBiblioDbName + "']");
                    if (nodeDatabase == null)
                    {
                        strError = "配置DOM中名字为 '" + strBiblioDbName + "' 的书目库(biblioDbName属性)相关<database>元素没有找到，无法在其下创建评注库 " + strName;
                        return 0;
                    }

                    string strOldCommentDbName = DomUtil.GetAttr(nodeDatabase,
                        "commentDbName");
                    if (strOldCommentDbName == strName)
                    {
                        strError = "从属于书目库 '" + strBiblioDbName + "' 的评注库 '" + strName + "' 定义已经存在，不能重复创建";
                        goto ERROR1;
                    }

                    if (String.IsNullOrEmpty(strOldCommentDbName) == false)
                    {
                        strError = "要创建从属于书目库 '" + strBiblioDbName + "' 的新评注库 '" + strName + "'，必须先删除已经存在的评注库 '"
                            + strOldCommentDbName + "'";
                        goto ERROR1;
                    }

                    string strTemplateDir = this.DataDir + "\\templates\\" + "comment";

                    // 根据预先的定义，创建一个数据库
                    nRet = CreateDatabase(channel,
                        strTemplateDir,
                        strName,
                        strLogFileName,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    created_dbnames.Add(strName);

                    bDbChanged = true;

                    DomUtil.SetAttr(nodeDatabase, "commentDbName", strName);

                    // 2008/12/4
                    // <itemdbgroup>内容更新，刷新配套的内存结构
                    nRet = this.LoadItemDbGroupParam(this.LibraryCfgDom,
                        out strError);
                    if (nRet == -1)
                    {
                        this.WriteErrorLog(strError);
                        goto ERROR1;
                    }

                    this.Changed = true;
                    database_nodes.Add(request_node);
                }
                #endregion
#endif
                #region reader
                else if (strType == "reader")
                {
                    // 创建读者库

                    // 2009/11/13
                    XmlElement exist_database_node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("readerdbgroup/database[@name='" + strName + "']") as XmlElement;
                    if (bRecreate == true && exist_database_node == null)
                    {
                        strError = "library.xml 中并不存在读者库 '" + strName + "' 的定义，无法进行重新创建";
                        return 0;
                    }

                    if (bRecreate == false)
                    {
                        // 检查cfgdom中是否已经存在同名的读者库
                        if (this.IsReaderDbName(strName) == true)
                        {
                            strError = "读者库 '" + strName + "' 的定义已经存在，不能重复创建";
                            goto ERROR1;
                        }
                    }
                    else
                    {
                        if (exist_database_node != null)
                        {
                            string strExistLibraryCode = DomUtil.GetAttr(exist_database_node, "libraryCode");

                            // 2012/9/9
                            // 分馆用户只允许修改馆代码属于管辖分馆的读者库
                            if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                            {
                                if (string.IsNullOrEmpty(strExistLibraryCode) == true
                                    || StringUtil.IsInList(strExistLibraryCode, strLibraryCodeList) == false)
                                {
                                    strError = "重新创建读者库 '" + strName + "' 被拒绝。当前用户只能重新创建图书馆代码完全完全属于 '" + strLibraryCodeList + "' 范围的读者库";
                                    goto ERROR1;
                                }
                            }
                        }
                    }

                    RequestBiblioDatabase info = null;

                    if (strInfo == "*" || strInfo == "existing")    // 使用已经存在的 database 定义
                        info = RequestBiblioDatabase.FromReaderCfgNode(exist_database_node);
                    else
                        info = RequestBiblioDatabase.FromRequest(request_node);
#if NO
                    string strLibraryCode = DomUtil.GetAttr(request_node,
    "libraryCode");
#endif

                    // 2012/9/9
                    // 分馆用户只允许处理馆代码为特定范围的读者库
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        if (string.IsNullOrEmpty(info.LibraryCode) == true
                            || IsListInList(info.LibraryCode, strLibraryCodeList) == false)
                        {
                            strError = "当前用户只能创建馆代码完全属于 '" + strLibraryCodeList + "' 范围内的读者库";
                            return -1;
                        }
                    }

                    // 检查dp2kernel中是否有和读者库同名的数据库存在
                    {
                        // 数据库是否已经存在？
                        // return:
                        //      -1  error
                        //      0   not exist
                        //      1   exist
                        //      2   其他类型的同名对象已经存在
                        nRet = DatabaseUtility.IsDatabaseExist(
                            channel,
                            strName,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (nRet >= 1)
                            goto ERROR1;
                    }

                    string strTemplateDir = this.DataDir + "\\templates\\" + "reader";

                    // 根据预先的定义，创建一个数据库
                    nRet = CreateDatabase(channel,
                        strTemplateDir,
                        strName,
                        strLogFileName,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    created_dbnames.Add(strName);

                    bDbChanged = true;

#if NO
                    string strInCirculation = DomUtil.GetAttr(request_node,
                        "inCirculation");
                    if (String.IsNullOrEmpty(strInCirculation) == true)
                        strInCirculation = "true";  // 缺省为true
#endif

                    // 检查一个单独的图书馆代码是否格式正确
                    // 要求不能为 '*'，不能包含逗号
                    // return:
                    //      -1  校验函数本身出错了
                    //      0   校验正确
                    //      1   校验发现问题。strError中有描述
                    nRet = VerifySingleLibraryCode(info.LibraryCode,
        out strError);
                    if (nRet != 0)
                    {
                        strError = "图书馆代码 '" + info.LibraryCode + "' 格式错误: " + strError;
                        goto ERROR1;
                    }

                    // 在CfgDom中增加相关的配置信息
                    XmlNode root = this.LibraryCfgDom.DocumentElement.SelectSingleNode("readerdbgroup");
                    if (root == null)
                    {
                        root = this.LibraryCfgDom.CreateElement("readerdbgroup");
                        this.LibraryCfgDom.DocumentElement.AppendChild(root);
                    }

                    XmlElement nodeNewDatabase = null;
                    if (bRecreate == false)
                    {
                        nodeNewDatabase = this.LibraryCfgDom.CreateElement("database");
                        root.AppendChild(nodeNewDatabase);
                    }
                    else
                    {
                        nodeNewDatabase = exist_database_node;
                    }

#if NO
                    DomUtil.SetAttr(nodeNewDatabase, "name", strName);
                    DomUtil.SetAttr(nodeNewDatabase, "inCirculation", strInCirculation);
                    DomUtil.SetAttr(nodeNewDatabase, "libraryCode", strLibraryCode);    // 2012/9/7
#endif
                    info.WriteReaderCfgNode(nodeNewDatabase);

                    // <readerdbgroup>内容更新，刷新配套的内存结构
                    this.LoadReaderDbGroupParam(this.LibraryCfgDom);
                    this.Changed = true;

                    // 在重新创建时，记载下全部参数。这样可以避免日志恢复时候依赖当时的 library.xml 中的内容
                    if (bRecreate)
                        info.FillRequestNode(request_node);

                    database_nodes.Add(request_node);
                }
                #endregion

                else if (strType == "publisher"
                    || strType == "zhongcihao"
                    || strType == "dictionary"
                    || strType == "inventory")
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许创建或重新创建出版者库、种次号库、字典库和盘点库";
                        goto ERROR1;
                    }

                    // 看看同名的 publisher/zhongcihao/dictionary/inventory 数据库是否已经存在?
                    XmlElement exist_database_node = this.LibraryCfgDom.DocumentElement.SelectSingleNode("utilDb/database[@name='" + strName + "']") as XmlElement;
                    if (bRecreate == false)
                    {
                        if (exist_database_node != null)
                        {
                            strError = strType + "库 '" + strName + "' 的定义已经存在，不能重复创建";
                            goto ERROR1;
                        }
                    }
                    else
                    {
                        if (exist_database_node == null)
                        {
                            strError = strType + "库 '" + strName + "' 的定义并不存在，无法进行重复创建";
                            return 0;
                        }
                    }

                    RequestBiblioDatabase info = null;

                    if (strInfo == "*" || strInfo == "existing")    // 使用已经存在的 database 定义
                        info = RequestBiblioDatabase.FromUtilityCfgNode(exist_database_node);
                    else
                        info = RequestBiblioDatabase.FromRequest(request_node);

                    if (info.Type != strType)
                    {
                        strError = strType + "库 '" + strName + "' 的现有类型 '" + info.Type + "' 和参数 strType '" + strType + "' 不符";
                        goto ERROR1;
                    }

                    // TODO: 是否限定publisher库只能创建一个？
                    // 而zhongcihao库显然是可以创建多个的

                    // 检查dp2kernel中是否有和 publisher/zhongcihao/dictionary/inventory 库同名的数据库存在
                    {
                        // 数据库是否已经存在？
                        // return:
                        //      -1  error
                        //      0   not exist
                        //      1   exist
                        //      2   其他类型的同名对象已经存在
                        nRet = DatabaseUtility.IsDatabaseExist(
                            channel,
                            strName,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (nRet >= 1)
                            goto ERROR1;
                    }

                    string strTemplateDir = this.DataDir + "\\templates\\" + strType;

                    // 根据预先的定义，创建一个数据库
                    nRet = CreateDatabase(channel,
                        strTemplateDir,
                        strName,
                        strLogFileName,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    created_dbnames.Add(strName);
                    bDbChanged = true;  // 2012/12/12

                    // 在CfgDom中增加相关的配置信息
                    XmlNode root = this.LibraryCfgDom.DocumentElement.SelectSingleNode("utilDb");
                    if (root == null)
                    {
                        root = this.LibraryCfgDom.CreateElement("utilDb");
                        this.LibraryCfgDom.DocumentElement.AppendChild(root);
                    }

                    XmlElement nodeNewDatabase = null;
                    if (bRecreate == false)
                    {
                        nodeNewDatabase = this.LibraryCfgDom.CreateElement("database");
                        root.AppendChild(nodeNewDatabase);
                    }
                    else
                    {
                        nodeNewDatabase = exist_database_node;
                    }

                    info.WriteUtilityCfgNode(nodeNewDatabase);

#if NO
                    DomUtil.SetAttr(nodeNewDatabase, "name", strName);
                    DomUtil.SetAttr(nodeNewDatabase, "type", strType);
#endif
                    this.Changed = true;
                    database_nodes.Add(request_node);
                }
                else if (strType == "arrived"
                    || strType == "amerce"
                    || strType == "message"
                    || strType == "invoice"
                    || strType == "pinyin"
                    || strType == "gcat"
                    || strType == "word")
                {
                    string strTypeCaption = GetTypeCaption(strType);

                    if (strType == "test")
                    {

                    }
                    else
                    {
                        if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                        {
                            strError = "当前用户不是全局用户，不允许创建或重新创建" + strTypeCaption + "库";
                            goto ERROR1;
                        }
                    }

                    string strCurrentDbName = this.GetSingleDbName(strType);

#if NO
                    // 看看同名的 arrived 数据库是否已经存在?
                    if (bRecreate == false)
                    {
                        if (strCurrentDbName == strName)
                        {
                            strError = strTypeCaption + "库 '" + strName + "' 的定义已经存在，不能重复创建";
                            goto ERROR1;
                        }
                    }

                    if (String.IsNullOrEmpty(strCurrentDbName) == false)
                    {
                        if (bRecreate == true)
                        {
                            if (strCurrentDbName != strName)
                            {
                                strError = "已经存在一个" + strTypeCaption + "库 '" + strCurrentDbName + "' 定义，和您请求重新创建的" + strTypeCaption + "库 '" + strName + "' 名字不同。无法直接进行重新创建。请先删除已存在的数据库再进行创建";
                                goto ERROR1;
                            }
                        }
                        else
                        {
                            strError = "要创建新的" + strTypeCaption + "库 '" + strName + "'，必须先删除已经存在的" + strTypeCaption + "库 '"
                                + strCurrentDbName + "'";
                            goto ERROR1;
                        }
                    }

                    // 检查dp2kernel中是否有和 即将创建的 库同名的数据库存在
                    {
                        // 数据库是否已经存在？
                        // return:
                        //      -1  error
                        //      0   not exist
                        //      1   exist
                        //      2   其他类型的同名对象已经存在
                        nRet = DatabaseUtility.IsDatabaseExist(
                            channel,
                            strName,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (nRet >= 1)
                            goto ERROR1;
                    }

                    string strTemplateDir = this.DataDir + "\\templates\\" + strType;   //  "arrived";

                    // 根据预先的定义，创建一个数据库
                    nRet = CreateDatabase(channel,
                        strTemplateDir,
                        strName,
                         strLogFileName,
                       out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    created_dbnames.Add(strName);
                    bDbChanged = true;  // 2012/12/12

                    // 在CfgDom中增加相关的配置信息
                    SetSingleDbName(strType, strName);
                    this.Changed = true;
#endif
                    nRet = CreateDatabase(
    channel,
    strType,
    strTypeCaption,
    strName,
    strLibraryCodeList,
    bRecreate,
    strLogFileName,
    ref bDbChanged,
    ref created_dbnames,
    ref strCurrentDbName,
    out strError);
                    if (nRet == -1)
                        goto ERROR1;

                    ChangeSingleDbName(strType, strName);

                    database_nodes.Add(request_node);
                }
#if NO
                else if (strType == "amerce")
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许创建或重新创建违约金库";
                        goto ERROR1;
                    }

                    // 看看同名的amerce数据库是否已经存在?
                    if (bRecreate == false)
                    {
                        if (this.AmerceDbName == strName)
                        {
                            strError = "违约金库 '" + strName + "' 的定义已经存在，不能重复创建";
                            goto ERROR1;
                        }
                    }

                    if (String.IsNullOrEmpty(this.AmerceDbName) == false)
                    {
                        if (bRecreate == true)
                        {
                            if (this.AmerceDbName != strName)
                            {
                                strError = "已经存在一个违约金库 '" + this.AmerceDbName + "' 定义，和您请求重新创建的违约金库 '" + strName + "' 名字不同。无法直接进行重新创建。请先删除已存在的数据库再进行创建";
                                goto ERROR1;
                            }
                        }
                        else
                        {
                            strError = "要创建新的违约金库 '" + strName + "'，必须先删除已经存在的违约金库 '"
                                + this.AmerceDbName + "'";
                            goto ERROR1;
                        }
                    }

                    // 检查dp2kernel中是否有和amerce库同名的数据库存在
                    {
                        // 数据库是否已经存在？
                        // return:
                        //      -1  error
                        //      0   not exist
                        //      1   exist
                        //      2   其他类型的同名对象已经存在
                        nRet = DatabaseUtility.IsDatabaseExist(
                            channel,
                            strName,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (nRet >= 1)
                            goto ERROR1;
                    }

                    string strTemplateDir = this.DataDir + "\\templates\\" + "amerce";

                    // 根据预先的定义，创建一个数据库
                    nRet = CreateDatabase(channel,
                        strTemplateDir,
                        strName,
                        strLogFileName,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    created_dbnames.Add(strName);
                    bDbChanged = true;  // 2012/12/12

                    // 在CfgDom中增加相关的配置信息
                    this.AmerceDbName = strName;
                    this.Changed = true;
                    database_nodes.Add(request_node);
                }
#endif
#if NO
                else if (strType == "message")
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许创建或重新创建消息库";
                        goto ERROR1;
                    }

                    // 看看同名的message数据库是否已经存在?
                    if (bRecreate == false)
                    {
                        if (this.MessageDbName == strName)
                        {
                            strError = "消息库 '" + strName + "' 的定义已经存在，不能重复创建";
                            goto ERROR1;
                        }
                    }

                    if (String.IsNullOrEmpty(this.MessageDbName) == false)
                    {
                        if (bRecreate == true)
                        {
                            if (this.MessageDbName != strName)
                            {
                                strError = "已经存在一个消息库 '" + this.MessageDbName + "' 定义，和您请求重新创建的消息库 '" + strName + "' 名字不同。无法直接进行重新创建。请先删除已存在的数据库再进行创建";
                                goto ERROR1;
                            }
                        }
                        else
                        {
                            strError = "要创建新的消息库 '" + strName + "'，必须先删除已经存在的消息库 '"
                                + this.MessageDbName + "'";
                            goto ERROR1;
                        }
                    }

                    // 检查dp2kernel中是否有和message库同名的数据库存在
                    {
                        // 数据库是否已经存在？
                        // return:
                        //      -1  error
                        //      0   not exist
                        //      1   exist
                        //      2   其他类型的同名对象已经存在
                        nRet = DatabaseUtility.IsDatabaseExist(
                            channel,
                            strName,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (nRet >= 1)
                            goto ERROR1;
                    }

                    string strTemplateDir = this.DataDir + "\\templates\\" + "message";

                    // 根据预先的定义，创建一个数据库
                    nRet = CreateDatabase(channel,
                        strTemplateDir,
                        strName,
                        strLogFileName,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    created_dbnames.Add(strName);
                    bDbChanged = true;  // 2012/12/12

                    // 在CfgDom中增加相关的配置信息
                    this.MessageDbName = strName;
                    this.Changed = true;
                    database_nodes.Add(request_node);
                }
#endif
#if NO
                else if (strType == "invoice")
                {
                    if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                    {
                        strError = "当前用户不是全局用户，不允许创建或重新创建发票库";
                        goto ERROR1;
                    }

                    // 看看同名的invoice数据库是否已经存在?
                    if (bRecreate == false)
                    {
                        if (this.InvoiceDbName == strName)
                        {
                            strError = "发票库 '" + strName + "' 的定义已经存在，不能重复创建";
                            goto ERROR1;
                        }
                    }

                    if (String.IsNullOrEmpty(this.InvoiceDbName) == false)
                    {
                        if (bRecreate == true)
                        {
                            if (this.InvoiceDbName != strName)
                            {
                                strError = "已经存在一个发票库 '" + this.InvoiceDbName + "' 定义，和您请求重新创建的发票库 '" + strName + "' 名字不同。无法直接进行重新创建。请先删除已存在的数据库再进行创建";
                                goto ERROR1;
                            }
                        }
                        else
                        {
                            strError = "要创建新的发票库 '" + strName + "'，必须先删除已经存在的发票库 '"
                                + this.InvoiceDbName + "'";
                            goto ERROR1;
                        }
                    }

                    // 检查dp2kernel中是否有和invoice库同名的数据库存在
                    {
                        // 数据库是否已经存在？
                        // return:
                        //      -1  error
                        //      0   not exist
                        //      1   exist
                        //      2   其他类型的同名对象已经存在
                        nRet = DatabaseUtility.IsDatabaseExist(
                            channel,
                            strName,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                        if (nRet >= 1)
                            goto ERROR1;
                    }

                    string strTemplateDir = this.DataDir + "\\templates\\" + "invoice";

                    // 根据预先的定义，创建一个数据库
                    nRet = CreateDatabase(channel,
                        strTemplateDir,
                        strName,
                        strLogFileName,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                    created_dbnames.Add(strName);
                    bDbChanged = true;  // 2012/12/12

                    // 在CfgDom中增加相关的配置信息
                    this.InvoiceDbName = strName;
                    this.Changed = true;
                    database_nodes.Add(request_node);
                }
#endif
#if NO
                else if (strType == "pinyin"
                    || strType == "gcat"
                    || strType == "word")
                {
                    string strTypeCaption = "";
                    string strDbName = "";
                    if (strType == "pinyin")
                    {
                        strTypeCaption = "拼音";
                        strDbName = this.PinyinDbName;
                    }
                    if (strType == "gcat")
                    {
                        strTypeCaption = "著者号码";
                        strDbName = this.GcatDbName;
                    }
                    if (strType == "word")
                    {
                        strTypeCaption = "词";
                        strDbName = this.WordDbName;
                    }
                    nRet = CreateDatabase(
                        channel,
                        strType,
                        strTypeCaption,
                        strName,
                        strLibraryCodeList,
                        bRecreate,
                        strLogFileName,
                        ref bDbChanged,
                        ref created_dbnames,
                        ref strDbName,
                        out strError);
                    if (strType == "pinyin")
                        this.PinyinDbName = strDbName;
                    if (strType == "gcat")
                        this.GcatDbName = strDbName;
                    if (strType == "word")
                        this.WordDbName = strDbName;
                    if (nRet == -1)
                        goto ERROR1;
                    database_nodes.Add(request_node);
                }
#endif
                else
                {
                    strError = "未知的数据库类型 '" + strType + "'";
                    goto ERROR1;
                }

                if (this.Changed == true)
                    this.ActivateManagerThread();

                created_dbnames.Clear();
            }

            // 写入操作日志
            {
                XmlDocument domOperLog = new XmlDocument();
                domOperLog.LoadXml("<root />");

                DomUtil.SetElementText(domOperLog.DocumentElement,
                    "operation",
                    "manageDatabase");
                DomUtil.SetElementText(domOperLog.DocumentElement,
    "action",
    "createDatabase");

                XmlNode new_node = DomUtil.SetElementText(domOperLog.DocumentElement, "databases", "");
                StringBuilder text = new StringBuilder();
                foreach (XmlElement node in database_nodes)
                {
                    text.Append(node.OuterXml);
                }
                new_node.InnerXml = text.ToString();

                DomUtil.SetElementText(domOperLog.DocumentElement, "operator",
                    sessioninfo.UserID);

                string strOperTime = this.Clock.GetClock();

                DomUtil.SetElementText(domOperLog.DocumentElement, "operTime",
                    strOperTime);

                if (File.Exists(strLogFileName))
                {
                    using (Stream stream = File.OpenRead(strLogFileName))
                    {
                        nRet = this.OperLog.WriteOperLog(domOperLog,
                            sessioninfo.ClientAddress,
                            stream,
                            out strError);
                        if (nRet == -1)
                        {
                            strError = "ManageDatabase() API createDatabase 写入日志时发生错误: " + strError;
                            goto ERROR1;
                        }
                    }
                }
                else
                {
                    // 2017/8/11 增加
                    nRet = this.OperLog.WriteOperLog(domOperLog,
    sessioninfo.ClientAddress,
    out strError);
                }
            }

            Debug.Assert(created_dbnames.Count == 0, "");

            if (this.Changed == true)
                this.ActivateManagerThread();

            if (bDbChanged == true)
            {
                nRet = InitialKdbs(
                    Channels,
                    out strError);
                if (nRet == -1)
                    return -1;
                // 重新初始化虚拟库定义
                this.vdbs = null;
                nRet = this.InitialVdbs(Channels,
                    out strError);
                if (nRet == -1)
                    return -1;
            }

            return 1;
        ERROR1:
            List<string> error_deleting_dbnames = new List<string>();
            // 将本次已经创建的数据库在返回前删除掉
            for (int i = 0; i < created_dbnames.Count; i++)
            {
                string strDbName = created_dbnames[i];

                string strError_1 = "";

                long lRet = channel.DoDeleteDB(strDbName, out strError_1);
                if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    continue;
                if (lRet == -1)
                    error_deleting_dbnames.Add(strDbName + "[错误:" + strError_1 + "]");
            }

            if (error_deleting_dbnames.Count > 0)
            {
                strError = strError + ";\r\n并在删除刚创建的数据库时发生错误，下列数据库未被删除:" + StringUtil.MakePathList(error_deleting_dbnames);
                return -1;
            }

            return -1;
        }

        // 为 library.xml itemdbgroup 下删除 database 元素
        // 注：来自日志记录中的 database 元素，其 name 属性表明了要删除的数据库名。这个名字可能是书目库名字(代表了若干下属库)，也可能是一个实体库名字(这时就只删除这一个数据库即可)
        // parameters:
        //      elements    日志记录中的 database 元素节点数组
        // return:
        //      -1  出错
        //      0   没有发生修改
        //      1   发生了修改
        public static int DeleteDatabaseElement(XmlDocument cfg_dom,
            XmlNodeList elements,
            out string strError)
        {
            strError = "";

            if (cfg_dom.DocumentElement == null)
            {
                strError = "cfg_dom 缺乏根元素";
                return -1;
            }

            XmlElement container = cfg_dom.DocumentElement.SelectSingleNode("itemdbgroup") as XmlElement;
            if (container == null)
                return 0;

            bool bChanged = false;
            foreach (XmlElement source in elements)
            {
                string name = source.GetAttribute("name");
                if (string.IsNullOrEmpty(name))
                    continue;

                // 看看是不是书目库名
                XmlElement database = container.SelectSingleNode("database[@biblioDbName='" + name + "']") as XmlElement;
                if (database != null)
                {
                    database.ParentNode.RemoveChild(database);
                    bChanged = true;
                    continue;
                }

                // 是实体库、订购库、期库、评注库名么?
                XmlAttribute attr = container.SelectSingleNode("database[@name='" + name + "' OR @orderDbName='" + name + "' OR @issueDbName='" + name + "' OR @commentDbName='" + name + "']") as XmlAttribute;
                if (attr != null)
                {
                    attr.ParentNode.RemoveChild(attr);
                    bChanged = true;
                    continue;
                }

                // 是其他类型的数据库

            }

            if (bChanged)
                return 1;
            return 0;
        }

        // 确保在 root 下具有名为 element_name 的元素。如果没有，则自动创建一个
        public static XmlElement EnsureElement(XmlElement root,
            string element_name,
            ref bool bChanged)
        {
            XmlElement result = root.SelectSingleNode(element_name) as XmlElement;
            if (result == null)
            {
                result = root.OwnerDocument.CreateElement(element_name);
                root.AppendChild(result);
                bChanged = true;
            }

            return result;
        }

        // 为 library.xml itemdbgroup 下追加 database 元素
        // parameters:
        //      elements    日志记录中的 database 元素节点数组
        // return:
        //      -1  出错
        //      0   没有发生修改
        //      1   发生了修改
        public int AppendDatabaseElement(XmlDocument cfg_dom,
            XmlNodeList elements,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (cfg_dom.DocumentElement == null)
            {
                strError = "cfg_dom 缺乏根元素";
                return -1;
            }

            bool bChanged = false;

            foreach (XmlElement source in elements)
            {
                string strType = source.GetAttribute("type");

                RequestBiblioDatabase info = RequestBiblioDatabase.FromRequest(source);

                if (strType == "biblio")
                {
                    XmlElement itemdbgroup = EnsureElement(cfg_dom.DocumentElement, "itemdbgroup", ref bChanged);

                    // 以前可能存在 database 元素，需要覆盖
                    XmlElement database = itemdbgroup.SelectSingleNode("database[@biblioDbName='" + info.Name + "']") as XmlElement;

                    if (database == null)
                    {
                        database = cfg_dom.CreateElement("database");
                        itemdbgroup.AppendChild(database);
                    }

                    info.WriteBiblioCfgNode(database);

                    // <itemdbgroup>内容更新，刷新配套的内存结构
                    nRet = this.LoadItemDbGroupParam(cfg_dom,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    bChanged = true;
                    continue;
                }

                // 零星创建 entity issue order comment 库
                if (strType == "entity"
                    || strType == "order"
                    || strType == "issue"
                    || strType == "comment")
                {
                    if (string.IsNullOrEmpty(info.OwnerDbName))
                    {
                        strError = "日志记录元素 database 中缺乏 biblioDbName 属性。无法定位 library.xml 中 database 元素和修改";
                        return -1;
                    }

                    XmlElement itemdbgroup = EnsureElement(cfg_dom.DocumentElement, "itemdbgroup", ref bChanged);

                    // 根据 biblioDbName 属性找到 database 元素
                    XmlElement database = itemdbgroup.SelectSingleNode("database[@biblioDbName='" + info.OwnerDbName + "']") as XmlElement;
                    if (database == null)
                    {
                        strError = "试图修改 library.xml 中 itemdbgroup 元素下 database 元素时，没有找到属性 biblioDbName 为 '" + info.OwnerDbName + "' 的元素";
                        return -1;
                    }
                    info.WritePartBiblioCfgNode(database);

                    // <itemdbgroup>内容更新，刷新配套的内存结构
                    nRet = this.LoadItemDbGroupParam(this.LibraryCfgDom,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    bChanged = true;
                    continue;
                }

                if (strType == "reader")
                {
                    XmlElement readerdbgroup = EnsureElement(cfg_dom.DocumentElement, "readerdbgroup", ref bChanged);

                    XmlElement database = cfg_dom.CreateElement("database");
                    readerdbgroup.AppendChild(database);

                    info.WriteReaderCfgNode(database);

                    nRet = this.LoadReaderDbGroupParam(this.LibraryCfgDom);
                    if (nRet == -1)
                        return -1;

                    bChanged = true;
                    continue;
                }

                if (strType == "publisher"
                    || strType == "zhongcihao"
                    || strType == "dictionary"
                    || strType == "inventory")
                {
                    XmlElement utilDb = EnsureElement(cfg_dom.DocumentElement, "utilDb", ref bChanged);

                    XmlElement database = cfg_dom.CreateElement("database");
                    utilDb.AppendChild(database);

                    info.WriteUtilityCfgNode(database);
                    bChanged = true;
                    continue;
                }

                if (IsSingleDbType(strType))
                {
                    ChangeSingleDbName(strType, info.Name);

                    this.Changed = true;
                    bChanged = true;
                    continue;
                }

                strError = "(类型 '" + strType + "')无法处理的请求元素 " + source.OuterXml;
                return -1;
            }

            // 其他类型的数据库

            if (bChanged)
                return 1;
            return 0;
        }

        // 修改内存中那些单个数据库的名字。
        // return:
        //      返回修改前的数据库名
        string ChangeSingleDbName(string strType, string strDbName)
        {
            string strOldDbName = GetSingleDbName(strType);
            switch (strType)
            {
                case "arrived":
                    this.ArrivedDbName = strDbName;
                    break;
                case "amerce":
                    this.AmerceDbName = strDbName;
                    break;
                case "message":
                    this.MessageDbName = strDbName;
                    break;
                case "invoice":
                    this.InvoiceDbName = strDbName;
                    break;
                case "pinyin":
                    this.PinyinDbName = strDbName;
                    break;
                case "gcat":
                    this.GcatDbName = strDbName;
                    break;
                case "word":
                    this.WordDbName = strDbName;
                    break;
                default:
                    throw new ArgumentException("未知的 strType 值 '" + strType + "'", "strType");
            }
            return strOldDbName;
        }

        string GetSingleDbName(string strType)
        {
            switch (strType)
            {
                case "arrived":
                    return this.ArrivedDbName;
                case "amerce":
                    return this.AmerceDbName;
                case "message":
                    return this.MessageDbName;
                case "invoice":
                    return this.InvoiceDbName;
                case "pinyin":
                    return this.PinyinDbName;
                case "gcat":
                    return this.GcatDbName;
                case "word":
                    return this.WordDbName;
                default:
                    throw new ArgumentException("未知的 strType 值 '" + strType + "'", "strType");
            }
        }

        // 是否为单个数据库类型?
        //  单个数据库是指那些 DbName 保存在内存变量中的数据库类型。(一般不要从 cfg_dom 中直接存取)
        static bool IsSingleDbType(string strType)
        {
            switch (strType)
            {
                case "arrived":
                case "amerce":
                case "message":
                case "invoice":
                case "pinyin":
                case "gcat":
                case "word":
                    return true;
                default:
                    return false;
            }
        }


        static string GetTypeCaption(string strType)
        {
            switch (strType)
            {
                case "arrived":
                    return "预约到书";
                case "amerce":
                    return "违约金";
                case "message":
                    return "消息";
                case "invoice":
                    return "发票";
                case "pinyin":
                    return "拼音";
                case "gcat":
                    return "著者号码";
                case "word":
                    return "词";
                case "biblio":
                    return "书目";
                case "entity":
                    return "实体";
                case "order":
                    return "订购";
                case "issue":
                    return "期";
                case "comment":
                    return "评注";
                default:
                    throw new ArgumentException("未知的 strType 值 '" + strType + "'", "strType");
            }
        }

        // 2016/11/20
        // paremeters:
        //      strName     拟创建的数据库名
        //      strDbName   [in]调用前要设置为当前已经存在的数据库名；
        //                  [out]调用后自动变成新创建的数据库名
        int CreateDatabase(
            RmsChannel channel,
            string strType,
            string strTypeCaption,
            string strName,
            string strLibraryCodeList,
            bool bRecreate,
            string strLogFileName,
            ref bool bDbChanged,
            ref List<string> created_dbnames,
            ref string strDbName,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
            {
                strError = "当前用户不是全局用户，不允许创建或重新创建" + strTypeCaption + "库";
                return -1;
            }

            // 看看同名的pinyin数据库是否已经存在?
            if (bRecreate == false)
            {
                if (strDbName == strName)
                {
                    strError = strTypeCaption + "库 '" + strName + "' 的定义已经存在，不能重复创建";
                    goto ERROR1;
                }
            }

            if (String.IsNullOrEmpty(strDbName) == false)
            {
                if (bRecreate == true)
                {
                    if (strDbName != strName)
                    {
                        strError = "已经存在一个" + strTypeCaption + "库 '" + strDbName + "' 定义，和您请求重新创建的" + strTypeCaption + "库 '" + strName + "' 名字不同。无法直接进行重新创建。请先删除已存在的数据库再进行创建";
                        goto ERROR1;
                    }
                }
                else
                {
                    strError = "要创建新的" + strTypeCaption + "库 '" + strName + "'，必须先删除已经存在的" + strTypeCaption + "库 '"
                        + strDbName + "'";
                    goto ERROR1;
                }
            }

            // 检查dp2kernel中是否有和xxx库同名的数据库存在
            {
                // 数据库是否已经存在？
                // return:
                //      -1  error
                //      0   not exist
                //      1   exist
                //      2   其他类型的同名对象已经存在
                nRet = DatabaseUtility.IsDatabaseExist(
                    channel,
                    strName,
                    out strError);
                if (nRet == -1)
                    goto ERROR1;
                if (nRet >= 1)
                    goto ERROR1;
            }

            string strTemplateDir = this.DataDir + "\\templates\\" + strType;

            // 根据预先的定义，创建一个数据库
            nRet = CreateDatabase(channel,
                strTemplateDir,
                strName,
                strLogFileName,
                out strError);
            if (nRet == -1)
                goto ERROR1;
            created_dbnames.Add(strName);
            bDbChanged = true;

            // 在CfgDom中增加相关的配置信息
            strDbName = strName;
            this.Changed = true;
            return 1;
        ERROR1:
            return -1;
        }

        static string ConvertCrLf(string strText)
        {
            strText = strText.Replace("\r\n", "\r");
            strText = strText.Replace("\n", "\r");
            return strText.Replace("\r", "\r\n");
        }

        // 根据数据库模板的定义，刷新一个已经存在的数据库的定义
        // parameters:
        //      bRecoverModeKeys    是否采用 keys_recover 来刷新 keys 配置文件
        // return:
        //      -1
        //      0   keys定义没有更新
        //      1   keys定义更新了
        int RefreshDatabase(RmsChannel channel,
            string strTemplateDir,
            string strDatabaseName,
            string strIncludeFilenames,
            string strExcludeFilenames,
            bool bRecoverModeKeys,
            string strLogFileName,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            string strTempDir = "";
            if (string.IsNullOrEmpty(strLogFileName) == false)
            {
                strTempDir = Path.Combine(this.TempDir, "~" + Guid.NewGuid().ToString());
                PathUtil.TryCreateDir(strTempDir);
            }

            try
            {
                int nCfgFileCount = 0;

                strIncludeFilenames = strIncludeFilenames.ToLower();
                strExcludeFilenames = strExcludeFilenames.ToLower();

                bool bKeysChanged = false;

                DirectoryInfo di = new DirectoryInfo(strTemplateDir);
                FileInfo[] fis = di.GetFiles();

                // 创建所有文件对象
                for (int i = 0; i < fis.Length; i++)
                {
                    string strName = fis[i].Name;
                    if (strName == "." || strName == "..")
                        continue;

                    if (FileUtil.IsBackupFile(strName) == true)
                        continue;

                    // 2015/9/28
                    if (strName.ToLower() == "keys_recover")
                        continue;


                    // 如果Include和exclude里面都有一个文件名，优先依exclude(排除)
                    if (StringUtil.IsInList(strName, strExcludeFilenames) == true)
                        continue;

                    if (strIncludeFilenames != "*")
                    {
                        if (StringUtil.IsInList(strName, strIncludeFilenames) == false)
                            continue;
                    }

                    string strFullPath = fis[i].FullName;

                    if (bRecoverModeKeys == true && strName == "keys")
                    {
                        string strFullPathRecover = Path.Combine(Path.GetDirectoryName(strFullPath), "keys_recover");
                        if (File.Exists(strFullPathRecover) == true)
                            strFullPath = strFullPathRecover;
                    }

                    nRet = DatabaseUtility.ConvertGb2312TextfileToUtf8(strFullPath,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    string strExistContent = "";
                    string strNewContent = "";

                    using (Stream new_stream = new FileStream(strFullPath, FileMode.Open))
                    {
                        using (StreamReader sr = new StreamReader(new_stream, Encoding.UTF8))
                        {
                            strNewContent = ConvertCrLf(sr.ReadToEnd());
                        }
                    }

#if NO
                using (Stream new_stream = new FileStream(strFullPath, FileMode.Open))
                {
                    new_stream.Seek(0, SeekOrigin.Begin);
#endif

                    string strPath = strDatabaseName + "/cfgs/" + strName;

                    // 获取已有的配置文件对象
                    byte[] timestamp = null;
                    string strOutputPath = "";
                    string strMetaData = "";

                    string strStyle = "content,data,metadata,timestamp,outputpath";
                    using (MemoryStream exist_stream = new MemoryStream())
                    {
                        long lRet = channel.GetRes(
                            strPath,
                            exist_stream,
                            null,	// stop,
                            strStyle,
                            null,	// byte [] input_timestamp,
                            out strMetaData,
                            out timestamp,
                            out strOutputPath,
                            out strError);
                        if (lRet == -1)
                        {
                            // 配置文件不存在，怎么返回错误码的?
                            if (channel.ErrorCode == ChannelErrorCode.NotFound)
                            {
                                timestamp = null;
                                goto DO_CREATE;
                            }
                            return -1;
                        }

                        exist_stream.Seek(0, SeekOrigin.Begin);
                        using (StreamReader sr = new StreamReader(exist_stream, Encoding.UTF8))
                        {
                            strExistContent = ConvertCrLf(sr.ReadToEnd());
                        }

                        // 比较本地的和服务器的有无区别，无区别就不要上载了
                        if (strExistContent == strNewContent)
                            continue;

                        // 保存修改前的配置文件
                        if (string.IsNullOrEmpty(strTempDir) == false)
                        {
                            string strTargetFilePath = Path.Combine(strTempDir, strDatabaseName, "old_cfgs\\" + strName);
                            PathUtil.TryCreateDir(Path.GetDirectoryName(strTargetFilePath));

                            // 注: exist_stream 此时已经被关闭了
                            //exist_stream.Seek(0, SeekOrigin.Begin);
                            using (Stream target_stream = File.Create(strTargetFilePath))
                            {
                                //StreamUtil.DumpStream(exist_stream, target_stream);
                                byte[] buffer = Encoding.UTF8.GetBytes(strExistContent);
                                target_stream.Write(buffer, 0, buffer.Length);
                            }
                            nCfgFileCount++;
                        }
                    }

#if NO
                    // 比较本地的和服务器的有无区别，无区别就不要上载了
                    if (strExistContent == strNewContent)
                        continue;
#endif

                DO_CREATE:
                    using (Stream new_stream = new FileStream(strFullPath, FileMode.Open))
                    {
                        new_stream.Seek(0, SeekOrigin.Begin);

                        // 在服务器端创建对象
                        // parameters:
                        //      strStyle    风格。当创建目录的时候，为"createdir"，否则为空
                        // return:
                        //		-1	错误
                        //		1	已经存在同名对象
                        //		0	正常返回
                        nRet = DatabaseUtility.NewServerSideObject(
                            channel,
                            strPath,
                            "",
                            new_stream,
                            timestamp,
                            out strError);
                        if (nRet == -1)
                            return -1;
                        if (nRet == 1)
                        {
                            strError = "NewServerSideObject()发现已经存在同名对象: " + strError;
                            return -1;
                        }

                        if (strName.ToLower() == "keys")
                            bKeysChanged = true;
                    }

                    // 保存修改后的配置文件
                    if (string.IsNullOrEmpty(strTempDir) == false)
                    {
                        string strNewTargetFilePath = Path.Combine(strTempDir, strDatabaseName, "cfgs\\" + strName);
                        PathUtil.TryCreateDir(Path.GetDirectoryName(strNewTargetFilePath));
                        File.Copy(strFullPath, strNewTargetFilePath);
                        nCfgFileCount++;
                    }
                }

                if (nCfgFileCount > 0)
                {
                    nRet = CompressDirectory(
        strTempDir,
        strTempDir,
        strLogFileName,
        Encoding.UTF8,
        true,
        out strError);
                    if (nRet == -1)
                        return -1;
                }

                if (bKeysChanged == true)
                {
                    // 对数据库及时调用刷新keys表的API
                    long lRet = channel.DoRefreshDB(
                        "begin",
                        strDatabaseName,
                        false,
                        out strError);
                    if (lRet == -1)
                    {
                        strError = "数据库 '" + strDatabaseName + "' 的定义已经被成功刷新，但在刷新内核Keys表操作时失败: " + strError;
                        return -1;
                    }
                    return 1;
                }

                return 0;
            }
            finally
            {
                if (string.IsNullOrEmpty(strTempDir) == false)
                    PathUtil.DeleteDirectory(strTempDir);
            }
        }

        // 压缩一个目录到 .zip 文件
        // parameters:
        //      strBase 在 .zip 文件中的文件名要从全路径中去掉的前面部分
        static int CompressDirectory(
            string strDirectory,
            string strBase,
            string strZipFileName,
            Encoding encoding,
            bool bAppend,
            out string strError)
        {
            strError = "";

            try
            {
                DirectoryInfo di = new DirectoryInfo(strDirectory);
                if (di.Exists == false)
                {
                    strError = "directory '" + strDirectory + "' not exist";
                    return -1;
                }
                strDirectory = di.FullName;
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                return -1;
            }

            if (bAppend == false)
            {
                if (File.Exists(strZipFileName) == true)
                {
                    try
                    {
                        File.Delete(strZipFileName);
                    }
                    catch (FileNotFoundException)
                    {
                    }
                    catch (DirectoryNotFoundException)
                    {

                    }
                }
            }

            List<string> filenames = GetFileNames(strDirectory);

            if (filenames.Count == 0)
                return 0;

            // string strHead = Path.GetDirectoryName(strDirectory);
            // Console.WriteLine("head=["+strHead+"]");

            using (ZipFile zip = new ZipFile(strZipFileName, encoding))
            {
                // http://stackoverflow.com/questions/15337186/dotnetzip-badreadexception-on-extract
                // https://dotnetzip.codeplex.com/workitem/14087
                // uncommenting the following line can be used as a work-around
                zip.ParallelDeflateThreshold = -1;

                foreach (string filename in filenames)
                {
                    // string strShortFileName = filename.Substring(strHead.Length + 1);
                    string strShortFileName = filename.Substring(strBase.Length + 1);
                    string directoryPathInArchive = Path.GetDirectoryName(strShortFileName);
                    zip.AddFile(filename, directoryPathInArchive);
                }

                zip.UseZip64WhenSaving = Zip64Option.AsNecessary;
                zip.Save(strZipFileName);
            }

            return filenames.Count;
        }

        // 获得一个目录下的全部文件名。包括子目录中的
        static List<string> GetFileNames(string strDataDir)
        {
            DirectoryInfo di = new DirectoryInfo(strDataDir);

            List<string> result = new List<string>();

            FileInfo[] fis = di.GetFiles();
            foreach (FileInfo fi in fis)
            {
                result.Add(fi.FullName);
            }

            // 处理下级目录，递归
            DirectoryInfo[] dis = di.GetDirectories();
            foreach (DirectoryInfo subdir in dis)
            {
                result.AddRange(GetFileNames(subdir.FullName));
            }

            return result;
        }

        // 根据数据库模板的定义，创建一个数据库
        // parameters:
        //      strLogFileName  日志文件路径。所谓日志文件，就是一个 .zip 文件，里面包含了创建数据库所需的定义和配套文件
        //                      如果此参数为空，表示不创建日志文件
        int CreateDatabase(RmsChannel channel,
            string strTemplateDir,
            string strDatabaseName,
            string strLogFileName,
            out string strError)
        {
            strError = "";

            int nRet = 0;

            string strTempDir = "";

            if (string.IsNullOrEmpty(strLogFileName) == false)
            {
                strTempDir = Path.Combine(this.TempDir, "~" + Guid.NewGuid().ToString());
                PathUtil.TryCreateDir(strTempDir);
            }

            try
            {
                nRet = DatabaseUtility.CreateDatabase(channel,
    strTemplateDir,
    strDatabaseName,
    strTempDir,
    out strError);
                if (nRet == -1)
                    return -1;

                if (string.IsNullOrEmpty(strLogFileName) == false)
                {
                    nRet = CompressDirectory(
                        strTempDir,
                        strTempDir,
                        strLogFileName,
                        Encoding.UTF8,
                        true,
                        out strError);
                    if (nRet == -1)
                        return -1;
                }

                return 0;
            }
            finally
            {
                if (string.IsNullOrEmpty(strTempDir) == false)
                    PathUtil.DeleteDirectory(strTempDir);
            }
        }

        void GetSubdirs(string strCurrentDir,
            ref List<string> results)
        {
            DirectoryInfo di = new DirectoryInfo(strCurrentDir + "\\");
            DirectoryInfo[] dia = di.GetDirectories("*.*");

            // Array.Sort(dia, new DirectoryInfoCompare());
            for (int i = 0; i < dia.Length; i++)
            {
                string strThis = dia[i].FullName;
                results.Add(strThis);
                GetSubdirs(strThis, ref results);
            }
        }

        // 获得数据库信息
        // parameters:
        //      strLibraryCodeList  当前用户的管辖分馆代码列表
        // return:
        //      -1  出错
        //      0   没有找到
        //      1   成功
        int GetDatabaseInfo(
            string strLibraryCodeList,
            string strDatabaseNames,
            out string strOutputInfo,
            out string strError)
        {
            strOutputInfo = "";
            strError = "";

            if (String.IsNullOrEmpty(strDatabaseNames) == true)
                strDatabaseNames = "#biblio,#reader,#arrived,#amerce,#invoice,#util,#message,#pinyin,#gcat,#word,#_accessLog,#_hitcount,#_chargingOper,#_biblioSummary";  // 注: #util 相当于 #zhongcihao,#publisher,#dictionary,#inventory

            // 用于构造返回结果字符串的DOM
            XmlDocument dom = new XmlDocument();
            dom.LoadXml("<root />");

            string[] names = strDatabaseNames.Split(new char[] { ',' });
            for (int i = 0; i < names.Length; i++)
            {
            CONTINUE:
                string strName = names[i].Trim();
                if (String.IsNullOrEmpty(strName) == true)
                    continue;

                // 特殊名字
                if (strName[0] == '#')
                {
                    if (strName == "#reader")
                    {
                        // 读者库
                        for (int j = 0; j < this.ReaderDbs.Count; j++)
                        {
                            string strDbName = this.ReaderDbs[j].DbName;
                            string strLibraryCode = this.ReaderDbs[j].LibraryCode;

                            // 2012/9/9
                            // 只允许当前用户看到自己管辖的读者库
                            if (SessionInfo.IsGlobalUser(strLibraryCodeList) == false)
                            {
                                if (string.IsNullOrEmpty(strLibraryCode) == true
                                    || StringUtil.IsInList(strLibraryCode, strLibraryCodeList) == false)
                                    continue;
                            }

                            XmlNode nodeDatabase = dom.CreateElement("database");
                            dom.DocumentElement.AppendChild(nodeDatabase);

                            DomUtil.SetAttr(nodeDatabase, "type", "reader");
                            DomUtil.SetAttr(nodeDatabase, "name", strDbName);
                            string strInCirculation = this.ReaderDbs[j].InCirculation == true ? "true" : "false";
                            DomUtil.SetAttr(nodeDatabase, "inCirculation", strInCirculation);

                            DomUtil.SetAttr(nodeDatabase, "libraryCode", strLibraryCode);
                        }
                    }
                    else if (strName == "#biblio")
                    {
                        // 实体库(书目库)
                        for (int j = 0; j < this.ItemDbs.Count; j++)
                        {
                            XmlNode nodeDatabase = dom.CreateElement("database");
                            dom.DocumentElement.AppendChild(nodeDatabase);

                            ItemDbCfg cfg = this.ItemDbs[j];

                            DomUtil.SetAttr(nodeDatabase, "type", "biblio");
                            DomUtil.SetAttr(nodeDatabase, "name", cfg.BiblioDbName);
                            DomUtil.SetAttr(nodeDatabase, "syntax", cfg.BiblioDbSyntax);
                            DomUtil.SetAttr(nodeDatabase, "entityDbName", cfg.DbName);
                            DomUtil.SetAttr(nodeDatabase, "orderDbName", cfg.OrderDbName);
                            DomUtil.SetAttr(nodeDatabase, "issueDbName", cfg.IssueDbName);
                            DomUtil.SetAttr(nodeDatabase, "commentDbName", cfg.CommentDbName);
                            DomUtil.SetAttr(nodeDatabase, "unionCatalogStyle", cfg.UnionCatalogStyle);
                            string strInCirculation = cfg.InCirculation == true ? "true" : "false";
                            DomUtil.SetAttr(nodeDatabase, "inCirculation", strInCirculation);
                            DomUtil.SetAttr(nodeDatabase, "role", cfg.Role);    // 2009/10/23
                            DomUtil.SetAttr(nodeDatabase, "replication", cfg.Replication);    // 2009/10/23
                            /*
                            DomUtil.SetAttr(nodeDatabase, "biblioDbName", cfg.BiblioDbName);
                            DomUtil.SetAttr(nodeDatabase, "itemDbName", cfg.DbName);
                             * */
                        }
                    }
                    else if (strName == "#arrived")
                    {
                        if (String.IsNullOrEmpty(this.ArrivedDbName) == true)
                            continue;

                        XmlNode nodeDatabase = dom.CreateElement("database");
                        dom.DocumentElement.AppendChild(nodeDatabase);

                        DomUtil.SetAttr(nodeDatabase, "type", "arrived");
                        DomUtil.SetAttr(nodeDatabase, "name", this.ArrivedDbName);
                    }
                    else if (strName == "#amerce")
                    {
                        if (String.IsNullOrEmpty(this.AmerceDbName) == true)
                            continue;

                        XmlNode nodeDatabase = dom.CreateElement("database");
                        dom.DocumentElement.AppendChild(nodeDatabase);

                        DomUtil.SetAttr(nodeDatabase, "type", "amerce");
                        DomUtil.SetAttr(nodeDatabase, "name", this.AmerceDbName);
                    }
                    else if (strName == "#invoice")
                    {
                        if (String.IsNullOrEmpty(this.InvoiceDbName) == true)
                            continue;

                        XmlNode nodeDatabase = dom.CreateElement("database");
                        dom.DocumentElement.AppendChild(nodeDatabase);

                        DomUtil.SetAttr(nodeDatabase, "type", "invoice");
                        DomUtil.SetAttr(nodeDatabase, "name", this.InvoiceDbName);
                    }
                    else if (strName == "#message")
                    {
                        if (String.IsNullOrEmpty(this.MessageDbName) == true)
                            continue;

                        XmlNode nodeDatabase = dom.CreateElement("database");
                        dom.DocumentElement.AppendChild(nodeDatabase);

                        DomUtil.SetAttr(nodeDatabase, "type", "message");
                        DomUtil.SetAttr(nodeDatabase, "name", this.MessageDbName);
                    }
                    else if (strName == "#pinyin"
                        || strName == "#gcat"
                        || strName == "#word")
                    {
                        string strDbName = "";
                        if (strName == "#pinyin")
                            strDbName = this.PinyinDbName;
                        if (strName == "#gcat")
                            strDbName = this.GcatDbName;
                        if (strName == "#word")
                            strDbName = this.WordDbName;

                        if (String.IsNullOrEmpty(strDbName) == true)
                            continue;

                        XmlNode nodeDatabase = dom.CreateElement("database");
                        dom.DocumentElement.AppendChild(nodeDatabase);

                        DomUtil.SetAttr(nodeDatabase, "type", strName.Substring(1));
                        DomUtil.SetAttr(nodeDatabase, "name", strDbName);
                    }
                    else if (strName == "#util"
                        || strName == "#publisher"
                        || strName == "#zhongcihao"
                        || strName == "#dictionary"
                        || strName == "#inventory")
                    {
                        string strType = "";
                        if (strName != "#util")
                            strType = strName.Substring(1);
                        XmlNodeList nodes = null;
                        if (string.IsNullOrEmpty(strType) == true)
                            nodes = this.LibraryCfgDom.DocumentElement.SelectNodes("utilDb/database");
                        else
                            nodes = this.LibraryCfgDom.DocumentElement.SelectNodes("utilDb/database[@type='" + strType + "']");
                        foreach (XmlNode node in nodes)
                        {
                            XmlNode nodeDatabase = dom.CreateElement("database");
                            dom.DocumentElement.AppendChild(nodeDatabase);

                            DomUtil.SetAttr(nodeDatabase, "type", DomUtil.GetAttr(node, "type"));
                            DomUtil.SetAttr(nodeDatabase, "name", DomUtil.GetAttr(node, "name"));
                        }
                    }
#if NO
                    else if (strName == "#zhongcihao")
                    {
                        XmlNodeList nodes = this.LibraryCfgDom.DocumentElement.SelectNodes("utilDb/database[@type='zhongcihao']");
                        for (int j = 0; j < nodes.Count; j++)
                        {
                            XmlNode nodeDatabase = dom.CreateElement("database");
                            dom.DocumentElement.AppendChild(nodeDatabase);

                            DomUtil.SetAttr(nodeDatabase, "type", "zhongcihao");
                            string strTemp = DomUtil.GetAttr(nodes[j], "name");
                            DomUtil.SetAttr(nodeDatabase, "name", strTemp);
                        }
                    }
#endif
                    else if (strName == "#_accessLog")
                    {
                        // 2015/11/26
                        if (string.IsNullOrEmpty(this.MongoDbConnStr) == false
                            && this.AccessLogDatabase != null)
                        {
                            XmlNode nodeDatabase = dom.CreateElement("database");
                            dom.DocumentElement.AppendChild(nodeDatabase);

                            DomUtil.SetAttr(nodeDatabase, "type", "_accessLog");
                            DomUtil.SetAttr(nodeDatabase, "name", AccessLogDbName);
                        }
                    }
                    else if (strName == "#_hitcount")
                    {
                        // 2015/11/26
                        if (string.IsNullOrEmpty(this.MongoDbConnStr) == false
                            && this.AccessLogDatabase != null)
                        {
                            XmlNode nodeDatabase = dom.CreateElement("database");
                            dom.DocumentElement.AppendChild(nodeDatabase);

                            DomUtil.SetAttr(nodeDatabase, "type", "_hitcount");
                            DomUtil.SetAttr(nodeDatabase, "name", HitCountDbName);
                        }
                    }
                    else if (strName == "#_chargingOper")
                    {
                        // 2016/1/10
                        if (string.IsNullOrEmpty(this.MongoDbConnStr) == false
                            && this.ChargingOperDatabase != null)
                        {
                            XmlNode nodeDatabase = dom.CreateElement("database");
                            dom.DocumentElement.AppendChild(nodeDatabase);

                            DomUtil.SetAttr(nodeDatabase, "type", "_chargingOper");
                            DomUtil.SetAttr(nodeDatabase, "name", ChargingHistoryDbName);
                        }
                    }
                    else if (strName == "#_biblioSummary")
                    {
                        // 2016/8/30
                        if (string.IsNullOrEmpty(this.MongoDbConnStr) == false
                            && this.SummaryCollection != null)
                        {
                            XmlNode nodeDatabase = dom.CreateElement("database");
                            dom.DocumentElement.AppendChild(nodeDatabase);

                            DomUtil.SetAttr(nodeDatabase, "type", "_biblioSummary");
                            DomUtil.SetAttr(nodeDatabase, "name", BiblioSummaryDbName);
                        }
                    }
                    else
                    {
                        strError = "不可识别的数据库名 '" + strName + "'";
                        return 0;
                    }
                    continue;
                }

                // 普通名字

                // 是否为预约到书队列库？
                if (strName == this.ArrivedDbName)
                {
                    XmlNode nodeDatabase = dom.CreateElement("database");
                    dom.DocumentElement.AppendChild(nodeDatabase);

                    DomUtil.SetAttr(nodeDatabase, "type", "arrived");
                    DomUtil.SetAttr(nodeDatabase, "name", this.ArrivedDbName);
                    goto CONTINUE;
                }

                // 是否为违约金库?
                if (strName == this.AmerceDbName)
                {
                    XmlNode nodeDatabase = dom.CreateElement("database");
                    dom.DocumentElement.AppendChild(nodeDatabase);

                    DomUtil.SetAttr(nodeDatabase, "type", "amerce");
                    DomUtil.SetAttr(nodeDatabase, "name", this.AmerceDbName);
                    goto CONTINUE;
                }

                // 是否为发票库?
                if (strName == this.InvoiceDbName)
                {
                    XmlNode nodeDatabase = dom.CreateElement("database");
                    dom.DocumentElement.AppendChild(nodeDatabase);

                    DomUtil.SetAttr(nodeDatabase, "type", "invoice");
                    DomUtil.SetAttr(nodeDatabase, "name", this.InvoiceDbName);
                    goto CONTINUE;
                }

                // 是否为消息库?
                if (strName == this.MessageDbName)
                {
                    XmlNode nodeDatabase = dom.CreateElement("database");
                    dom.DocumentElement.AppendChild(nodeDatabase);

                    DomUtil.SetAttr(nodeDatabase, "type", "message");
                    DomUtil.SetAttr(nodeDatabase, "name", this.MessageDbName);
                    goto CONTINUE;
                }

                // 是否为拼音库?
                if (strName == this.PinyinDbName)
                {
                    XmlNode nodeDatabase = dom.CreateElement("database");
                    dom.DocumentElement.AppendChild(nodeDatabase);

                    DomUtil.SetAttr(nodeDatabase, "type", "pinyin");
                    DomUtil.SetAttr(nodeDatabase, "name", this.PinyinDbName);
                    goto CONTINUE;
                }

                // 是否为著者号码库?
                if (strName == this.GcatDbName)
                {
                    XmlNode nodeDatabase = dom.CreateElement("database");
                    dom.DocumentElement.AppendChild(nodeDatabase);

                    DomUtil.SetAttr(nodeDatabase, "type", "gcat");
                    DomUtil.SetAttr(nodeDatabase, "name", this.GcatDbName);
                    goto CONTINUE;
                }

                // 是否为词库?
                if (strName == this.WordDbName)
                {
                    XmlNode nodeDatabase = dom.CreateElement("database");
                    dom.DocumentElement.AppendChild(nodeDatabase);

                    DomUtil.SetAttr(nodeDatabase, "type", "word");
                    DomUtil.SetAttr(nodeDatabase, "name", this.WordDbName);
                    goto CONTINUE;
                }

                // 是否为读者库？
                for (int j = 0; j < this.ReaderDbs.Count; j++)
                {
                    string strDbName = this.ReaderDbs[j].DbName;
                    if (strName == strDbName)
                    {
                        XmlNode nodeDatabase = dom.CreateElement("database");
                        dom.DocumentElement.AppendChild(nodeDatabase);

                        DomUtil.SetAttr(nodeDatabase, "type", "reader");
                        DomUtil.SetAttr(nodeDatabase, "name", strDbName);
                        string strInCirculation = this.ReaderDbs[j].InCirculation == true ? "true" : "false";
                        DomUtil.SetAttr(nodeDatabase, "inCirculation", strInCirculation);

                        goto CONTINUE;
                    }
                }

                // 是否为书目库?
                for (int j = 0; j < this.ItemDbs.Count; j++)
                {
                    string strDbName = this.ItemDbs[j].BiblioDbName;
                    if (strName == strDbName)
                    {
                        XmlNode nodeDatabase = dom.CreateElement("database");
                        dom.DocumentElement.AppendChild(nodeDatabase);

                        ItemDbCfg cfg = this.ItemDbs[j];

                        DomUtil.SetAttr(nodeDatabase, "type", "biblio");
                        DomUtil.SetAttr(nodeDatabase, "name", cfg.BiblioDbName);
                        DomUtil.SetAttr(nodeDatabase, "syntax", cfg.BiblioDbSyntax);
                        DomUtil.SetAttr(nodeDatabase, "entityDbName", cfg.DbName);
                        DomUtil.SetAttr(nodeDatabase, "orderDbName", cfg.OrderDbName);
                        DomUtil.SetAttr(nodeDatabase, "issueDbName", cfg.IssueDbName);
                        DomUtil.SetAttr(nodeDatabase, "commentDbName", cfg.CommentDbName);
                        DomUtil.SetAttr(nodeDatabase, "unionCatalogStyle", cfg.UnionCatalogStyle);
                        DomUtil.SetAttr(nodeDatabase, "replication", cfg.Replication);

                        string strInCirculation = cfg.InCirculation == true ? "true" : "false";
                        DomUtil.SetAttr(nodeDatabase, "inCirculation", strInCirculation);

                        goto CONTINUE;
                    }
                }

                // 是否为实体库?
                for (int j = 0; j < this.ItemDbs.Count; j++)
                {
                    string strDbName = this.ItemDbs[j].DbName;
                    if (strName == strDbName)
                    {
                        XmlNode nodeDatabase = dom.CreateElement("database");
                        dom.DocumentElement.AppendChild(nodeDatabase);

                        ItemDbCfg cfg = this.ItemDbs[j];

                        DomUtil.SetAttr(nodeDatabase, "type", "entity");
                        DomUtil.SetAttr(nodeDatabase, "name", strDbName);
                        DomUtil.SetAttr(nodeDatabase, "biblioDbName", cfg.BiblioDbSyntax);

                        string strInCirculation = cfg.InCirculation == true ? "true" : "false";
                        DomUtil.SetAttr(nodeDatabase, "inCirculation", strInCirculation);

                        goto CONTINUE;
                    }
                }

                // 是否为订购库?
                for (int j = 0; j < this.ItemDbs.Count; j++)
                {
                    string strDbName = this.ItemDbs[j].OrderDbName;
                    if (strName == strDbName)
                    {
                        XmlNode nodeDatabase = dom.CreateElement("database");
                        dom.DocumentElement.AppendChild(nodeDatabase);

                        ItemDbCfg cfg = this.ItemDbs[j];

                        DomUtil.SetAttr(nodeDatabase, "type", "order");
                        DomUtil.SetAttr(nodeDatabase, "name", strDbName);
                        DomUtil.SetAttr(nodeDatabase, "biblioDbName", cfg.BiblioDbSyntax);
                        goto CONTINUE;
                    }
                }

                // 是否为期刊库?
                for (int j = 0; j < this.ItemDbs.Count; j++)
                {
                    string strDbName = this.ItemDbs[j].IssueDbName;
                    if (strName == strDbName)
                    {
                        XmlNode nodeDatabase = dom.CreateElement("database");
                        dom.DocumentElement.AppendChild(nodeDatabase);

                        ItemDbCfg cfg = this.ItemDbs[j];

                        DomUtil.SetAttr(nodeDatabase, "type", "issue");
                        DomUtil.SetAttr(nodeDatabase, "name", strDbName);
                        DomUtil.SetAttr(nodeDatabase, "biblioDbName", cfg.BiblioDbSyntax);
                        goto CONTINUE;
                    }
                }

                // 是否为评注库?
                for (int j = 0; j < this.ItemDbs.Count; j++)
                {
                    string strDbName = this.ItemDbs[j].CommentDbName;
                    if (strName == strDbName)
                    {
                        XmlNode nodeDatabase = dom.CreateElement("database");
                        dom.DocumentElement.AppendChild(nodeDatabase);

                        ItemDbCfg cfg = this.ItemDbs[j];

                        DomUtil.SetAttr(nodeDatabase, "type", "comment");
                        DomUtil.SetAttr(nodeDatabase, "name", strDbName);
                        DomUtil.SetAttr(nodeDatabase, "biblioDbName", cfg.BiblioDbSyntax);
                        goto CONTINUE;
                    }
                }

                // 是否为 publisher/zhongcihao/dictionary/inventory 库?
                {
                    XmlNode nodeUtilDatabase = this.LibraryCfgDom.DocumentElement.SelectSingleNode("utilDb/database[@name='" + strName + "']");
                    if (nodeUtilDatabase != null)
                    {
                        string strType = DomUtil.GetAttr(nodeUtilDatabase, "type");

                        XmlNode nodeDatabase = dom.CreateElement("database");
                        dom.DocumentElement.AppendChild(nodeDatabase);

                        DomUtil.SetAttr(nodeDatabase, "type", strType);
                        DomUtil.SetAttr(nodeDatabase, "name", strName);
                        goto CONTINUE;
                    }
                }

                strError = "不存在数据库名 '" + strName + "'";
                return 0;
            }

            strOutputInfo = dom.OuterXml;
            return 1;
        }

        // 初始化所有数据库
        public int ClearAllDbs(
            RmsChannelCollection Channels,
            out string strError)
        {
            strError = "";

            string strTempError = "";

            RmsChannel channel = Channels.GetChannel(this.WsUrl);
            if (channel == null)
            {
                strError = "GetChannel error";
                return -1;
            }

            long lRet = 0;

            // 大书目库
            for (int i = 0; i < this.ItemDbs.Count; i++)
            {
                ItemDbCfg cfg = this.ItemDbs[i];

                // 实体库
                {
                    string strEntityDbName = cfg.DbName;

                    if (String.IsNullOrEmpty(strEntityDbName) == false)
                    {
                        lRet = channel.DoInitialDB(strEntityDbName,
                            out strTempError);
                        if (lRet == -1)
                        {
                            strError += "清除实体库 '" + strEntityDbName + "' 内数据时候发生错误：" + strTempError + "; ";
                        }
                    }
                }

                // 订购库
                {
                    string strOrderDbName = cfg.OrderDbName;

                    if (String.IsNullOrEmpty(strOrderDbName) == false)
                    {
                        lRet = channel.DoInitialDB(strOrderDbName,
                            out strTempError);
                        if (lRet == -1)
                        {
                            strError += "清除订购库 '" + strOrderDbName + "' 内数据时候发生错误：" + strTempError + "; ";
                        }
                    }
                }

                // 期库
                {
                    string strIssueDbName = cfg.IssueDbName;

                    if (String.IsNullOrEmpty(strIssueDbName) == false)
                    {
                        lRet = channel.DoInitialDB(strIssueDbName,
                            out strTempError);
                        if (lRet == -1)
                        {
                            strError += "清除期库 '" + strIssueDbName + "' 内数据时候发生错误：" + strTempError + "; ";
                        }
                    }
                }

                // 评注库
                {
                    string strCommentDbName = cfg.CommentDbName;

                    if (String.IsNullOrEmpty(strCommentDbName) == false)
                    {
                        lRet = channel.DoInitialDB(strCommentDbName,
                            out strTempError);
                        if (lRet == -1)
                        {
                            strError += "清除评注库 '" + strCommentDbName + "' 内数据时候发生错误：" + strTempError + "; ";
                        }
                    }
                }

                // 小书目库
                {
                    string strBiblioDbName = cfg.BiblioDbName;

                    if (String.IsNullOrEmpty(strBiblioDbName) == false)
                    {
                        lRet = channel.DoInitialDB(strBiblioDbName,
                            out strTempError);
                        if (lRet == -1)
                        {
                            strError += "清除小书目库 '" + strBiblioDbName + "' 内数据时候发生错误：" + strTempError + "; ";
                        }
                    }
                }
            }

            // 读者库
            for (int i = 0; i < this.ReaderDbs.Count; i++)
            {
                string strDbName = this.ReaderDbs[i].DbName;

                if (String.IsNullOrEmpty(strDbName) == false)
                {
                    lRet = channel.DoInitialDB(strDbName,
                        out strTempError);
                    if (lRet == -1)
                    {
                        strError += "清除读者库 '" + strDbName + "' 内数据时候发生错误：" + strTempError + "; ";
                    }
                }
            }

            // 预约到书队列库
            if (String.IsNullOrEmpty(this.ArrivedDbName) == false)
            {
                string strDbName = this.ArrivedDbName;
                lRet = channel.DoInitialDB(strDbName,
                    out strTempError);
                if (lRet == -1)
                {
                    strError += "清除预约到书库 '" + strDbName + "' 内数据时候发生错误：" + strTempError + "; ";
                }
            }

            // 违约金库
            if (String.IsNullOrEmpty(this.AmerceDbName) == false)
            {
                string strDbName = this.AmerceDbName;
                lRet = channel.DoInitialDB(strDbName,
                    out strTempError);
                if (lRet == -1)
                {
                    strError += "清除违约金库 '" + strDbName + "' 内数据时候发生错误：" + strTempError + "; ";
                }
            }

            // 发票库
            if (String.IsNullOrEmpty(this.InvoiceDbName) == false)
            {
                string strDbName = this.InvoiceDbName;
                lRet = channel.DoInitialDB(strDbName,
                    out strTempError);
                if (lRet == -1)
                {
                    strError += "清除发票库 '" + strDbName + "' 内数据时候发生错误：" + strTempError + "; ";
                }
            }

            // 消息库
            if (String.IsNullOrEmpty(this.MessageDbName) == false)
            {
                string strDbName = this.MessageDbName;
                lRet = channel.DoInitialDB(strDbName,
                    out strTempError);
                if (lRet == -1)
                {
                    strError += "清除消息库 '" + strDbName + "' 内数据时候发生错误：" + strTempError + "; ";
                }
            }

            // 实用库
            XmlNodeList nodes = this.LibraryCfgDom.DocumentElement.SelectNodes("utilDb/database");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];
                string strDbName = DomUtil.GetAttr(node, "name");
                string strType = DomUtil.GetAttr(node, "type");
                if (String.IsNullOrEmpty(strDbName) == false)
                {
                    lRet = channel.DoInitialDB(strDbName,
                        out strTempError);
                    if (lRet == -1)
                    {
                        strError += "清除类型为 " + strType + " 的实用库 '" + strDbName + "' 内数据时候发生错误：" + strTempError + "; ";
                    }
                }
            }

            if (String.IsNullOrEmpty(strError) == false)
                return -1;

            return 0;
        }

        // 删除所有用到的内核数据库
        // 专门开发给安装程序卸载时候使用
        public static int DeleteAllDatabase(
            RmsChannel channel,
            XmlDocument cfg_dom,
            out string strError)
        {
            strError = "";

            string strTempError = "";

            long lRet = 0;

            // 大书目库
            XmlNodeList nodes = cfg_dom.DocumentElement.SelectNodes("itemdbgroup/database");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                // 实体库
                string strEntityDbName = DomUtil.GetAttr(node, "name");

                if (String.IsNullOrEmpty(strEntityDbName) == false)
                {
                    lRet = channel.DoDeleteDB(strEntityDbName,
                        out strTempError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError += "删除实体库 '" + strEntityDbName + "' 内数据时候发生错误：" + strTempError + "; ";
                    }
                }

                // 订购库
                string strOrderDbName = DomUtil.GetAttr(node, "orderDbName");

                if (String.IsNullOrEmpty(strOrderDbName) == false)
                {
                    lRet = channel.DoDeleteDB(strOrderDbName,
                        out strTempError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError += "删除订购库 '" + strOrderDbName + "' 内数据时候发生错误：" + strTempError + "; ";
                    }
                }

                // 期库
                string strIssueDbName = DomUtil.GetAttr(node, "issueDbName");

                if (String.IsNullOrEmpty(strIssueDbName) == false)
                {
                    lRet = channel.DoDeleteDB(strIssueDbName,
                        out strTempError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError += "删除期库 '" + strIssueDbName + "' 内数据时候发生错误：" + strTempError + "; ";
                    }
                }

                // 小书目库
                string strBiblioDbName = DomUtil.GetAttr(node, "biblioDbName");

                if (String.IsNullOrEmpty(strBiblioDbName) == false)
                {
                    lRet = channel.DoDeleteDB(strBiblioDbName,
                        out strTempError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError += "删除小书目库 '" + strBiblioDbName + "' 内数据时候发生错误：" + strTempError + "; ";
                    }
                }
            }

            // 读者库
            nodes = cfg_dom.DocumentElement.SelectNodes("readerdbgroup/database");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];
                string strDbName = DomUtil.GetAttr(node, "name");

                if (String.IsNullOrEmpty(strDbName) == false)
                {
                    lRet = channel.DoDeleteDB(strDbName,
                        out strTempError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError += "删除读者库 '" + strDbName + "' 内数据时候发生错误：" + strTempError + "; ";
                    }
                }
            }

            // 预约到书队列库
            XmlNode arrived_node = cfg_dom.DocumentElement.SelectSingleNode("arrived");
            if (arrived_node != null)
            {
                string strArrivedDbName = DomUtil.GetAttr(arrived_node, "dbname");
                if (String.IsNullOrEmpty(strArrivedDbName) == false)
                {
                    lRet = channel.DoDeleteDB(strArrivedDbName,
                        out strTempError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError += "删除预约到书库 '" + strArrivedDbName + "' 内数据时候发生错误：" + strTempError + "; ";
                    }

                }
            }

            // 违约金库
            XmlNode amerce_node = cfg_dom.DocumentElement.SelectSingleNode("amerce");
            if (amerce_node != null)
            {
                string strAmerceDbName = DomUtil.GetAttr(amerce_node, "dbname");
                if (String.IsNullOrEmpty(strAmerceDbName) == false)
                {
                    lRet = channel.DoDeleteDB(strAmerceDbName,
                        out strTempError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError += "删除违约金库 '" + strAmerceDbName + "' 内数据时候发生错误：" + strTempError + "; ";
                    }
                }
            }

            // 发票库
            XmlNode invoice_node = cfg_dom.DocumentElement.SelectSingleNode("invoice");
            if (invoice_node != null)
            {
                string strInvoiceDbName = DomUtil.GetAttr(amerce_node, "dbname");
                if (String.IsNullOrEmpty(strInvoiceDbName) == false)
                {
                    lRet = channel.DoDeleteDB(strInvoiceDbName,
                        out strTempError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError += "删除发票库 '" + strInvoiceDbName + "' 内数据时候发生错误：" + strTempError + "; ";
                    }
                }
            }

            // 消息库
            XmlNode message_node = cfg_dom.DocumentElement.SelectSingleNode("message");
            if (message_node != null)
            {
                string strMessageDbName = DomUtil.GetAttr(message_node, "dbname");
                if (String.IsNullOrEmpty(strMessageDbName) == false)
                {
                    lRet = channel.DoDeleteDB(strMessageDbName,
                        out strTempError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError += "删除消息库 '" + strMessageDbName + "' 内数据时候发生错误：" + strTempError + "; ";
                    }
                }
            }

            // 实用库
            nodes = cfg_dom.DocumentElement.SelectNodes("utilDb/database");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];
                string strDbName = DomUtil.GetAttr(node, "name");
                string strType = DomUtil.GetAttr(node, "type");
                if (String.IsNullOrEmpty(strDbName) == false)
                {
                    lRet = channel.DoDeleteDB(strDbName,
                        out strTempError);
                    if (lRet == -1 && channel.ErrorCode != ChannelErrorCode.NotFound)
                    {
                        strError += "删除类型为 " + strType + " 的实用库 '" + strDbName + "' 内数据时发生错误：" + strTempError + "; ";
                    }
                }
            }


            if (String.IsNullOrEmpty(strError) == false)
                return -1;

            return 0;
        }
    }
}
