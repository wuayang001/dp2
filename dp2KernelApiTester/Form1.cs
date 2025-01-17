﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

using DigitalPlatform;
using DigitalPlatform.CirculationClient;
using DigitalPlatform.CommonControl;
using DigitalPlatform.GUI;
using DigitalPlatform.Text;

namespace dp2KernelApiTester
{
    public partial class MainForm : Form
    {
        CancellationTokenSource _cancelApp = new CancellationTokenSource();

        public MainForm()
        {
            InitializeComponent();

            FormClientInfo.MainForm = this;
        }

        private void MenuItem_settings_Click(object sender, EventArgs e)
        {
            OpenSettingDialog(this);
        }

        public void OpenSettingDialog(Form parent,
string style = "")
        {
            using (SettingDialog dlg = new SettingDialog())
            {
                // GuiUtil.SetControlFont(dlg, parent.Font);
                ClientInfo.MemoryState(dlg, "settingDialog", "state");
                dlg.ShowDialog(parent);
            }
        }

        #region console

        /// <summary>
        /// 清除已有的 HTML 显示
        /// </summary>
        public void ClearHtml(WebBrowser webBrowser)
        {
            string strCssUrl = Path.Combine(ClientInfo.DataDir, "history.css");

            string strLink = "<link href='" + strCssUrl + "' type='text/css' rel='stylesheet' />";

            string strJs = "";

            {
                HtmlDocument doc = webBrowser.Document;

                if (doc == null)
                {
                    webBrowser.Navigate("about:blank");
                    doc = webBrowser.Document;
                }
                doc = doc.OpenNew(true);
            }

            WriteHtml(webBrowser,
                "<html><head>" + strLink + strJs + "</head><body>");
        }


        /// <summary>
        /// 将浏览器控件中已有的内容清除，并为后面输出的纯文本显示做好准备
        /// </summary>
        /// <param name="webBrowser">浏览器控件</param>
        public static void ClearForPureTextOutputing(WebBrowser webBrowser)
        {
            HtmlDocument doc = webBrowser.Document;

            if (doc == null)
            {
                webBrowser.Navigate("about:blank");
                doc = webBrowser.Document;
            }

            string strHead = "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\"><html xmlns=\"http://www.w3.org/1999/xhtml\"><head>"
    // + "<link rel='stylesheet' href='"+strCssFileName+"' type='text/css'>"
    + "<style media='screen' type='text/css'>"
    + "body { font-family:Microsoft YaHei; background-color:#555555; color:#eeeeee; } " // background-color:#555555
    + "</style>"
    + "</head><body>";

            doc = doc.OpenNew(true);
            doc.Write(strHead + "<pre style=\"font-family:Consolas; \">");  // Calibri
        }

        /// <summary>
        /// 将 HTML 信息输出到控制台，显示出来。
        /// </summary>
        /// <param name="strText">要输出的 HTML 字符串</param>
        public void WriteToConsole(string strText)
        {
            WriteHtml(this.webBrowser1, strText);
        }

        /// <summary>
        /// 将文本信息输出到控制台，显示出来
        /// </summary>
        /// <param name="strText">要输出的文本字符串</param>
        public void WriteTextToConsole(string strText)
        {
            WriteHtml(this.webBrowser1, HttpUtility.HtmlEncode(strText));
        }

        /// <summary>
        /// 向一个浏览器控件中追加写入 HTML 字符串
        /// 不支持异步调用
        /// </summary>
        /// <param name="webBrowser">浏览器控件</param>
        /// <param name="strHtml">HTML 字符串</param>
        public static void WriteHtml(WebBrowser webBrowser,
    string strHtml)
        {

            HtmlDocument doc = webBrowser.Document;

            if (doc == null)
            {
                webBrowser.Navigate("about:blank");
                doc = webBrowser.Document;
#if NO
                webBrowser.DocumentText = "<h1>hello</h1>";
                doc = webBrowser.Document;
                Debug.Assert(doc != null, "");
#endif
            }

            // doc = doc.OpenNew(true);
            doc.Write(strHtml);
        }


        void AppendSectionTitle(string strText)
        {
            AppendCrLn();
            AppendString("*** " + strText + " ***\r\n");
            AppendCurrentTime();
            AppendCrLn();
        }

        void AppendCurrentTime()
        {
            AppendString("*** " + DateTime.Now.ToString() + " ***\r\n");
        }

        void AppendCrLn()
        {
            AppendString("\r\n");
        }

