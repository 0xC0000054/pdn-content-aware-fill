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
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;

namespace ContentAwareFill
{
    internal partial class ContentAwareFillConfigDialog : EffectConfigDialog
    {
        private Surface destination;
        private MaskSurface sourceMask;
        private MaskSurface destinationMask;
        private bool formClosePending;

        public ContentAwareFillConfigDialog()
        {
            InitializeComponent();
            UI.InitScaling(this);
            formClosePending = false;
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

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (backgroundWorker1.IsBusy)
            {
                if (DialogResult == DialogResult.Cancel)
                {
                    formClosePending = true;
                    backgroundWorker1.CancelAsync();
                }

                e.Cancel = true;
            }

            base.OnFormClosing(e);
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
                int width = EffectSourceSurface.Width;
                int height = EffectSourceSurface.Height;

                sourceMask = new MaskSurface(width, height);
                destinationMask = ContentAwareFillEffect.CreateMask(Selection, width, height);

                // Render the filled selection after the mask surfaces are initialized.
                FillSelection();
            }
        }

        protected override void InitialInitToken()
        {
            theEffectToken = new ContentAwareFillConfigToken(null, 50, SampleSource.Sides, FillDirection.InwardToCenter);
        }

        protected override void InitDialogFromToken(EffectConfigToken effectTokenCopy)
        {
            ContentAwareFillConfigToken token = (ContentAwareFillConfigToken)effectTokenCopy;

            sampleSizeTrackBar.Value = token.SampleSize;
            sampleFromCombo.SelectedIndex = (int)token.SampleFrom;
            fillDirectionCombo.SelectedIndex = (int)token.FillDirection;
        }

        private DialogResult ShowMessage(string message, MessageBoxIcon icon)
        {
            MessageBoxOptions options = RightToLeft == RightToLeft.Yes ? MessageBoxOptions.RtlReading : 0;

            return MessageBox.Show(this, message, Text, MessageBoxButtons.OK, icon, MessageBoxDefaultButton.Button1, options);
        }

