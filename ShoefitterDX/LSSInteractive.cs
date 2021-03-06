﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using SAGESharp.LSS;

namespace ShoefitterDX
{
    public partial class LSSInteractive : WeifenLuo.WinFormsUI.Docking.DockContent
    {
        private Button LastClicked; // Store the last-clicked button for the Ctrl+Enter command.
        private SAGESharp.OSI.OSIFile _lastResult = null;
        private SAGESharp.OSI.OSIFile LastResult
        {
            get => _lastResult;
            set
            {
                _lastResult = value;
                SaveResultButton.Enabled = value != null;
            }
        }
        private string LastSavedFilename = "";

        public LSSInteractive()
        {
            InitializeComponent();
            TabText = "LSSInteractive";
            LastClicked = CompileButton;
        }

        private bool TryScan(out List<Token> tokens)
        {
            List<CompileMessage> errors = new List<CompileMessage>();
            tokens = Scanner.Scan(SourceTextBox.Text.Replace("\r\n", "\n"), "<LSSInteractive>", errors, false, false);
            if (errors.Count == 0)
            {
                return true;
            }
            else
            {
                ResultTextBox.Text = errors.Count + " Scan Errors:\r\n";
                foreach (CompileMessage err in errors)
                {
                    ResultTextBox.Text += "    " + err.ToString() + "\r\n";
                }
                ResultTextBox.Text += "Tokens: \r\n";
                foreach (Token t in tokens)
                {
                    ResultTextBox.Text += "    " + t.ToString().Replace("\r\n", "\\n").Replace("\n", "\\n").Replace("\t", "\\t") + "\r\n";
                }
                tokens = null;
                return false;
            }
        }

        private bool TryParse(out Parser.Result result)
        {
            if (TryScan(out List<Token> tokens))
            {
                Parser p = new Parser();
                try
                {
                    result = p.Parse(tokens);
                }
                catch (Exception ex)
                {
                    result = new Parser.Result();
                    result.Messages.Add(new CompileMessage("Parser exception: \n\n" + ex.ToString(), "LSS991", CompileMessage.MessageSeverity.Fatal, "<LSSInteractive>", 0, 0, 0));
                }
                if (result.Messages.Count == 0)
                {
                    return true;
                }
                else
                {
                    ResultTextBox.Text = result.Messages.Count + " Parse Errors: \n";
                    foreach (CompileMessage error in result.Messages)
                    {
                        ResultTextBox.Text += "    " + error.ToString() + "\n";
                    }
                    result = null;
                    return false;
                }
            }
            else
            {
                result = null;
                return false;
            }
        }

        private bool TryCompile(out Compiler.Result result)
        {
            if (TryParse(out Parser.Result parsed))
            {
                try
                {
                    result = Compiler.CompileParsed(parsed);
                }
                catch (Exception ex)
                {
                    result = new Compiler.Result(new SAGESharp.OSI.OSIFile());
                    result.Messages.Add(new CompileMessage("Compiler exception: \n\n" + ex.ToString(), "LSS992", CompileMessage.MessageSeverity.Fatal, "<LSSInteractive>", 0, 0, 0));
                }
                if (result.Messages.Count == 0)
                {
                    return true;
                }
                else
                {
                    ResultTextBox.Text = result.Messages.Count + " Compile Errors: \n";
                    foreach (CompileMessage error in result.Messages)
                    {
                        ResultTextBox.AppendText("    " + error.ToString() + "\n");
                    }
                    result = null;
                    return false;
                }
            }
            else
            {
                result = null;
                return false;
            }
        }

        private void ScanButton_Click(object sender, EventArgs e)
        {
            LastClicked = ScanButton;
            LastResult = null;

            ResultTextBox.BeginUpdate(); // Don't re-render on text change
            if (TryScan(out List<Token> tokens))
            {
                ResultTextBox.Text = "";
                foreach (Token t in tokens)
                {
                    ResultTextBox.Text += t.ToString().Replace("\r\n", "\\n").Replace("\n", "\\n").Replace("\t", "\\t") + "\r\n";
                }
            }
            ResultTextBox.EndUpdate(); // Ok we're done changing the text rapidly
        }

        private void ParseButton_Click(object sender, EventArgs e)
        {
            LastClicked = ParseButton;
            LastResult = null;

            ResultTextBox.BeginUpdate();
            if (TryParse(out Parser.Result result))
            {
                ResultTextBox.Text = PrettyPrinter.Print(result);
            }
            ResultTextBox.EndUpdate();
        }

        private void CompileButton_Click(object sender, EventArgs e)
        {
            LastClicked = CompileButton;

            ResultTextBox.BeginUpdate();
            if (TryCompile(out Compiler.Result result))
            {
                ResultTextBox.Text = "";
                LastResult = result.OSI;
                LastResult.UpdateBytecodeLayout();
                ResultTextBox.AppendText(LastResult.ToString());
            }
            else
            {
                LastResult = null;
            }
            ResultTextBox.EndUpdate();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.Enter))
            {
                LastClicked.PerformClick();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void SaveResultButton_Click(object sender, EventArgs e)
        {
            if (LastResult != null)
            {
                SaveFileDialog dialog = new SaveFileDialog();
                dialog.Filter = "OSI File (*.osi)|*.osi";
                if (!String.IsNullOrEmpty(LastSavedFilename))
                {
                    dialog.FileName = System.IO.Path.GetFileName(LastSavedFilename);
                    dialog.InitialDirectory = System.IO.Path.GetDirectoryName(LastSavedFilename);
                }
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    LastSavedFilename = dialog.FileName;
                    try
                    {
                        using (System.IO.FileStream stream = new System.IO.FileStream(dialog.FileName, System.IO.FileMode.Create))
                        using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(stream))
                        {
                            LastResult.Write(writer);
                        }
                    }
                    catch (Exception ex)
                    {
                        // TODO: Actual logging solution
                        MessageBox.Show("Exception writing OSI:\n\n" + ex.ToString());
                    }
                }
            }
        }
    }
}