        // 线程安全
        // parameters:
        //      style 为 begin end warning error green 之一
        public void AppendString(string strText, string style = "")
        {
            /*
            if (this.webBrowser1.InvokeRequired)
            {
                this.webBrowser1.Invoke(new Action<string>(AppendString), strText);
                return;
            }
            this.WriteTextToConsole(strText);
            ScrollToEnd();
            */
            strText = strText.Replace("\r", "\n");
            strText = strText.TrimEnd(new char[] { '\n' });
            string[] lines = strText.Split(new char[] { '\n' });
            foreach (string line in lines)
            {
                AppendHtml($"<div class='debug {style}'>" + HttpUtility.HtmlEncode(line).Replace(" ", "&nbsp;") + "</div>");
            }
        }

        // 线程安全
        public void AppendHtml(string strText)
        {
            if (this.webBrowser1.InvokeRequired)
            {
                this.webBrowser1.Invoke(new Action<string>(AppendHtml), strText);
                return;
            }
            this.WriteToConsole(strText);
            ScrollToEnd();
        }

        void ScrollToEnd()
        {
            this.webBrowser1.ScrollToEnd();
        }

        // 线程安全
        public void ShowProgressMessage(string strID,
            string strText)
        {
            if (this.webBrowser1 == null || this.webBrowser1.IsDisposed == true)
                return;

            if (this.webBrowser1.InvokeRequired)
            {
                this.webBrowser1.Invoke(new Action<string, string>(ShowProgressMessage), strID, strText);
                return;
            }

            if (webBrowser1.Document == null)
                return;

            HtmlElement obj = this.webBrowser1.Document.GetElementById(strID);
            if (obj != null)
            {
                obj.InnerText = strText;
                return;
            }

            // <div class='debug {style}'>
            AppendHtml("<div id='" + strID + "' class='debug'>" + HttpUtility.HtmlEncode(strText) + "</div>");
        }


        #endregion

        private void MainForm_Load(object sender, EventArgs e)
        {
            var ret = FormClientInfo.Initial("dp2KernelApiTester",
() => StringUtil.IsDevelopMode());
            if (ret == false)
            {
                Application.Exit();
                return;
            }

            // ClearForPureTextOutputing(this.webBrowser1);
            ClearHtml(this.webBrowser1);

            AppendString("Form1_Load\r\n");

            LoadSettings();

            DataModel.Initial();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _cancelApp?.Dispose();

            DataModel.Free();

            SaveSettings();
        }

        void LoadSettings()
        {
            this.UiState = ClientInfo.Config.Get("global", "ui_state", "");

            // 恢复 MainForm 的显示状态
            {
                var state = ClientInfo.Config.Get("mainForm", "state", "");
                if (string.IsNullOrEmpty(state) == false)
                {
                    FormProperty.SetProperty(state, this, ClientInfo.IsMinimizeMode());
                }
            }
        }

        void SaveSettings()
        {
            // 保存 MainForm 的显示状态
            {
                var state = FormProperty.GetProperty(this);
                ClientInfo.Config.Set("mainForm", "state", state);
            }

            ClientInfo.Config?.Set("global", "ui_state", this.UiState);
            ClientInfo.Finish();
        }

        public string UiState
        {
            get
            {
                List<object> controls = new List<object>
                {
                    //this.tabControl1,
                    //this.listView_writeHistory,
                };
                return GuiState.GetUiState(controls);
            }
            set
            {
                List<object> controls = new List<object>
                {
                    //this.tabControl1,
                    //this.listView_writeHistory,
                };
                GuiState.SetUiState(controls, value);
            }
        }

        private async void MenuItem_test_initializeDatabase_Click(object sender, EventArgs e)
        {
            using (var cancel = CancellationTokenSource.CreateLinkedTokenSource(this._cancelApp.Token))
            {
                _cancelCurrent = cancel;
                await Task.Run(() =>
                {
                    EnableControls(false);
                    try
                    {
                        var result = TestCreateDatabase.TestAll(cancel.Token,
                            "refresh_database,create_records,buildkeys");
                        if (result.Value == -1)
                            DataModel.SetMessage(result.ErrorInfo, "error");
                    }
                    catch (Exception ex)
                    {
                        AppendString($"exception: {ex.Message}");
                    }
                    finally
                    {
                        EnableControls(true);
                    }
                });
            }
        }

        private async void MenuItem_test_records_Click(object sender, EventArgs e)
        {
            using (var cancel = CancellationTokenSource.CreateLinkedTokenSource(this._cancelApp.Token))
            {
                _cancelCurrent = cancel;
                await Task.Run(() =>
                {
                    EnableControls(false);
                    try
                    {
                        var result = TestRecord.TestAll(cancel.Token,
                            "");
                        if (result.Value == -1)
                            DataModel.SetMessage(result.ErrorInfo, "error");
                    }
                    catch (Exception ex)
                    {
                        AppendString($"exception: {ex.Message}");
                    }
                    finally
                    {
                        EnableControls(true);
                    }
                });
            }
        }