        protected override void InitTokenFromDialog()
        {
            ContentAwareFillConfigToken token = (ContentAwareFillConfigToken)theEffectToken;

            token.Destination = destination;
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

                FillSelection();
            }
        }

        private void sampleSizeUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (sampleSizeUpDown.Value != sampleSizeTrackBar.Value)
            {
                sampleSizeTrackBar.Value = (int)sampleSizeUpDown.Value;

                FillSelection();
            }
        }

        private void sampleFromCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            FillSelection();
        }

        private void fillDirectionCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            FillSelection();
        }

        private void resetButton_Click(object sender, EventArgs e)
        {
            sampleSizeTrackBar.Value = 50;
        }

        private void FillSelection()
        {
            if (sampleFromCombo.SelectedIndex != -1 &&
                fillDirectionCombo.SelectedIndex != -1 &&
                destinationMask != null)
            {
                if (!backgroundWorker1.IsBusy)
                {
                    int sampleSize = sampleSizeTrackBar.Value;
                    SampleSource sampleFrom = (SampleSource)sampleFromCombo.SelectedIndex;
                    FillDirection fillDirection = (FillDirection)fillDirectionCombo.SelectedIndex;

                    backgroundWorker1.RunWorkerAsync(new WorkerArgs(sampleSize, sampleFrom, fillDirection));
                }
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        private unsafe void RenderSourceMask(PdnRegion region)
        {
            sourceMask.Clear();

            Rectangle[] scans = region.GetRegionScansReadOnlyInt();

            for (int i = 0; i < scans.Length; i++)
            {
                Rectangle rect = scans[i];

                for (int y = rect.Top; y < rect.Bottom; y++)
                {
                    byte* ptr = sourceMask.GetPointAddressUnchecked(rect.Left, y);

                    for (int x = rect.Left; x < rect.Right; x++)
                    {
                        *ptr = 255;
                        ptr++;
                    }
                }
            }

#if DEBUG
            using (Bitmap image = new Bitmap(sourceMask.Width, sourceMask.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                System.Drawing.Imaging.BitmapData data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height),
                                                                        System.Drawing.Imaging.ImageLockMode.WriteOnly, image.PixelFormat);

                try
                {
                    for (int y = 0; y < image.Height; y++)
                    {
                        byte* src = sourceMask.GetRowAddressUnchecked(y);
                        byte* dst = (byte*)data.Scan0 + (y * data.Stride);

                        for (int x = 0; x < image.Width; x++)
                        {
                            dst[0] = dst[1] = dst[2] = *src;
                            dst[3] = 255;

                            src++;
                            dst += 4;
                        }
                    }
                }
                finally
                {
                    image.UnlockBits(data);
                }
            }
#endif
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = (BackgroundWorker)sender;
            WorkerArgs args = (WorkerArgs)e.Argument;

            using (PdnRegion sampleArea = ContentAwareFillEffect.CreateSampleRegion(EffectSourceSurface.Bounds, Selection, args.sampleSize))
            {
                RenderSourceMask(sampleArea);

                SampleSource sampleFrom = args.sampleFrom;
                FillDirection fillDirection = args.fillDirection;

                MatchContextType matchContext = ContentAwareFillEffect.GetMatchContextType(sampleFrom, fillDirection);

                Rectangle expandedBounds = sampleArea.GetBoundsInt();
                Rectangle originalBounds = Selection.GetBoundsInt();

                Size croppedSourceSize;

                switch (sampleFrom)
                {
                    case SampleSource.Sides:
                        croppedSourceSize = new Size(expandedBounds.X + expandedBounds.Width, originalBounds.Y + originalBounds.Height);
                        break;
                    case SampleSource.TopAndBottom:
                        croppedSourceSize = new Size(originalBounds.X + originalBounds.Width, expandedBounds.Y + expandedBounds.Height);
                        break;
                    case SampleSource.AllAround:
                    default:
                        croppedSourceSize = new Size(expandedBounds.X + expandedBounds.Width, expandedBounds.Y + expandedBounds.Height);
                        break;
                }

                ResynthesizerParameters parameters = new ResynthesizerParameters(false, false, matchContext, 0.0, 0.117, 16, 500);

                using (Resynthesizer synth = new Resynthesizer(parameters, EffectSourceSurface, destinationMask, sourceMask,
                    expandedBounds, croppedSourceSize, worker.ReportProgress))
                {
                    if (!synth.ContentAwareFill(() => worker.CancellationPending))
                    {
                        e.Cancel = true;
                        return;
                    }
#if DEBUG
                    using (Bitmap image = synth.Target.CreateAliasedBitmap())
                    {
                    }
#endif
                    if (destination != null)
                    {
                        destination.Dispose();
                    }
                    destination = synth.Target.Clone();
                }
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage.Clamp(0, 100);
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar1.Value = 0;

            if (!e.Cancelled)
            {
                if (e.Error != null)
                {
                    ShowMessage(e.Error.Message, MessageBoxIcon.Error);
                }
                FinishTokenUpdate();
            }
            else
            {
                if (formClosePending)
                {
                    Close();
                }
            }
        }

        private void donateLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Services.GetService<PaintDotNet.AppModel.IShellService>().LaunchUrl(this, @"https://forums.getpaint.net/index.php?showtopic=112730");
        }

        private sealed class WorkerArgs
        {
            public readonly int sampleSize;
            public readonly SampleSource sampleFrom;
            public readonly FillDirection fillDirection;

            public WorkerArgs(int sampleSize, SampleSource sampleFrom, FillDirection fillDirection)
            {
                this.sampleSize = sampleSize;
                this.sampleFrom = sampleFrom;
                this.fillDirection = fillDirection;
            }
        }
    }
}
