/*
*  This file is part of pdn-content-aware-fill, A Resynthesizer-based
*  content aware fill Effect plug-in for Paint.NET.
*
*  Copyright (C) 2018 Nicholas Hayes
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

using PaintDotNet;
using PaintDotNet.Effects;
using System;
using System.Windows.Forms;

namespace ContentAwareFill
{
    internal partial class ContentAwareFillConfigDialog : EffectConfigDialog
    {
        private int ignoreTokenChangedEventCount;

        public ContentAwareFillConfigDialog()
        {
            InitializeComponent();
            UI.InitScaling(this);
            ignoreTokenChangedEventCount = 0;
            PluginThemingUtil.EnableEffectDialogTheme(this);
        }

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

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            if (Selection.GetBoundsInt() == EffectSourceSurface.Bounds)
            {
                if (ShowMessage(Properties.Resources.NoSelectionError, MessageBoxIcon.None) == DialogResult.OK)
                {
                    Close();
                }
            }
            else
            {
                ContentAwareFillEffect effect = (ContentAwareFillEffect)Effect;
                effect.ConfigDialogProgress += UpdateProgress;
                effect.ConfigDialogHandleError += HandleError;
            }
        }

        private void PushIgnoreTokenChangedEvents()
        {
            ignoreTokenChangedEventCount++;
        }

        private void PopIgnoreTokenChangedEvents()
        {
            ignoreTokenChangedEventCount--;

            if (ignoreTokenChangedEventCount == 0)
            {
                FinishTokenUpdate();
            }
        }

        private void UpdateConfigToken()
        {
            if (ignoreTokenChangedEventCount == 0)
            {
                FinishTokenUpdate();
            }
        }

        protected override void InitialInitToken()
        {
            theEffectToken = new ContentAwareFillConfigToken(50, SampleSource.Sides, FillDirection.InwardToCenter);
        }

        protected override void InitDialogFromToken(EffectConfigToken effectTokenCopy)
        {
            ContentAwareFillConfigToken token = (ContentAwareFillConfigToken)effectTokenCopy;

            // Call FinishTokenUpdate after the controls are initialized.
            PushIgnoreTokenChangedEvents();

            sampleSizeTrackBar.Value = token.SampleSize;
            sampleFromCombo.SelectedIndex = (int)token.SampleFrom;
            fillDirectionCombo.SelectedIndex = (int)token.FillDirection;

            PopIgnoreTokenChangedEvents();
        }

        private DialogResult ShowMessage(string message, MessageBoxIcon icon)
        {
            MessageBoxOptions options = RightToLeft == RightToLeft.Yes ? MessageBoxOptions.RtlReading : 0;

            if (InvokeRequired)
            {
                return (DialogResult)Invoke(new Func<string, DialogResult>((string text) =>
                       MessageBox.Show(this, text, Text, MessageBoxButtons.OK, icon, MessageBoxDefaultButton.Button1, options)),
                       message);
            }
            else
            {
                return MessageBox.Show(this, message, Text, MessageBoxButtons.OK, icon, MessageBoxDefaultButton.Button1, options);
            }
        }

        protected override void InitTokenFromDialog()
        {
            ContentAwareFillConfigToken token = (ContentAwareFillConfigToken)theEffectToken;

            token.SampleSize = sampleSizeTrackBar.Value;
            token.SampleFrom =  (SampleSource)sampleFromCombo.SelectedIndex;
            token.FillDirection = (FillDirection)fillDirectionCombo.SelectedIndex;
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            FinishTokenUpdate();
            Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void sampleSizeTrackBar_ValueChanged(object sender, EventArgs e)
        {
            if (sampleSizeTrackBar.Value != (int)sampleSizeUpDown.Value)
            {
                sampleSizeUpDown.Value = sampleSizeTrackBar.Value;

                UpdateConfigToken();
            }
        }

        private void sampleSizeUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (sampleSizeUpDown.Value != sampleSizeTrackBar.Value)
            {
                sampleSizeTrackBar.Value = (int)sampleSizeUpDown.Value;

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

        private void resetButton_Click(object sender, EventArgs e)
        {
            sampleSizeTrackBar.Value = 50;
        }

        private void donateLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Services.GetService<PaintDotNet.AppModel.IShellService>().LaunchUrl(this, "https://forums.getpaint.net/index.php?showtopic=112730");
        }

        private void HandleError(object sender, ConfigDialogHandleErrorEventArgs e)
        {
            ShowMessage(e.Exception.Message, MessageBoxIcon.Error);
        }

        private void UpdateProgress(object sender, ConfigDialogProgressEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<int>((int value) => progressBar1.Value = value), e.ProgressPercentage);
            }
            else
            {
                progressBar1.Value = e.ProgressPercentage;
            }
        }
    }
}