        private async void MenuItem_test_search_Click(object sender, EventArgs e)
        {
            using (var cancel = CancellationTokenSource.CreateLinkedTokenSource(this._cancelApp.Token))
            {
                _cancelCurrent = cancel;
                await Task.Run(() =>
                {
                    EnableControls(false);
                    try
                    {
                        var result = TestSearch.TestAll(cancel.Token,
                            "");
                        if (result.Value == -1)
                            DataModel.SetMessage(result.ErrorInfo, "error");
                    }
                    catch (Exception ex)
                    {
                        AppendString($"exception: {ex.Message}");
                    }
                    finally
                    {
                        EnableControls(true);
                    }
                });
            }
        }

        private async void MenuItem_test_refreshKeys_Click(object sender, EventArgs e)
        {
            using (var cancel = CancellationTokenSource.CreateLinkedTokenSource(this._cancelApp.Token))
            {
                _cancelCurrent = cancel;
                await Task.Run(() =>
                {
                    EnableControls(false);
                    try
                    {
                        var result = TestRebuildKeys.TestAll(cancel.Token,
                            "");
                        if (result.Value == -1)
                            DataModel.SetMessage(result.ErrorInfo, "error");
                    }
                    catch (Exception ex)
                    {
                        AppendString($"exception: {ex.Message}");
                    }
                    finally
                    {
                        EnableControls(true);
                    }
                });
            }
        }

        private async void MenuItem_fragmentWrite_Click(object sender, EventArgs e)
        {
            using (var cancel = CancellationTokenSource.CreateLinkedTokenSource(this._cancelApp.Token))
            {
                _cancelCurrent = cancel;
                await Task.Run(() =>
                {
                    EnableControls(false);
                    try
                    {
                        var result = TestRecord.PrepareEnvironment();
                        if (result.Value == -1)
                            DataModel.SetMessage(result.ErrorInfo, "error");

                        /*
                        result = TestRecord.SpecialTest(1000); // 1000
                        if (result.Value == -1)
                            DataModel.SetMessage(result.ErrorInfo, "error");
                        */

                        result = TestRecord.FragmentCreateRecords(
                            cancel.Token,
                            1000, 1, "overlap"); // 1000
                        if (result.Value == -1)
                            DataModel.SetMessage(result.ErrorInfo, "error");

                        DataModel.SetMessage("碎片式写入完成", "green");
                    }
                    catch (Exception ex)
                    {
                        AppendString($"exception: {ex.Message}");
                    }
                    finally
                    {
                        EnableControls(true);
                    }
                });
            }
        }

        void EnableControls(bool enable)
        {
            this.Invoke(new Action(() =>
            {
                this.menuStrip1.Enabled = enable;
                this.toolStripButton_stop.Enabled = !enable;
            }));
        }

        private async void MenuItem_test_largeObject_Click(object sender, EventArgs e)
        {
            using (var cancel = CancellationTokenSource.CreateLinkedTokenSource(this._cancelApp.Token))
            {
                _cancelCurrent = cancel;
                await Task.Run(() =>
                {
                    EnableControls(false);
                    try
                    {
                        var result = TestRecord.PrepareEnvironment();
                        if (result.Value == -1)
                            DataModel.SetMessage(result.ErrorInfo, "error");

                        result = TestRecord.LargeObjectTest(cancel.Token,
                            5);
                        if (result.Value == -1)
                            DataModel.SetMessage(result.ErrorInfo, "error");

                        DataModel.SetMessage("大对象写入完成", "green");
                    }
                    catch (Exception ex)
                    {
                        AppendString($"exception: {ex.Message}");
                    }
                    finally
                    {
                        EnableControls(true);
                    }
                });
            }
        }

        private async void MenuItem_test_pdf_Click(object sender, EventArgs e)
        {
            using (var cancel = CancellationTokenSource.CreateLinkedTokenSource(this._cancelApp.Token))
            {
                _cancelCurrent = cancel;
                await Task.Run(() =>
                {
                    EnableControls(false);
                    try
                    {
                        var result = TestPdfPage.TestAll(cancel.Token,
                            "");
                        if (result.Value == -1)
                            DataModel.SetMessage(result.ErrorInfo, "error");
                    }
                    catch (Exception ex)
                    {
                        AppendString($"exception: {ex.Message}");
                    }
                    finally
                    {
                        EnableControls(true);
                    }
                });
            }
        }

        CancellationTokenSource _cancelCurrent = null;

        private void toolStripButton_stop_Click(object sender, EventArgs e)
        {
            _cancelCurrent?.Cancel();
        }
    }
}
