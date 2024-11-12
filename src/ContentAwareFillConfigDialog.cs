/*
*  This file is part of pdn-content-aware-fill, A Resynthesizer-based
*  content aware fill Effect plug-in for Paint.NET.
*
*  Copyright (C) 2018, 2020, 2021, 2022, 2023, 2024 Nicholas Hayes
*
*  This program is free software; you can redistribute it and/or modify
*  it under the terms of the GNU General Public License as published by
*  the Free Software Foundation; either version 2 of the License, or
*  (at your option) any later version.
*
*  This program is distributed in the hope that it will be useful,
*  but WITHOUT ANY WARRANTY; without even the implied warranty of
*  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
*  GNU General Public License for more details.
*
*  You should have received a copy of the GNU General Public License
*  along with this program; if not, write to the Free Software
*  Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
*
*/

using ContentAwareFill.Properties;
using PaintDotNet;
using PaintDotNet.AppModel;
using PaintDotNet.Effects;
using PaintDotNet.Imaging;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace ContentAwareFill
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Style",
        "IDE1006:Naming Styles",
        Justification = "The VS designer generates methods that start with a lower case letter.")]
    internal partial class ContentAwareFillConfigDialog : EffectConfigForm<ContentAwareFillEffect, ContentAwareFillConfigToken>
    {
        private int ignoreTokenChangedEventCount;
        private bool formClosePending;
        private bool restartBackgroundWorker;
        private bool selectionValid;
        private bool ranFirstAutoRender;
        private bool setRenderingStatusText;
        private IBitmap<ColorBgra32> output;
        private ResynthesizerRunner resynthesizer;
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Usage",
            "CA2213:Disposable fields should be disposed",
            Justification = "False positive with the analyzer.")]
        private CancellationTokenSource resynthesizerTokenSource;

        public ContentAwareFillConfigDialog()
        {
            InitializeComponent();
            UI.InitScaling(this);
            this.toolStripStatusLabel1.Text = Resources.StatusReadyText;
            this.ignoreTokenChangedEventCount = 0;
            this.formClosePending = false;
            this.selectionValid = false;
            this.ranFirstAutoRender = false;
            this.output = null;
            this.resynthesizer = null;

            PluginThemingUtil.UpdateControlBackColor(this);
            PluginThemingUtil.UpdateControlForeColor(this);
        }

        protected override bool UseAppThemeColorsDefault => true;

        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e);

            PluginThemingUtil.UpdateControlBackColor(this);
        }

        protected override void OnForeColorChanged(EventArgs e)
        {
            base.OnForeColorChanged(e);

            PluginThemingUtil.UpdateControlForeColor(this);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (this.backgroundWorker.IsBusy)
            {
                e.Cancel = true;
                this.formClosePending = true;

                if (this.DialogResult == DialogResult.Cancel)
                {
                    this.resynthesizerTokenSource.Cancel();
                }
            }

            base.OnFormClosing(e);
        }

        protected override void OnLoading()
        {
            base.OnLoading();

            UpdateResetButtonIconForDpi();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // This plugin does not support processing a selection of the whole image, it needs some
            // unselected pixels to replace the contents of the selected area.
            // When there is no active selection Paint.NET acts as if the whole image/layer is selected.
            if (ContentAwareFillEffect.IsWholeImageSelected(this.Environment))
            {
                if (ShowErrorMessage(Resources.WholeImageSelected) == DialogResult.OK)
                {
                    Close();
                }
            }
            else
            {
                this.selectionValid = true;

                if (this.ignoreTokenChangedEventCount == 0 && this.autoRenderCb.Checked && !this.ranFirstAutoRender)
                {
                    StartBackgroundWorker();
                }
            }
        }

        private void PushIgnoreTokenChangedEvents()
        {
            this.ignoreTokenChangedEventCount++;
        }

        private void PopIgnoreTokenChangedEvents()
        {
            this.ignoreTokenChangedEventCount--;

            if (this.ignoreTokenChangedEventCount == 0 && this.autoRenderCb.Checked && this.selectionValid)
            {
                this.ranFirstAutoRender = true;
                StartBackgroundWorker();
            }
        }

        private void UpdateConfigToken()
        {
            if (this.ignoreTokenChangedEventCount == 0 && this.autoRenderCb.Checked)
            {
                StartBackgroundWorker();
            }
        }

        protected override EffectConfigToken OnCreateInitialToken()
        {
            return new ContentAwareFillConfigToken(50,
                                                   SampleSource.Sides,
                                                   FillDirection.InwardToCenter,
                                                   Resynthesizer.DefaultSeed,
                                                   true,
                                                   null);
        }

        protected override void OnUpdateDialogFromToken(ContentAwareFillConfigToken token)
        {
            // Call FinishTokenUpdate after the controls are initialized.
            PushIgnoreTokenChangedEvents();

            this.sampleSizeTrackBar.Value = token.SampleSize;
            this.sampleFromCombo.SelectedIndex = (int)token.SampleFrom;
            this.fillDirectionCombo.SelectedIndex = (int)token.FillDirection;
            this.seedUpDown.Value = token.Seed;
            this.autoRenderCb.Checked = token.RenderAutomatically;

            PopIgnoreTokenChangedEvents();
        }

        protected override void OnUpdateTokenFromDialog(ContentAwareFillConfigToken token)
        {
            token.SampleSize = this.sampleSizeTrackBar.Value;
            token.SampleFrom = (SampleSource)this.sampleFromCombo.SelectedIndex;
            token.FillDirection = (FillDirection)this.fillDirectionCombo.SelectedIndex;
            token.Seed = (int)this.seedUpDown.Value;
            token.RenderAutomatically = this.autoRenderCb.Checked;
            token.Output = this.output;
        }

        private void UpdateResetButtonIconForDpi()
        {
            int dpi = this.DeviceDpi;

            if (dpi > 96)
            {
                if (dpi <= 120)
                {
                    this.sliderResetButton.Image = new Bitmap(typeof(ContentAwareFillConfigDialog), "Resources.Icons.ResetIcon-120.png");
                }
                else if (dpi <= 144)
                {
                    this.sliderResetButton.Image = new Bitmap(typeof(ContentAwareFillConfigDialog), "Resources.Icons.ResetIcon-144.png");
                }
                else if (dpi <= 192)
                {
                    this.sliderResetButton.Image = new Bitmap(typeof(ContentAwareFillConfigDialog), "Resources.Icons.ResetIcon-192.png");
                }
                else
                {
                    this.sliderResetButton.Image = new Bitmap(typeof(ContentAwareFillConfigDialog), "Resources.Icons.ResetIcon-384.png");
                }
            }
        }

        private DialogResult ShowErrorMessage(string message)
        {
            if (this.InvokeRequired)
            {
                Invoke(new Action<string>((string text) =>
                       this.Services.GetService<IExceptionDialogService>().ShowErrorDialog(this, text, string.Empty)),
                       message);
            }
            else
            {
                this.Services.GetService<IExceptionDialogService>().ShowErrorDialog(this, message, string.Empty);
            }

            return DialogResult.OK;
        }

        private DialogResult ShowErrorMessage(Exception exception)
        {
            if (this.InvokeRequired)
            {
                Invoke(new Action<Exception>((Exception ex) =>
                       this.Services.GetService<IExceptionDialogService>().ShowErrorDialog(this, ex)),
                       exception);
            }
            else
            {
                this.Services.GetService<IExceptionDialogService>().ShowErrorDialog(this, exception);
            }

            return DialogResult.OK;
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            Close();
        }

        private void sampleSizeTrackBar_ValueChanged(object sender, EventArgs e)
        {
            if (this.sampleSizeTrackBar.Value != (int)this.sampleSizeUpDown.Value)
            {
                this.sampleSizeUpDown.Value = this.sampleSizeTrackBar.Value;

                UpdateConfigToken();
            }
        }

        private void sampleSizeUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (this.sampleSizeUpDown.Value != this.sampleSizeTrackBar.Value)
            {
                this.sampleSizeTrackBar.Value = (int)this.sampleSizeUpDown.Value;

                UpdateConfigToken();
            }
        }

        private void sampleFromCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateConfigToken();
        }

        private void fillDirectionCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateConfigToken();
        }

        private void sliderResetButton_Click(object sender, EventArgs e)
        {
            this.sampleSizeTrackBar.Value = 50;
        }

        private void donateLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.Services.GetService<IShellService>().LaunchUrl(this, "https://forums.getpaint.net/index.php?showtopic=112730");
        }

        private void autoRenderCb_CheckedChanged(object sender, EventArgs e)
        {
            this.applyButton.Enabled = !this.autoRenderCb.Checked;
        }

        private void applyButton_Click(object sender, EventArgs e)
        {
            StartBackgroundWorker();
        }

        private void seedUpDown_ValueChanged(object sender, EventArgs e)
        {
            UpdateConfigToken();
        }

        private void seedResetButton_Click(object sender, EventArgs e)
        {
            this.seedUpDown.Value = Resynthesizer.DefaultSeed;
        }

        private void StartBackgroundWorker()
        {
            if (this.backgroundWorker.IsBusy)
            {
                this.restartBackgroundWorker = true;
                this.resynthesizerTokenSource.Cancel();
            }
            else
            {
                this.resynthesizer ??= new ResynthesizerRunner(this.Environment, this.Services);
                this.resynthesizerTokenSource = new CancellationTokenSource();

                this.resynthesizer.SetParameters(this.sampleSizeTrackBar.Value,
                                                 (SampleSource)this.sampleFromCombo.SelectedIndex,
                                                 (FillDirection)this.fillDirectionCombo.SelectedIndex,
                                                 (int)this.seedUpDown.Value);

                this.backgroundWorker.RunWorkerAsync();
            }
        }

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Invoke(() =>
            {
                this.toolStripStatusLabel1.Text = Resources.StatusInitializingText;
                this.toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                this.setRenderingStatusText = false;
            });

            BackgroundWorker worker = (BackgroundWorker)sender;

            IBitmap<ColorBgra32> output = this.resynthesizer.Run(this.resynthesizerTokenSource.Token,
                                                                 worker.ReportProgress);

            if (output != null)
            {
                e.Result = output;
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (!this.setRenderingStatusText)
            {
                this.setRenderingStatusText = true;
                this.toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                this.toolStripStatusLabel1.Text = Resources.StatusRenderingText;
            }

            this.toolStripProgressBar1.Value = e.ProgressPercentage;
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (this.resynthesizerTokenSource != null)
            {
                this.resynthesizerTokenSource.Dispose();
                this.resynthesizerTokenSource = null;
            }

            if (this.restartBackgroundWorker)
            {
                this.restartBackgroundWorker = false;
                this.resynthesizer.SetParameters(this.sampleSizeTrackBar.Value,
                                                 (SampleSource)this.sampleFromCombo.SelectedIndex,
                                                 (FillDirection)this.fillDirectionCombo.SelectedIndex,
                                                 (int)this.seedUpDown.Value);
                this.resynthesizerTokenSource = new CancellationTokenSource();

                this.backgroundWorker.RunWorkerAsync();
            }
            else
            {
                this.toolStripStatusLabel1.Text = Resources.StatusReadyText;
                this.toolStripProgressBar1.Value = 0;

                if (e.Error != null)
                {
                    ShowErrorMessage(e.Error);
                }
                else
                {
                    if (this.output != null)
                    {
                        this.output.Dispose();
                        this.output = null;
                    }

                    if (!e.Cancelled)
                    {
                        this.output = (IBitmap<ColorBgra32>)e.Result;
                        UpdateTokenFromDialog();
                    }

                    if (this.formClosePending)
                    {
                        Close();
                    }
                }
            }
        }
    }
}
